// REFERENCIA: las claves y textos se ajustan según el diseño final del layout y los flujos definidos
export const APP_LOCALE = {
  nav: {
    wells:      'Pozos',
    operations: 'Operaciones',
    production: 'Producción',
    admin:      'Administración',
  },
  actions: {
    save:    'Guardar',
    cancel:  'Cancelar',
    confirm: 'Confirmar',
    delete:  'Eliminar',
    back:    'Volver',
  },
  errors: {
    generic:    'Ocurrió un error inesperado.',
    noConnection: 'Sin conexión. Verifica tu red.',
    forbidden:  'No tienes permisos para realizar esta acción.',
  },
} as const;