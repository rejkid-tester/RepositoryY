import { Component } from '@angular/core';

@Component({
  standalone: true,
  selector: 'app-help',
  template: `
    <div class="page">
      <h2>Help</h2>
      <p>If you need assistance, contact support or review the FAQ below.</p>
      <div class="help-grid">
        <div class="help-card">
          <h3>Account access</h3>
          <p>Reset your password from the Login screen or contact support.</p>
        </div>
        <div class="help-card">
          <h3>Verification issues</h3>
          <p>Make sure the 6-digit code is current. Request a new one if expired.</p>
        </div>
        <div class="help-card">
          <h3>Contact</h3>
          <p>Email: support@example.com<br/>Hours: Mon–Fri, 9am–5pm</p>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .page { padding: 24px; }
    .help-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
      gap: 16px;
      margin-top: 16px;
    }
    .help-card {
      border: 1px solid #e6eaf2;
      border-radius: 12px;
      padding: 16px;
      background: #ffffff;
      box-shadow: 0 6px 18px rgba(15, 23, 42, 0.06);
    }
    .help-card h3 {
      margin: 0 0 8px;
      font-size: 1rem;
    }
  `]
})
export class HelpComponent {}
