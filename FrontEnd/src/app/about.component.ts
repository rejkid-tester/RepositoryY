import { Component } from '@angular/core';

@Component({
  standalone: true,
  selector: 'app-about',
  template: `
    <div class="page">
      <h2>About</h2>
      <p>This is a simple tasks application with account and MFA features.</p>
    </div>
  `,
  styles: [`
    .page { padding: 24px; }
  `]
})
export class AboutComponent {}
