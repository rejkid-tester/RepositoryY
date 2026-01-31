import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { AccountRoutingModule } from './account-routing.module';
import { AccountLayoutComponent } from './account-layout.component';
import { LoginPromptComponent } from './login-prompt.component';
import { RegisterDialogComponent } from './register-dialog.component';
import { VerifyEmailComponent } from './verify-email.component';
import { ForgotPasswordComponent } from './forgot-password.component';

@NgModule({
  declarations: [],
  imports: [
    CommonModule,
    FormsModule,
    AccountRoutingModule,
    AccountLayoutComponent,
    LoginPromptComponent,
    RegisterDialogComponent,
    VerifyEmailComponent,
    ForgotPasswordComponent
  ]
})
export class AccountModule { }
