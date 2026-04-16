import type { WellDetailDTO, WellListItemDTO, WellLocationDTO } from './well.dto';
import type { Clasificacion, TipoAngulo, TipoObjetivo, TipoTerminacion, TipoTrayectoria, TipoUbicacion, WellStatus } from './well-enums';
import type { Well, WellListItem, WellLocation } from './well.model';

// ─── Mappers: DTO → Modelo de dominio ─────────────────────────────────────────
// El backend serializa en camelCase, por lo que el mapeo es casi directo.
// Los casts de tipo garantizan que el modelo frontend esté tipado con los enums correctos.

export function mapWellListItemDTOToModel(dto: WellListItemDTO): WellListItem {
  return {
    id: dto.id,
    nombrePozo: dto.nombrePozo,
    operadora: dto.operadora,
    contrato: dto.contrato,
    campo: dto.campo,
    clasificacion: dto.clasificacion as Clasificacion,
    estado: dto.estado as WellStatus,
    createdAt: dto.createdAt,
  };
}

function mapWellLocationDTOToModel(dto: WellLocationDTO): WellLocation {
  return {
    departamentoId: dto.departamentoId,
    departamento: dto.departamento,
    codigoDaneDpto: dto.codigoDaneDpto,
    municipioId: dto.municipioId,
    municipio: dto.municipio,
    codigoDaneMpio: dto.codigoDaneMpio,
    clusterId: dto.clusterId,
    cluster: dto.cluster,
  };
}

export function mapWellDetailDTOToModel(dto: WellDetailDTO): Well {
  return {
    id: dto.id,
    operadora: dto.operadora,
    contratoId: dto.contratoId,
    contrato: dto.contrato,
    tipoContrato: dto.tipoContrato,
    cuenca: dto.cuenca,
    campoId: dto.campoId,
    campo: dto.campo,
    tipoTrayectoria: dto.tipoTrayectoria as TipoTrayectoria,
    clasificacion: dto.clasificacion as Clasificacion,
    denominacion: dto.denominacion,
    consecutivo: dto.consecutivo,
    nombrePozo: dto.nombrePozo,
    tipoUbicacion: dto.tipoUbicacion as TipoUbicacion,
    tipoAngulo: dto.tipoAngulo as TipoAngulo,
    tipoObjetivo: dto.tipoObjetivo as TipoObjetivo,
    tipoTerminacion: dto.tipoTerminacion as TipoTerminacion,
    estado: dto.estado as WellStatus,
    ubicacion: mapWellLocationDTOToModel(dto.ubicacion),
    createdAt: dto.createdAt,
    lastModifiedAt: dto.lastModifiedAt,
  };
}
