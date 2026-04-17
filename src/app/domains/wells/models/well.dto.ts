// DTOs: reflejan el contrato OpenAPI contract.yml (camelCase wire format)

export interface WellListItemDTO {
  id: string;
  nombrePozo: string;
  operadora: string;
  contrato: string;
  campo: string;
  clasificacion: string;
  estado: string;
  uwi: string | null;
  createdAt: string;
}

export interface WellLocationDTO {
  departamentoId: number;
  departamento: string;
  codigoDaneDpto: string;
  municipioId: number;
  municipio: string;
  codigoDaneMpio: string;
  clusterId: number | null;
  cluster: string | null;
}

export interface WellDetailDTO {
  id: string;
  operadora: string;
  contratoId: number;
  contrato: string;
  tipoContrato: string;
  cuenca: string;
  campoId: number;
  campo: string;
  tipoTrayectoria: string;
  clasificacion: string;
  denominacion: string;
  consecutivo: string;
  nombrePozo: string;
  tipoUbicacion: string;
  tipoAngulo: string;
  tipoObjetivo: string;
  tipoTerminacion: string;
  estado: string;
  uwi: string | null;
  ubicacion: WellLocationDTO;
  createdAt: string;
  lastModifiedAt: string | null;
}

export interface CreateWellRequestDTO {
  contratoId: number;
  campoId: number;
  tipoTrayectoria: string;
  clasificacion: string;
  denominacion: string;
  consecutivo: string;
  tipoUbicacion: string;
  tipoAngulo: string;
  tipoObjetivo: string;
  tipoTerminacion: string;
  departamentoId: number;
  municipioId: number;
  clusterId?: number | null;
}

export interface UpdateWellRequestDTO {
  contratoId: number;
  campoId: number;
  tipoTrayectoria: string;
  clasificacion: string;
  denominacion: string;
  consecutivo: string;
  tipoUbicacion: string;
  tipoAngulo: string;
  tipoObjetivo: string;
  tipoTerminacion: string;
  departamentoId: number;
  municipioId: number;
  clusterId?: number | null;
}

export interface PagedResponseDTO<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}
