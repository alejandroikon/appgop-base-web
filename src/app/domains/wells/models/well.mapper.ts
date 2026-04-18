import type { WellDetailDTO, WellListItemDTO } from './well.dto';
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
import type { Well, WellListItem } from './well.model';

// ─── Mappers: DTO → Modelo de dominio (V2.0) ─────────────────────────────────

export function mapWellListItemDTOToModel(dto: WellListItemDTO): WellListItem {
  return {
    id:               dto.id,
    nombrePozo:       dto.nombrePozo,
    operadora:        dto.operadora,
    contrato:         dto.contrato,
    campo:            dto.campo ?? null,
    clasificacion:    dto.clasificacion as Clasificacion,
    subClasificacion: (dto.subClasificacion ?? null) as SubClasificacion | null,
    estado:           dto.estado as WellStatus,
    uwi:              dto.uwi ?? null,
    createdAt:        dto.createdAt,
  };
}

export function mapWellDetailDTOToModel(dto: WellDetailDTO): Well {
  return {
    id:               dto.id,
    operadora:        dto.operadora,
    contratoId:       dto.contratoId ?? null,
    contrato:         dto.contrato ?? null,
    tipoContrato:     dto.tipoContrato ?? null,
    cuenca:           dto.cuenca ?? null,
    campoId:          dto.campoId ?? null,
    campo:            dto.campo ?? null,
    denominacion:     dto.denominacion ?? null,
    consecutivo:      dto.consecutivo ?? null,
    nombrePozo:       dto.nombrePozo ?? null,
    tipoTrayectoria:  (dto.tipoTrayectoria ?? null) as TipoTrayectoria | null,
    clasificacion:    (dto.clasificacion ?? null) as Clasificacion | null,
    subClasificacion: (dto.subClasificacion ?? null) as SubClasificacion | null,
    tipoUbicacion:    (dto.tipoUbicacion ?? null) as TipoUbicacion | null,
    tipoAngulo:       (dto.tipoAngulo ?? null) as TipoAngulo | null,
    tipoObjetivo:     (dto.tipoObjetivo ?? null) as TipoObjetivo | null,
    tipoTerminacion:  (dto.tipoTerminacion ?? null) as TipoTerminacion | null,
    estado:           dto.estado as WellStatus,
    uwi:              dto.uwi ?? null,
    departamentoId:   dto.departamentoId ?? null,
    departamento:     dto.departamento ?? null,
    codigoDaneDpto:   dto.codigoDaneDpto ?? null,
    municipioId:      dto.municipioId ?? null,
    municipio:        dto.municipio ?? null,
    codigoDaneMpio:   dto.codigoDaneMpio ?? null,
    clusterId:        dto.clusterId ?? null,
    cluster:          dto.cluster ?? null,
    forma101Radicada: dto.forma101Radicada ?? false,
    createdAt:        dto.createdAt,
    lastModifiedAt:   dto.lastModifiedAt ?? null,
  };
}
