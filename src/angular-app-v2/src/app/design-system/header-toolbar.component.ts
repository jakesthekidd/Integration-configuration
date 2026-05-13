import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TooltipModule } from 'primeng/tooltip';

export interface HeaderApp {
  id: string;
  label: string;
  icon: string;
}

const DEFAULT_APPS: HeaderApp[] = [
  { id: 'people', label: 'People...', icon: 'pi pi-users' },
  { id: 'telematics', label: 'Telematics', icon: 'pi pi-truck' },
  { id: 'wallet', label: 'Wallet', icon: 'pi pi-wallet' },
  { id: 'command', label: 'Comand...', icon: 'pi pi-inbox' },
  { id: 'mobile', label: 'Mobile...', icon: 'pi pi-mobile' },
  { id: 'driverc', label: 'Driver C...', icon: 'pi pi-comments' },
  { id: 'tfx', label: 'TFX', icon: 'pi pi-sparkles' },
  { id: 'workflows', label: 'Workflo...', icon: 'pi pi-share-alt' },
];

/**
 * HeaderToolbarComponent — ported from
 * transflo-design-system/src/stories/app-shell/header-toolbar.stories.ts @ bd2a05e
 *
 * Top-level branded bar with logo, page link, app switcher dropdown, and user menu dropdown.
 *
 * Local additions on top of the story version (preserve on sync):
 *  - `apps` is an Input so the host can drive the switcher list (instead of the demo hardcoded set)
 *  - `(appChange)` emits when a user picks an app — host wires routing
 */
@Component({
  selector: 'app-header-toolbar',
  standalone: true,
  imports: [CommonModule, TooltipModule],
  template: `
    <header class="header-toolbar">
      <a class="header-toolbar__logo" href="#">
        <span class="header-toolbar__logo-text">TRANSFLO</span>
        <span class="header-toolbar__logo-r">®</span>
      </a>

      <div class="header-toolbar__right">
        <a class="header-toolbar__link" href="#">{{ pageLabel }}</a>

        <button
          class="header-toolbar__icon-btn"
          [class.header-toolbar__icon-btn--active]="openMenu === 'apps'"
          (click)="toggleMenu('apps')"
          pTooltip="Apps"
          tooltipPosition="bottom"
        >
          <i class="pi pi-th-large"></i>
        </button>

        <button
          class="header-toolbar__avatar"
          [class.header-toolbar__avatar--active]="openMenu === 'user'"
          (click)="toggleMenu('user')"
          pTooltip="Account"
          tooltipPosition="bottom"
        >
          <span>{{ userInitials }}</span>
        </button>

        <div *ngIf="openMenu === 'apps'" class="header-dropdown header-dropdown--apps">
          <div class="apps-grid">
            <button
              *ngFor="let app of apps"
              class="apps-grid__app"
              [class.apps-grid__app--active]="app.id === activeApp"
              (click)="onAppClick(app.id)"
              type="button"
            >
              <span class="apps-grid__icon-wrap">
                <i [class]="app.icon"></i>
              </span>
              <span class="apps-grid__label">{{ app.label }}</span>
            </button>
          </div>
          <button class="apps-return" type="button">
            <span class="apps-return__icon">T</span>
            <span>Return to Portal</span>
          </button>
        </div>

        <div *ngIf="openMenu === 'user'" class="header-dropdown header-dropdown--user">
          <button class="header-dropdown__close" (click)="openMenu = null" type="button">
            <i class="pi pi-times"></i>
          </button>
          <div class="user-menu__email">{{ userEmail }}</div>
          <div class="user-menu__avatar"><span>{{ userInitials }}</span></div>
          <div class="user-menu__greeting">Hi, {{ userFirstName }}</div>
          <button class="user-menu__settings-btn" type="button">Account Settings</button>
          <div class="user-menu__links">
            <a href="#">Privacy Policy</a>
            <span class="user-menu__dot">•</span>
            <a href="#">Terms of Service</a>
          </div>
          <div class="user-menu__actions">
            <button class="user-menu__action-btn" pTooltip="Help" tooltipPosition="bottom" type="button">
              <i class="pi pi-question-circle"></i>
            </button>
            <button class="user-menu__action-btn" pTooltip="Sign out" tooltipPosition="bottom" type="button">
              <i class="pi pi-sign-out"></i>
            </button>
          </div>
        </div>
      </div>
    </header>
  `,
  styles: [
    `
      :host {
        display: block;
      }

      .header-toolbar {
        position: relative;
        display: flex;
        align-items: center;
        justify-content: space-between;
        height: 56px;
        padding: 0 24px;
        background: #ffffff;
        border-bottom: 1px solid #e5e7eb;
        font-family: sans-serif;
      }

      .header-toolbar__logo {
        display: flex;
        align-items: center;
        gap: 2px;
        text-decoration: none;
      }
      .header-toolbar__logo-text {
        color: #2474bb;
        font-weight: 800;
        font-size: 20px;
        letter-spacing: 0.5px;
        font-style: italic;
      }
      .header-toolbar__logo-r {
        color: #2474bb;
        font-size: 9px;
        font-weight: 700;
        margin-top: -8px;
      }

      .header-toolbar__right {
        display: flex;
        align-items: center;
        gap: 18px;
        position: relative;
      }

      .header-toolbar__link {
        color: #2474bb;
        text-decoration: none;
        font-size: 13px;
        font-weight: 500;
        cursor: pointer;
      }
      .header-toolbar__link:hover {
        text-decoration: underline;
      }

      .header-toolbar__icon-btn {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 36px;
        height: 36px;
        background: transparent;
        border: none;
        border-radius: 6px;
        color: #6b7280;
        cursor: pointer;
        font-size: 18px;
        transition: background 0.15s, color 0.15s;
      }
      .header-toolbar__icon-btn:hover {
        background: #f3f4f6;
        color: #374151;
      }
      .header-toolbar__icon-btn--active,
      .header-toolbar__icon-btn--active:hover {
        background: #2474bb;
        color: #ffffff;
      }

      .header-toolbar__avatar {
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
        border: none;
        cursor: pointer;
        letter-spacing: 0.5px;
        transition: box-shadow 0.15s;
      }
      .header-toolbar__avatar--active {
        box-shadow: 0 0 0 3px rgba(36, 116, 187, 0.3);
      }

      .header-dropdown {
        position: absolute;
        top: calc(100% + 8px);
        right: 0;
        background: #ffffff;
        border-radius: 12px;
        box-shadow: 0 8px 24px rgba(0, 0, 0, 0.12), 0 2px 6px rgba(0, 0, 0, 0.06);
        z-index: 1000;
        animation: dropdown-in 0.18s ease-out;
      }
      @keyframes dropdown-in {
        from {
          opacity: 0;
          transform: translateY(-6px);
        }
        to {
          opacity: 1;
          transform: translateY(0);
        }
      }

      .header-dropdown--apps {
        padding: 20px;
        width: 320px;
      }
      .apps-grid {
        display: grid;
        grid-template-columns: repeat(3, 1fr);
        gap: 8px;
      }
      .apps-grid__app {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 6px;
        padding: 10px 6px;
        background: transparent;
        border: none;
        border-radius: 8px;
        cursor: pointer;
        transition: background 0.15s;
      }
      .apps-grid__app:hover {
        background: #f9fafb;
      }
      .apps-grid__app--active {
        background: #2474bb;
      }
      .apps-grid__app--active .apps-grid__label {
        color: #ffffff;
      }
      .apps-grid__icon-wrap {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 38px;
        height: 38px;
        border-radius: 8px;
        background: #dbeafe;
        color: #2474bb;
        font-size: 18px;
      }
      .apps-grid__app--active .apps-grid__icon-wrap {
        background: #ffffff;
        color: #2474bb;
      }
      .apps-grid__label {
        font-size: 11px;
        font-weight: 600;
        color: #2474bb;
        max-width: 84px;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
      }
      .apps-return {
        display: flex;
        align-items: center;
        gap: 8px;
        margin-top: 16px;
        margin-left: auto;
        padding: 8px 14px;
        background: #ffffff;
        border: 1px solid #2474bb;
        border-radius: 6px;
        color: #2474bb;
        font-size: 12px;
        font-weight: 600;
        cursor: pointer;
        transition: background 0.15s;
      }
      .apps-return:hover {
        background: #f0f7ff;
      }
      .apps-return__icon {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 16px;
        height: 16px;
        background: #2474bb;
        color: #ffffff;
        font-style: italic;
        font-weight: 800;
        font-size: 10px;
        border-radius: 3px;
      }

      .header-dropdown--user {
        padding: 16px 22px 18px 22px;
        width: 280px;
        text-align: center;
      }
      .header-dropdown__close {
        position: absolute;
        top: 12px;
        right: 12px;
        width: 24px;
        height: 24px;
        background: transparent;
        border: none;
        color: #6b7280;
        cursor: pointer;
        display: flex;
        align-items: center;
        justify-content: center;
        font-size: 14px;
      }
      .user-menu__email {
        font-size: 12px;
        color: #374151;
        margin-bottom: 14px;
        margin-top: 4px;
      }
      .user-menu__avatar {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 56px;
        height: 56px;
        border-radius: 50%;
        background: #374151;
        color: #ffffff;
        font-size: 16px;
        font-weight: 700;
        margin: 0 auto 10px auto;
        letter-spacing: 0.5px;
      }
      .user-menu__greeting {
        font-size: 14px;
        color: #111827;
        margin-bottom: 14px;
      }
      .user-menu__settings-btn {
        width: 100%;
        padding: 8px 14px;
        background: #eff6ff;
        border: none;
        border-radius: 6px;
        color: #2474bb;
        font-size: 12px;
        font-weight: 600;
        cursor: pointer;
        transition: background 0.15s;
        margin-bottom: 14px;
      }
      .user-menu__settings-btn:hover {
        background: #dbeafe;
      }
      .user-menu__links {
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 6px;
        font-size: 11px;
        margin-bottom: 14px;
      }
      .user-menu__links a {
        color: #6b7280;
        text-decoration: none;
      }
      .user-menu__links a:hover {
        color: #374151;
        text-decoration: underline;
      }
      .user-menu__dot {
        color: #6b7280;
      }
      .user-menu__actions {
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 8px;
      }
      .user-menu__action-btn {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 32px;
        height: 32px;
        background: #374151;
        color: #ffffff;
        border: none;
        border-radius: 50%;
        cursor: pointer;
        font-size: 13px;
        transition: background 0.15s;
      }
      .user-menu__action-btn:hover {
        background: #1f2937;
      }
    `,
  ],
})
export class HeaderToolbarComponent {
  @Input() pageLabel = 'Dashboard';
  @Input() userInitials = 'JS';
  @Input() userEmail = 'email@email.com';
  @Input() userFirstName = 'John';
  @Input() activeApp = 'workflows';
  @Input() openMenu: 'apps' | 'user' | null = null;

  /** Host-controlled apps list. Falls back to the story demo set if not provided. */
  @Input() apps: HeaderApp[] = DEFAULT_APPS;

  /** Fires when the user picks an app from the switcher. Host wires this to routing. */
  @Output() appChange = new EventEmitter<string>();

  toggleMenu(menu: 'apps' | 'user') {
    this.openMenu = this.openMenu === menu ? null : menu;
  }

  onAppClick(id: string) {
    this.activeApp = id;
    this.openMenu = null;
    this.appChange.emit(id);
  }
}
