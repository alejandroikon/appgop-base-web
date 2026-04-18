import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { MessageService } from 'primeng/api';
import { catchError, map, of, switchMap, tap } from 'rxjs';
import { WellsApiService } from '@wells/services';
import { WellsActions } from './wells.actions';
import { WELL_CREATE_LOCALE } from '../features/well-create/locale';

// ─── Efecto: Crear pozo ───────────────────────────────────────────────────────

export const createWell$ = createEffect(
  (
    actions$       = inject(Actions),
    wellsApi       = inject(WellsApiService),
    messageService = inject(MessageService),
    router         = inject(Router),
  ) =>
    actions$.pipe(
      ofType(WellsActions.createWell),
      switchMap(({ payload }) =>
        wellsApi.createWell(payload).pipe(
          map((well) => WellsActions.createWellSuccess({ well })),
          catchError((err: unknown) => {
            const msg = err instanceof Error ? err.message : WELL_CREATE_LOCALE.messages.saveError;
            return of(WellsActions.createWellFailure({ error: msg }));
          }),
        ),
      ),
    ),
  { functional: true },
);

// ─── Efecto: Navegar tras creación exitosa ────────────────────────────────────

export const createWellSuccess$ = createEffect(
  (
    actions$       = inject(Actions),
    messageService = inject(MessageService),
    router         = inject(Router),
  ) =>
    actions$.pipe(
      ofType(WellsActions.createWellSuccess),
      tap(({ well }) => {
        const msg = well.estado === 'CREADO'
          ? WELL_CREATE_LOCALE.messages.finalizeSuccess
          : WELL_CREATE_LOCALE.messages.draftSuccess;
        messageService.add({ severity: 'success', summary: WELL_CREATE_LOCALE.messages.successSummary, detail: msg });
        router.navigate(['/wells/manage']);
      }),
    ),
  { functional: true, dispatch: false },
);

// ─── Efecto: Actualizar pozo ──────────────────────────────────────────────────

export const updateWell$ = createEffect(
  (
    actions$       = inject(Actions),
    wellsApi       = inject(WellsApiService),
  ) =>
    actions$.pipe(
      ofType(WellsActions.updateWell),
      switchMap(({ id, payload }) =>
        wellsApi.updateWell(id, payload).pipe(
          map((well) => WellsActions.updateWellSuccess({ well })),
          catchError((err: unknown) => {
            const msg = err instanceof Error ? err.message : WELL_CREATE_LOCALE.messages.saveError;
            return of(WellsActions.updateWellFailure({ error: msg }));
          }),
        ),
      ),
    ),
  { functional: true },
);

// ─── Efecto: Navegar tras actualización exitosa ───────────────────────────────

export const updateWellSuccess$ = createEffect(
  (
    actions$       = inject(Actions),
    messageService = inject(MessageService),
    router         = inject(Router),
  ) =>
    actions$.pipe(
      ofType(WellsActions.updateWellSuccess),
      tap(({ well }) => {
        const msg = well.estado === 'CREADO'
          ? WELL_CREATE_LOCALE.messages.finalizeSuccess
          : WELL_CREATE_LOCALE.messages.draftSuccess;
        messageService.add({ severity: 'success', summary: WELL_CREATE_LOCALE.messages.successSummary, detail: msg });
        router.navigate(['/wells/manage']);
      }),
    ),
  { functional: true, dispatch: false },
);
