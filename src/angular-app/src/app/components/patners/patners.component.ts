import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { GeneralService } from '../../services/general.service';
import { CreatePartnerRequest, Partner } from '../../models/partner.model';
import { catchError, finalize, tap } from 'rxjs/operators';
import { of } from 'rxjs';

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

  newPartner: CreatePartnerRequest = { name: '', description: '' };

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

    this.apiService
      .createPartner(this.newPartner)
      .pipe(
        tap((response) => {
          if (response.success) {
            this.showCreateForm = false;
            this.newPartner = { name: '', description: '' };
            this.loadPartners();
            this.generalService.success('Partner created successfully.');
          }
        }),

        catchError((err) => {
          const message = err?.error?.message || 'Failed to create partner.';
          this.generalService.error(message);
          return of(null);
        }),

        finalize(() => {
          this.creating = false;
        }),
      )
      .subscribe();
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
    this.newPartner = { name: '', description: '' };
    this.error = null;
  }
}
