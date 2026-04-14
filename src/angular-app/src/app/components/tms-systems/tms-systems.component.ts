import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { GeneralService } from '../../services/general.service';
import { TmsSystem, CreateTmsSystemRequest } from '../../models/tms-system.model';

@Component({
  selector: 'app-tms-systems',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './tms-systems.component.html',
  styleUrl: './tms-systems.component.scss',
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
