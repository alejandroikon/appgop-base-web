// T-INFRA-26: Staging environment — FE apuntando al BE en Azure App Service
// RN-INFRA-08: API URL leída de env var NG_APP_API_URL (set en Netlify por contexto)

import { AppEnvironment } from './environment.interface';

export const environment: AppEnvironment = {
  name: 'staging',
  production: false,
  hosts: {
    // NG_APP_API_URL se inyecta en build time por Netlify vía environment variables
    // Valor por defecto para builds locales apuntando al staging de Azure
    gopApi: (window as Window & { NG_APP_API_URL?: string }).NG_APP_API_URL
      ?? 'https://app-gop360-staging-api.azurewebsites.net',
  },
  featureFlags: {
    enableBetaFeatures: true,
    useMocks: false,  // Staging usa BE real — no mocks
  },
  loginBgUrl: 'assets/images/login-bg.jpg',
};
