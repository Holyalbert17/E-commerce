import { Component, OnInit, effect, inject, signal } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { BrandingService } from '../core/branding.service';
import { DxIconComponent } from '../shared/dx-icon.component';

type CmsSlug = 'faq' | 'terminos' | 'como-comprar';

@Component({
  standalone: true,
  imports: [RouterLink, DxIconComponent],
  template: `
    <section class="cms" aria-labelledby="cms-title">
      <header>
        <div>
          <h1 id="cms-title">{{ title() }}</h1>
          <p class="lead">{{ subtitle() }}</p>
        </div>
        <a routerLink="/catalogo"><dx-icon name="grid" [size]="14" /> Catálogo</a>
      </header>

      <article class="panel">
        @if (html(); as content) {
          <div class="body" [innerHTML]="content"></div>
        } @else {
          <p class="empty">
            Esta sección aún no tiene contenido publicado. Volvé más tarde o escribinos desde
            <a routerLink="/contacto">Contacto</a>.
          </p>
        }
      </article>
    </section>
  `,
  styles: [
    `
      header {
        display: flex;
        justify-content: space-between;
        align-items: baseline;
        gap: 1rem;
        flex-wrap: wrap;
        margin-bottom: 1.25rem;
      }
      h1 {
        margin: 0;
        font-family: var(--dx-display);
        font-size: clamp(1.5rem, 3vw, 2rem);
      }
      .lead {
        margin: 0.35rem 0 0;
        color: var(--dx-mute);
      }
      a {
        color: var(--dx-red);
        font-weight: 700;
        text-decoration: none;
      }
      .panel {
        background: #fff;
        border: 1px solid var(--dx-line);
        border-radius: 14px;
        padding: 1.35rem 1.5rem;
        box-shadow: var(--dx-shadow);
      }
      .body {
        line-height: 1.55;
        color: var(--dx-ink-soft);
      }
      .body :is(h2, h3) {
        color: var(--dx-ink);
        font-family: var(--dx-display);
        margin: 1.1rem 0 0.45rem;
      }
      .body p {
        margin: 0 0 0.75rem;
      }
      .body ul {
        margin: 0 0 0.85rem;
        padding-left: 1.2rem;
      }
      .empty {
        margin: 0;
        color: var(--dx-mute);
      }
    `,
  ],
})
export class CmsPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly brand = inject(BrandingService);
  private readonly sanitizer = inject(DomSanitizer);

  private slug: CmsSlug = 'faq';
  readonly title = signal('Información');
  readonly subtitle = signal('');
  readonly html = signal<SafeHtml | null>(null);

  constructor() {
    effect(() => {
      this.brand.branding();
      this.apply();
    });
  }

  ngOnInit(): void {
    this.slug = (this.route.snapshot.data['slug'] || 'faq') as CmsSlug;
    this.apply();
  }

  private apply(): void {
    const b = this.brand.branding();
    const map: Record<CmsSlug, { title: string; subtitle: string; raw: string | null | undefined }> = {
      faq: {
        title: 'Preguntas frecuentes',
        subtitle: 'Respuestas rápidas sobre pedidos, envíos y pagos.',
        raw: b?.contenidoFaqHtml,
      },
      terminos: {
        title: 'Términos y condiciones',
        subtitle: 'Condiciones de uso de la tienda online.',
        raw: b?.contenidoTerminosHtml,
      },
      'como-comprar': {
        title: 'Cómo comprar',
        subtitle: 'Guía paso a paso para completar tu pedido.',
        raw: b?.contenidoComoComprarHtml,
      },
    };
    const entry = map[this.slug] ?? map.faq;
    this.title.set(entry.title);
    this.subtitle.set(entry.subtitle);
    const raw = (entry.raw || '').trim() || this.defaultHtml(this.slug);
    this.html.set(this.sanitizer.bypassSecurityTrustHtml(raw));
  }

  /** Contenido útil si el admin aún no publicó HTML en branding. */
  private defaultHtml(slug: CmsSlug): string {
    switch (slug) {
      case 'faq':
        return `
          <h2>¿Cómo hago un pedido?</h2>
          <p>Elegí productos en el catálogo, agregalos al carrito y completá el checkout con tu dirección o sucursal de retiro.</p>
          <h2>¿Puedo pagar al recibir?</h2>
          <p>Sí, en la mayoría de los envíos y retiros podés elegir pagar en la entrega. Otras formas de pago aparecen según la configuración de la tienda.</p>
          <h2>¿Cómo veo el estado de mi pedido?</h2>
          <p>Iniciá sesión y entrá a <strong>Mis pedidos</strong>. Ahí ves el número, el estado y el detalle de lo comprado.</p>
          <h2>¿Qué pasa si un producto no tiene stock?</h2>
          <p>En el catálogo se indica “Sin stock”. Podés seguirlo desde Favoritos o Seguidos para enterarte cuando vuelva a estar disponible.</p>
          <h2>¿Necesito ayuda?</h2>
          <p>Escribinos desde <a href="/contacto">Contacto</a> o usá el chat de soporte si está activo.</p>
        `;
      case 'terminos':
        return `
          <h2>Uso de la tienda</h2>
          <p>Al usar esta tienda online aceptás estas condiciones. Los precios, stock y promociones pueden cambiar sin previo aviso hasta confirmar el pedido.</p>
          <h2>Pedidos y facturación</h2>
          <p>El pedido se confirma al finalizar el checkout. Los datos de facturación (RUC/CI y razón social) deben ser correctos; es tu responsabilidad verificarlos antes de confirmar.</p>
          <h2>Envíos y retiros</h2>
          <p>Los plazos de entrega son estimados y dependen de la zona, el stock y la logística. En retiro en sucursal, el pedido queda disponible según el horario indicado en el checkout.</p>
          <h2>Cupones y descuentos</h2>
          <p>Los cupones tienen vigencia, condiciones de uso y pueden no combinarse con otras promociones. La tienda puede anular un cupón inválido o vencido.</p>
          <h2>Privacidad</h2>
          <p>Usamos tus datos para procesar pedidos, contacto comercial y mejoras del servicio, conforme a la normativa aplicable.</p>
          <p>Para consultas legales o comerciales: <a href="/contacto">Contacto</a>.</p>
        `;
      case 'como-comprar':
        return `
          <h2>1. Explorá el catálogo</h2>
          <p>Usá la búsqueda o las categorías. Podés ordenar, filtrar y ver productos en grilla o lista.</p>
          <h2>2. Agregá al carrito</h2>
          <p>Desde el listado o la ficha del producto elegí la cantidad y sumá al carrito. El mini-carrito te muestra el resumen al instante.</p>
          <h2>3. Revisá el carrito</h2>
          <p>Verificá cantidades, precios y stock. Si tenés un cupón, podés aplicarlo en el checkout.</p>
          <h2>4. Completá el checkout</h2>
          <ul>
            <li>Datos de contacto y facturación</li>
            <li>Envío a domicilio o retiro en sucursal</li>
            <li>Forma de pago disponible</li>
          </ul>
          <h2>5. Confirmá y seguí tu pedido</h2>
          <p>Al confirmar recibís el número de pedido. En <strong>Mis pedidos</strong> podés ver el avance. ¿Dudas? Andá a <a href="/faq">Preguntas frecuentes</a> o <a href="/contacto">Contacto</a>.</p>
        `;
    }
  }
}
