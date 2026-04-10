export const LOGIN_LOCALE = {
  title: 'Bienvenido de nuevo',
  subtitle: 'Ingresa a tu cuenta para continuar',
  fields: {
    email: 'CORREO ELECTRÓNICO',
    password: 'CONTRASEÑA',
    emailPlaceholder: 'usuario@dominio.com',
  },
  actions: {
    submit: 'Iniciar Sesión',
    submitting: 'Ingresando...',
    forgotPassword: '¿Olvidó su contraseña?',
    showPassword: 'Mostrar contraseña',
    hidePassword: 'Ocultar contraseña',
  },
  errors: {
    required: 'Este campo es requerido',
    emailInvalid: 'Ingrese un correo electrónico válido',
    credentials: 'Correo o contraseña incorrectos. Verifique sus datos.',
    noConnection: 'Sin conexión. Verifica tu red e intenta de nuevo.',
  },
  layout: {
    brandTitle: 'Bienvenido a GOP 360°',
    brandSubtitle: 'Accede a tu cuenta para gestionar todos los recursos de manera eficiente y segura.',
    version: 'GOP 360° v1.0.0',
  },
} as const;
