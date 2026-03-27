import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../services/api.service';
import { GeneralService } from '../services/general.service';
import { LookupTable, CreateLookupTableRequest } from '../models/lookup-table.model';
import { TmsSystem } from '../models/tms-system.model';

@Component({
  selector: 'app-lookup-tables',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="container">
      <h2>Lookup Tables Management</h2>

      <div class="filters">
        <label>
          Filter by TMS:
          <select [(ngModel)]="selectedTmsId" (change)="onTmsChange()">
            <option value="">All TMS Systems</option>
            <option *ngFor="let tms of tmsSystems" [value]="tms.id">
              {{tms.displayName}}
            </option>
          </select>
        </label>
        <button class="btn-primary" (click)="showCreateForm = !showCreateForm">
          {{ showCreateForm ? 'Cancel' : 'Add New Lookup Table' }}
        </button>
      </div>

      <div *ngIf="showCreateForm" class="form-container">
        <h3>Create Lookup Table</h3>
        <form (ngSubmit)="createLookupTable()" #lookupForm="ngForm">
          <div class="form-group">
            <label>TMS System: <span class="required">*</span></label>
            <select [(ngModel)]="newLookup.tmsSystemId" name="tmsSystemId" required>
              <option value="">Select TMS System</option>
              <option *ngFor="let tms of tmsSystems" [value]="tms.id">
                {{tms.displayName}}
              </option>
            </select>
          </div>

          <div class="form-group">
            <label>Field Name: <span class="required">*</span></label>
            <input type="text" [(ngModel)]="newLookup.fieldName" name="fieldName" required
                   placeholder="e.g., PaymentTerms" />
            <small>The field name this lookup table applies to</small>
          </div>

          <div class="form-group">
            <label>Lookup Table Name: <span class="required">*</span></label>
            <input type="text" [(ngModel)]="newLookup.name" name="name" required
                   placeholder="e.g., Payment Terms Mapping" />
          </div>

          <div class="form-group">
            <label>Description:</label>
            <textarea [(ngModel)]="newLookup.description" name="description" rows="2"
                      placeholder="Description of what this lookup table does"></textarea>
          </div>

          <div class="form-group">
            <label>Mappings:</label>
            <textarea [(ngModel)]="newLookup.mappings" name="mappings" rows="6"
                      placeholder='{"NET30": "Net 30 Days", "NET60": "Net 60 Days", "COD": "Cash on Delivery"}'></textarea>
            <small>JSON object with key-value mappings</small>
          </div>

          <div class="form-group">
            <label>Default Value:</label>
            <input type="text" [(ngModel)]="newLookup.defaultValue" name="defaultValue"
                   placeholder="Value to use if no mapping found" />
          </div>

          <div class="form-group checkbox">
            <label>
              <input type="checkbox" [(ngModel)]="newLookup.isCaseSensitive" name="isCaseSensitive" />
              Case Sensitive Matching
            </label>
          </div>

          <div class="form-actions">
            <button type="submit" class="btn-primary" [disabled]="!lookupForm.form.valid">Create</button>
            <button type="button" class="btn-secondary" (click)="resetForm()">Reset</button>
          </div>
        </form>
      </div>

      <div *ngIf="error" class="error">{{ error }}</div>
      <div *ngIf="success" class="success">{{ success }}</div>

      <div class="table-container">
        <table>
          <thead>
            <tr>
              <th>TMS System</th>
              <th>Field Name</th>
              <th>Name</th>
              <th>Description</th>
              <th>Default Value</th>
              <th>Case Sensitive</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let lookup of lookupTables">
              <td>{{ getTmsName(lookup.tmsSystemId) }}</td>
              <td><code>{{ lookup.fieldName }}</code></td>
              <td>{{ lookup.name }}</td>
              <td>{{ lookup.description || '-' }}</td>
              <td>{{ lookup.defaultValue || '-' }}</td>
              <td>{{ lookup.isCaseSensitive ? 'Yes' : 'No' }}</td>
              <td>
                <button class="btn-small btn-info" (click)="viewMappings(lookup)">View</button>
                <button class="btn-small btn-danger" (click)="deleteLookupTable(lookup.id)">Delete</button>
              </td>
            </tr>
            <tr *ngIf="lookupTables.length === 0">
              <td colspan="7" class="no-data">No lookup tables found</td>
            </tr>
          </tbody>
        </table>
      </div>

      <div *ngIf="selectedLookup" class="modal-overlay" (click)="closeModal()">
        <div class="modal-content" (click)="$event.stopPropagation()">
          <div class="modal-header">
            <h3>{{ selectedLookup.name }}</h3>
            <button class="close-btn" (click)="closeModal()">&times;</button>
          </div>
          <div class="modal-body">
            <p><strong>TMS System:</strong> {{ getTmsName(selectedLookup.tmsSystemId) }}</p>
            <p><strong>Field Name:</strong> <code>{{ selectedLookup.fieldName }}</code></p>
            <p *ngIf="selectedLookup.description"><strong>Description:</strong> {{ selectedLookup.description }}</p>
            <p><strong>Default Value:</strong> {{ selectedLookup.defaultValue || 'None' }}</p>
            <p><strong>Case Sensitive:</strong> {{ selectedLookup.isCaseSensitive ? 'Yes' : 'No' }}</p>

            <h4>Mappings:</h4>
            <div class="mappings-view" *ngIf="parsedMappings">
              <table class="mappings-table">
                <thead>
                  <tr>
                    <th>Source Value</th>
                    <th>Target Value</th>
                  </tr>
                </thead>
                <tbody>
                  <tr *ngFor="let mapping of parsedMappings">
                    <td><code>{{ mapping.key }}</code></td>
                    <td>{{ mapping.value }}</td>
                  </tr>
                </tbody>
              </table>
            </div>
            <div *ngIf="!parsedMappings" class="no-mappings">No mappings defined</div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .container {
      max-width: 1200px;
      margin: 0 auto;
    }

    h2 {
      color: #2c3e50;
      margin-bottom: 20px;
    }

    .filters {
      display: flex;
      gap: 15px;
      align-items: center;
      margin-bottom: 20px;
      padding: 15px;
      background: #f8f9fa;
      border-radius: 4px;
    }

    .filters label {
      display: flex;
      align-items: center;
      gap: 10px;
    }

    .filters select {
      padding: 8px;
      border: 1px solid #ddd;
      border-radius: 4px;
    }

    .form-container {
      background: #f8f9fa;
      padding: 20px;
      border-radius: 4px;
      margin-bottom: 20px;
    }

    .form-container h3 {
      margin-top: 0;
      color: #2c3e50;
    }

    .form-group {
      margin-bottom: 15px;
    }

    .form-group label {
      display: block;
      font-weight: 500;
      margin-bottom: 5px;
      color: #555;
    }

    .form-group input[type="text"],
    .form-group select,
    .form-group textarea {
      width: 100%;
      padding: 8px;
      border: 1px solid #ddd;
      border-radius: 4px;
      font-family: inherit;
    }

    .form-group textarea {
      font-family: 'Courier New', monospace;
      font-size: 12px;
    }

    .form-group small {
      display: block;
      color: #666;
      font-size: 12px;
      margin-top: 4px;
    }

    .form-group.checkbox {
      display: flex;
      align-items: center;
    }

    .form-group.checkbox label {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-bottom: 0;
    }

    .form-group.checkbox input[type="checkbox"] {
      width: auto;
    }

    .required {
      color: #e74c3c;
    }

    .form-actions {
      display: flex;
      gap: 10px;
      margin-top: 20px;
    }

    .btn-primary, .btn-secondary, .btn-small, .btn-danger, .btn-info {
      padding: 8px 16px;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-size: 14px;
    }

    .btn-primary {
      background: #3498db;
      color: white;
    }

    .btn-primary:hover:not(:disabled) {
      background: #2980b9;
    }

    .btn-primary:disabled {
      background: #95a5a6;
      cursor: not-allowed;
    }

    .btn-secondary {
      background: #95a5a6;
      color: white;
    }

    .btn-secondary:hover {
      background: #7f8c8d;
    }

    .btn-danger {
      background: #e74c3c;
      color: white;
    }

    .btn-danger:hover {
      background: #c0392b;
    }

    .btn-info {
      background: #16a085;
      color: white;
    }

    .btn-info:hover {
      background: #138d75;
    }

    .btn-small {
      padding: 4px 12px;
      font-size: 12px;
      margin-right: 5px;
    }

    .error {
      background: #fee;
      color: #c33;
      padding: 10px;
      border-radius: 4px;
      margin-bottom: 15px;
    }

    .success {
      background: #efe;
      color: #3c3;
      padding: 10px;
      border-radius: 4px;
      margin-bottom: 15px;
    }

    .table-container {
      overflow-x: auto;
    }

    table {
      width: 100%;
      border-collapse: collapse;
      background: white;
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
    }

    th, td {
      padding: 12px;
      text-align: left;
      border-bottom: 1px solid #ddd;
    }

    th {
      background: #34495e;
      color: white;
      font-weight: 500;
    }

    tbody tr:hover {
      background: #f5f5f5;
    }

    code {
      background: #f4f4f4;
      padding: 2px 6px;
      border-radius: 3px;
      font-family: 'Courier New', monospace;
      font-size: 12px;
    }

    .no-data {
      text-align: center;
      color: #999;
      font-style: italic;
    }

    .modal-overlay {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(0, 0, 0, 0.5);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1000;
    }

    .modal-content {
      background: white;
      border-radius: 8px;
      max-width: 600px;
      width: 90%;
      max-height: 80vh;
      overflow-y: auto;
      box-shadow: 0 4px 6px rgba(0,0,0,0.1);
    }

    .modal-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 20px;
      border-bottom: 1px solid #ddd;
    }

    .modal-header h3 {
      margin: 0;
      color: #2c3e50;
    }

    .close-btn {
      background: none;
      border: none;
      font-size: 24px;
      cursor: pointer;
      color: #999;
      padding: 0;
      width: 30px;
      height: 30px;
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .close-btn:hover {
      color: #333;
    }

    .modal-body {
      padding: 20px;
    }

    .modal-body p {
      margin: 10px 0;
    }

    .modal-body h4 {
      margin-top: 20px;
      margin-bottom: 10px;
      color: #2c3e50;
    }

    .mappings-view {
      background: #f8f9fa;
      padding: 15px;
      border-radius: 4px;
    }

    .mappings-table {
      width: 100%;
      background: white;
    }

    .mappings-table th {
      background: #34495e;
    }

    .no-mappings {
      text-align: center;
      color: #999;
      font-style: italic;
      padding: 20px;
    }
  `]
})
export class LookupTablesComponent implements OnInit {
  lookupTables: LookupTable[] = [];
  tmsSystems: TmsSystem[] = [];
  selectedTmsId: string = '';
  showCreateForm: boolean = false;
  error: string = '';
  success: string = '';
  selectedLookup: LookupTable | null = null;
  parsedMappings: Array<{ key: string, value: string }> = [];

  newLookup: CreateLookupTableRequest = this.getEmptyLookup();

  constructor(private apiService: ApiService, private generalService: GeneralService) { }

  ngOnInit() {
    this.loadTmsSystems();
    this.loadLookupTables();
  }

  loadTmsSystems() {
    this.apiService.getTmsSystems(true).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.tmsSystems = response.data.systems;
        }
      },
      error: (err) => {
        this.error = 'Failed to load TMS systems';
        console.error(err);
      }
    });
  }

  loadLookupTables() {
    this.apiService.getLookupTables(this.selectedTmsId || undefined).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.lookupTables = response.data.lookupTables;
        }
      },
      error: (err) => {
        this.error = 'Failed to load lookup tables';
        console.error(err);
      }
    });
  }

  onTmsChange() {
    this.loadLookupTables();
  }

  isValidJSON(str: string): boolean {
    if (typeof str !== "string") return false; // Must be a string

    try {
      const parsed = JSON.parse(str);

      // Ensure the parsed result is an object, array, or primitive allowed in JSON
      return typeof parsed === "object" || typeof parsed === "number" ||
        typeof parsed === "boolean" || parsed === null || typeof parsed === "string";
    } catch (e) {
      return false; // Parsing failed
    }
  }

  createLookupTable() {
    this.error = '';
    this.success = '';
    var mappingStr = "" + this.newLookup.mappings?.toString();
    if (!this.isValidJSON(mappingStr)) {
      this.error = 'Wrong mapping JSON Entries, Failed to create lookup table';
      return;
    }

    this.apiService.createLookupTable(this.newLookup).subscribe({
      next: (response) => {
        if (response.success) {
          this.success = 'Lookup table created successfully';
          this.showCreateForm = false;
          this.resetForm();
          this.loadLookupTables();
        }
      },
      error: (err) => {
        this.error = err.error?.message || 'Failed to create lookup table';
        console.error(err);
      }
    });
  }


  deleteLookupTable(id: string) {
    this.generalService.confirm({
      title: 'Delete Lookup Table',
      text: 'Are you sure you want to delete this lookup table?',
      confirmText: 'Yes, Delete',
      confirmColor: '#e74c3c',
      icon: 'warning'
    }).then((result: any) => {
      if (!result.isConfirmed) return;

      this.error = '';
      this.success = '';

      this.apiService.deleteLookupTable(id).subscribe({
        next: () => {
          this.generalService.success('Lookup table deleted successfully');
          this.loadLookupTables();
        },
        error: (err) => {
          this.error = err.error?.message || 'Failed to delete lookup table';
          console.error(err);
        }
      });
    });
  }

  viewMappings(lookup: LookupTable) {
    this.selectedLookup = lookup;
    this.parsedMappings = [];

    if (lookup.mappings) {
      try {
        const mappingsObj = JSON.parse(lookup.mappings);
        this.parsedMappings = Object.entries(mappingsObj).map(([key, value]) => ({
          key,
          value: String(value)
        }));
      } catch (e) {
        console.error('Failed to parse mappings', e);
      }
    }
  }

  closeModal() {
    this.selectedLookup = null;
    this.parsedMappings = [];
  }

  getTmsName(tmsId: string): string {
    const tms = this.tmsSystems.find(t => t.id === tmsId);
    return tms ? tms.displayName : tmsId;
  }

  resetForm() {
    this.newLookup = this.getEmptyLookup();
  }

  private getEmptyLookup(): CreateLookupTableRequest {
    return {
      tmsSystemId: '',
      fieldName: '',
      name: '',
      description: '',
      mappings: '',
      defaultValue: '',
      isCaseSensitive: true
    };
  }
}
