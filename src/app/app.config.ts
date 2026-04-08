import { ApplicationConfig, isDevMode, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideStore } from '@ngrx/store';
import { provideStoreDevtools } from '@ngrx/store-devtools';
import { provideEffects } from '@ngrx/effects';
import { providePrimeNG } from 'primeng/config';
import { MessageService } from 'primeng/api';
import Aura from '@primeuix/themes/aura';

import { routes } from './app.routes';
import { authInterceptor } from '@core/http/auth.interceptor';
import { errorInterceptor } from '@core/http/error.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),

    // Router
    provideRouter(routes, withComponentInputBinding()),

    // HTTP + interceptores (orden: auth primero, errores después)
    provideHttpClient(
      withInterceptors([authInterceptor, errorInterceptor])
    ),

    // NgRx Store global (los feature states se registran en cada dominio)
    provideStore(),
    provideEffects(),
    provideStoreDevtools({
      maxAge: 25,
      logOnly: !isDevMode(),
      connectInZone: true,
    }),

    // PrimeNG — tema Aura + MessageService global para toasts
    providePrimeNG({ theme: { preset: Aura } }),
    MessageService,
  ],
};