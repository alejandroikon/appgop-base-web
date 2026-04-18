import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { API } from '@core/http/api-endpoints';
import {
  CampoItemDTO,
  ClusterItemDTO,
  ContratoItemDTO,
  CreateWellRequestDTO,
  DepartamentoItemDTO,
  MunicipioItemDTO,
  PagedResponseDTO,
  UpdateWellRequestDTO,
  WellDetailDTO,
  WellListItemDTO,
  UwiPreviewResponseDTO,
  WellNamePreviewResponseDTO,
  CreateClusterRequestDTO,
  mapWellDetailDTOToModel,
  mapWellListItemDTOToModel,
} from '@wells/models';
import type {
  Contrato,
  Campo,
  Departamento,
  Municipio,
  Cluster,
  Well,
  WellListItem,
  WellsQueryParams,
} from '@wells/models';

@Injectable({ providedIn: 'root' })
export class WellsApiService {
  private readonly http = inject(HttpClient);

  // ─── Wells CRUD ──────────────────────────────────────────────────────────────

  getWells(params: WellsQueryParams): Observable<PagedResponseDTO<WellListItem>> {
    let httpParams = new HttpParams()
      .set('page', params.page.toString())
      .set('pageSize', params.pageSize.toString());

    if (params.search)     httpParams = httpParams.set('search', params.search);
    if (params.estado)     httpParams = httpParams.set('estado', params.estado);
    if (params.contratoId) httpParams = httpParams.set('contratoId', params.contratoId.toString());
    if (params.sortBy)     httpParams = httpParams.set('sortBy', params.sortBy);
    if (params.sortDir)    httpParams = httpParams.set('sortDir', params.sortDir);

    return this.http
      .get<PagedResponseDTO<WellListItemDTO>>(API.wells.base, { params: httpParams })
      .pipe(
        map((res) => ({
          ...res,
          items: res.items.map(mapWellListItemDTOToModel),
        })),
      );
  }

  getWell(id: string): Observable<Well> {
    return this.http
      .get<WellDetailDTO>(API.wells.byId(id))
      .pipe(map(mapWellDetailDTOToModel));
  }

  createWell(data: CreateWellRequestDTO): Observable<Well> {
    return this.http
      .post<WellDetailDTO>(API.wells.base, data)
      .pipe(map(mapWellDetailDTOToModel));
  }

  updateWell(id: string, data: UpdateWellRequestDTO): Observable<Well> {
    return this.http
      .put<WellDetailDTO>(API.wells.byId(id), data)
      .pipe(map(mapWellDetailDTOToModel));
  }

  deleteWell(id: string): Observable<void> {
    return this.http.delete<void>(API.wells.byId(id));
  }

  // ─── Catálogos ───────────────────────────────────────────────────────────────

  getContratos(): Observable<Contrato[]> {
    return this.http
      .get<ContratoItemDTO[]>(API.catalogs.contratos)
      .pipe(map((items) => items as Contrato[]));
  }

  getCampos(contratoId: number): Observable<Campo[]> {
    const params = new HttpParams().set('contratoId', contratoId.toString());
    return this.http
      .get<CampoItemDTO[]>(API.catalogs.campos, { params })
      .pipe(map((items) => items as Campo[]));
  }

  getDepartamentos(): Observable<Departamento[]> {
    return this.http
      .get<DepartamentoItemDTO[]>(API.catalogs.departamentos)
      .pipe(map((items) => items as Departamento[]));
  }

  getMunicipios(departamentoId: number): Observable<Municipio[]> {
    const params = new HttpParams().set('departamentoId', departamentoId.toString());
    return this.http
      .get<MunicipioItemDTO[]>(API.catalogs.municipios, { params })
      .pipe(map((items) => items as Municipio[]));
  }

  getClusters(campoId: number): Observable<Cluster[]> {
    const params = new HttpParams().set('campoId', campoId.toString());
    return this.http
      .get<ClusterItemDTO[]>(API.catalogs.clusters, { params })
      .pipe(map((items) => items as Cluster[]));
  }

  createCluster(data: CreateClusterRequestDTO): Observable<Cluster> {
    return this.http
      .post<ClusterItemDTO>(API.catalogs.clusters, data)
      .pipe(map((item) => item as Cluster));
  }

  // ─── Preview de nombre (RN-16..22) ──────────────────────────────────────────

  previewWellName(
    contratoId: number,
    campoId: number | null,
    denominacion: string,
    consecutivo: number,
    excludeWellId?: string,
  ): Observable<WellNamePreviewResponseDTO> {
    let params = new HttpParams()
      .set('contratoId', contratoId.toString())
      .set('denominacion', denominacion)
      .set('consecutivo', consecutivo.toString());
    if (campoId !== null && campoId !== undefined) {
      params = params.set('campoId', campoId.toString());
    }
    if (excludeWellId) {
      params = params.set('excludeWellId', excludeWellId);
    }
    return this.http.get<WellNamePreviewResponseDTO>(API.wells.previewName, { params });
  }

  // ─── Preview de UWI (RN-27..36) ─────────────────────────────────────────────

  previewUwi(params: {
    codigoDaneDpto: string;
    codigoDaneMpio: string;
    denominacion:   string;
    consecutivo:    number;
    clusterNombre?: string;
    tipoAngulo:     string;
    tipoTrayectoria: string;
    tipoObjetivo:   string;
    tipoTerminacion: string;
    isAnh?:         boolean;
    excludeWellId?: string;
  }): Observable<UwiPreviewResponseDTO> {
    let httpParams = new HttpParams()
      .set('codigoDaneDpto',  params.codigoDaneDpto)
      .set('codigoDaneMpio',  params.codigoDaneMpio)
      .set('denominacion',    params.denominacion)
      .set('consecutivo',     params.consecutivo.toString())
      .set('tipoAngulo',      params.tipoAngulo)
      .set('tipoTrayectoria', params.tipoTrayectoria)
      .set('tipoObjetivo',    params.tipoObjetivo)
      .set('tipoTerminacion', params.tipoTerminacion);

    if (params.clusterNombre)  httpParams = httpParams.set('clusterNombre', params.clusterNombre);
    if (params.isAnh)          httpParams = httpParams.set('isAnh', 'true');
    if (params.excludeWellId)  httpParams = httpParams.set('excludeWellId', params.excludeWellId);

    return this.http.get<UwiPreviewResponseDTO>(API.wells.previewUwi, { params: httpParams });
  }
}
