import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TooltipModule } from 'primeng/tooltip';

/**
 * SideNavComponent — ported verbatim from
 * transflo-design-system/src/stories/app-shell/side-nav.stories.ts
 *
 * Vertical icon rail with a logo mark, primary nav items, and bottom utilities.
 */
@Component({
  selector: 'app-side-nav',
  standalone: true,
  imports: [CommonModule, TooltipModule],
  template: `
    <nav class="side-nav">
      <div class="side-nav__logo">
        <div class="side-nav__logo-mark">
          <svg width="28" height="28" viewBox="0 0 32 32" fill="none" xmlns="http://www.w3.org/2000/svg">
            <rect width="32" height="32" rx="6" fill="#2474BB" />
            <text
              x="50%"
              y="54%"
              dominant-baseline="middle"
              text-anchor="middle"
              font-family="Arial, sans-serif"
              font-weight="800"
              font-size="18"
              fill="white"
            >
              T
            </text>
          </svg>
        </div>
      </div>

      <div class="side-nav__items">
        <button
          *ngFor="let item of navItems"
          class="side-nav__item"
          [class.side-nav__item--active]="activeItem === item.id"
          [pTooltip]="item.label"
          tooltipPosition="right"
          (click)="activeItem = item.id"
        >
          <i [class]="item.icon"></i>
        </button>
      </div>

      <div class="side-nav__bottom">
        <button class="side-nav__item" pTooltip="Settings" tooltipPosition="right">
          <i class="pi pi-cog"></i>
        </button>
        <div class="side-nav__avatar"><span>JS</span></div>
      </div>
    </nav>
  `,
  styles: [
    `
      :host {
        display: block;
        height: 100%;
      }
      .side-nav {
        display: flex;
        flex-direction: column;
        align-items: center;
        width: 66px;
        height: 100%;
        background: #ffffff;
        border-right: 1px solid #e5e7eb;
        padding: 12px 0;
        box-sizing: border-box;
      }
      .side-nav__logo {
        margin-bottom: 24px;
      }
      .side-nav__logo-mark {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 40px;
        height: 40px;
        cursor: pointer;
      }
      .side-nav__items {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 4px;
        flex: 1;
        width: 100%;
        padding: 0 8px;
        box-sizing: border-box;
      }
      .side-nav__item {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 42px;
        height: 42px;
        border: none;
        border-radius: 8px;
        background: transparent;
        color: #6b7280;
        cursor: pointer;
        transition: background 0.15s, color 0.15s;
        font-size: 18px;
      }
      .side-nav__item:hover {
        background: #f3f4f6;
        color: #374151;
      }
      .side-nav__item--active {
        background: #eff6ff;
        color: #2474bb;
      }
      .side-nav__item--active:hover {
        background: #dbeafe;
        color: #1e40af;
      }
      .side-nav__bottom {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 8px;
        padding: 0 8px;
        width: 100%;
        box-sizing: border-box;
      }
      .side-nav__avatar {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 36px;
        height: 36px;
        border-radius: 50%;
        background: #374151;
        color: #ffffff;
        font-size: 11px;
        font-weight: 700;
        font-family: sans-serif;
        cursor: pointer;
        letter-spacing: 0.5px;
      }
    `,
  ],
})
export class SideNavComponent {
  @Input() activeItem = 'workflows';

  navItems = [
    { id: 'loads', icon: 'pi pi-truck', label: 'Loads' },
    { id: 'workflows', icon: 'pi pi-share-alt', label: 'Workflows' },
    { id: 'reports', icon: 'pi pi-chart-bar', label: 'Reports' },
    { id: 'documents', icon: 'pi pi-file', label: 'Documents' },
  ];
}
