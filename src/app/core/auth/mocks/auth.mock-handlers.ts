import { HttpRequest, HttpResponse } from '@angular/common/http';
import { MockHandler } from '@core/http/mock.interceptor';
import { TokenResponseDTO, UserProfileDTO } from '@shared/models';
import { MOCK_USERS } from '../auth.mock';

const MOCK_ACCESS_TOKEN_PREFIX = 'mock-access-token';
const MOCK_REFRESH_TOKEN_PREFIX = 'mock-refresh-token';

function buildMockTokenResponse(userId: string): TokenResponseDTO {
  const user = MOCK_USERS.find((u) => u.id === userId);
  if (!user) throw new Error('Usuario mock no encontrado');
  const { password: _, ...profile } = user;
  return {
    accessToken: `${MOCK_ACCESS_TOKEN_PREFIX}-${userId}`,
    refreshToken: `${MOCK_REFRESH_TOKEN_PREFIX}-${userId}`,
    expiresIn: 1800,
    user: profile,
  };
}

export const authMockHandlers: MockHandler[] = [
  {
    urlPattern: /\/api\/v1\/auth\/login$/,
    method: 'POST',
    handle: (req: HttpRequest<unknown>): HttpResponse<unknown> | null => {
      const body = req.body as { email?: string; password?: string } | null;
      if (!body?.email || !body?.password) {
        return new HttpResponse({ status: 422, body: { detail: 'Campos requeridos.' } });
      }
      const normalizedEmail = body.email.trim().toLowerCase();
      const user = MOCK_USERS.find(
        (u) => u.email === normalizedEmail && u.password === body.password
      );
      if (!user) {
        return new HttpResponse({
          status: 401,
          body: { detail: 'Correo o contraseña incorrectos.' },
        });
      }
      return new HttpResponse<TokenResponseDTO>({
        status: 200,
        body: buildMockTokenResponse(user.id),
      });
    },
  },
  {
    urlPattern: /\/api\/v1\/auth\/refresh$/,
    method: 'POST',
    handle: (req: HttpRequest<unknown>): HttpResponse<unknown> | null => {
      const body = req.body as { refreshToken?: string } | null;
      if (!body?.refreshToken) {
        return new HttpResponse({ status: 422, body: { detail: 'Token requerido.' } });
      }
      const userId = body.refreshToken.replace(`${MOCK_REFRESH_TOKEN_PREFIX}-`, '');
      const user = MOCK_USERS.find((u) => u.id === userId);
      if (!user) {
        return new HttpResponse({
          status: 401,
          body: { detail: 'Token de actualización inválido.' },
        });
      }
      return new HttpResponse<TokenResponseDTO>({
        status: 200,
        body: buildMockTokenResponse(user.id),
      });
    },
  },
  {
    urlPattern: /\/api\/v1\/auth\/me$/,
    method: 'GET',
    handle: (req: HttpRequest<unknown>): HttpResponse<unknown> | null => {
      const authHeader = req.headers.get('Authorization') ?? '';
      const token = authHeader.replace('Bearer ', '');
      const userId = token.replace(`${MOCK_ACCESS_TOKEN_PREFIX}-`, '');
      const user = MOCK_USERS.find((u) => u.id === userId);
      if (!user) {
        return new HttpResponse({
          status: 401,
          body: { detail: 'Token de autenticación requerido.' },
        });
      }
      const { password: _, ...profile } = user;
      return new HttpResponse<UserProfileDTO>({ status: 200, body: profile });
    },
  },
];
