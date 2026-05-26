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
      background: rgba(15, 23, 42, 0.55);
      backdrop-filter: blur(3px);
    }

    .gate__card {
      width: 100%;
      max-width: 400px;
      background: #ffffff;
      border-radius: 12px;
      padding: 36px 32px 28px;
      box-shadow: 0 20px 48px rgba(0,0,0,0.25);
      display: flex;
      flex-direction: column;
      align-items: center;
    }

    .gate__logo {
      width: 52px;
      height: 52px;
      border-radius: 12px;
      background: var(--tf-primary, #1a56db);
      display: flex;
      align-items: center;
      justify-content: center;
      margin-bottom: 20px;
    }
    .gate__logo i {
      font-size: 1.4rem;
      color: #ffffff;
    }

    .gate__title {
      font-size: 1.25rem;
      font-weight: 700;
      color: #0f172a;
      margin: 0 0 6px;
      text-align: center;
    }

    .gate__sub {
      font-size: 0.8125rem;
      color: #64748b;
      text-align: center;
      margin: 0 0 28px;
      line-height: 1.5;
    }

    .gate__form {
      width: 100%;
      display: flex;
      flex-direction: column;
      gap: 10px;
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
      margin-top: 20px;
      font-size: 0.75rem;
      color: #94a3b8;
      text-align: center;
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
