import { environment } from '@env/environment';

const { hosts } = environment;

// Los grupos y paths se agregan aquí conforme se especifiquen y desarrollen las features de cada dominio
export const API = {
  auth: {
    login:   `${hosts.gopApi}/api/v1/auth/login`,
    refresh: `${hosts.gopApi}/api/v1/auth/refresh`,
    logout:  `${hosts.gopApi}/api/v1/auth/logout`,
  },
} as const;