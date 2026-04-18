import { createActionGroup, emptyProps, props } from '@ngrx/store';
import type { CreateWellRequestDTO, UpdateWellRequestDTO, Well } from '@wells/models';

export const WellsActions = createActionGroup({
  source: 'Wells',
  events: {
    // ─── Creación / Actualización ────────────────────────────────────────────
    'Create Well': props<{ payload: CreateWellRequestDTO }>(),
    'Create Well Success': props<{ well: Well }>(),
    'Create Well Failure': props<{ error: string }>(),

    'Update Well': props<{ id: string; payload: UpdateWellRequestDTO }>(),
    'Update Well Success': props<{ well: Well }>(),
    'Update Well Failure': props<{ error: string }>(),

    // ─── Reset ──────────────────────────────────────────────────────────────
    'Reset Wells Form': emptyProps(),
  },
});
