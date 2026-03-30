import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../services/api.service';
import { GeneralService } from '../services/general.service';
import { TmsSystem, CreateTmsSystemRequest } from '../models/tms-system.model';

@Component({
  selector: 'app-tms-systems',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="container">
      <h2>TMS Systems</h2>

      <div class="actions">
        <button (click)="showCreateForm = !showCreateForm" class="btn btn-primary">
          {{ showCreateForm ? 'Cancel' : 'Create New System' }}
        </button>
        <label class="filter">
          <input type="checkbox" [(ngModel)]="activeOnly" (change)="loadSystems()" />
          Show Active Only
        </label>
      </div>

      <div *ngIf="showCreateForm" class="form-container">
        <h3>Create TMS System</h3>
        <form (ngSubmit)="createSystem()">
          <div class="form-group">
            <label for="systemName">Name:</label>
            <input id="systemName" type="text" [(ngModel)]="newSystem.name" name="name" required />
          </div>
          <div class="form-group">
            <label for="displayName">Display Name:</label>
            <input id="displayName" type="text" [(ngModel)]="newSystem.displayName" name="displayName" required />
          </div>
          <div class="form-group">
            <label for="sysDescription">Description:</label>
            <textarea id="sysDescription" [(ngModel)]="newSystem.description" name="description"></textarea>
          </div>
          <div class="form-group">
            <label for="sysVersion">Version:</label>
            <input id="sysVersion" type="text" [(ngModel)]="newSystem.version" name="version" />
          </div>
          <button type="submit" class="btn btn-success">Create</button>
        </form>
      </div>

      <div *ngIf="error" class="error">{{ error }}</div>

      <table *ngIf="systems.length > 0" class="data-table">
        <thead>
          <tr>
            <th>Name</th>
            <th>Display Name</th>
            <th>Version</th>
            <th>Status</th>
            <th>Created</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let system of systems">
            <td>{{ system.name }}</td>
            <td>{{ system.displayName }}</td>
            <td>{{ system.version }}</td>
            <td>
              <span [class.active]="system.isActive" [class.inactive]="!system.isActive">
                {{ system.isActive ? 'Active' : 'Inactive' }}
              </span>
            </td>
            <td>{{ system.createdAt | date: 'short' }}</td>
            <td>
              <button (click)="deleteSystem(system.id)" class="btn btn-danger btn-sm">Delete</button>
            </td>
          </tr>
        </tbody>
      </table>

      <p *ngIf="systems.length === 0 && !loading">No TMS systems found.</p>
      <p *ngIf="loading">Loading...</p>
    </div>
  `,
  styles: [
    `
      .container {
        padding: 20px;
        max-width: 1200px;
        margin: 0 auto;
      }

      .actions {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin: 20px 0;
      }

      .filter {
        display: flex;
        align-items: center;
        gap: 8px;
      }

      .form-container {
        background: #f5f5f5;
        padding: 20px;
        border-radius: 8px;
        margin: 20px 0;
      }

      .form-group {
        margin-bottom: 15px;
      }

      .form-group label {
        display: block;
        margin-bottom: 5px;
        font-weight: bold;
      }

      .form-group input,
      .form-group textarea {
        width: 100%;
        padding: 8px;
        border: 1px solid #ddd;
        border-radius: 4px;
      }

      .btn {
        padding: 10px 20px;
        border: none;
        border-radius: 4px;
        cursor: pointer;
        font-size: 14px;
      }

      .btn-primary {
        background: #007bff;
        color: white;
      }

      .btn-success {
        background: #28a745;
        color: white;
      }

      .btn-danger {
        background: #dc3545;
        color: white;
      }

      .btn-sm {
        padding: 5px 10px;
        font-size: 12px;
      }

      .data-table {
        width: 100%;
        border-collapse: collapse;
        margin-top: 20px;
      }

      .data-table th,
      .data-table td {
        padding: 12px;
        text-align: left;
        border-bottom: 1px solid #ddd;
      }

      .data-table th {
        background: #f8f9fa;
        font-weight: bold;
      }

      .active {
        color: #28a745;
        font-weight: bold;
      }

      .inactive {
        color: #dc3545;
      }

      .error {
        color: #dc3545;
        padding: 10px;
        background: #f8d7da;
        border-radius: 4px;
        margin: 10px 0;
      }
    `,
  ],
})
export class TmsSystemsComponent implements OnInit {
  systems: TmsSystem[] = [];
  loading = false;
  error: string | null = null;
  showCreateForm = false;
  activeOnly = false;
  newSystem: CreateTmsSystemRequest = {
    name: '',
    displayName: '',
    description: '',
    version: '1.0',
  };

  constructor(
    private apiService: ApiService,
    private generalService: GeneralService,
  ) {}

  ngOnInit() {
    this.loadSystems();
  }

  loadSystems() {
    this.loading = true;
    this.error = null;

    this.apiService.getTmsSystems(this.activeOnly).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.systems = response.data.systems;
        }
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load TMS systems';
        this.loading = false;
        console.error(err);
      },
    });
  }

  createSystem() {
    this.apiService.createTmsSystem(this.newSystem).subscribe({
      next: (response) => {
        if (response.success) {
          this.showCreateForm = false;
          this.newSystem = {
            name: '',
            displayName: '',
            description: '',
            version: '1.0',
          };
          this.loadSystems();
        }
      },
      error: (err) => {
        this.error = 'Failed to create TMS system';
        console.error(err);
      },
    });
  }

  deleteSystem(id: string) {
    this.generalService
      .confirm({
        title: 'Delete TMS System',
        text: 'Are you sure you want to delete this TMS system?',
        confirmText: 'Yes, Delete',
        confirmColor: '#e74c3c',
        icon: 'warning',
      })
      .then((result) => {
        if (!result.isConfirmed) return;

        this.apiService.deleteTmsSystem(id).subscribe({
          next: () => {
            this.generalService.success('TMS system deleted successfully');
            this.loadSystems();
          },
          error: (err) => {
            this.error = 'Failed to delete TMS system';
            console.error(err);
          },
        });
      });
  }
}
