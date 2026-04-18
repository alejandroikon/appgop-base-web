import { createFeature, createReducer, on } from '@ngrx/store';
import { WellsActions } from './wells.actions';

export interface WellsState {
  isSaving:  boolean;
  saveError: string | null;
}

const initialState: WellsState = {
  isSaving:  false,
  saveError: null,
};

const wellsReducer = createReducer(
  initialState,

  on(WellsActions.createWell, WellsActions.updateWell, (state) => ({
    ...state,
    isSaving:  true,
    saveError: null,
  })),

  on(WellsActions.createWellSuccess, WellsActions.updateWellSuccess, (state) => ({
    ...state,
    isSaving: false,
  })),

  on(WellsActions.createWellFailure, WellsActions.updateWellFailure, (state, { error }) => ({
    ...state,
    isSaving:  false,
    saveError: error,
  })),

  on(WellsActions.resetWellsForm, (state) => ({
    ...state,
    isSaving:  false,
    saveError: null,
  })),
);

export const wellsFeature = createFeature({
  name: 'wells',
  reducer: wellsReducer,
});
