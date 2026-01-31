import { Component } from '@angular/core';

@Component({
  standalone: true,
  selector: 'app-tasks',
  template: `
    <div class="tasks-page">
      <h2>Tasks</h2>
      <p>Welcome — your tasks will appear here.</p>
    </div>
  `,
  styles: [`
    .tasks-page { padding: 24px; }
  `]
})
export class TasksComponent {}
