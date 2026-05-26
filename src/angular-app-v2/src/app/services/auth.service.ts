import { Injectable, signal } from '@angular/core';

const SESSION_KEY = 'tf_int_auth';

/**
 * Lightweight gate service for the Integration Library.
 * Any non-empty password unlocks access for the duration of the browser session.
 * Persisted to sessionStorage so a page refresh doesn't re-prompt within the same tab.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  readonly isAuthenticated = signal<boolean>(
    sessionStorage.getItem(SESSION_KEY) === '1',
  );

  unlock(password: string): boolean {
    if (!password?.trim()) return false;
    sessionStorage.setItem(SESSION_KEY, '1');
    this.isAuthenticated.set(true);
    return true;
  }
}
