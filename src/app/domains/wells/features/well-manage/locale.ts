export const WELL_MANAGE_LOCALE = {
  title: 'Gestión de Pozos',
  actions: {
    create: 'Nuevo Pozo',
    edit:   'Editar',
    delete: 'Eliminar',
    view:   'Ver Detalle',
  },
  columns: {
    nombrePozo:   'Nombre del Pozo',
    operadora:    'Operadora',
    contrato:     'Contrato',
    campo:        'Campo',
    clasificacion:'Clasificación',
    estado:       'Estado',
    createdAt:    'Fecha Creación',
    acciones:     'Acciones',
  },
  filters: {
    search:       'Buscar por nombre...',
    contrato:     'Filtrar por contrato',
    allContratos: 'Todos los contratos',
  },
  messages: {
    deleteConfirm:  '¿Está seguro de eliminar este pozo? Esta acción no se puede deshacer.',
    deleteSuccess:  'Pozo eliminado exitosamente.',
    empty:          'No se encontraron pozos.',
    loading:        'Cargando pozos...',
    pageReport:     'Mostrando {first} a {last} de {totalRecords} pozos',
  },
} as const;
