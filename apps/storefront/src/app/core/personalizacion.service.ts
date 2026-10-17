import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { ApiService } from './api.service';

export interface HomeSectionConfig {
  id: string;
  visible: boolean;
  orden: number;
}

export interface HomeLayoutConfig {
  sections: HomeSectionConfig[];
  mostrarBannerMayorista?: boolean;
  /** Barra superior CMS (top_info_bar). */
  mostrarTopBar?: boolean;
  /** Franja de beneficios bajo el hero. */
  mostrarTrust?: boolean;
  /** Cupón en sección ofertas. */
  mostrarCupon?: boolean;
  /** Rail de productos de vitrinas TipoGrupo=3. */
  mostrarProductosOferta?: boolean;
  /** Panel destacado en el hero (promo/publicidad). */
  mostrarHeroFeature?: boolean;
}

export interface TemaConfig {
  nombre: string;
  primary?: string;
  accent?: string;
  ink?: string;
  paper?: string;
  displayFont?: string | null;
  bannerUrl?: string | null;
}

export interface CheckoutTextosConfig {
  titulo: string;
  subtitulo?: string;
  ctaConfirmar?: string;
  hintMapa?: string;
  msgExito?: string;
}

export interface CarritoTextosConfig {
  titulo: string;
  seguirComprando?: string;
  vacioTitulo?: string;
  vacioSub?: string;
  ctaCatalogo?: string;
  hintEnvio?: string;
  ctaVaciar?: string;
  ctaGuardarLista?: string;
  ctaCheckout?: string;
  ctaStockBlock?: string;
}

export interface WidgetsConfig {
  chat: boolean;
  resenas: boolean;
  favoritos: boolean;
  seguidos: boolean;
  bannerMayorista: boolean;
  facebook: boolean;
  instagram: boolean;
  whatsapp: boolean;
  youtube: boolean;
  twitter: boolean;
  tiktok: boolean;
}

/** Catálogo completo de secciones del home (orden por defecto). */
export const DEFAULT_HOME_SECTIONS: HomeSectionConfig[] = [
  { id: 'hero', visible: true, orden: 1 },
  { id: 'categorias', visible: true, orden: 2 },
  { id: 'publicidad', visible: true, orden: 3 },
  { id: 'flyers', visible: true, orden: 4 },
  { id: 'recomendados', visible: true, orden: 5 },
  { id: 'masVendidos', visible: true, orden: 6 },
  { id: 'newsletter', visible: true, orden: 7 },
  { id: 'catalogoCta', visible: true, orden: 8 },
];

/** Une defaults con lo guardado en admin (agrega secciones nuevas sin perder flags). */
export function mergeHomeSections(stored: HomeSectionConfig[] | null | undefined): HomeSectionConfig[] {
  const list = Array.isArray(stored) ? stored : [];
  const byId = new Map(list.map((s) => [s.id, s]));
  const out: HomeSectionConfig[] = [];

  for (const def of DEFAULT_HOME_SECTIONS) {
    const s = byId.get(def.id);
    if (s) {
      out.push({
        id: def.id,
        visible: s.visible !== false,
        orden: typeof s.orden === 'number' ? s.orden : def.orden,
      });
      byId.delete(def.id);
    } else {
      out.push({ ...def });
    }
  }

  for (const s of byId.values()) {
    out.push({
      id: s.id,
      visible: s.visible !== false,
      orden: typeof s.orden === 'number' ? s.orden : 99,
    });
  }

  return out;
}

@Injectable({ providedIn: 'root' })
export class PersonalizacionService {
  private readonly api = inject(ApiService);
  private readonly claves = signal<Record<string, string>>({});
  /** Solo para el iframe de edición: nunca persiste ni modifica la tienda pública. */
  private readonly previewClaves = signal<Record<string, string>>({});
  readonly loaded = signal(false);

  constructor() {
    effect(() => this.applyTema(this.tema()));
  }

  readonly layout = computed<HomeLayoutConfig>(() => {
    const parsed = this.parse('home.layout', {
      sections: DEFAULT_HOME_SECTIONS,
      mostrarBannerMayorista: true,
      mostrarTopBar: true,
      mostrarTrust: true,
      // Opt-in: la franja BIENVENIDO10 solo aparece si marketing la activa.
      mostrarCupon: false,
      mostrarProductosOferta: true,
      mostrarHeroFeature: true,
    } as HomeLayoutConfig);
    return {
      sections: mergeHomeSections(parsed.sections),
      mostrarBannerMayorista: parsed.mostrarBannerMayorista !== false,
      mostrarTopBar: parsed.mostrarTopBar !== false,
      mostrarTrust: parsed.mostrarTrust !== false,
      mostrarCupon: parsed.mostrarCupon === true,
      mostrarProductosOferta: parsed.mostrarProductosOferta !== false,
      mostrarHeroFeature: parsed.mostrarHeroFeature !== false,
    };
  });

  /** Secciones visibles del home, ordenadas por `orden` (admin). */
  readonly orderedSections = computed(() =>
    [...this.layout().sections]
      .filter((s) => s.visible !== false)
      .sort((a, b) => a.orden - b.orden || a.id.localeCompare(b.id)),
  );

  readonly tema = computed<TemaConfig>(() =>
    this.parse('tema.activo', {
      nombre: 'default',
      primary: '#c62828',
      accent: '#b45309',
    }),
  );

  readonly temaBannerUrl = computed(() => this.tema().bannerUrl?.trim() || null);

  readonly checkoutTextos = computed<CheckoutTextosConfig>(() =>
    this.parse('checkout.textos', {
      titulo: 'Finalizar compra',
      subtitulo: 'Completá tus datos y la forma de entrega',
      ctaConfirmar: 'Confirmar pedido',
      hintMapa: 'Tocá el mapa para marcar la ubicación.',
      msgExito: '¡Gracias por tu compra!',
    }),
  );

  readonly carritoTextos = computed<CarritoTextosConfig>(() =>
    this.parse('carrito.textos', {
      titulo: 'Carrito',
      seguirComprando: 'Seguir comprando',
      vacioTitulo: 'Tu carrito está vacío.',
      vacioSub: 'Agregá productos desde el catálogo para empezar.',
      ctaCatalogo: 'Ir al catálogo',
      hintEnvio: 'El costo de envío se calcula en el checkout.',
      ctaVaciar: 'Vaciar',
      ctaGuardarLista: 'Guardar como lista',
      ctaCheckout: 'Continuar al checkout',
      ctaStockBlock: 'Revisá stock para continuar',
    }),
  );

  readonly widgets = computed<WidgetsConfig>(() => {
    const o = this.parseRaw('widgets.toggles') as Record<string, unknown> | null;
    return {
      chat: o?.['chat'] !== false,
      resenas: o?.['resenas'] !== false && o?.['reseñas'] !== false,
      favoritos: o?.['favoritos'] !== false,
      seguidos: o?.['seguidos'] !== false,
      bannerMayorista: o?.['bannerMayorista'] !== false,
      facebook: o?.['facebook'] !== false,
      instagram: o?.['instagram'] !== false,
      whatsapp: o?.['whatsapp'] !== false,
      youtube: o?.['youtube'] !== false,
      twitter: o?.['twitter'] !== false,
      tiktok: o?.['tiktok'] !== false,
    };
  });

  load(): void {
    this.api.getPersonalizacionPublica().subscribe({
      next: (dto) => {
        this.claves.set(dto.claves || dto.Claves || {});
        this.loaded.set(true);
      },
      error: () => this.loaded.set(true),
    });
  }

  /** Recarga al volver a la pestaña (cambios del admin sin redeploy). */
  watchFocusReload(): void {
    if (typeof window === 'undefined') return;
    window.addEventListener('focus', () => this.load());
  }

  setPreview(clave: string, valorJson: string | null): void {
    const next = { ...this.previewClaves() };
    if (valorJson === null) delete next[clave];
    else next[clave] = valorJson;
    this.previewClaves.set(next);
  }

  sectionVisible(id: string): boolean {
    return this.orderedSections().some((s) => s.id === id);
  }

  showBannerMayorista(): boolean {
    return this.layout().mostrarBannerMayorista !== false && this.widgets().bannerMayorista;
  }

  showTopBar(): boolean {
    return this.layout().mostrarTopBar !== false;
  }

  showTrust(): boolean {
    return this.layout().mostrarTrust !== false;
  }

  showCupon(): boolean {
    // Franja CMS coupon_strip: solo si la sección Promociones está on y el toggle personalización.
    return this.sectionVisible('flyers') && this.layout().mostrarCupon === true;
  }

  showProductosOferta(): boolean {
    return this.sectionVisible('flyers') && this.layout().mostrarProductosOferta !== false;
  }

  showHeroFeature(): boolean {
    return this.sectionVisible('hero') && this.layout().mostrarHeroFeature !== false;
  }

  private parseRaw(clave: string): unknown | null {
    const raw = this.previewClaves()[clave] ?? this.claves()[clave];
    if (!raw) return null;
    try {
      return JSON.parse(raw);
    } catch {
      return null;
    }
  }

  private parse<T>(clave: string, fallback: T): T {
    const parsed = this.parseRaw(clave);
    if (!parsed || typeof parsed !== 'object') return fallback;
    return { ...fallback, ...(parsed as object) } as T;
  }

  private applyTema(t: TemaConfig): void {
    const root = document.documentElement;
    if (t.primary) {
      root.style.setProperty('--dx-red', t.primary);
      root.style.setProperty('--dx-price', t.primary);
      // Mantener hover legible aunque el preview cambie el primario.
      root.style.setProperty('--dx-red-dark', `color-mix(in srgb, ${t.primary} 72%, #000)`);
      root.style.setProperty('--dx-red-soft', `color-mix(in srgb, ${t.primary} 14%, #fff)`);
    }
    if (t.accent) root.style.setProperty('--dx-focus', t.accent);
    if (t.ink) root.style.setProperty('--dx-ink', t.ink);
    if (t.paper) root.style.setProperty('--dx-bg', t.paper);
    if (t.displayFont) root.style.setProperty('--dx-display', t.displayFont);
    if (t.bannerUrl?.trim()) {
      const banner = this.api.resolveMediaUrl(t.bannerUrl.trim()) || t.bannerUrl.trim();
      root.style.setProperty('--dx-tema-banner', `url("${banner.replace(/"/g, '\\"')}")`);
    } else {
      root.style.removeProperty('--dx-tema-banner');
    }
  }
}
