import { HttpRequest, HttpResponse } from '@angular/common/http';
import { MockHandler } from '@core/http/mock.interceptor';
import type {
  CampoItemDTO,
  ClusterItemDTO,
  ContratoItemDTO,
  CreateClusterRequestDTO,
  DepartamentoItemDTO,
  MunicipioItemDTO,
  PagedResponseDTO,
  WellDetailDTO,
  WellListItemDTO,
} from '@wells/models';

// ─── Datos semilla V2.0 ────────────────────────────────────────────────────────

const MOCK_CONTRATOS: ContratoItemDTO[] = [
  { id: 1, nombre: 'E&P Llanos',     tipo: 'E&P',  cuenca: 'Llanos Orientales',   ubicacionDefault: 'CONTINENTAL' },
  { id: 2, nombre: 'E&P Piedemonte', tipo: 'E&P',  cuenca: 'Piedemonte Llanero',  ubicacionDefault: 'CONTINENTAL' },
  { id: 3, nombre: 'TEA Caguán',     tipo: 'TEA',  cuenca: 'Caguán-Putumayo',     ubicacionDefault: 'CONTINENTAL' },
  { id: 4, nombre: 'VPAA Estratigráfico ANH', tipo: 'VPAA', cuenca: 'Llanos Orientales', ubicacionDefault: 'CONTINENTAL' },
];

const MOCK_CAMPOS: CampoItemDTO[] = [
  { id: 1, nombre: 'Rubiales',  contratoId: 1 },
  { id: 2, nombre: 'Quifa',     contratoId: 1 },
  { id: 3, nombre: 'Cupiagua',  contratoId: 2 },
  { id: 4, nombre: 'Cusiana',   contratoId: 2 },
  { id: 5, nombre: 'Acacías',   contratoId: 3 },
];

const MOCK_DEPARTAMENTOS: DepartamentoItemDTO[] = [
  { id: 1, nombre: 'Meta',      codigoDane: '50' },
  { id: 2, nombre: 'Casanare',  codigoDane: '85' },
  { id: 3, nombre: 'Putumayo',  codigoDane: '86' },
  { id: 4, nombre: 'Santander', codigoDane: '68' },
];

const MOCK_MUNICIPIOS: MunicipioItemDTO[] = [
  { id: 1, nombre: 'Puerto Gaitán',    departamentoId: 1, codigoDane: '50568' },
  { id: 2, nombre: 'San Martín',       departamentoId: 1, codigoDane: '50686' },
  { id: 3, nombre: 'Villanueva',       departamentoId: 2, codigoDane: '85440' },
  { id: 4, nombre: 'Tauramena',        departamentoId: 2, codigoDane: '85410' },
  { id: 5, nombre: 'Puerto Asís',      departamentoId: 3, codigoDane: '86568' },
  { id: 6, nombre: 'Orito',            departamentoId: 3, codigoDane: '86320' },
  { id: 7, nombre: 'Barrancabermeja',  departamentoId: 4, codigoDane: '68081' },
];

const MOCK_CLUSTERS: ClusterItemDTO[] = [
  { id: 1, nombre: 'Locación A',      abreviatura: 'LA', campoId: 1 },
  { id: 2, nombre: 'Locación B',      abreviatura: 'LB', campoId: 1 },
  { id: 3, nombre: 'Cluster Norte 3', abreviatura: 'CN', campoId: 2 },
  { id: 4, nombre: 'Pad Sur 15',      abreviatura: 'PS', campoId: 3 },
];

const MOCK_WELLS_LIST: WellListItemDTO[] = [
  {
    id: '550e8400-e29b-41d4-a716-446655440000',
    nombrePozo: 'RUBIALES-CUSIANA RENATA-1',
    operadora: 'Ecopetrol S.A.',
    contrato: 'E&P Llanos',
    campo: 'Rubiales',
    clasificacion: 'EXPLORATORIO',
    subClasificacion: 'A3',
    estado: 'CREADO',
    uwi: '50568CURE0001LA0000VPH-OH',
    createdAt: '2026-01-15T14:30:00Z',
  },
  {
    id: '550e8400-e29b-41d4-a716-446655440001',
    nombrePozo: 'QUIFA-ALPHA-1',
    operadora: 'Ecopetrol S.A.',
    contrato: 'E&P Llanos',
    campo: 'Quifa',
    clasificacion: 'DESARROLLO',
    subClasificacion: null,
    estado: 'BORRADOR',
    uwi: null,
    createdAt: '2026-02-10T09:00:00Z',
  },
  {
    id: '550e8400-e29b-41d4-a716-446655440002',
    nombrePozo: 'CUPIAGUA-BETA-2',
    operadora: 'Equion Energía',
    contrato: 'E&P Piedemonte',
    campo: 'Cupiagua',
    clasificacion: 'EXPLORATORIO',
    subClasificacion: 'A2b',
    estado: 'CREADO',
    uwi: '85440BETA0002PS0015HST2I-CD',
    createdAt: '2026-01-20T11:15:00Z',
  },
];

const MOCK_WELLS_DETAIL: Record<string, WellDetailDTO> = {
  '550e8400-e29b-41d4-a716-446655440000': {
    id: '550e8400-e29b-41d4-a716-446655440000',
    operadora: 'Ecopetrol S.A.',
    contratoId: 1,
    contrato: 'E&P Llanos',
    tipoContrato: 'E&P',
    cuenca: 'Llanos Orientales',
    campoId: 1,
    campo: 'Rubiales',
    tipoTrayectoria: 'O',
    clasificacion: 'EXPLORATORIO',
    subClasificacion: 'A3',
    denominacion: 'CUSIANA RENATA',
    consecutivo: 1,
    nombrePozo: 'RUBIALES-CUSIANA RENATA-1',
    tipoUbicacion: 'CONTINENTAL',
    tipoAngulo: 'V',
    tipoObjetivo: 'PH',
    tipoTerminacion: 'OH',
    estado: 'CREADO',
    uwi: '50568CURE0001LA0000VPH-OH',
    departamentoId: 1,
    departamento: 'Meta',
    codigoDaneDpto: '50',
    municipioId: 1,
    municipio: 'Puerto Gaitán',
    codigoDaneMpio: '568',
    clusterId: 1,
    cluster: 'Locación A',
    forma101Radicada: false,
    createdAt: '2026-01-15T14:30:00Z',
    lastModifiedAt: null,
  },
  '550e8400-e29b-41d4-a716-446655440001': {
    id: '550e8400-e29b-41d4-a716-446655440001',
    operadora: 'Ecopetrol S.A.',
    contratoId: 1,
    contrato: 'E&P Llanos',
    tipoContrato: 'E&P',
    cuenca: 'Llanos Orientales',
    campoId: 2,
    campo: 'Quifa',
    tipoTrayectoria: null,
    clasificacion: 'DESARROLLO',
    subClasificacion: null,
    denominacion: 'ALPHA',
    consecutivo: 1,
    nombrePozo: 'QUIFA-ALPHA-1',
    tipoUbicacion: null,
    tipoAngulo: null,
    tipoObjetivo: null,
    tipoTerminacion: null,
    estado: 'BORRADOR',
    uwi: null,
    departamentoId: null,
    departamento: null,
    codigoDaneDpto: null,
    municipioId: null,
    municipio: null,
    codigoDaneMpio: null,
    clusterId: null,
    cluster: null,
    forma101Radicada: false,
    createdAt: '2026-02-10T09:00:00Z',
    lastModifiedAt: null,
  },
};

// ─── DB en memoria ─────────────────────────────────────────────────────────────

let mockWellsDb = [...MOCK_WELLS_LIST];
const mockWellsDetailDb: Record<string, WellDetailDTO> = { ...MOCK_WELLS_DETAIL };
let mockClusters       = [...MOCK_CLUSTERS];
let nextIdCounter      = 100;
let nextClusterCounter = 10;

// ─── Helpers ──────────────────────────────────────────────────────────────────

function generateUwiMock(body: Record<string, unknown>, dpto: DepartamentoItemDTO | null | undefined, mpio: MunicipioItemDTO | null | undefined): string {
  const dptoCode = dpto?.codigoDane ?? '00';
  const mpioCode = mpio?.codigoDane?.slice(-3) ?? '000';
  const denom    = String(body['denominacion'] ?? '').toUpperCase().replace(/[^A-Z]/g, '').slice(0, 4).padEnd(4, 'X');
  const consec   = String(body['consecutivo'] ?? 1).padStart(4, '0');
  const angulo   = String(body['tipoAngulo'] ?? 'V');
  const objetivo = String(body['tipoObjetivo'] ?? 'PH');
  const term     = String(body['tipoTerminacion'] ?? 'CD');
  const tray     = body['tipoTrayectoria'] === 'O' ? '' : String(body['tipoTrayectoria'] ?? '');
  return `${dptoCode}${mpioCode}${denom}${consec}CX0000${angulo}${tray}${objetivo}-${term}`;
}

// ─── Handlers V2.0 ────────────────────────────────────────────────────────────

export const wellsMockHandlers: MockHandler[] = [

  // GET /api/v1/wells — listado paginado
  {
    urlPattern: /\/api\/v1\/wells$/,
    method: 'GET',
    handle: (req: HttpRequest<unknown>): HttpResponse<unknown> => {
      const url      = new URL(req.url, 'http://localhost');
      const page     = parseInt(url.searchParams.get('page')     ?? '1',  10);
      const pageSize = parseInt(url.searchParams.get('pageSize') ?? '20', 10);
      const search   = url.searchParams.get('search')?.toLowerCase() ?? '';
      const estado   = url.searchParams.get('estado') ?? '';

      let filtered = [...mockWellsDb];
      if (search)  filtered = filtered.filter((w) => w.nombrePozo.toLowerCase().includes(search) || (w.uwi ?? '').toLowerCase().includes(search));
      if (estado)  filtered = filtered.filter((w) => w.estado === estado);

      const total = filtered.length;
      const items = filtered.slice((page - 1) * pageSize, page * pageSize);
      const body: PagedResponseDTO<WellListItemDTO> = { items, total, page, pageSize };
      return new HttpResponse({ status: 200, body });
    },
  },

  // GET /api/v1/wells/preview-uwi — preview del UWI (unicidad)
  {
    urlPattern: /\/api\/v1\/wells\/preview-uwi/,
    method: 'GET',
    handle: (req: HttpRequest<unknown>): HttpResponse<unknown> => {
      const url          = new URL(req.url, 'http://localhost');
      const dpto         = url.searchParams.get('codigoDaneDpto') ?? '';
      const mpio         = url.searchParams.get('codigoDaneMpio') ?? '';
      const denom        = url.searchParams.get('denominacion')   ?? '';
      const consec       = parseInt(url.searchParams.get('consecutivo') ?? '1', 10);
      const tipoAngulo   = url.searchParams.get('tipoAngulo')     ?? 'V';
      const trayectoria  = url.searchParams.get('tipoTrayectoria') ?? 'O';
      const objetivo     = url.searchParams.get('tipoObjetivo')   ?? 'PH';
      const terminacion  = url.searchParams.get('tipoTerminacion') ?? 'CD';
      const isAnh        = url.searchParams.get('isAnh') === 'true';
      const excludeWellId = url.searchParams.get('excludeWellId') ?? null;
      const clusterNombre = url.searchParams.get('clusterNombre') ?? null;

      // Calcular sigla (simplificada para mock)
      const words = denom.toUpperCase().replace(/[^A-Z\s]/g, '').trim().split(/\s+/).filter(Boolean);
      let sigla: string;
      if (isAnh) {
        sigla = `ANH${words[0]?.[0] ?? 'X'}`;
      } else if (words.length === 1) {
        sigla = words[0].slice(0, 4).padEnd(4, 'X');
      } else {
        sigla = (words[0].slice(0, 2) + words[1].slice(0, 2)).padEnd(4, 'X');
      }

      const mpioCode = mpio.length === 5 ? mpio.slice(2) : mpio.slice(-3).padStart(3, '0');
      const numero   = consec.toString().padStart(4, '0');

      // Cluster code
      let clusterCode = 'CX0000';
      if (clusterNombre) {
        const cluster = mockClusters.find((c) => c.nombre === clusterNombre);
        const abrev   = (cluster?.abreviatura ?? clusterNombre.slice(0, 2)).toUpperCase();
        clusterCode   = abrev + '0000';
      }

      const trayCode = trayectoria === 'O' ? '' : trayectoria;
      const uwi = `${dpto}${mpioCode}${sigla}${numero}${clusterCode}${tipoAngulo}${trayCode}${objetivo}-${terminacion}`;

      const isUnique = !mockWellsDb.some(
        (w) => w.uwi === uwi && w.id !== excludeWellId,
      );

      return new HttpResponse({
        status: 200,
        body: {
          uwi,
          isUnique,
          components: {
            dptoCode: dpto, mpioCode, sigla, numero, clusterCode,
            anguloCode: tipoAngulo, trayectoriaCode: trayCode,
            objetivoCode: objetivo, terminacionCode: terminacion,
          },
        },
      });
    },
  },

  // GET /api/v1/wells/preview-name — preview del nombre del pozo
  {
    urlPattern: /\/api\/v1\/wells\/preview-name/,
    method: 'GET',
    handle: (req: HttpRequest<unknown>): HttpResponse<unknown> => {
      const url          = new URL(req.url, 'http://localhost');
      const contratoId   = parseInt(url.searchParams.get('contratoId') ?? '0', 10);
      const campoId      = url.searchParams.get('campoId') ? parseInt(url.searchParams.get('campoId')!, 10) : null;
      const denominacion = url.searchParams.get('denominacion') ?? '';
      const consecutivo  = parseInt(url.searchParams.get('consecutivo') ?? '1', 10);
      const excludeWellId = url.searchParams.get('excludeWellId') ?? null;

      const campo    = campoId ? MOCK_CAMPOS.find((c) => c.id === campoId) : null;
      const contrato = MOCK_CONTRATOS.find((c) => c.id === contratoId);

      const prefijo    = (campo?.nombre ?? contrato?.nombre ?? '').toUpperCase();
      const nombrePozo = prefijo
        ? `${prefijo}-${denominacion.trim().toUpperCase()}-${consecutivo}`
        : '';

      const isUnique = !mockWellsDb.some(
        (w) => w.nombrePozo === nombrePozo && w.id !== excludeWellId,
      );

      return new HttpResponse({ status: 200, body: { nombrePozo, isUnique } });
    },
  },

  // GET /api/v1/wells/:id — detalle
  {
    urlPattern: /\/api\/v1\/wells\/[^/?]+(?:\?.*)?$/,
    method: 'GET',
    handle: (req: HttpRequest<unknown>): HttpResponse<unknown> | null => {
      const id = req.url.split('/').pop()?.split('?')[0] ?? '';
      const well = mockWellsDetailDb[id];
      if (!well) {
        return new HttpResponse({
          status: 404,
          body: { type: 'https://tools.ietf.org/html/rfc7807', title: 'Not Found', status: 404, detail: 'No se encontró un pozo con el ID proporcionado.' },
        });
      }
      return new HttpResponse({ status: 200, body: well });
    },
  },

  // POST /api/v1/wells — crear (DRAFT o FINALIZE)
  {
    urlPattern: /\/api\/v1\/wells$/,
    method: 'POST',
    handle: (req: HttpRequest<unknown>): HttpResponse<unknown> => {
      const body     = req.body as Record<string, unknown>;
      const action   = String(body['action'] ?? 'DRAFT');
      const contrato = MOCK_CONTRATOS.find((c) => c.id === body['contratoId']);
      const campo    = body['campoId'] ? MOCK_CAMPOS.find((c) => c.id === body['campoId']) : null;
      const dpto     = body['departamentoId'] ? MOCK_DEPARTAMENTOS.find((d) => d.id === body['departamentoId']) : null;
      const mpio     = body['municipioId']    ? MOCK_MUNICIPIOS.find((m) => m.id === body['municipioId'])    : null;
      const cluster  = body['clusterId']      ? mockClusters.find((c) => c.id === body['clusterId'])         : null;

      const prefijo    = (campo?.nombre ?? contrato?.cuenca ?? 'Desconocido').toUpperCase();
      const denom      = String(body['denominacion'] ?? '').toUpperCase();
      const consec     = Number(body['consecutivo'] ?? 1);
      const nombrePozo = denom ? `${prefijo}-${denom}-${consec}` : '';
      const newId      = `mock-${nextIdCounter++}-${Date.now()}`;
      const estado     = action === 'FINALIZE' ? 'CREADO' : 'BORRADOR';
      const uwi        = action === 'FINALIZE'
        ? generateUwiMock(body, dpto, mpio)
        : null;

      const newDetail: WellDetailDTO = {
        id: newId,
        operadora:       'Ecopetrol S.A.',
        contratoId:      body['contratoId'] as number ?? null,
        contrato:        contrato?.nombre ?? null,
        tipoContrato:    contrato?.tipo ?? null,
        cuenca:          contrato?.cuenca ?? null,
        campoId:         body['campoId'] as number ?? null,
        campo:           campo?.nombre ?? null,
        tipoTrayectoria: body['tipoTrayectoria'] as string ?? null,
        clasificacion:   body['clasificacion'] as string ?? null,
        subClasificacion: body['subClasificacion'] as string ?? null,
        denominacion:    denom || null,
        consecutivo:     consec || null,
        nombrePozo:      nombrePozo || null,
        tipoUbicacion:   body['tipoUbicacion'] as string ?? null,
        tipoAngulo:      body['tipoAngulo'] as string ?? null,
        tipoObjetivo:    body['tipoObjetivo'] as string ?? null,
        tipoTerminacion: body['tipoTerminacion'] as string ?? null,
        estado,
        uwi,
        departamentoId:  body['departamentoId'] as number ?? null,
        departamento:    dpto?.nombre ?? null,
        codigoDaneDpto:  dpto?.codigoDane ?? null,
        municipioId:     body['municipioId'] as number ?? null,
        municipio:       mpio?.nombre ?? null,
        codigoDaneMpio:  mpio?.codigoDane?.slice(-3) ?? null,
        clusterId:       cluster?.id ?? null,
        cluster:         cluster?.nombre ?? null,
        forma101Radicada: false,
        createdAt:       new Date().toISOString(),
        lastModifiedAt:  null,
      };

      mockWellsDetailDb[newId] = newDetail;
      mockWellsDb.push({
        id: newId,
        nombrePozo:       nombrePozo,
        operadora:        'Ecopetrol S.A.',
        contrato:         contrato?.nombre ?? '',
        campo:            campo?.nombre ?? null,
        clasificacion:    body['clasificacion'] as string ?? 'EXPLORATORIO',
        subClasificacion: body['subClasificacion'] as string ?? null,
        estado,
        uwi,
        createdAt:        newDetail.createdAt,
      });

      return new HttpResponse({ status: 201, body: newDetail });
    },
  },

  // PUT /api/v1/wells/:id — actualizar
  {
    urlPattern: /\/api\/v1\/wells\/[^/?]+(?:\?.*)?$/,
    method: 'PUT',
    handle: (req: HttpRequest<unknown>): HttpResponse<unknown> => {
      const id       = req.url.split('/').pop()?.split('?')[0] ?? '';
      const existing = mockWellsDetailDb[id];

      if (!existing) {
        return new HttpResponse({ status: 404, body: { detail: 'No se encontró el pozo.' } });
      }
      if (existing.forma101Radicada) {
        return new HttpResponse({ status: 422, body: { detail: 'Este pozo no puede editarse porque tiene Forma 101 radicada.' } });
      }

      const body    = req.body as Record<string, unknown>;
      const action  = String(body['action'] ?? 'SAVE');
      const contrato = body['contratoId'] ? MOCK_CONTRATOS.find((c) => c.id === body['contratoId']) : null;
      const campo    = body['campoId']    ? MOCK_CAMPOS.find((c) => c.id === body['campoId'])    : null;
      const dpto     = body['departamentoId'] ? MOCK_DEPARTAMENTOS.find((d) => d.id === body['departamentoId']) : null;
      const mpio     = body['municipioId']    ? MOCK_MUNICIPIOS.find((m) => m.id === body['municipioId'])    : null;
      const cluster  = body['clusterId']      ? mockClusters.find((c) => c.id === body['clusterId'])         : null;

      const prefijo    = (campo?.nombre ?? contrato?.cuenca ?? existing.cuenca ?? '').toUpperCase();
      const denom      = String(body['denominacion'] ?? existing.denominacion ?? '').toUpperCase();
      const consec     = Number(body['consecutivo'] ?? existing.consecutivo ?? 1);
      const nombrePozo = denom ? `${prefijo}-${denom}-${consec}` : (existing.nombrePozo ?? '');
      const newEstado  = action === 'FINALIZE' ? 'CREADO' : existing.estado;
      const newUwi     = action === 'FINALIZE'
        ? generateUwiMock(body, dpto, mpio)
        : existing.uwi;

      const updated: WellDetailDTO = {
        ...existing,
        contratoId:      (body['contratoId'] as number) ?? existing.contratoId,
        contrato:        contrato?.nombre ?? existing.contrato,
        tipoContrato:    contrato?.tipo ?? existing.tipoContrato,
        cuenca:          contrato?.cuenca ?? existing.cuenca,
        campoId:         (body['campoId'] as number) ?? existing.campoId,
        campo:           campo?.nombre ?? existing.campo,
        tipoTrayectoria: (body['tipoTrayectoria'] as string) ?? existing.tipoTrayectoria,
        clasificacion:   (body['clasificacion'] as string) ?? existing.clasificacion,
        subClasificacion: (body['subClasificacion'] as string) ?? existing.subClasificacion,
        denominacion:    denom || existing.denominacion,
        consecutivo:     consec || existing.consecutivo,
        nombrePozo,
        tipoUbicacion:   (body['tipoUbicacion'] as string) ?? existing.tipoUbicacion,
        tipoAngulo:      (body['tipoAngulo'] as string) ?? existing.tipoAngulo,
        tipoObjetivo:    (body['tipoObjetivo'] as string) ?? existing.tipoObjetivo,
        tipoTerminacion: (body['tipoTerminacion'] as string) ?? existing.tipoTerminacion,
        estado:          newEstado,
        uwi:             newUwi,
        departamentoId:  (body['departamentoId'] as number) ?? existing.departamentoId,
        departamento:    dpto?.nombre ?? existing.departamento,
        codigoDaneDpto:  dpto?.codigoDane ?? existing.codigoDaneDpto,
        municipioId:     (body['municipioId'] as number) ?? existing.municipioId,
        municipio:       mpio?.nombre ?? existing.municipio,
        codigoDaneMpio:  mpio?.codigoDane?.slice(-3) ?? existing.codigoDaneMpio,
        clusterId:       cluster?.id ?? null,
        cluster:         cluster?.nombre ?? null,
        lastModifiedAt:  new Date().toISOString(),
      };

      mockWellsDetailDb[id] = updated;
      const listIdx = mockWellsDb.findIndex((w) => w.id === id);
      if (listIdx >= 0) {
        mockWellsDb[listIdx] = {
          ...mockWellsDb[listIdx],
          nombrePozo,
          contrato:        updated.contrato ?? '',
          campo:           updated.campo,
          estado:          newEstado,
          uwi:             newUwi,
          subClasificacion: updated.subClasificacion ?? null,
        };
      }

      return new HttpResponse({ status: 200, body: updated });
    },
  },

  // DELETE /api/v1/wells/:id — eliminar
  {
    urlPattern: /\/api\/v1\/wells\/[^/?]+(?:\?.*)?$/,
    method: 'DELETE',
    handle: (req: HttpRequest<unknown>): HttpResponse<unknown> => {
      const id       = req.url.split('/').pop()?.split('?')[0] ?? '';
      const existing = mockWellsDetailDb[id];
      if (!existing) {
        return new HttpResponse({ status: 404, body: { detail: 'No se encontró el pozo.' } });
      }
      if (existing.forma101Radicada) {
        return new HttpResponse({ status: 422, body: { detail: 'No se puede eliminar un pozo con Forma 101 radicada.' } });
      }
      delete mockWellsDetailDb[id];
      mockWellsDb = mockWellsDb.filter((w) => w.id !== id);
      return new HttpResponse({ status: 204, body: null });
    },
  },

  // GET /api/v1/catalogs/contratos
  {
    urlPattern: /\/api\/v1\/catalogs\/contratos$/,
    method: 'GET',
    handle: (): HttpResponse<unknown> =>
      new HttpResponse({ status: 200, body: MOCK_CONTRATOS }),
  },

  // GET /api/v1/catalogs/campos?contratoId=N
  {
    urlPattern: /\/api\/v1\/catalogs\/campos/,
    method: 'GET',
    handle: (req: HttpRequest<unknown>): HttpResponse<unknown> => {
      const url        = new URL(req.url, 'http://localhost');
      const contratoId = parseInt(url.searchParams.get('contratoId') ?? '0', 10);
      return new HttpResponse({ status: 200, body: MOCK_CAMPOS.filter((c) => c.contratoId === contratoId) });
    },
  },

  // GET /api/v1/catalogs/departamentos
  {
    urlPattern: /\/api\/v1\/catalogs\/departamentos$/,
    method: 'GET',
    handle: (): HttpResponse<unknown> =>
      new HttpResponse({ status: 200, body: MOCK_DEPARTAMENTOS }),
  },

  // GET /api/v1/catalogs/municipios?departamentoId=N
  {
    urlPattern: /\/api\/v1\/catalogs\/municipios/,
    method: 'GET',
    handle: (req: HttpRequest<unknown>): HttpResponse<unknown> => {
      const url   = new URL(req.url, 'http://localhost');
      const dptoId = parseInt(url.searchParams.get('departamentoId') ?? '0', 10);
      return new HttpResponse({ status: 200, body: MOCK_MUNICIPIOS.filter((m) => m.departamentoId === dptoId) });
    },
  },

  // GET /api/v1/catalogs/clusters?campoId=N
  {
    urlPattern: /\/api\/v1\/catalogs\/clusters/,
    method: 'GET',
    handle: (req: HttpRequest<unknown>): HttpResponse<unknown> => {
      const url    = new URL(req.url, 'http://localhost');
      const campoId = parseInt(url.searchParams.get('campoId') ?? '0', 10);
      return new HttpResponse({ status: 200, body: mockClusters.filter((c) => c.campoId === campoId) });
    },
  },

  // POST /api/v1/catalogs/clusters — crear cluster
  {
    urlPattern: /\/api\/v1\/catalogs\/clusters$/,
    method: 'POST',
    handle: (req: HttpRequest<unknown>): HttpResponse<unknown> => {
      const body    = req.body as CreateClusterRequestDTO;
      const nombre  = body.nombre?.trim() ?? '';
      const campoId = body.campoId;

      if (!nombre || !campoId) {
        return new HttpResponse({ status: 422, body: { detail: 'nombre y campoId son requeridos.' } });
      }

      const exists = mockClusters.some(
        (c) => c.nombre.toLowerCase() === nombre.toLowerCase() && c.campoId === campoId,
      );
      if (exists) {
        return new HttpResponse({ status: 409, body: { detail: `Ya existe un cluster con nombre "${nombre}" en este campo.` } });
      }

      const abreviatura = nombre
        .toUpperCase()
        .replace(/[^A-ZÁÉÍÓÚ]/g, '')
        .slice(0, 2)
        .padEnd(2, 'X');

      const newCluster: ClusterItemDTO = {
        id:          nextClusterCounter++,
        nombre,
        abreviatura,
        campoId,
      };
      mockClusters.push(newCluster);
      return new HttpResponse({ status: 201, body: newCluster });
    },
  },
];
