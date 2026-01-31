import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AboutComponent } from './about.component';
import { HelpComponent } from './help.component';
import { ProfileComponent } from './data-containers/profile.component';
import { UsersTableComponent } from './users/users-table.component';
// Account components are lazy-loaded via AccountModule


const routes: Routes = [
  // Redirect root to account/login so login dialog appears on startup
  { path: '', redirectTo: 'account/login', pathMatch: 'full' },

  // (post-login) routes
  { path: 'profile', component: ProfileComponent },
  { path: 'users', component: UsersTableComponent },
  { path: 'about', component: AboutComponent },
  { path: 'help', component: HelpComponent },

  // Account routes (lazy-loaded)
  {
    path: 'account',
    loadChildren: () => import('./account/account.module').then(m => m.AccountModule)
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
