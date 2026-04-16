// Barrel export: punto único de entrada para todos los modelos del dominio wells

// Enums y constantes de opciones
export type {
  WellStatus,
  TipoTrayectoria,
  Clasificacion,
  TipoUbicacion,
  TipoAngulo,
  TipoObjetivo,
  TipoTerminacion,
} from './well-enums';
export {
  TIPO_TRAYECTORIA_OPTIONS,
  CLASIFICACION_OPTIONS,
  TIPO_UBICACION_OPTIONS,
  TIPO_ANGULO_OPTIONS,
  TIPO_OBJETIVO_OPTIONS,
  TIPO_TERMINACION_OPTIONS,
  WELL_STATUS_OPTIONS,
} from './well-enums';

// DTOs de pozos
export type {
  WellListItemDTO,
  WellLocationDTO,
  WellDetailDTO,
  CreateWellRequestDTO,
  UpdateWellRequestDTO,
  PagedResponseDTO,
} from './well.dto';

// DTOs de catálogos
export type {
  ContratoItemDTO,
  CampoItemDTO,
  DepartamentoItemDTO,
  MunicipioItemDTO,
  ClusterItemDTO,
} from './catalog.dto';

// Modelos de dominio
export type { WellListItem, WellLocation, Well, WellsQueryParams } from './well.model';
export type { Contrato, Campo, Departamento, Municipio, Cluster } from './catalog.model';

// DTO de preview de nombre (feature 006)
export type { WellNamePreviewDTO } from './well-name-preview.dto';

// Mappers
export { mapWellListItemDTOToModel, mapWellDetailDTOToModel } from './well.mapper';
