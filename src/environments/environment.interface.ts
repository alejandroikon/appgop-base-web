export interface AppEnvironment {
  name: 'development' | 'staging' | 'qa' | 'production';
  production: boolean;
  hosts: {
    gopApi: string; // API principal del sistema GOP
    // Agregar aquí cada nuevo host externo al integrarse (ej: catalogApi, anhApi, gisApi)
  };
  featureFlags: {
    enableBetaFeatures: boolean;
    useMocks: boolean;
  };
  loginBgUrl: string;
}