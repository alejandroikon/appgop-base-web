import { environment } from '@env/environment';

const { hosts } = environment;

// Los grupos y paths se agregan aquí conforme se especifiquen y desarrollen las features de cada dominio
export const API = {
  auth: {
    login:   `${hosts.gopApi}/api/v1/auth/login`,
    refresh: `${hosts.gopApi}/api/v1/auth/refresh`,
    me:      `${hosts.gopApi}/api/v1/auth/me`,
  },
  wells: {
    base:        `${hosts.gopApi}/api/v1/wells`,
    byId:        (id: string) => `${hosts.gopApi}/api/v1/wells/${id}`,
    previewName: `${hosts.gopApi}/api/v1/wells/preview-name`,
  },
  catalogs: {
    contratos:     `${hosts.gopApi}/api/v1/catalogs/contratos`,
    campos:        `${hosts.gopApi}/api/v1/catalogs/campos`,
    departamentos: `${hosts.gopApi}/api/v1/catalogs/departamentos`,
    municipios:    `${hosts.gopApi}/api/v1/catalogs/municipios`,
    clusters:      `${hosts.gopApi}/api/v1/catalogs/clusters`,
  },
} as const;
