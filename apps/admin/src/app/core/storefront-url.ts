import { environment } from '../../environments/environment';

/** URL pública del storefront para el tenant actual (refleja cambios de admin). */
export function storefrontUrl(slug: string, path = '/catalogo'): string {
  const base = (environment.storefrontBaseUrl || 'http://localhost:4200').replace(/\/$/, '');
  const cleanPath = path.startsWith('/') ? path : `/${path}`;
  const q = new URLSearchParams({
    tenant: slug || environment.defaultTenantSlug || 'casa-imperial',
  });
  return `${base}${cleanPath}?${q.toString()}`;
}

export function openStorefront(slug: string, path = '/catalogo'): void {
  window.open(storefrontUrl(slug, path), '_blank', 'noopener');
}
