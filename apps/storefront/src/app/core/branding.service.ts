import { Injectable, inject, signal } from '@angular/core';
import { ApiService, StorefrontBranding } from './api.service';

@Injectable({ providedIn: 'root' })
export class BrandingService {
  private readonly api = inject(ApiService);
  readonly branding = signal<StorefrontBranding | null>(null);
  readonly loaded = signal(false);

  load(): void {
    this.api.getBranding().subscribe({
      next: (b) => {
        this.branding.set(b);
        this.apply(b);
        this.loaded.set(true);
      },
      error: () => {
        this.loaded.set(true);
      },
    });
  }

  media(url: string | null | undefined): string | null {
    return this.api.resolveMediaUrl(url);
  }

  private apply(b: StorefrontBranding): void {
    const root = document.documentElement;
    if (b.colorPrimario) {
      root.style.setProperty('--dx-red', b.colorPrimario);
      root.style.setProperty('--dx-price', b.colorPrimario);
      // Hover/contraste: oscurecer el primario (nunca usar el secundario — suele ser claro).
      const dark = this.shadeHex(b.colorPrimario, -0.28);
      if (dark) root.style.setProperty('--dx-red-dark', dark);
      const soft = this.hexToRgba(b.colorPrimario, 0.12);
      if (soft) root.style.setProperty('--dx-red-soft', soft);
      const body = document.body;
      if (body) {
        body.style.backgroundImage = `radial-gradient(ellipse 90% 45% at 50% -15%, ${this.hexToRgba(b.colorPrimario, 0.07) || 'transparent'}, transparent), linear-gradient(180deg, #fafafa 0%, var(--dx-bg) 35%)`;
      }
    }
    if (b.colorSecundario) {
      root.style.setProperty('--dx-secondary', b.colorSecundario);
    }
    if (b.colorAcento) {
      root.style.setProperty('--dx-focus', b.colorAcento);
    }

    document.title = `${b.nombreTienda} · Tienda`;

    const themeMeta = document.querySelector('meta[name="theme-color"]');
    if (themeMeta && b.colorPrimario) {
      themeMeta.setAttribute('content', b.colorPrimario);
    }

    if (b.faviconUrl) {
      const href = this.api.resolveMediaUrl(b.faviconUrl);
      if (href) {
        let link = document.querySelector("link[rel='icon']") as HTMLLinkElement | null;
        if (!link) {
          link = document.createElement('link');
          link.rel = 'icon';
          document.head.appendChild(link);
        }
        link.href = href;
      }
    }
  }

  /** amount negativo oscurece (−0.28 ≈ hover de botón). */
  private shadeHex(hex: string, amount: number): string | null {
    const rgb = this.parseHex(hex);
    if (!rgb) return null;
    const mix = (c: number) =>
      Math.round(Math.min(255, Math.max(0, c + (amount < 0 ? c * amount : (255 - c) * amount))));
    const r = mix(rgb.r).toString(16).padStart(2, '0');
    const g = mix(rgb.g).toString(16).padStart(2, '0');
    const b = mix(rgb.b).toString(16).padStart(2, '0');
    return `#${r}${g}${b}`;
  }

  private parseHex(hex: string): { r: number; g: number; b: number } | null {
    const h = hex.replace('#', '').trim();
    if (!(h.length === 6 || h.length === 3)) return null;
    const full = h.length === 3 ? h.split('').map((c) => c + c).join('') : h;
    const n = parseInt(full, 16);
    if (Number.isNaN(n)) return null;
    return { r: (n >> 16) & 255, g: (n >> 8) & 255, b: n & 255 };
  }

  private hexToRgba(hex: string, alpha: number): string | null {
    const rgb = this.parseHex(hex);
    if (!rgb) return null;
    return `rgba(${rgb.r}, ${rgb.g}, ${rgb.b}, ${alpha})`;
  }
}
