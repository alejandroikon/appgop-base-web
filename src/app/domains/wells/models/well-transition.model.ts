// Tipos de acción de transición alineados con contract.yml §TransitionRequest
export type TransitionAction = 'ENVIAR' | 'APROBAR_UWI' | 'DEVOLVER' | 'FISCALIZAR';

// Resultado de una transición ejecutada (respuesta del endpoint PATCH /wells/{id}/transition)
export interface TransitionResult {
  id: string;
  estado: string;
  estadoAnterior: string;
  uwi: string | null;
  action: TransitionAction;
  comment: string | null;
  transitionedAt: string;
  transitionedBy: string;
}

// Entrada del historial de transiciones (respuesta del endpoint GET /wells/{id}/history)
export interface TransitionHistoryItem {
  id: string;
  fromState: string;
  toState: string;
  action: TransitionAction;
  comment: string | null;
  performedBy: string;
  performedByName: string;
  performedByRole: string;
  createdAt: string;
}
