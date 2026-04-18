import type {
  Clasificacion,
  SubClasificacion,
  TipoAngulo,
  TipoObjetivo,
  TipoTerminacion,
  TipoTrayectoria,
  TipoUbicacion,
  WellStatus,
} from './well-enums';

// ─── Modelos de dominio frontend (V2.0) ──────────────────────────────────────

export interface WellListItem {
  id:               string;
  nombrePozo:       string;
  operadora:        string;
  contrato:         string;
  campo:            string | null;
  clasificacion:    Clasificacion;
  subClasificacion: SubClasificacion | null;
  estado:           WellStatus;
  uwi:              string | null;
  createdAt:        string;
}

export interface Well {
  id:               string;
  operadora:        string;
  contratoId:       number | null;
  contrato:         string | null;
  tipoContrato:     string | null;
  cuenca:           string | null;
  campoId:          number | null;
  campo:            string | null;
  denominacion:     string | null;
  consecutivo:      number | null;
  nombrePozo:       string | null;
  tipoTrayectoria:  TipoTrayectoria | null;
  clasificacion:    Clasificacion | null;
  subClasificacion: SubClasificacion | null;
  tipoUbicacion:    TipoUbicacion | null;
  tipoAngulo:       TipoAngulo | null;
  tipoObjetivo:     TipoObjetivo | null;
  tipoTerminacion:  TipoTerminacion | null;
  estado:           WellStatus;
  uwi:              string | null;
  // Ubicación aplanada (V2.0 — sin sub-objeto WellLocation)
  departamentoId:   number | null;
  departamento:     string | null;
  codigoDaneDpto:   string | null;
  municipioId:      number | null;
  municipio:        string | null;
  codigoDaneMpio:   string | null;
  clusterId:        number | null;
  cluster:          string | null;
  forma101Radicada: boolean;
  createdAt:        string;
  lastModifiedAt:   string | null;
}

// Parámetros de consulta para el listado de pozos
export interface WellsQueryParams {
  page:       number;
  pageSize:   number;
  search?:    string;
  estado?:    WellStatus;
  contratoId?: number;
  sortBy?:    string;
  sortDir?:   'asc' | 'desc';
}
