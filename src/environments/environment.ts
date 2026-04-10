import { AppEnvironment } from './environment.interface';

export const environment: AppEnvironment = {
  name: 'development',
  production: false,
  hosts: {
    gopApi: 'http://localhost:3000',
  },
  featureFlags: {
    enableBetaFeatures: true,
  },
  loginBgUrl: 'assets/images/login-bg.jpg',
};