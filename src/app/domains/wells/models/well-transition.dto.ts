// DTOs: reflejan exactamente los schemas del contract.yml 007-well-state-machine

export interface TransitionRequestDTO {
  action: string;
  comment?: string | null;
}

export interface TransitionResultDTO {
  id: string;
  estado: string;
  estadoAnterior: string;
  uwi: string | null;
  action: string;
  comment: string | null;
  transitionedAt: string;
  transitionedBy: string;
}

export interface TransitionHistoryItemDTO {
  id: string;
  fromState: string;
  toState: string;
  action: string;
  comment: string | null;
  performedBy: string;
  performedByName: string;
  performedByRole: string;
  createdAt: string;
}
