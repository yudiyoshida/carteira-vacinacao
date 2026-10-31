import { RoutesEnum } from './routes.enum';

export function routePath(...segments: RoutesEnum[]): string {
  return `/${segments.join('/')}`;
}
