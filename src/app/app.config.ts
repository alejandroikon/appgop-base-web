import { ApplicationConfig, isDevMode, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideState, provideStore } from '@ngrx/store';
import { provideStoreDevtools } from '@ngrx/store-devtools';
import { provideEffects } from '@ngrx/effects';
import { authFeature, AuthEffects } from '@core/auth/store';
import { providePrimeNG } from 'primeng/config';
import { MessageService } from 'primeng/api';
import Aura from '@primeuix/themes/aura';

import { routes } from './app.routes';
import { mockInterceptor } from '@core/http/mock.interceptor';
import { authInterceptor } from '@core/http/auth.interceptor';
import { tokenRefreshInterceptor } from '@core/http/token-refresh.interceptor';
import { errorInterceptor } from '@core/http/error.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),

    // Router
    provideRouter(routes, withComponentInputBinding()),

    // HTTP + interceptores (orden: mock → auth → refresh → errores)
    provideHttpClient(
      withInterceptors([
        mockInterceptor,
        authInterceptor,
        tokenRefreshInterceptor,
        errorInterceptor,
      ])
    ),

    // NgRx Store global
    provideStore(),
    provideState(authFeature),
    provideEffects(AuthEffects),
    provideStoreDevtools({
      maxAge: 25,
      logOnly: !isDevMode(),
      connectInZone: true,
    }),

    // PrimeNG — tema Aura (modo claro forzado) + MessageService global para toasts
    providePrimeNG({
      theme: {
        preset: Aura,
        options: { darkModeSelector: false },
      },
    }),
    MessageService,
  ],
};
