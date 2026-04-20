import { Component } from '@angular/core';
import { AppTab } from './constants/app-tabs.constants';
import { CommonModule } from '@angular/common';
import { TmsSystemsComponent } from './components/tms-systems/tms-systems.component';
import { TemplatesComponent } from './components/templates/templates.component';
import { LookupTablesComponent } from './components/lookup-tables/lookup-tables.component';
import { TransformationTestComponent } from './components/transformation-test/transformation-test.component';
import { TransformationLogsComponent } from './components/transformation-logs/transformation-logs.component';
import { CustomersComponent } from './components/customers/customers.component';
import { IntegrationsComponent } from './components/integrations/integrations.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    CustomersComponent,
    TmsSystemsComponent,
    TemplatesComponent,
    LookupTablesComponent,
    TransformationTestComponent,
    TransformationLogsComponent,
    IntegrationsComponent,
  ],
  template: `
    <header>
      <h1>Field Mapping System</h1>
      <nav>
        <button [class.active]="currentTab === AppTab.Customers" (click)="currentTab = AppTab.Customers">
          Customers
        </button>
        <button [class.active]="currentTab === AppTab.Tms" (click)="currentTab = AppTab.Tms">TMS Systems</button>
        <button [class.active]="currentTab === AppTab.Templates" (click)="currentTab = AppTab.Templates">
          Templates
        </button>
        <button [class.active]="currentTab === AppTab.Lookups" (click)="currentTab = AppTab.Lookups">
          Lookup Tables
        </button>
        <button [class.active]="currentTab === AppTab.Test" (click)="currentTab = AppTab.Test">Test Transform</button>
        <button [class.active]="currentTab === AppTab.Logs" (click)="currentTab = AppTab.Logs">Logs</button>
        <button [class.active]="currentTab === AppTab.Integrations" (click)="currentTab = AppTab.Integrations">
          Integrations
        </button>
      </nav>
    </header>
    <main>
      <app-customers *ngIf="currentTab === AppTab.Customers"></app-customers>
      <app-tms-systems *ngIf="currentTab === AppTab.Tms"></app-tms-systems>
      <app-templates *ngIf="currentTab === AppTab.Templates"></app-templates>
      <app-lookup-tables *ngIf="currentTab === AppTab.Lookups"></app-lookup-tables>
      <app-transformation-test *ngIf="currentTab === AppTab.Test"></app-transformation-test>
      <app-transformation-logs *ngIf="currentTab === AppTab.Logs"></app-transformation-logs>
      <app-integrations *ngIf="currentTab === AppTab.Integrations"></app-integrations>
    </main>
  `,
  styles: [
    `
      header {
        background: #2c3e50;
        color: white;
        padding: 20px;
        box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
      }

      header h1 {
        margin: 0 0 15px 0;
        font-size: 24px;
      }

      nav {
        display: flex;
        gap: 10px;
      }

      nav button {
        color: white;
        background: rgba(255, 255, 255, 0.1);
        border: none;
        padding: 10px 20px;
        border-radius: 4px;
        cursor: pointer;
        font-size: 14px;
        font-weight: 500;
        transition: all 0.2s;
      }

      nav button:hover {
        background: rgba(255, 255, 255, 0.2);
      }

      nav button.active {
        background: #3498db;
      }

      main {
        padding: 20px;
      }
    `,
  ],
})
export class AppComponent {
  readonly AppTab = AppTab;
  title = 'Field Mapping System';
  currentTab: AppTab = AppTab.Tms;
}
