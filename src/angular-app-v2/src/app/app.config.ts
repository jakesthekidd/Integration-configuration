import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { providePrimeNG } from 'primeng/config';
import { ConfirmationService, MessageService } from 'primeng/api';

import { routes } from './app.routes';
import { mockApiInterceptor } from './mocks/mock-api.interceptor';
import TransfloTheme from '../theme/transflo-theme';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withInterceptors([mockApiInterceptor])),
    provideAnimationsAsync(),
    providePrimeNG({
      theme: {
        preset: TransfloTheme,
        options: {
          darkModeSelector: '.p-dark',
          cssLayer: false,
        },
      },
    }),
    ConfirmationService,
    MessageService,
  ],
};
