import { AfterViewInit, Component, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { UserService } from '../services/user.service';
import { AdminUserResponse } from '../responses/admin-user-response';

@Component({
  standalone: true,
  selector: 'app-users-table',
  imports: [
    CommonModule,
    MatTableModule,
    MatPaginatorModule,
    MatSortModule,
    MatProgressSpinnerModule,
    MatFormFieldModule,
    MatInputModule
  ],
  template: `
    <div class="users-page">
      <div class="users-header">
        <div>
          <h2>Users</h2>
          <p>Admin-only list of registered users.</p>
        </div>
        <mat-form-field appearance="outline" class="filter">
          <mat-label>Filter</mat-label>
          <input matInput (keyup)="applyFilter($event)" placeholder="Search users" />
        </mat-form-field>
      </div>

      @if (loading) {
        <div class="loading">
          <mat-spinner diameter="36"></mat-spinner>
          <span>Loading users...</span>
        </div>
      } @else {
        @if (errorMessage) {
          <div class="error">{{ errorMessage }}</div>
        }

        <div class="table-wrap">
          <table mat-table [dataSource]="dataSource" matSort class="mat-elevation-z1">

            <ng-container matColumnDef="id">
              <th mat-header-cell *matHeaderCellDef mat-sort-header>ID</th>
              <td mat-cell *matCellDef="let user">{{ user.id }}</td>
            </ng-container>

            <ng-container matColumnDef="name">
              <th mat-header-cell *matHeaderCellDef mat-sort-header>Name</th>
              <td mat-cell *matCellDef="let user">{{ user.firstName }} {{ user.lastName }}</td>
            </ng-container>

            <ng-container matColumnDef="email">
              <th mat-header-cell *matHeaderCellDef mat-sort-header>Email</th>
              <td mat-cell *matCellDef="let user">{{ user.email }}</td>
            </ng-container>

            <ng-container matColumnDef="role">
              <th mat-header-cell *matHeaderCellDef mat-sort-header>Role</th>
              <td mat-cell *matCellDef="let user">{{ roleLabel(user.role) }}</td>
            </ng-container>

            <ng-container matColumnDef="active">
              <th mat-header-cell *matHeaderCellDef mat-sort-header>Active</th>
              <td mat-cell *matCellDef="let user">{{ user.active ? 'Yes' : 'No' }}</td>
            </ng-container>

            <ng-container matColumnDef="created">
              <th mat-header-cell *matHeaderCellDef mat-sort-header>Created</th>
              <td mat-cell *matCellDef="let user">{{ formatDate(user.created) }}</td>
            </ng-container>

            <ng-container matColumnDef="verified">
              <th mat-header-cell *matHeaderCellDef mat-sort-header>Verified</th>
              <td mat-cell *matCellDef="let user">{{ user.verified ? formatDate(user.verified) : '—' }}</td>
            </ng-container>

            <ng-container matColumnDef="mfa">
              <th mat-header-cell *matHeaderCellDef mat-sort-header>MFA</th>
              <td mat-cell *matCellDef="let user">{{ user.mfaEnabled ? 'Enabled' : 'Off' }}</td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
            <tr mat-row *matRowDef="let row; columns: displayedColumns"></tr>
          </table>
        </div>

        <mat-paginator [pageSizeOptions]="[10, 25, 50]" showFirstLastButtons></mat-paginator>
      }
    </div>
  `,
  styles: [`
    .users-page { display: flex; flex-direction: column; gap: 16px; }
    .users-header { display: flex; align-items: center; justify-content: space-between; gap: 16px; flex-wrap: wrap; }
    .users-header h2 { margin: 0 0 4px; }
    .filter { min-width: 220px; }
    .loading { display: flex; align-items: center; gap: 12px; padding: 16px; }
    .error { background: #fee2e2; color: #991b1b; padding: 12px 14px; border-radius: 10px; border: 1px solid #fecaca; }
    .table-wrap { overflow: auto; border-radius: 12px; }
    table { width: 100%; min-width: 820px; }
  `]
})
export class UsersTableComponent implements AfterViewInit, OnInit {
  displayedColumns: string[] = ['id', 'name', 'email', 'role', 'active', 'created', 'verified', 'mfa'];
  dataSource = new MatTableDataSource<AdminUserResponse>([]);
  loading = true;
  errorMessage = '';

  @ViewChild(MatPaginator)
  set paginator(paginator: MatPaginator | undefined) {
    if (paginator) {
      this.dataSource.paginator = paginator;
    }
  }

  @ViewChild(MatSort)
  set sort(sort: MatSort | undefined) {
    if (sort) {
      this.dataSource.sort = sort;
    }
  }

  constructor(private userService: UserService) {
    this.dataSource.filterPredicate = (data, filter) => {
      const value = `${data.id} ${data.email} ${data.firstName} ${data.lastName} ${this.roleLabel(data.role)}`.toLowerCase();
      return value.includes(filter);
    };
  }

  ngOnInit(): void {
    this.loading = true;
    this.errorMessage = '';
    this.userService.getAllUsers().subscribe({
      next: users => {
        this.loading = false;
        this.dataSource.data = users || [];
      },
      error: err => {
        this.loading = false;
        this.errorMessage = err?.error?.message || err?.message || 'Failed to load users.';
      }
    });
  }

  ngAfterViewInit(): void {
    // handled by ViewChild setters
  }

  applyFilter(event: Event): void {
    const value = (event.target as HTMLInputElement).value || '';
    this.dataSource.filter = value.trim().toLowerCase();
    if (this.dataSource.paginator) {
      this.dataSource.paginator.firstPage();
    }
  }

  roleLabel(role: number): string {
    return role === 0 ? 'Admin' : 'User';
  }

  formatDate(value?: string | null): string {
    if (!value) return '—';
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? value : date.toLocaleDateString();
  }
}
