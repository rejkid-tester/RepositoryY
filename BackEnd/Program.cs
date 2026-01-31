using Backend;
using Backend.Helpers;
using Backend.Interfaces;
using Backend.Middleware;
using Backend.Services;
using BackEnd.Interfaces;
using BackEnd.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NLog;
using NLog.Web;
using Scalar.AspNetCore;
using System.Text;


try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.Configure<AppSettings>(builder.Configuration);

    // After builder is created and configuration is loaded
    TokenHelper.Configure(builder.Configuration);
    builder.Services.AddHttpClient<ISmsService, SmsServiceTwilio>();
    builder.Services.AddHttpClient<IEmailService, EmailServiceBrevo>();
    builder.Services.AddHttpClient<ISmsService, SmsServiceBrevo>();



    // Configure NLog as the logging provider (loads NLog.config from the app base directory)
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();
    builder.Logging.AddConsole();

    Logger logger = LogManager.GetCurrentClassLogger();
    logger.Info("Program Started");

    // Required for attribute-routed controllers (app.MapControllers())
    builder.Services.AddControllers();

    // Required for EmailService (IHttpClientFactory)
    builder.Services.AddHttpClient();

    // Required for TokenService/UserService (UserDbContext)
    // Note: ensure you have a connection string named "DefaultConnection" in appsettings.json/user-secrets.
    builder.Services.AddDbContext<UserDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("TasksDbConnectionString"))
    );

    builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;

        var accessTokenKey =
            Environment.GetEnvironmentVariable("ACCESS_TOKEN_KEY")
            ?? builder.Configuration["AppSettings:ACCESS_TOKEN_KEY"];

        var keyBytes = TokenHelper.GetAccessTokenKeyBytesOrThrow(accessTokenKey);
        options.TokenValidationParameters = TokenHelper.CreateTokenValidationParameters(keyBytes);
    });
    builder.Services.AddAuthorization();

    // === SERVICES (keep your existing registrations here) ===
    builder.Services.AddTransient<ITokenService, TokenService>();
    builder.Services.AddTransient<IUserService, UserService>();
    builder.Services.AddScoped<IEmailService, EmailServiceBrevo>();

    builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));

    var app = builder.Build();

    // === MIDDLEWARE (keep your existing pipeline here) ===
    // global cors policy
    app.UseCors(x => x

        .WithOrigins("http://localhost:4200", "http://192.168.0.15")
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials());

    if (app.Environment.IsDevelopment())
    {
        app.MapScalarApiReference(options =>
        {
            options
            .WithTheme(ScalarTheme.None);
            options.HideDarkModeToggle = true;
            options.HideClientButton = true;
        });
        app.MapOpenApi();
    }

    // custom jwt auth middleware
    app.UseMiddleware<JwtMiddleware>();
    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    LogManager.GetCurrentClassLogger().Info("Listening on: {Urls}", string.Join(";", app.Urls));

    app.Run();
}
catch (Exception ex)
{
    Console.Error.WriteLine("Fatal startup error:");
    Console.Error.WriteLine(ex);
    throw;
}