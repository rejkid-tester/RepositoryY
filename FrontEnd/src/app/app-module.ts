import { NgModule, provideAppInitializer } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';

import { AppRoutingModule } from './app-routing-module';
import { App } from './app';
import { LoginPromptComponent } from './account/login-prompt.component';
import { FormsModule } from '@angular/forms';
import { AuthInterceptorProvider } from './interceptors/auth.interceptor';
import { appInitializer } from './helpers/app.initializer';

@NgModule({
  declarations: [
    App
  ],
  imports: [
    BrowserModule,
    FormsModule,
    AppRoutingModule,
    LoginPromptComponent
  ],
  providers: [
    provideHttpClient(withInterceptorsFromDi()),
    AuthInterceptorProvider,
    provideAppInitializer(appInitializer),
  ],
  bootstrap: [App]
})
export class AppModule { }
