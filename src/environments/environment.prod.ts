import { AppEnvironment } from './environment.interface';

export const environment: AppEnvironment = {
  name: 'production',
  production: true,
  hosts: {
    gopApi: 'https://api.gop.internal',
  },
  featureFlags: {
    enableBetaFeatures: false,
  },
};