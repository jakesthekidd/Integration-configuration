import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { GeneralService } from '../../services/general.service';
import { LookupTable, CreateLookupTableRequest } from '../../models/lookup-table.model';
import { TmsSystem } from '../../models/tms-system.model';

@Component({
  selector: 'app-lookup-tables',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './lookup-tables.component.html',
  styleUrl: './lookup-tables.component.scss',
})
export class LookupTablesComponent implements OnInit {
  lookupTables: LookupTable[] = [];
  tmsSystems: TmsSystem[] = [];
  selectedTmsId: string = '';
  showCreateForm: boolean = false;
  error: string = '';
  success: string = '';
  selectedLookup: LookupTable | null = null;
  parsedMappings: Array<{ key: string; value: string }> = [];

  newLookup: CreateLookupTableRequest = this.getEmptyLookup();

  constructor(
    private apiService: ApiService,
    private generalService: GeneralService,
  ) {}

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
      },
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
      },
    });
  }

  onTmsChange() {
    this.loadLookupTables();
  }

  isValidJSON(str: string): boolean {
    if (typeof str !== 'string') return false; // Must be a string

    try {
      const parsed = JSON.parse(str);

      // Ensure the parsed result is an object, array, or primitive allowed in JSON
      return (
        typeof parsed === 'object' ||
        typeof parsed === 'number' ||
        typeof parsed === 'boolean' ||
        parsed === null ||
        typeof parsed === 'string'
      );
    } catch (e) {
      return false; // Parsing failed
    }
  }

  createLookupTable() {
    this.error = '';
    this.success = '';
    const mappingStr = '' + this.newLookup.mappings?.toString();
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
      },
    });
  }

  deleteLookupTable(id: string) {
    this.generalService
      .confirm({
        title: 'Delete Lookup Table',
        text: 'Are you sure you want to delete this lookup table?',
        confirmText: 'Yes, Delete',
        confirmColor: '#e74c3c',
        icon: 'warning',
      })
      .then((result) => {
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
          },
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
          value: String(value),
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
    const tms = this.tmsSystems.find((t) => t.id === tmsId);
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
      isCaseSensitive: true,
    };
  }
}
