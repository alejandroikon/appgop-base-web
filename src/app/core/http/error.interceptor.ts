import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { MessageService } from 'primeng/api';

import { AuthService } from '@core/auth/auth.service';

// Mensajes de fallback cuando el backend no retorna un mensaje legible
const HTTP_ERROR_MESSAGES: Record<number, string> = {
  403: 'No tienes permisos para realizar esta acción.',
  404: 'El recurso solicitado no fue encontrado.',
  422: 'Los datos enviados no son válidos.',
  500: 'Error interno del servidor.',
  503: 'Servicio no disponible. Intenta más tarde.',
};
const FALLBACK_MESSAGE = 'Ocurrió un error inesperado.';
const NO_CONNECTION_MESSAGE = 'Sin conexión. Verifica tu red e intenta de nuevo.';

// Prioridad: mensaje del backend → fallback por código HTTP → fallback genérico
function resolveErrorDetail(error: HttpErrorResponse, fallback: string): string {
  return error.error?.message ?? error.error?.detail ?? fallback;
}

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      const messageService = inject(MessageService);

      switch (error.status) {
        case 401:
          inject(AuthService).logout();
          break;

        case 0:
          messageService.add({
            severity: 'error',
            summary: 'Sin conexión',
            detail: NO_CONNECTION_MESSAGE,
          });
          break;

        default: {
          const fallback = HTTP_ERROR_MESSAGES[error.status] ?? FALLBACK_MESSAGE;
          const detail = resolveErrorDetail(error, fallback);
          messageService.add({ severity: 'error', summary: 'Error', detail });
          break;
        }
      }

      return throwError(() => error);
    })
  );
};