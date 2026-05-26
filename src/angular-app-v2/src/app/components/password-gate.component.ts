import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PasswordModule } from 'primeng/password';
import { ButtonModule } from 'primeng/button';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-password-gate',
  imports: [FormsModule, PasswordModule, ButtonModule],
  template: `
    <div class="gate">
      <div class="gate__card">
        <div class="gate__logo">
          <i class="pi pi-lock"></i>
        </div>
        <h1 class="gate__title">Integration Library</h1>
        <p class="gate__sub">This area is restricted to internal use. Enter the access password to continue.</p>

        <form class="gate__form" (ngSubmit)="submit()" autocomplete="off">
          <p-password
            [(ngModel)]="password"
            name="password"
            placeholder="Enter password"
            [feedback]="false"
            [toggleMask]="true"
            [style]="{ width: '100%' }"
            [inputStyle]="{ width: '100%' }"
            (keydown.enter)="submit()"
          />
          @if (error()) {
            <p class="gate__error">
              <i class="pi pi-exclamation-circle"></i>
              Please enter a password.
            </p>
          }
          <p-button
            type="submit"
            label="Continue"
            icon="pi pi-arrow-right"
            iconPos="right"
            severity="primary"
            styleClass="gate__btn"
            [style]="{ width: '100%' }"
            (onClick)="submit()"
          />
        </form>

        <p class="gate__footer">Transflo · Integration Platform</p>
      </div>
    </div>
  `,
  styles: [`
    .gate {
      position: fixed;
      inset: 0;
      z-index: 9999;
      display: flex;
      align-items: center;
      justify-content: center;
      background: #0f172a;
    }

    .gate__card {
      width: 100%;
      max-width: 420px;
      background: #ffffff;
      border-radius: 16px;
      padding: 48px 40px 40px;
      box-shadow: 0 24px 64px rgba(0,0,0,0.4);
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 0;
    }

    .gate__logo {
      width: 64px;
      height: 64px;
      border-radius: 16px;
      background: var(--tf-primary, #1a56db);
      display: flex;
      align-items: center;
      justify-content: center;
      margin-bottom: 24px;
    }
    .gate__logo i {
      font-size: 1.75rem;
      color: #ffffff;
    }

    .gate__title {
      font-size: 1.5rem;
      font-weight: 700;
      color: #0f172a;
      margin: 0 0 8px;
      text-align: center;
    }

    .gate__sub {
      font-size: 0.875rem;
      color: #64748b;
      text-align: center;
      margin: 0 0 32px;
      line-height: 1.5;
    }

    .gate__form {
      width: 100%;
      display: flex;
      flex-direction: column;
      gap: 12px;
    }

    .gate__error {
      display: flex;
      align-items: center;
      gap: 6px;
      font-size: 0.8125rem;
      color: #dc2626;
      margin: 0;
    }

    .gate__footer {
      margin-top: 28px;
      font-size: 0.75rem;
      color: #94a3b8;
      text-align: center;
    }

    :host ::ng-deep .gate__btn {
      margin-top: 4px;
    }
    :host ::ng-deep .p-password,
    :host ::ng-deep .p-password input {
      width: 100%;
    }
  `],
})
export class PasswordGateComponent {
  private auth = inject(AuthService);

  password = '';
  error = signal(false);

  submit() {
    const ok = this.auth.unlock(this.password);
    this.error.set(!ok);
  }
}
