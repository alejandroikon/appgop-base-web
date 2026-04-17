import type { TransitionAction, TransitionHistoryItem, TransitionResult } from './well-transition.model';
import type { TransitionHistoryItemDTO, TransitionResultDTO } from './well-transition.dto';

// Función pura — DTO → Modelo de dominio para resultado de transición
export function mapTransitionResultDTOToModel(dto: TransitionResultDTO): TransitionResult {
  return {
    id: dto.id,
    estado: dto.estado,
    estadoAnterior: dto.estadoAnterior,
    uwi: dto.uwi,
    action: dto.action as TransitionAction,
    comment: dto.comment,
    transitionedAt: dto.transitionedAt,
    transitionedBy: dto.transitionedBy,
  };
}

// Función pura — DTO → Modelo de dominio para ítem de historial
export function mapTransitionHistoryItemDTOToModel(dto: TransitionHistoryItemDTO): TransitionHistoryItem {
  return {
    id: dto.id,
    fromState: dto.fromState,
    toState: dto.toState,
    action: dto.action as TransitionAction,
    comment: dto.comment,
    performedBy: dto.performedBy,
    performedByName: dto.performedByName,
    performedByRole: dto.performedByRole,
    createdAt: dto.createdAt,
  };
}
