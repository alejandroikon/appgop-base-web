import { HttpRequest, HttpResponse } from '@angular/common/http';
import { MockHandler } from '@core/http/mock.interceptor';
import type {
  CampoItemDTO,
  ClusterItemDTO,
  ContratoItemDTO,
  DepartamentoItemDTO,
  MunicipioItemDTO,
  PagedResponseDTO,
  WellDetailDTO,
  WellListItemDTO,
  TransitionResultDTO,
  TransitionHistoryItemDTO,
} from '@wells/models';

// ─── Datos semilla ─────────────────────────────────────────────────────────────

const MOCK_CONTRATOS: ContratoItemDTO[] = [
  { id: 1, nombre: 'Contrato E&P Llanos',     tipo: 'E&P',        cuenca: 'Llanos Orientales' },
  { id: 2, nombre: 'Contrato E&P Piedemonte', tipo: 'E&P',        cuenca: 'Piedemonte Llanero' },
  { id: 3, nombre: 'Contrato TEA Caguán',     tipo: 'TEA',        cuenca: 'Caguán-Putumayo'   },
];

const MOCK_CAMPOS: CampoItemDTO[] = [
  { id: 1, nombre: 'Campo Rubiales',    contratoId: 1 },
  { id: 2, nombre: 'Campo Quifa',       contratoId: 1 },
  { id: 3, nombre: 'Campo Cupiagua',    contratoId: 2 },
  { id: 4, nombre: 'Campo Cuisiana',    contratoId: 2 },
  { id: 5, nombre: 'Campo Acacías',     contratoId: 3 },
];

const MOCK_DEPARTAMENTOS: DepartamentoItemDTO[] = [
  { id: 1, nombre: 'Meta',           codigoDane: '50' },
  { id: 2, nombre: 'Casanare',       codigoDane: '85' },
  { id: 3, nombre: 'Putumayo',       codigoDane: '86' },
  { id: 4, nombre: 'Arauca',         codigoDane: '81' },
];

const MOCK_MUNICIPIOS: MunicipioItemDTO[] = [
  { id: 1,  nombre: 'Puerto Gaitán',  departamentoId: 1, codigoDane: '50568' },
  { id: 2,  nombre: 'San Martín',     departamentoId: 1, codigoDane: '50686' },
  { id: 3,  nombre: 'Villanueva',     departamentoId: 2, codigoDane: '85440' },
  { id: 4,  nombre: 'Tauramena',      departamentoId: 2, codigoDane: '85410' },
  { id: 5,  nombre: 'Puerto Asís',    departamentoId: 3, codigoDane: '86568' },
  { id: 6,  nombre: 'Arauca',         departamentoId: 4, codigoDane: '81001' },
];

const MOCK_CLUSTERS: ClusterItemDTO[] = [
  { id: 1, nombre: 'Cluster Norte',  campoId: 1 },
  { id: 2, nombre: 'Cluster Sur',    campoId: 1 },
  { id: 3, nombre: 'Cluster Este',   campoId: 2 },
  { id: 4, nombre: 'Cluster Oeste',  campoId: 3 },
];

const MOCK_WELLS_LIST: WellListItemDTO[] = [
  {
    id: '550e8400-e29b-41d4-a716-446655440000',
    nombrePozo: 'Llanos Orientales-ALPHA-01',
    operadora: 'Ecopetrol S.A.',
    contrato: 'Contrato E&P Llanos',
    campo: 'Campo Rubiales',
    clasificacion: 'EXPLORATORIO',
    estado: 'BORRADOR',
    uwi: null,
    createdAt: '2024-11-15T14:30:00Z',
  },
  {
    id: '550e8400-e29b-41d4-a716-446655440001',
    nombrePozo: 'Llanos Orientales-BETA-02',
    operadora: 'Ecopetrol S.A.',
    contrato: 'Contrato E&P Llanos',
    campo: 'Campo Quifa',
    clasificacion: 'DESARROLLO',
    estado: 'PENDING_UWI',
    uwi: 'CO-50-50568-BETA-02-ST',
    createdAt: '2024-11-10T09:00:00Z',
  },
  {
    id: '550e8400-e29b-41d4-a716-446655440002',
    nombrePozo: 'Piedemonte Llanero-GAMMA-01',
    operadora: 'Equion Energía',
    contrato: 'Contrato E&P Piedemonte',
    campo: 'Campo Cupiagua',
    clasificacion: 'EXPLORATORIO',
    estado: 'READY_FISCAL',
    uwi: 'CO-85-85440-GAMMA-01-ST',
    createdAt: '2024-10-20T11:15:00Z',
  },
  {
    id: '550e8400-e29b-41d4-a716-446655440003',
    nombrePozo: 'Caguán-Putumayo-DELTA-03',
    operadora: 'Gran Tierra Energy',
    contrato: 'Contrato TEA Caguán',
    campo: 'Campo Acacías',
    clasificacion: 'ESTRATIGRAFICO',
    estado: 'FISCALIZADO',
    uwi: 'CO-86-86568-DELTA-03-ST',
    createdAt: '2024-09-05T08:00:00Z',
  },
  {
    id: '550e8400-e29b-41d4-a716-446655440004',
    nombrePozo: 'Llanos Orientales-EPSILON-01',
    operadora: 'Ecopetrol S.A.',
    contrato: 'Contrato E&P Llanos',
    campo: 'Campo Rubiales',
    clasificacion: 'DESARROLLO',
    estado: 'BORRADOR',
    uwi: null,
    createdAt: '2024-12-01T16:45:00Z',
  },
];

const MOCK_WELLS_DETAIL: Record<string, WellDetailDTO> = {
  '550e8400-e29b-41d4-a716-446655440000': {
    id: '550e8400-e29b-41d4-a716-446655440000',
    operadora: 'Ecopetrol S.A.',
    contratoId: 1,
    contrato: 'Contrato E&P Llanos',
    tipoContrato: 'E&P',
    cuenca: 'Llanos Orientales',
    campoId: 1,
    campo: 'Campo Rubiales',
    tipoTrayectoria: 'ST',
    clasificacion: 'EXPLORATORIO',
    denominacion: 'ALPHA',
    consecutivo: '01',
    nombrePozo: 'Llanos Orientales-ALPHA-01',
    tipoUbicacion: 'CONTINENTAL',
    tipoAngulo: 'V',
    tipoObjetivo: 'PH',
    tipoTerminacion: 'CD',
    estado: 'BORRADOR',
    uwi: null,
    ubicacion: {
      departamentoId: 1,
      departamento: 'Meta',
      codigoDaneDpto: '50',
      municipioId: 1,
      municipio: 'Puerto Gaitán',
      codigoDaneMpio: '50568',
      clusterId: 1,
      cluster: 'Cluster Norte',
    },
    createdAt: '2024-11-15T14:30:00Z',
    lastModifiedAt: null,
  },
};

// ─── Almacenamiento en memoria para mocks de escritura ───────────────────────

let mockWellsDb = [...MOCK_WELLS_LIST];
const mockWellsDetailDb: Record<string, WellDetailDTO> = { ...MOCK_WELLS_DETAIL };
let nextIdCounter = 100;

// ─── Handlers ─────────────────────────────────────────────────────────────────

export const wellsMockHandlers: MockHandler[] = [
  // GET /api/v1/wells — listado paginado
  {
    urlPattern: /\/api\/v1\/wells$/,
    method: 'GET',
    handle: (req: HttpRequest<unknown>): HttpResponse<unknown> => {
      const url = new URL(req.url, 'http://localhost');
      const page     = parseInt(url.searchParams.get('page')     ?? '1',  10);
      const pageSize = parseInt(url.searchParams.get('pageSize') ?? '20', 10);
      const search   = url.searchParams.get('search')?.toLowerCase()    ?? '';
      const contratoIdStr = url.searchParams.get('contratoId');

      let filtered = [...mockWellsDb];

      if (search) {
        filtered = filtered.filter((w) =>
          w.nombrePozo.toLowerCase().includes(search) ||
          w.operadora.toLowerCase().includes(search)
        );
      }
      if (contratoIdStr) {
        const cId = parseInt(contratoIdStr, 10);
        const contrato = MOCK_CONTRATOS.find((c) => c.id === cId)?.nombre;
        if (contrato) filtered = filtered.filter((w) => w.contrato === contrato);
      }

      const total = filtered.length;
      const items = filtered.slice((page - 1) * pageSize, page * pageSize);
      const body: PagedResponseDTO<WellListItemDTO> = { items, total, page, pageSize };
      return new HttpResponse({ status: 200, body });
    },
  },

  // GET /api/v1/wells/preview-name — previsualizar nombre del pozo
  // IMPORTANTE: debe estar antes del handler de /:id para que el interceptor lo encuentre primero
  {
    urlPattern: /\/api\/v1\/wells\/preview-name/,
    method: 'GET',
    handle: (req: HttpRequest<unknown>): HttpResponse<unknown> => {
      const url = new URL(req.url, 'http://localhost');
      const contratoId   = parseInt(url.searchParams.get('contratoId')   ?? '0', 10);
      const denominacion = url.searchParams.get('denominacion')            ?? '';
      const consecutivo  = url.searchParams.get('consecutivo')             ?? '';
      const excludeWellId = url.searchParams.get('excludeWellId')          ?? null;

      const contrato = MOCK_CONTRATOS.find((c) => c.id === contratoId);
      const nombrePozo = contrato
        ? `${contrato.cuenca}-${denominacion.trim()}-${consecutivo}`
        : '';

      const exists = mockWellsDb.some(
        (w) => w.nombrePozo === nombrePozo && w.id !== excludeWellId,
      );

      return new HttpResponse({
        status: 200,
        body: { nombrePozo, available: !exists },
      });
    },
  },

  // GET /api/v1/wells/:id — detalle
  {
    urlPattern: /\/api\/v1\/wells\/[^/]+$/,
    method: 'GET',
    handle: (req: HttpRequest<unknown>): HttpResponse<unknown> | null => {
      const id = req.url.split('/').pop() ?? '';
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

  // POST /api/v1/wells — crear
  {
    urlPattern: /\/api\/v1\/wells$/,
    method: 'POST',
    handle: (req: HttpRequest<unknown>): HttpResponse<unknown> => {
      const body = req.body as Record<string, unknown>;
      const contrato = MOCK_CONTRATOS.find((c) => c.id === body['contratoId']);
      const campo    = MOCK_CAMPOS.find((c)    => c.id === body['campoId']);
      const dpto     = MOCK_DEPARTAMENTOS.find((d) => d.id === body['departamentoId']);
      const mpio     = MOCK_MUNICIPIOS.find((m)    => m.id === body['municipioId']);
      const cluster  = body['clusterId'] ? MOCK_CLUSTERS.find((c) => c.id === body['clusterId']) : null;

      const newId = `mock-${nextIdCounter++}-${Date.now()}`;
      const nombrePozo = `${contrato?.cuenca ?? 'Cuenca'}-${body['denominacion']}-${body['consecutivo']}`;

      const newDetail: WellDetailDTO = {
        id: newId,
        operadora: 'Ecopetrol S.A.',
        contratoId: body['contratoId'] as number,
        contrato: contrato?.nombre ?? '',
        tipoContrato: contrato?.tipo ?? '',
        cuenca: contrato?.cuenca ?? '',
        campoId: body['campoId'] as number,
        campo: campo?.nombre ?? '',
        tipoTrayectoria: body['tipoTrayectoria'] as string,
        clasificacion: body['clasificacion'] as string,
        denominacion: body['denominacion'] as string,
        consecutivo: body['consecutivo'] as string,
        nombrePozo,
        tipoUbicacion: body['tipoUbicacion'] as string,
        tipoAngulo: body['tipoAngulo'] as string,
        tipoObjetivo: body['tipoObjetivo'] as string,
        tipoTerminacion: body['tipoTerminacion'] as string,
        estado: 'BORRADOR',
        uwi: null,
        ubicacion: {
          departamentoId: body['departamentoId'] as number,
          departamento: dpto?.nombre ?? '',
          codigoDaneDpto: dpto?.codigoDane ?? '',
          municipioId: body['municipioId'] as number,
          municipio: mpio?.nombre ?? '',
          codigoDaneMpio: mpio?.codigoDane ?? '',
          clusterId: cluster?.id ?? null,
          cluster: cluster?.nombre ?? null,
        },
        createdAt: new Date().toISOString(),
        lastModifiedAt: null,
      };

      mockWellsDetailDb[newId] = newDetail;
      mockWellsDb.push({
        id: newId,
        nombrePozo,
        operadora: 'Ecopetrol S.A.',
        contrato: contrato?.nombre ?? '',
        campo: campo?.nombre ?? '',
        clasificacion: body['clasificacion'] as string,
        estado: 'BORRADOR',
        uwi: null,
        createdAt: newDetail.createdAt,
      });

      return new HttpResponse({ status: 201, body: newDetail });
    },
  },

  // PUT /api/v1/wells/:id — actualizar
  {
    urlPattern: /\/api\/v1\/wells\/[^/]+$/,
    method: 'PUT',
    handle: (req: HttpRequest<unknown>): HttpResponse<unknown> => {
      const id = req.url.split('/').pop() ?? '';
      const existing = mockWellsDetailDb[id];
      if (!existing) {
        return new HttpResponse({ status: 404, body: { detail: 'No se encontró el pozo.' } });
      }
      if (existing.estado !== 'BORRADOR') {
        return new HttpResponse({ status: 422, body: { detail: 'Solo se pueden editar pozos en estado BORRADOR.' } });
      }

      const body      = req.body as Record<string, unknown>;
      const contrato  = MOCK_CONTRATOS.find((c) => c.id === body['contratoId']);
      const campo     = MOCK_CAMPOS.find((c)    => c.id === body['campoId']);
      const dpto      = MOCK_DEPARTAMENTOS.find((d) => d.id === body['departamentoId']);
      const mpio      = MOCK_MUNICIPIOS.find((m)    => m.id === body['municipioId']);
      const cluster   = body['clusterId'] ? MOCK_CLUSTERS.find((c) => c.id === body['clusterId']) : null;
      const nombrePozo = `${contrato?.cuenca ?? existing.cuenca}-${body['denominacion']}-${body['consecutivo']}`;

      const updated: WellDetailDTO = {
        ...existing,
        contratoId: body['contratoId'] as number,
        contrato: contrato?.nombre ?? existing.contrato,
        tipoContrato: contrato?.tipo ?? existing.tipoContrato,
        cuenca: contrato?.cuenca ?? existing.cuenca,
        campoId: body['campoId'] as number,
        campo: campo?.nombre ?? existing.campo,
        tipoTrayectoria: body['tipoTrayectoria'] as string,
        clasificacion: body['clasificacion'] as string,
        denominacion: body['denominacion'] as string,
        consecutivo: body['consecutivo'] as string,
        nombrePozo,
        tipoUbicacion: body['tipoUbicacion'] as string,
        tipoAngulo: body['tipoAngulo'] as string,
        tipoObjetivo: body['tipoObjetivo'] as string,
        tipoTerminacion: body['tipoTerminacion'] as string,
        ubicacion: {
          departamentoId: body['departamentoId'] as number,
          departamento: dpto?.nombre ?? existing.ubicacion.departamento,
          codigoDaneDpto: dpto?.codigoDane ?? existing.ubicacion.codigoDaneDpto,
          municipioId: body['municipioId'] as number,
          municipio: mpio?.nombre ?? existing.ubicacion.municipio,
          codigoDaneMpio: mpio?.codigoDane ?? existing.ubicacion.codigoDaneMpio,
          clusterId: cluster?.id ?? null,
          cluster: cluster?.nombre ?? null,
        },
        lastModifiedAt: new Date().toISOString(),
      };

      mockWellsDetailDb[id] = updated;
      const listIdx = mockWellsDb.findIndex((w) => w.id === id);
      if (listIdx >= 0) {
        mockWellsDb[listIdx] = { ...mockWellsDb[listIdx], nombrePozo, contrato: updated.contrato, campo: updated.campo };
      }

      return new HttpResponse({ status: 200, body: updated });
    },
  },

  // DELETE /api/v1/wells/:id — eliminar
  {
    urlPattern: /\/api\/v1\/wells\/[^/]+$/,
    method: 'DELETE',
    handle: (req: HttpRequest<unknown>): HttpResponse<unknown> => {
      const id = req.url.split('/').pop() ?? '';
      const existing = mockWellsDetailDb[id];
      if (!existing) {
        return new HttpResponse({ status: 404, body: { detail: 'No se encontró el pozo.' } });
      }
      if (existing.estado !== 'BORRADOR') {
        return new HttpResponse({ status: 422, body: { detail: 'Solo se pueden eliminar pozos en estado BORRADOR.' } });
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
      const url = new URL(req.url, 'http://localhost');
      const contratoId = parseInt(url.searchParams.get('contratoId') ?? '0', 10);
      const result = MOCK_CAMPOS.filter((c) => c.contratoId === contratoId);
      return new HttpResponse({ status: 200, body: result });
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
      const url = new URL(req.url, 'http://localhost');
      const dtoId = parseInt(url.searchParams.get('departamentoId') ?? '0', 10);
      const result = MOCK_MUNICIPIOS.filter((m) => m.departamentoId === dtoId);
      return new HttpResponse({ status: 200, body: result });
    },
  },

  // GET /api/v1/catalogs/clusters?campoId=N
  {
    urlPattern: /\/api\/v1\/catalogs\/clusters/,
    method: 'GET',
    handle: (req: HttpRequest<unknown>): HttpResponse<unknown> => {
      const url = new URL(req.url, 'http://localhost');
      const campoId = parseInt(url.searchParams.get('campoId') ?? '0', 10);
      const result = MOCK_CLUSTERS.filter((c) => c.campoId === campoId);
      return new HttpResponse({ status: 200, body: result });
    },
  },

  // PATCH /api/v1/wells/:id/transition — transición de estado
  {
    urlPattern: /\/api\/v1\/wells\/[^/]+\/transition$/,
    method: 'PATCH',
    handle: (req: HttpRequest<unknown>): HttpResponse<unknown> => {
      const segments = req.url.split('/');
      const id = segments[segments.length - 2] ?? '';
      const well = mockWellsDetailDb[id];

      if (!well) {
        return new HttpResponse({
          status: 404,
          body: {
            type: 'https://tools.ietf.org/html/rfc7807',
            title: 'Resource Not Found',
            status: 404,
            detail: 'No se encontró un pozo con el ID proporcionado.',
          },
        });
      }

      const body = req.body as { action: string; comment?: string | null };
      const action = body.action;
      const comment = body.comment ?? null;

      // Tabla de transiciones válidas: estado actual → nuevo estado
      const TRANSITION_MAP: Record<string, Record<string, string>> = {
        BORRADOR:     { ENVIAR: 'PENDING_UWI' },
        PENDING_UWI:  { APROBAR_UWI: 'READY_FISCAL', DEVOLVER: 'BORRADOR' },
        READY_FISCAL: { FISCALIZAR: 'FISCALIZADO', DEVOLVER: 'BORRADOR' },
        FISCALIZADO:  {},
      };

      const nuevoEstado = TRANSITION_MAP[well.estado]?.[action];
      if (!nuevoEstado) {
        return new HttpResponse({
          status: 409,
          body: {
            type: 'https://tools.ietf.org/html/rfc7807',
            title: 'Conflict',
            status: 409,
            detail: `La acción ${action} no es válida desde el estado ${well.estado}.`,
          },
        });
      }

      const estadoAnterior = well.estado;
      // Generar UWI mock al hacer ENVIAR (primera transición)
      const uwi = action === 'ENVIAR'
        ? 'CO-50-50568-ALPHA-01-ST'
        : well.uwi;

      // Actualizar en memoria
      mockWellsDetailDb[id] = { ...well, estado: nuevoEstado, uwi };
      const listIdx = mockWellsDb.findIndex((w) => w.id === id);
      if (listIdx >= 0) {
        mockWellsDb[listIdx] = { ...mockWellsDb[listIdx], estado: nuevoEstado, uwi };
      }

      const result: TransitionResultDTO = {
        id,
        estado: nuevoEstado,
        estadoAnterior,
        uwi,
        action,
        comment,
        transitionedAt: new Date().toISOString(),
        transitionedBy: 'Usuario Mock',
      };
      return new HttpResponse({ status: 200, body: result });
    },
  },

  // GET /api/v1/wells/:id/history — historial de transiciones
  {
    urlPattern: /\/api\/v1\/wells\/[^/]+\/history$/,
    method: 'GET',
    handle: (req: HttpRequest<unknown>): HttpResponse<unknown> => {
      const segments = req.url.split('/');
      const id = segments[segments.length - 2] ?? '';
      const well = mockWellsDetailDb[id];

      if (!well) {
        return new HttpResponse({
          status: 404,
          body: {
            type: 'https://tools.ietf.org/html/rfc7807',
            title: 'Resource Not Found',
            status: 404,
            detail: 'No se encontró un pozo con el ID proporcionado.',
          },
        });
      }

      const history: TransitionHistoryItemDTO[] = [
        {
          id: '770e8400-e29b-41d4-a716-446655440001',
          fromState: 'PENDING_UWI',
          toState: 'BORRADOR',
          action: 'DEVOLVER',
          comment: 'Denominación incorrecta, verificar nomenclatura del campo.',
          performedBy: '660e8400-e29b-41d4-a716-446655440000',
          performedByName: 'María Gómez',
          performedByRole: 'SUPERVISOR',
          createdAt: '2024-11-16T10:00:00Z',
        },
        {
          id: '770e8400-e29b-41d4-a716-446655440000',
          fromState: 'BORRADOR',
          toState: 'PENDING_UWI',
          action: 'ENVIAR',
          comment: null,
          performedBy: '660e8400-e29b-41d4-a716-446655440000',
          performedByName: 'Juan Pérez',
          performedByRole: 'OPERADOR',
          createdAt: '2024-11-15T14:30:00Z',
        },
      ];

      return new HttpResponse({ status: 200, body: history });
    },
  },
];
