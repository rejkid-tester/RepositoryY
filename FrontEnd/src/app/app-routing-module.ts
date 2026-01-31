import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AboutComponent } from './about.component';
import { HelpComponent } from './help.component';
import { ProfileComponent } from './data-containers/profile.component';
import { UsersTableComponent } from './users/users-table.component';
import { AccountLayoutComponent } from './account/account-layout.component';
import { LoginPromptComponent } from './account/login-prompt.component';
import { RegisterDialogComponent } from './account/register-dialog.component';
import { VerifyEmailComponent } from './account/verify-email.component';
import { ForgotPasswordComponent } from './account/forgot-password.component';
import { ResetPasswordComponent } from './account/reset-password.component';


const routes: Routes = [
  // Redirect root to account/login so login dialog appears on startup
  { path: '', redirectTo: 'account/login', pathMatch: 'full' },

  // (post-login) routes
  { path: 'profile', component: ProfileComponent },
  { path: 'users', component: UsersTableComponent },
  { path: 'about', component: AboutComponent },
  { path: 'help', component: HelpComponent },

  // Account routes (eager, no lazy loading)
  {
    path: 'account',
    component: AccountLayoutComponent,
    children: [
      { path: '', redirectTo: 'login', pathMatch: 'full' },
      { path: 'login', component: LoginPromptComponent },
      { path: 'register', component: RegisterDialogComponent },
      { path: 'verify-email', component: VerifyEmailComponent },
      { path: 'forgot-password', component: ForgotPasswordComponent },
      { path: 'reset-password', component: ResetPasswordComponent },
      { path: 'profile', component: ProfileComponent },
    ]
  },

  // Fallback: send unknown routes to account login
  { path: '**', redirectTo: 'account/login' }
];

@NgModule({
  // Use HTML5 pushState routing. Ensure the server rewrites unknown URLs
  // to the SPA entry point (see web.config added to the project root).
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
