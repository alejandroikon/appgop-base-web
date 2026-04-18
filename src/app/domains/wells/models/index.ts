// Barrel export V2.0: punto único de entrada para todos los modelos del dominio wells

// Enums y constantes de opciones
export type {
  WellStatus,
  TipoTrayectoria,
  Clasificacion,
  SubClasificacion,
  TipoUbicacion,
  TipoAngulo,
  TipoObjetivo,
  TipoTerminacion,
} from './well-enums';
export {
  TIPO_TRAYECTORIA_OPTIONS,
  CLASIFICACION_OPTIONS,
  SUB_CLASIFICACION_OPTIONS,
  TIPO_UBICACION_OPTIONS,
  TIPO_ANGULO_OPTIONS,
  TIPO_OBJETIVO_OPTIONS,
  TIPO_TERMINACION_OPTIONS,
  WELL_STATUS_OPTIONS,
} from './well-enums';

// DTOs de pozos
export type {
  WellListItemDTO,
  WellDetailDTO,
  CreateWellRequestDTO,
  UpdateWellRequestDTO,
  UwiPreviewResponseDTO,
  UwiPreviewComponentsDTO,
  WellNamePreviewResponseDTO,
  PagedResponseDTO,
} from './well.dto';

// DTOs de catálogos
export type {
  ContratoItemDTO,
  CampoItemDTO,
  DepartamentoItemDTO,
  MunicipioItemDTO,
  ClusterItemDTO,
  CreateClusterRequestDTO,
} from './catalog.dto';

// Modelos de dominio
export type { WellListItem, Well, WellsQueryParams } from './well.model';
export type { Contrato, Campo, Departamento, Municipio, Cluster } from './catalog.model';

// Mappers de pozos
export { mapWellListItemDTOToModel, mapWellDetailDTOToModel } from './well.mapper';
