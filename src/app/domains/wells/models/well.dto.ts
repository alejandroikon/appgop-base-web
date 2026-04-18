// DTOs: reflejan el contrato OpenAPI V2.0 (docs/features/creacion-pozo/contract.yml)

export interface WellListItemDTO {
  id:               string;
  nombrePozo:       string;
  operadora:        string;
  contrato:         string;
  campo:            string | null;
  clasificacion:    string;
  subClasificacion: string | null;
  estado:           string;
  uwi:              string | null;
  createdAt:        string;
}

export interface WellDetailDTO {
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
  tipoTrayectoria:  string | null;
  clasificacion:    string | null;
  subClasificacion: string | null;
  tipoUbicacion:    string | null;
  tipoAngulo:       string | null;
  tipoObjetivo:     string | null;
  tipoTerminacion:  string | null;
  estado:           string;
  uwi:              string | null;
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

export interface CreateWellRequestDTO {
  action:          'DRAFT' | 'FINALIZE';
  contratoId?:     number | null;
  campoId?:        number | null;
  denominacion?:   string | null;
  consecutivo?:    number | null;
  tipoTrayectoria?: string | null;
  clasificacion?:  string | null;
  subClasificacion?: string | null;
  tipoUbicacion?:  string | null;
  tipoAngulo?:     string | null;
  tipoObjetivo?:   string | null;
  tipoTerminacion?: string | null;
  departamentoId?: number | null;
  municipioId?:    number | null;
  clusterId?:      number | null;
}

export interface UpdateWellRequestDTO {
  action:          'SAVE' | 'FINALIZE';
  contratoId?:     number | null;
  campoId?:        number | null;
  denominacion?:   string | null;
  consecutivo?:    number | null;
  tipoTrayectoria?: string | null;
  clasificacion?:  string | null;
  subClasificacion?: string | null;
  tipoUbicacion?:  string | null;
  tipoAngulo?:     string | null;
  tipoObjetivo?:   string | null;
  tipoTerminacion?: string | null;
  departamentoId?: number | null;
  municipioId?:    number | null;
  clusterId?:      number | null;
}

// ─── DTOs de preview ──────────────────────────────────────────────────────────

export interface UwiPreviewComponentsDTO {
  dptoCode:        string;
  mpioCode:        string;
  sigla:           string;
  numero:          string;
  clusterCode:     string;
  anguloCode:      string;
  trayectoriaCode: string;
  objetivoCode:    string;
  terminacionCode: string;
}

export interface UwiPreviewResponseDTO {
  uwi:        string;
  isUnique:   boolean;
  components: UwiPreviewComponentsDTO;
}

export interface WellNamePreviewResponseDTO {
  nombrePozo: string;
  isUnique:   boolean;
}

// ─── Paginación ───────────────────────────────────────────────────────────────

export interface PagedResponseDTO<T> {
  items:    T[];
  total:    number;
  page:     number;
  pageSize: number;
}
