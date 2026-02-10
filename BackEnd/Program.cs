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
    var tasksDbConnectionString = builder.Configuration.GetConnectionString("TasksDbConnectionString");
    if (string.IsNullOrWhiteSpace(tasksDbConnectionString))
    {
        LogManager.GetCurrentClassLogger().Error("Missing connection string: ConnectionStrings:TasksDbConnectionString");
    }
    else
    {
        try
        {
            var csb = new Npgsql.NpgsqlConnectionStringBuilder(tasksDbConnectionString);
            LogManager.GetCurrentClassLogger().Info("DB connection config: Host={Host} Port={Port} Database={Database} Username={Username}",
                csb.Host,
                csb.Port,
                csb.Database,
                csb.Username);
        }
        catch (Exception ex)
        {
            LogManager.GetCurrentClassLogger().Warn(ex, "Failed to parse TasksDbConnectionString.");
        }
    }
    builder.Services.AddDbContext<UserDbContext>(options =>
        options.UseNpgsql(tasksDbConnectionString)
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

    // Registers the required services
    builder.Services.AddOpenApi();

    var kestrelHttpsUrl = builder.Configuration["Kestrel:Endpoints:Https:Url"];
    var kestrelCertPath = builder.Configuration["Kestrel:Endpoints:Https:Certificate:Path"];
    var kestrelCertPassword = builder.Configuration["Kestrel:Endpoints:Https:Certificate:Password"];
    builder.Logging.AddFilter("Microsoft.AspNetCore.Server.Kestrel", Microsoft.Extensions.Logging.LogLevel.Information);
    Logger kestrelLogger = LogManager.GetCurrentClassLogger();
    var kestrelCertFullPath = string.IsNullOrWhiteSpace(kestrelCertPath)
        ? string.Empty
        : Path.GetFullPath(kestrelCertPath);
    var kestrelCertExists = !string.IsNullOrWhiteSpace(kestrelCertFullPath) && File.Exists(kestrelCertFullPath);
    kestrelLogger.Info("Kestrel HTTPS config: Url={Url} CertPath={CertPath} CertPasswordConfigured={PasswordConfigured}",
        kestrelHttpsUrl,
        kestrelCertFullPath,
        !string.IsNullOrWhiteSpace(kestrelCertPassword));
    kestrelLogger.Info("Kestrel certificate file check: Exists={Exists}", kestrelCertExists);

    var app = builder.Build();

    // === MIDDLEWARE (keep your existing pipeline here) ===
    // global cors policy
    app.UseCors(x => 

        x.SetIsOriginAllowed(origin =>
                System.Text.RegularExpressions.Regex.IsMatch(origin, @"^http://192\.168\.\d{1,3}\.\d{1,3}(:\d+)?$"))
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials());

    // Inside your application's startup code (e.g., Program.cs or Startup.cs)
    //using (var scope = app.Services.CreateScope())
    //{
    //    var dbContext = scope.ServiceProvider.GetRequiredService<UserDbContext>();
    //    dbContext.Database.Migrate();
    //}

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

        app.MapGet("/", () => Results.Redirect("/scalar/v1"));
    }

    // custom jwt auth middleware
    app.UseMiddleware<JwtMiddleware>();
    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    try
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();
        var canConnect = await db.Database.CanConnectAsync();
        LogManager.GetCurrentClassLogger().Info("Database connectivity check: CanConnect={CanConnect}", canConnect);
    }
    catch (Exception ex)
    {
        LogManager.GetCurrentClassLogger().Error(ex, "Database connectivity check failed.");
    }

    LogManager.GetCurrentClassLogger().Info("Listening on: {Urls}", string.Join(";", app.Urls));

    app.Run();
}
catch (Exception ex)
{
    Console.Error.WriteLine("Fatal startup error:");
    Console.Error.WriteLine(ex);
    throw;
}