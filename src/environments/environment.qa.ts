import { AppEnvironment } from './environment.interface';

export const environment: AppEnvironment = {
  name: 'qa',
  production: false,
  hosts: {
    gopApi: 'https://api-qa.gop.internal',
  },
  featureFlags: {
    enableBetaFeatures: true,
    useMocks: false,
  },
  loginBgUrl: 'assets/images/login-bg.jpg',
};