import type {
  Clasificacion,
  TipoAngulo,
  TipoObjetivo,
  TipoTerminacion,
  TipoTrayectoria,
  TipoUbicacion,
  WellStatus,
} from './well-enums';

// ─── Modelos de dominio frontend ─────────────────────────────────────────────

export interface WellListItem {
  id: string;
  nombrePozo: string;
  operadora: string;
  contrato: string;
  campo: string;
  clasificacion: Clasificacion;
  estado: WellStatus;
  uwi: string | null;
  createdAt: string;
}

export interface WellLocation {
  departamentoId: number;
  departamento: string;
  codigoDaneDpto: string;
  municipioId: number;
  municipio: string;
  codigoDaneMpio: string;
  clusterId: number | null;
  cluster: string | null;
}

export interface Well {
  id: string;
  operadora: string;
  contratoId: number;
  contrato: string;
  tipoContrato: string;
  cuenca: string;
  campoId: number;
  campo: string;
  tipoTrayectoria: TipoTrayectoria;
  clasificacion: Clasificacion;
  denominacion: string;
  consecutivo: string;
  nombrePozo: string;
  tipoUbicacion: TipoUbicacion;
  tipoAngulo: TipoAngulo;
  tipoObjetivo: TipoObjetivo;
  tipoTerminacion: TipoTerminacion;
  estado: WellStatus;
  uwi: string | null;
  ubicacion: WellLocation;
  createdAt: string;
  lastModifiedAt: string | null;
}

// Parámetros de consulta para el listado de pozos
export interface WellsQueryParams {
  page: number;
  pageSize: number;
  search?: string;
  contratoId?: number;
  sortBy?: string;
  sortDir?: 'asc' | 'desc';
}
