import { MockHandler } from './mock.interceptor';
import { authMockHandlers } from '@core/auth/mocks/auth.mock-handlers';

// Los handlers se registran conforme se desarrollan las features que los requieren
export const MOCK_HANDLERS: MockHandler[] = [
  ...authMockHandlers,
];
