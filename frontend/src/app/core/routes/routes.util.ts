import { RoutesEnum } from './routes.enum';

export function routePath(...segments: RoutesEnum[]): string {
  return `/${segments.join('/')}`;
}

// Each page has a title of its own, which the browser and screen readers announce (WCAG 2.4.2,
// TechSpec §11.3).
export function pageTitle(page: string): string {
  return `${page} | Cartão de Vacinação`;
}
