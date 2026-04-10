export const FORGOT_PASSWORD_LOCALE = {
  title: 'Recuperar contraseña',
  subtitle: 'Ingresa tu correo registrado y te enviaremos las instrucciones.',
  fields: {
    email: 'CORREO ELECTRÓNICO',
    emailPlaceholder: 'usuario@dominio.com',
  },
  actions: {
    submit: 'Enviar enlace de recuperación',
    submitting: 'Enviando...',
    backToLogin: 'Volver al inicio de sesión',
  },
  errors: {
    required: 'Este campo es requerido',
    emailInvalid: 'Ingrese un correo electrónico válido',
  },
  confirmation: {
    message: 'Si el correo está registrado, recibirás un enlace de recuperación en los próximos minutos.',
    action: 'Volver al inicio de sesión',
  },
} as const;
