import { HttpInterceptorFn, HttpRequest, HttpResponse } from '@angular/common/http';
import { of } from 'rxjs';
import { environment } from '@env/environment';
import { MOCK_HANDLERS } from './mock.registry';

export interface MockHandler {
  urlPattern: string | RegExp;
  method: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';
  handle: (req: HttpRequest<unknown>) => HttpResponse<unknown> | null;
}

export const mockInterceptor: HttpInterceptorFn = (req, next) => {
  if (!environment.featureFlags.useMocks) return next(req);

  const handler = MOCK_HANDLERS.find((h) => {
    const urlMatch =
      typeof h.urlPattern === 'string'
        ? req.url.includes(h.urlPattern)
        : h.urlPattern.test(req.url);
    return urlMatch && h.method === req.method;
  });

  if (handler) {
    const response = handler.handle(req);
    if (response) return of(response);
  }

  return next(req);
};
