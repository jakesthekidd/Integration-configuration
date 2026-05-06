import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { GeneralService } from '../../services/general.service';
import { Partner, CreatePartnerRequest } from '../../models/patners.model';

@Component({
  selector: 'app-patners',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './patners.component.html',
  styleUrls: ['./patners.component.scss'],
})
export class PatnersComponent implements OnInit {
  partners: Partner[] = [];
  loading = false;
  creating = false;
  error: string | null = null;
  showCreateForm = false;
  deleting: { [id: string]: boolean } = {};

  newPartner: CreatePartnerRequest = { name: '' };

  constructor(
    private apiService: ApiService,
    private generalService: GeneralService,
  ) {}

  ngOnInit() {
    this.loadPartners();
  }

  loadPartners() {
    this.loading = true;
    this.error = null;

    this.apiService.getPartners().subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.partners = response.data.partners;
        }
        this.loading = false;
      },
      error: () => {
        this.error = 'Failed to load partners.';
        this.loading = false;
      },
    });
  }

  createPartner() {
    if (!this.newPartner.name.trim()) return;

    this.creating = true;
    this.error = null;

    this.apiService.createPartner(this.newPartner).subscribe({
      next: (response) => {
        if (response.success) {
          this.showCreateForm = false;
          this.newPartner = { name: '' };
          this.loadPartners();
          this.generalService.success('Partner created successfully.');
        }
        this.creating = false;
      },
      error: (err) => {
        this.creating = false;
        const message = err?.error?.message || 'Failed to create partner.';
        // this.error = message;
        this.generalService.error(message);
      },
    });
  }

  deletePartner(id: string, name: string) {
    this.generalService
      .confirm({
        title: 'Delete Partner',
        text: `Are you sure you want to delete "${name}"?`,
        confirmText: 'Yes, Delete',
        confirmColor: '#dc3545',
        icon: 'warning',
      })
      .then((result) => {
        if (!result.isConfirmed) return;
        this.deleting[id] = true;

        this.apiService.deletePartner(id).subscribe({
          next: () => {
            this.generalService.success('Partner deleted successfully.');
            this.loadPartners();
            delete this.deleting[id];
          },
          error: () => {
            this.error = 'Failed to delete partner.';
            delete this.deleting[id];
          },
        });
      });
  }

  cancelCreate() {
    this.showCreateForm = false;
    this.newPartner = { name: '' };
    this.error = null;
  }
}
