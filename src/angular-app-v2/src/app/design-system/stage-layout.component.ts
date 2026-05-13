import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TooltipModule } from 'primeng/tooltip';
import { SideNavComponent } from './side-nav.component';

export interface StageTab {
  id: string;
  label: string;
}

/**
 * StageLayoutComponent — synced from
 * transflo-design-system/src/stories/app-shell/stage-layout.stories.ts @ bd2a05e
 * "Add Header Toolbar + Stage Layout refinements"
 *
 * Local additions on top of the story version (preserve on sync):
 *  - `<ng-content>` slot inside `.stage-content` for projected page content
 *  - `(tabChange)` event so a parent can wire navigation
 *  - `[showSideNav]` / `[showActionButtons]` flags so product use can disable them
 *  - Nav rows (breadcrumbs / tabs) suppressed when their data arrays are empty
 *  - Navy header proportions bumped to match production People Management / Workflow Studio:
 *    title row 48→60px, tabs/breadcrumb padding extended, title font 18→20px, page icon 20→22px
 *  - White content card overlap deepened from -10px to -24px (the "floating card" effect)
 */
@Component({
  selector: 'app-stage-layout',
  standalone: true,
  imports: [CommonModule, TooltipModule, SideNavComponent],
  template: `
    <div class="stage-shell">
      <app-side-nav *ngIf="showSideNav" [activeItem]="activeNavItem" />

      <div class="stage-main">
        <div class="stage-panel">
          <div class="stage-header">
            <div class="stage-header__title-row">
              <div class="stage-header__title-group">
                <i [class]="pageIcon + ' stage-header__page-icon'"></i>
                <span class="stage-header__title">{{ pageTitle }}</span>
              </div>
              <div class="stage-header__actions" *ngIf="showActionButtons">
                <button class="stage-header__action-btn" pTooltip="Notifications" tooltipPosition="bottom">
                  <i class="pi pi-bell"></i>
                </button>
                <button class="stage-header__action-btn" pTooltip="Help" tooltipPosition="bottom">
                  <i class="pi pi-question-circle"></i>
                </button>
                <button class="stage-header__action-btn" pTooltip="Close" tooltipPosition="bottom">
                  <i class="pi pi-times"></i>
                </button>
              </div>
            </div>
            <div class="stage-header__divider"></div>

            <div *ngIf="navType === 'breadcrumbs' && breadcrumbs.length" class="stage-header__breadcrumb-row">
              <nav class="stage-breadcrumb">
                <span
                  *ngFor="let crumb of breadcrumbs; let last = last"
                  class="stage-breadcrumb__item"
                  [class.stage-breadcrumb__item--active]="last"
                >
                  <span class="stage-breadcrumb__label">{{ crumb }}</span>
                  <i *ngIf="!last" class="pi pi-chevron-right stage-breadcrumb__sep"></i>
                </span>
              </nav>
            </div>

            <div *ngIf="navType === 'tabs' && tabs.length" class="stage-header__tabs-row">
              <nav class="stage-tabs">
                <button
                  *ngFor="let tab of tabs"
                  class="stage-tabs__tab"
                  [class.stage-tabs__tab--active]="tab.id === activeTab"
                  (click)="onTabClick(tab.id)"
                  type="button"
                >
                  {{ tab.label }}
                </button>
              </nav>
            </div>
          </div>

          <div class="stage-body" [class.stage-body--tabs]="navType === 'tabs'">
            <div
              class="stage-content"
              [class.stage-content--tabs]="navType === 'tabs'"
              [class.stage-content--has-nav]="
                (navType === 'tabs' && tabs.length) || (navType === 'breadcrumbs' && breadcrumbs.length)
              "
            >
              <div class="stage-content__scroll" *ngIf="showPlaceholder">
                <div class="stage-content__placeholder">
                  <i class="pi pi-inbox stage-content__placeholder-icon"></i>
                  <p>Page content renders here</p>
                </div>
              </div>
              <ng-content *ngIf="!showPlaceholder"></ng-content>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
        height: 100%;
        min-height: 0;
      }

      .stage-shell {
        display: flex;
        width: 100%;
        height: 100%;
        background: #ffffff;
        overflow: hidden;
      }

      .stage-main {
        flex: 1;
        padding: 12px;
        display: flex;
        min-width: 0;
      }

      .stage-panel {
        flex: 1;
        display: flex;
        flex-direction: column;
        border-radius: 8px;
        overflow: hidden;
        min-height: 0;
      }

      /* Local: navy is a FIXED-HEIGHT background slab — 163px, always.
         The slab height stays constant whether tabs/breadcrumbs are present;
         only the overlap of the white content card changes (see .stage-content rules).
         Measurements sourced from Figma "Correct Layout" frame 75:1876. */
      .stage-header {
        background: #2474bb;
        flex-shrink: 0;
        height: 163px;
        display: flex;
        flex-direction: column;
      }

      .stage-header__title-row {
        display: flex;
        align-items: center;
        justify-content: space-between;
        padding: 0 24px;
        height: 56px;
        flex-shrink: 0;
        box-sizing: border-box;
      }

      .stage-header__title-group {
        display: flex;
        align-items: center;
        gap: 12px;
      }

      .stage-header__page-icon {
        color: #ffffff;
        font-size: 16px;
      }

      .stage-header__title {
        color: #ffffff;
        font-size: 16px;
        font-weight: 600;
        font-family: sans-serif;
      }

      .stage-header__actions {
        display: flex;
        align-items: center;
        gap: 4px;
      }

      .stage-header__action-btn {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 32px;
        height: 32px;
        background: transparent;
        border: none;
        border-radius: 6px;
        color: rgba(255, 255, 255, 0.8);
        cursor: pointer;
        font-size: 16px;
        transition: background 0.15s, color 0.15s;
      }
      .stage-header__action-btn:hover {
        background: rgba(255, 255, 255, 0.15);
        color: #ffffff;
      }

      .stage-header__divider {
        height: 1px;
        background: rgba(255, 255, 255, 0.25);
        margin: 0 20px;
      }

      .stage-header__breadcrumb-row {
        padding: 8px 24px;
        flex-shrink: 0;
        display: flex;
        align-items: center;
      }

      .stage-breadcrumb {
        display: flex;
        align-items: center;
        gap: 6px;
      }
      .stage-breadcrumb__item {
        display: flex;
        align-items: center;
        gap: 6px;
        font-family: sans-serif;
        font-size: 13px;
      }
      .stage-breadcrumb__label {
        color: rgba(255, 255, 255, 0.7);
      }
      .stage-breadcrumb__item--active .stage-breadcrumb__label {
        color: #ffffff;
        font-weight: 600;
      }
      .stage-breadcrumb__sep {
        color: rgba(255, 255, 255, 0.4);
        font-size: 10px;
      }

      .stage-header__tabs-row {
        padding: 8px 24px;
        flex-shrink: 0;
        display: flex;
        align-items: center;
      }

      .stage-tabs {
        display: flex;
        align-items: center;
        gap: 28px;
      }

      .stage-tabs__tab {
        position: relative;
        padding: 4px 0;
        background: transparent;
        border: none;
        color: rgba(255, 255, 255, 0.65);
        font-family: sans-serif;
        font-size: 13px;
        font-weight: 500;
        cursor: pointer;
        transition: color 0.15s;
      }
      .stage-tabs__tab:hover {
        color: rgba(255, 255, 255, 0.9);
      }
      .stage-tabs__tab--active,
      .stage-tabs__tab--active:hover {
        color: #ffffff;
        font-weight: 700;
      }
      .stage-tabs__tab--active::after {
        content: '';
        position: absolute;
        left: 0;
        right: 0;
        bottom: -4px;
        height: 2px;
        background: #ffffff;
        border-radius: 1px;
      }

      .stage-body {
        flex: 1;
        background: #f3f5f7;
        padding: 0 12px 12px 12px;
        display: flex;
        flex-direction: column;
        min-height: 0;
      }

      /* Local: white card overlap is variable based on what's in the navy slab.
         Navy is 163px tall. Card pulls up into navy by either -99 (no nav) or -59 (nav).
            - No nav: visible navy = 64px = title row (56) + 8px breathing room
            - Has nav: visible navy = 104px = title (56) + tabs (40) + 8px breathing room
         Both values pulled directly from the Figma "Correct Layout" frame. */
      .stage-content {
        flex: 1;
        background: #ffffff;
        border-radius: 8px;
        box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
        overflow: hidden;
        min-height: 0;
        margin-top: -99px;
        position: relative;
        z-index: 1;
        display: flex;
        flex-direction: column;
      }

      .stage-content--has-nav {
        margin-top: -59px;
      }

      .stage-content__scroll {
        flex: 1;
        overflow: auto;
        min-height: 0;
        padding: 24px;
        -webkit-overflow-scrolling: touch;
      }

      .stage-content__scroll::-webkit-scrollbar {
        width: 8px;
      }
      .stage-content__scroll::-webkit-scrollbar-track {
        background: transparent;
      }
      .stage-content__scroll::-webkit-scrollbar-thumb {
        background: #d1d5db;
        border-radius: 4px;
      }
      .stage-content__scroll::-webkit-scrollbar-thumb:hover {
        background: #9ca3af;
      }

      .stage-content--tabs {
        border-radius: 8px;
      }

      .stage-body--tabs {
        padding-top: 0;
      }

      .stage-content__placeholder {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        height: 100%;
        min-height: 400px;
        gap: 12px;
        color: #9ca3af;
        font-family: sans-serif;
      }

      .stage-content__placeholder-icon {
        font-size: 48px;
        color: #d1d5db;
      }
    `,
  ],
})
export class StageLayoutComponent {
  @Input() pageTitle = 'Untitled Workflow';
  @Input() pageIcon = 'pi pi-share-alt';
  @Input() breadcrumbs: string[] = ['Workflow Generator', 'Workflow Builder'];
  @Input() activeNavItem = 'workflows';
  @Input() showPlaceholder = false;
  @Input() navType: 'breadcrumbs' | 'tabs' = 'breadcrumbs';
  @Input() tabs: StageTab[] = [];
  @Input() activeTab = '';
  @Input() showSideNav = false;
  @Input() showActionButtons = false;

  @Output() tabChange = new EventEmitter<string>();

  onTabClick(id: string) {
    this.activeTab = id;
    this.tabChange.emit(id);
  }
}
