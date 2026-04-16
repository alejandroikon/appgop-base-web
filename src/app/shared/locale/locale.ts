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
  auth: {
    sessionExpiredTitle:   'Sesión expirada',
    sessionExpiredMessage: 'Tu sesión ha expirado. Por favor, inicia sesión nuevamente.',
    logoutMessage:         'Has cerrado sesión correctamente.',
  },
  brand: {
    title:    'GOP 360°',
    welcome:  'Bienvenido a GOP 360°',
    subtitle: 'Accede a tu cuenta para gestionar todos los recursos de manera eficiente y segura.',
    version:  'GOP 360° v1.0.0',
  },
  home: {
    title:    'GOP 360°',
    subtitle: 'Plataforma en construcción',
  },
  sidebar: {
    dashboard:      'Panel de Control',
    wells:          'Gestión de Pozos',
    admin:          'Seguridad & Administración',
    adminUsers:     'Gestión de Usuarios',
    adminAuditLogs: 'Registros de Auditoría',
  },
  topHeader: {
    welcome:      'Bienvenido',
    logout:       'Cerrar Sesión',
    expandMenu:   'Expandir menú',
    collapseMenu: 'Colapsar menú',
    openMenu:     'Abrir menú de navegación',
  },
  placeholders: {
    title:    'Módulo en construcción',
    subtitle: 'Esta funcionalidad estará disponible próximamente.',
  },
} as const;