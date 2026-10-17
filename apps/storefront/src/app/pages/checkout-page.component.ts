import {
  AfterViewInit,
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  ViewChild,
  effect,
  inject,
  signal,
} from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  Agencia,
  ApiService,
  BarrioDelivery,
  Cart,
  CheckoutRequest,
  CiudadDelivery,
  HorarioAtencion,
  isLoggedIn,
} from '../core/api.service';
import { BrandingService } from '../core/branding.service';
import { SessionService } from '../core/session.service';
import { PersonalizacionService } from '../core/personalizacion.service';
import { ToastService } from '../core/toast.service';
import {
  addOsmTiles,
  hasLeaflet,
  isValidLatLng,
  mapsDirectionsUrl,
  mapsPinUrl,
  pinIcon,
} from '../core/map.util';
import { DxIconComponent } from '../shared/dx-icon.component';
import { environment } from '../../environments/environment';
declare const L: any;

const CHECKOUT_DRAFT_KEY = 'ecom_checkout_draft_v1';
const PENDING_COUPON_KEY = 'ecom_pending_coupon';

type CheckoutDraft = {
  delivery?: boolean;
  pagarEnLaEntrega?: boolean;
  razonSocial?: string;
  numeroDocumento?: string;
  nombre?: string;
  apellido?: string;
  email?: string;
  nroTelefono?: string;
  lugarEntrega?: string;
  observacion?: string;
  tipoComprobante?: 'factura' | 'ticket' | null;
  selectedCiudadId?: number | null;
  selectedBarrioId?: number | null;
  selectedAgenciaId?: number | null;
};

@Component({
  standalone: true,
  imports: [RouterLink, CurrencyPipe, FormsModule, DxIconComponent],
  template: `
    <section class="checkout">
      <header>
        <div>
          <p class="kicker">Paso final</p>
          <h1>{{ pers.checkoutTextos().titulo || 'Checkout' }}</h1>
          @if (pers.checkoutTextos().subtitulo; as sub) {
            <p class="sub">{{ sub }}</p>
          }
        </div>
        <a routerLink="/carrito">← Volver al carrito</a>
      </header>

      <ol class="steps" aria-label="Progreso">
        <li>
          <button
            type="button"
            class="step-btn"
            [class.on]="step() === 1"
            [class.done]="step() > 1"
            (click)="goStep(1)"
          >
            1. Datos
          </button>
        </li>
        <li>
          <button
            type="button"
            class="step-btn"
            [class.on]="step() === 2"
            [class.done]="step() > 2"
            (click)="goStep(2)"
          >
            2. Entrega y pago
          </button>
        </li>
        <li>
          <button
            type="button"
            class="step-btn"
            [class.on]="step() === 3"
            (click)="goStep(3)"
          >
            3. Confirmación
          </button>
        </li>
      </ol>

      @if (error()) {
        <p class="err" role="alert">{{ error() }}</p>
      }

      @if (cart(); as c) {
        @if (c.items.length === 0) {
          <div class="empty-box">
            <dx-icon name="cart" [size]="36" />
            <p>No hay ítems en el carrito.</p>
            <a class="dx-btn" routerLink="/catalogo">Ir al catálogo</a>
          </div>
        } @else {
          <div class="grid">
            <form class="form" (ngSubmit)="onFormSubmit()">
              @if (step() === 1) {
                <h2>Datos del cliente</h2>
                <label>
                  Razón social / Nombre
                  <input class="dx-input" [(ngModel)]="form.razonSocial" name="razonSocial" required />
                </label>
                <label>
                  Documento
                  <input
                    class="dx-input"
                    [(ngModel)]="form.numeroDocumento"
                    name="numeroDocumento"
                    required
                    (blur)="validateDocumento()"
                  />
                  @if (docWarning()) {
                    <small class="doc-warn" role="status">{{ docWarning() }}</small>
                  }
                </label>
                <div class="row">
                  <label>Nombre <input class="dx-input" [(ngModel)]="form.nombre" name="nombre" required /></label>
                  <label>Apellido <input class="dx-input" [(ngModel)]="form.apellido" name="apellido" required /></label>
                </div>
                <div class="row">
                  <label>Email <input class="dx-input" type="email" [(ngModel)]="form.email" name="email" required /></label>
                  <label>Teléfono <input class="dx-input" [(ngModel)]="form.nroTelefono" name="nroTelefono" required /></label>
                </div>
                <div class="nav-row">
                  <button type="submit" class="dx-btn dx-btn--lg">Continuar</button>
                </div>
              }

              @if (step() === 2) {
                <h2>Entrega y pago</h2>
                <fieldset>
                  <legend>Entrega</legend>
                  @if (deliveryModes().length > 1) {
                    <label>
                      Tipo
                      <select
                        class="dx-select"
                        [(ngModel)]="form.delivery"
                        name="delivery"
                        (ngModelChange)="onDeliveryChange($event)"
                      >
                        @for (m of deliveryModes(); track m.value) {
                          <option [ngValue]="m.value">{{ m.label }}</option>
                        }
                      </select>
                    </label>
                  } @else if (deliveryModes().length === 1) {
                    <p class="map-hint">Modalidad: <strong>{{ deliveryModes()[0].label }}</strong></p>
                  }

                  @if (form.delivery) {
                    @if (ciudades().length === 0) {
                      <p class="dx-alert dx-alert--error" role="alert">
                        No hay zonas de delivery habilitadas. Elegí retiro en sucursal o contactá a la tienda.
                      </p>
                    }
                    <label>
                      Ciudad
                      <select
                        class="dx-select"
                        [(ngModel)]="selectedCiudadId"
                        name="ciudad"
                        (ngModelChange)="onCiudadChange($event)"
                        required
                        [disabled]="ciudades().length === 0"
                      >
                        <option [ngValue]="null">{{ ciudades().length ? 'Seleccioná…' : 'Sin ciudades disponibles' }}</option>
                        @for (city of ciudades(); track city.id) {
                          <option [ngValue]="city.id">
                            {{ city.nombre }}@if (city.divisionPolitica) {
                              / {{ city.divisionPolitica }}
                            }
                          </option>
                        }
                      </select>
                    </label>
                    <label>
                      Barrio
                      <select
                        class="dx-select"
                        [(ngModel)]="selectedBarrioId"
                        name="barrio"
                        (ngModelChange)="onBarrioChange($event)"
                        required
                        [disabled]="!selectedCiudadId"
                      >
                        <option [ngValue]="null">{{ selectedCiudadId ? (barrios().length ? 'Seleccioná…' : 'Sin barrios en esta ciudad') : 'Primero elegí ciudad' }}</option>
                        @for (b of barrios(); track b.id) {
                          <option [ngValue]="b.id">
                            {{ b.nombre }}
                            @if (b.importeDelivery != null) {
                              ({{ b.importeDelivery | currency: 'PYG':'symbol-narrow':'1.0-0' }})
                            }
                          </option>
                        }
                      </select>
                    </label>
                    <label>
                      Dirección / referencia
                      <input class="dx-input" [(ngModel)]="form.lugarEntrega" name="lugarEntrega" required />
                    </label>
                    <div class="map-block">
                      <div class="map-toolbar">
                        <p class="map-hint">
                          {{
                            mapPinned()
                              ? 'Punto de entrega marcado. Podés arrastrar el pin o tocar otro lugar.'
                              : pers.checkoutTextos().hintMapa ||
                                'Tocá el mapa o usá tu ubicación para marcar dónde entregamos.'
                          }}
                        </p>
                        <button
                          type="button"
                          class="dx-btn dx-btn--ghost locate-btn"
                          (click)="useMyLocation()"
                          [disabled]="locating()"
                        >
                          <dx-icon name="locate" [size]="16" />
                          {{ locating() ? 'Buscando…' : 'Mi ubicación' }}
                        </button>
                      </div>
                      <div #mapHost class="map" role="application" aria-label="Mapa de entrega"></div>
                      <p class="map-status" [class.ok]="mapPinned()" role="status">
                        @if (mapPinned()) {
                          <dx-icon name="check" [size]="14" /> Ubicación confirmada
                        } @else {
                          <dx-icon name="map-pin" [size]="14" /> Falta marcar el punto en el mapa
                        }
                      </p>
                    </div>
                  } @else {
                    <label>
                      Sucursal de retiro
                      <select
                        class="dx-select"
                        [(ngModel)]="selectedAgenciaId"
                        name="agencia"
                        (ngModelChange)="onAgenciaChange($event)"
                        required
                      >
                        <option [ngValue]="null">Seleccioná sucursal…</option>
                        @for (a of agencias(); track a.id) {
                          <option [ngValue]="a.id">
                            {{ a.nombre }}@if (a.direccion) {
                              — {{ a.direccion }}
                            }
                          </option>
                        }
                      </select>
                    </label>
                    @if (selectedAgenciaId && cart()?.idAgenciaFacturacion === selectedAgenciaId) {
                      <p class="map-hint">Stock validado en la sucursal seleccionada.</p>
                    }
                    @if (pickupHoursMsg()) {
                      <p class="hours-msg" [class.hours-warn]="pickupBlocked()" role="status">{{ pickupHoursMsg() }}</p>
                    }
                    @if (selectedPickupHasMap()) {
                      <div class="map-block">
                        <p class="map-hint">Ubicación de la sucursal de retiro:</p>
                        <div #pickupMapHost class="map map--pickup" role="img" aria-label="Mapa de sucursal"></div>
                        <div class="pickup-links">
                          @if (pickupDirectionsUrl(); as dir) {
                            <a class="pickup-map" [href]="dir" target="_blank" rel="noopener">
                              <dx-icon name="locate" [size]="14" /> Cómo llegar
                            </a>
                          }
                          @if (pickupMapUrl(); as mapUrl) {
                            <a class="pickup-map" [href]="mapUrl" target="_blank" rel="noopener">
                              <dx-icon name="map-pin" [size]="14" /> Abrir en Maps
                            </a>
                          }
                        </div>
                      </div>
                    } @else {
                      @if (pickupMapUrl(); as mapUrl) {
                        <a class="pickup-map" [href]="mapUrl" target="_blank" rel="noopener">
                          Ver sucursal en mapa ↗
                        </a>
                      }
                    }
                  }
                </fieldset>

                <fieldset>
                  <legend>Pago</legend>
                  <div class="pay-options" role="radiogroup" aria-label="Forma de pago">
                    @for (p of paymentModes(); track p.label) {
                      <label
                        class="pay-card"
                        [class.on]="form.pagarEnLaEntrega === p.value"
                        (click)="setPago(p.value)"
                      >
                        <input
                          type="radio"
                          name="pago"
                          [checked]="form.pagarEnLaEntrega === p.value"
                          (change)="setPago(p.value)"
                        />
                        <span class="pay-ico" aria-hidden="true">
                          <dx-icon [name]="p.value ? 'package' : 'shield'" [size]="20" />
                        </span>
                        <span class="pay-body">
                          <strong>{{ p.label }}</strong>
                          <small>{{
                            p.value
                              ? 'Abonás al recibir el pedido o al retirarlo en sucursal.'
                              : 'Pago online con tarjeta al confirmar.'
                          }}</small>
                        </span>
                      </label>
                    }
                    <div class="pay-card pay-card--soon" aria-disabled="true">
                      <span class="pay-ico" aria-hidden="true">
                        <dx-icon name="shield" [size]="20" />
                      </span>
                      <span class="pay-body">
                        <strong>Pago online (Bancard)</strong>
                        <small>Próximamente. Por ahora confirmá con pago en la entrega.</small>
                      </span>
                      <span class="soon-badge">Pronto</span>
                    </div>
                  </div>
                </fieldset>

                @if (comprobanteOptions().length > 1) {
                  <fieldset>
                    <legend>Tipo de comprobante</legend>
                    <div class="radio-row" role="radiogroup" aria-label="Tipo de comprobante">
                      @for (opt of comprobanteOptions(); track opt.value) {
                        <label class="radio">
                          <input
                            type="radio"
                            name="tipoComprobante"
                            [value]="opt.value"
                            [(ngModel)]="form.tipoComprobante"
                          />
                          {{ opt.label }}
                        </label>
                      }
                    </div>
                  </fieldset>
                } @else if (comprobanteOptions().length === 1) {
                  <p class="map-hint">
                    Comprobante: <strong>{{ comprobanteOptions()[0].label }}</strong>
                  </p>
                }

                <label>
                  Observación
                  <textarea class="dx-input" [(ngModel)]="form.observacion" name="observacion" rows="3"></textarea>
                </label>

                <div class="nav-row">
                  <button type="button" class="dx-btn dx-btn--ghost" (click)="goStep(1)">Atrás</button>
                  <button type="submit" class="dx-btn dx-btn--lg" [disabled]="pickupBlocked()">Continuar</button>
                </div>
              }

              @if (step() === 3) {
                <h2>Confirmación</h2>
                <div class="review">
                  <div>
                    <strong>Cliente</strong>
                    <p>
                      {{ form.razonSocial }} · Doc. {{ form.numeroDocumento }}<br />
                      {{ form.nombre }} {{ form.apellido }}<br />
                      {{ form.email }} · {{ form.nroTelefono }}
                    </p>
                  </div>
                  <div>
                    <strong>Entrega</strong>
                    <p>
                      {{ form.delivery ? 'Delivery' : 'Pickup' }}
                      @if (form.lugarEntrega) {
                        — {{ form.lugarEntrega }}
                      }
                      @if (form.delivery && mapPinned()) {
                        <br />
                        <small class="coords">Ubicación confirmada en el mapa</small>
                      }
                    </p>
                  </div>
                  <div>
                    <strong>Pago</strong>
                    <p>
                      {{
                        form.pagarEnLaEntrega
                          ? 'Pagar en la entrega / retiro'
                          : 'Pagar al hacer el pedido (Bancard)'
                      }}
                    </p>
                    @if (form.pagarEnLaEntrega) {
                      <small class="coords">No se cobrará online. El pago se concreta al recibir.</small>
                    }
                  </div>
                  @if (form.tipoComprobante) {
                    <div>
                      <strong>Comprobante</strong>
                      <p>{{ form.tipoComprobante === 'ticket' ? 'Ticket' : 'Factura' }}</p>
                    </div>
                  }
                  @if (form.observacion?.trim()) {
                    <div>
                      <strong>Observación</strong>
                      <p>{{ form.observacion }}</p>
                    </div>
                  }
                </div>
                <div class="nav-row">
                  <button type="button" class="dx-btn dx-btn--ghost" (click)="goStep(2)">Atrás</button>
                  <button type="submit" class="dx-btn dx-btn--lg" [disabled]="busy() || pickupBlocked()">
                    {{ pers.checkoutTextos().ctaConfirmar || 'Confirmar pedido' }}
                  </button>
                </div>
              }
            </form>

            <aside class="summary">
              <h2>Resumen</h2>
              <ul>
                @for (line of c.items; track line.id) {
                  <li>
                    <span>
                      <strong>{{ line.nombre }}</strong>
                      <small>{{ line.cantidad }} × {{ line.embalaje || 'u' }}</small>
                    </span>
                    <strong>{{ line.montoTotal | currency: 'PYG':'symbol-narrow':'1.0-0' }}</strong>
                  </li>
                }
              </ul>
              @if (c.codigoCupon) {
                <p class="cupon-applied">
                  Cupón <strong>{{ c.codigoCupon }}</strong>
                  @if (c.montoDescuento) {
                    · −{{ c.montoDescuento | currency: 'PYG':'symbol-narrow':'1.0-0' }}
                  }
                </p>
              }
              <div class="cupon-row">
                <input
                  class="dx-input"
                  [(ngModel)]="cuponCodigo"
                  name="cuponCodigo"
                  placeholder="Código cupón"
                  [disabled]="cuponBusy()"
                />
                @if (c.codigoCupon) {
                  <button type="button" class="dx-btn dx-btn--ghost" (click)="quitarCupon()" [disabled]="cuponBusy()">
                    Quitar
                  </button>
                } @else {
                  <button
                    type="button"
                    class="dx-btn"
                    (click)="aplicarCupon()"
                    [disabled]="cuponBusy() || !cuponCodigo.trim()"
                  >
                    Aplicar
                  </button>
                }
              </div>
              @if (cuponMsg()) {
                <p class="cupon-msg" [class.err]="cuponErr()">{{ cuponMsg() }}</p>
              }
              <div class="totals">
                <div>
                  <span>Subtotal</span>
                  <strong>{{ subtotal(c) | currency: 'PYG':'symbol-narrow':'1.0-0' }}</strong>
                </div>
                @if (c.montoDescuento) {
                  <div class="disc">
                    <span>Descuento</span>
                    <strong>−{{ c.montoDescuento | currency: 'PYG':'symbol-narrow':'1.0-0' }}</strong>
                  </div>
                }
                @if (shippingFee() != null) {
                  <div>
                    <span>Envío</span>
                    <strong>{{ shippingFee()! | currency: 'PYG':'symbol-narrow':'1.0-0' }}</strong>
                  </div>
                }
                <div class="grand">
                  <span>Total</span>
                  <strong>{{ c.montoTotal | currency: 'PYG':'symbol-narrow':'1.0-0' }}</strong>
                </div>
              </div>
              <small class="canal-tag">
                Canal {{ session.canal() === 'B2B' && session.mayoristaAprobado() ? 'Mayorista' : 'Minorista' }}
                @if (!session.mayoristaAprobado()) {
                  · precios minorista
                }
              </small>
            </aside>
          </div>
        }
      } @else {
        <p class="empty" role="status" aria-busy="true">Cargando checkout…</p>
      }
    </section>
  `,
  styles: [
    `
      header {
        display: flex;
        justify-content: space-between;
        align-items: baseline;
        margin-bottom: 1.25rem;
        gap: 1rem;
        flex-wrap: wrap;
      }
      h1 {
        margin: 0;
        font-family: var(--dx-display);
        font-size: clamp(1.5rem, 3vw, 2rem);
      }
      .kicker {
        margin: 0 0 0.2rem;
        font-size: 0.78rem;
        font-weight: 700;
        letter-spacing: 0.08em;
        text-transform: uppercase;
        color: var(--dx-mute);
      }
      .sub {
        margin: 0.35rem 0 0;
        color: var(--dx-ink-soft);
      }
      .steps {
        list-style: none;
        display: flex;
        gap: 0.5rem;
        padding: 0;
        margin: 0 0 1.25rem;
        flex-wrap: wrap;
      }
      .steps li {
        padding: 0;
        margin: 0;
      }
      .step-btn {
        padding: 0.35rem 0.75rem;
        border-radius: 999px;
        border: none;
        background: #eee;
        color: #666;
        font-size: 0.82rem;
        font-weight: 700;
        cursor: pointer;
        font-family: inherit;
      }
      .step-btn.on {
        background: var(--dx-red);
        color: #fff;
      }
      .step-btn.done {
        background: #d1fae5;
        color: #065f46;
      }
      .nav-row {
        display: flex;
        gap: 0.75rem;
        flex-wrap: wrap;
        align-items: center;
        margin-top: 0.35rem;
      }
      .radio-row {
        display: flex;
        gap: 1rem;
        flex-wrap: wrap;
        margin: 0.25rem 0 0.35rem;
      }
      .radio {
        display: inline-flex;
        align-items: center;
        gap: 0.4rem;
        margin: 0;
        font-size: 0.9rem;
        font-weight: 600;
        cursor: pointer;
      }
      .pay-options {
        display: grid;
        gap: 0.55rem;
        margin-top: 0.35rem;
      }
      .pay-card {
        position: relative;
        display: grid;
        grid-template-columns: auto 1fr;
        gap: 0.75rem;
        align-items: start;
        margin: 0;
        padding: 0.85rem 1rem;
        border: 1px solid var(--dx-line);
        border-radius: 12px;
        background: #fff;
        cursor: pointer;
        transition:
          border-color 0.15s ease,
          box-shadow 0.15s ease,
          background 0.15s ease;
      }
      .pay-card input {
        position: absolute;
        opacity: 0;
        pointer-events: none;
      }
      .pay-card.on {
        border-color: var(--dx-red);
        background: #fff8f8;
        box-shadow: 0 0 0 1px rgba(198, 40, 40, 0.2);
      }
      .pay-card--soon {
        cursor: not-allowed;
        opacity: 0.72;
        background: #fafafa;
        grid-template-columns: auto 1fr auto;
      }
      .pay-ico {
        width: 40px;
        height: 40px;
        border-radius: 10px;
        background: #f3f3f3;
        color: var(--dx-ink);
        display: grid;
        place-items: center;
      }
      .pay-card.on .pay-ico {
        background: #ffe8e8;
        color: var(--dx-red);
      }
      .pay-body {
        display: grid;
        gap: 0.15rem;
        min-width: 0;
      }
      .pay-body strong {
        font-size: 0.95rem;
      }
      .pay-body small {
        color: var(--dx-mute);
        line-height: 1.35;
      }
      .soon-badge {
        align-self: center;
        font-size: 0.68rem;
        font-weight: 750;
        text-transform: uppercase;
        letter-spacing: 0.04em;
        color: #64748b;
        background: #e2e8f0;
        padding: 0.2rem 0.45rem;
        border-radius: 6px;
      }
      .review {
        display: grid;
        gap: 0.85rem;
        margin: 0 0 1rem;
        padding: 0.85rem 1rem;
        border: 1px solid var(--dx-line);
        border-radius: 12px;
        background: #fafafa;
      }
      .review strong {
        display: block;
        margin-bottom: 0.2rem;
        font-size: 0.82rem;
        letter-spacing: 0.04em;
        text-transform: uppercase;
        color: var(--dx-mute);
      }
      .review p {
        margin: 0;
        font-size: 0.95rem;
        line-height: 1.45;
      }
      h2 {
        margin: 0 0 0.85rem;
        font-family: var(--dx-display);
        font-size: 1.15rem;
      }
      a:not(.dx-btn) {
        color: var(--dx-red);
        font-weight: 700;
        text-decoration: none;
      }
      .grid {
        display: grid;
        grid-template-columns: 1.4fr 1fr;
        gap: 1.25rem;
      }
      @media (max-width: 800px) {
        .grid {
          grid-template-columns: 1fr;
        }
      }
      .form,
      .summary {
        background: #fff;
        border: 1px solid var(--dx-line);
        border-radius: 14px;
        padding: 1.15rem 1.25rem;
        box-shadow: var(--dx-shadow);
      }
      label {
        display: grid;
        gap: 0.35rem;
        margin-bottom: 0.75rem;
        font-size: 0.9rem;
        font-weight: 600;
      }
      .row {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 0.75rem;
      }
      @media (max-width: 560px) {
        .row {
          grid-template-columns: 1fr;
        }
      }
      fieldset {
        border: 1px solid var(--dx-line);
        border-radius: 12px;
        margin: 0 0 0.85rem;
        padding: 0.85rem;
      }
      legend {
        font-weight: 700;
        padding: 0 0.35rem;
      }
      .map {
        height: 300px;
        border-radius: 12px;
        border: 1px solid var(--dx-line);
        margin-bottom: 0.35rem;
        z-index: 0;
        background: #eef2f6;
      }
      .map--pickup {
        height: 220px;
      }
      .map-block {
        display: grid;
        gap: 0.45rem;
        margin-bottom: 0.5rem;
      }
      .map-toolbar {
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
        gap: 0.75rem;
        flex-wrap: wrap;
      }
      .map-toolbar .map-hint {
        flex: 1;
        margin: 0;
        min-width: 180px;
      }
      .locate-btn {
        flex-shrink: 0;
      }
      .map-hint {
        color: var(--dx-mute);
        font-size: 0.85rem;
        margin: 0 0 0.5rem;
      }
      .map-status {
        margin: 0;
        display: inline-flex;
        align-items: center;
        gap: 0.35rem;
        font-size: 0.88rem;
        font-weight: 650;
        color: #9a3412;
      }
      .map-status.ok {
        color: #065f46;
      }
      .pickup-links {
        display: flex;
        flex-wrap: wrap;
        gap: 0.85rem;
      }
      .pickup-map {
        display: inline-flex;
        align-items: center;
        gap: 0.3rem;
        margin: 0;
        font-weight: 700;
        color: var(--dx-red);
        text-decoration: none;
      }
      .hours-msg {
        margin: 0 0 0.75rem;
        padding: 0.65rem 0.85rem;
        border-radius: 10px;
        background: #ecfdf5;
        border: 1px solid #a7f3d0;
        color: #065f46;
        font-size: 0.88rem;
        font-weight: 600;
      }
      .hours-msg.hours-warn {
        background: #fff7ed;
        border-color: #fed7aa;
        color: #9a3412;
      }
      .summary ul {
        list-style: none;
        padding: 0;
        margin: 0 0 1rem;
        display: grid;
        gap: 0.55rem;
      }
      .summary li {
        display: flex;
        justify-content: space-between;
        gap: 0.75rem;
        font-size: 0.92rem;
      }
      .total {
        margin: 0;
        font-family: var(--dx-display);
        font-size: 1.5rem;
        color: var(--dx-price);
        font-weight: 800;
      }
      .subtotal {
        margin: 0 0 0.35rem;
        color: var(--dx-red);
        font-weight: 700;
        font-size: 0.92rem;
      }
      .cupon-applied {
        margin: 0 0 0.5rem;
        font-size: 0.88rem;
        color: var(--dx-ink-soft);
      }
      .cupon-row {
        display: grid;
        grid-template-columns: 1fr auto;
        gap: 0.45rem;
        margin-bottom: 0.5rem;
      }
      .cupon-msg {
        margin: 0 0 0.5rem;
        font-size: 0.85rem;
        color: #065f46;
      }
      .cupon-msg.err {
        color: var(--dx-warn);
      }
      .totals {
        display: grid;
        gap: 0.45rem;
        margin-top: 0.75rem;
        padding-top: 0.75rem;
        border-top: 1px solid var(--dx-line);
      }
      .totals > div {
        display: flex;
        justify-content: space-between;
        gap: 0.75rem;
        font-size: 0.92rem;
      }
      .totals .disc strong {
        color: #065f46;
      }
      .totals .grand {
        font-size: 1.1rem;
        font-weight: 800;
        margin-top: 0.25rem;
      }
      .canal-tag {
        display: block;
        margin-top: 0.75rem;
        color: var(--dx-mute);
      }
      .summary {
        position: sticky;
        top: 5.5rem;
        align-self: start;
      }
      .summary ul li {
        display: flex;
        justify-content: space-between;
        gap: 0.75rem;
        margin: 0.55rem 0;
      }
      .summary ul li span {
        display: grid;
        gap: 0.15rem;
      }
      .summary ul li small {
        color: var(--dx-mute);
        font-weight: 500;
      }
      .doc-warn {
        color: #b45309;
        font-weight: 600;
        font-size: 0.82rem;
      }
      .err {
        color: var(--dx-warn);
        background: #fff5f5;
        border: 1px solid #f5c2c0;
        border-radius: 10px;
        padding: 0.75rem 1rem;
      }
      .empty {
        background: #fff;
        border: 1px dashed var(--dx-line);
        border-radius: 12px;
        padding: 1.5rem;
        text-align: center;
        color: var(--dx-ink-soft);
      }
      .empty-box {
        display: grid;
        justify-items: center;
        gap: 0.65rem;
        padding: 2.25rem 1rem;
        text-align: center;
        background: #fff;
        border: 1px dashed var(--dx-line);
        border-radius: 14px;
        color: var(--dx-ink-soft);
      }
      .empty-box p {
        margin: 0;
      }
      .empty-box dx-icon {
        color: var(--dx-mute);
      }
    `,
  ],
})
export class CheckoutPageComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('mapHost') mapHost?: ElementRef<HTMLDivElement>;
  @ViewChild('pickupMapHost') pickupMapHost?: ElementRef<HTMLDivElement>;

  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  readonly session = inject(SessionService);
  readonly pers = inject(PersonalizacionService);
  readonly branding = inject(BrandingService);

  readonly step = signal<1 | 2 | 3>(1);
  readonly cart = signal<Cart | null>(null);
  readonly ciudades = signal<CiudadDelivery[]>([]);
  readonly barrios = signal<BarrioDelivery[]>([]);
  readonly agencias = signal<Agencia[]>([]);
  readonly error = signal<string | null>(null);
  readonly busy = signal(false);
  readonly cuponBusy = signal(false);
  readonly cuponMsg = signal<string | null>(null);
  readonly cuponErr = signal(false);
  readonly docWarning = signal<string | null>(null);
  readonly pickupHoursMsg = signal<string | null>(null);
  readonly pickupBlocked = signal(false);
  readonly mapPinned = signal(false);
  readonly locating = signal(false);

  cuponCodigo = '';

  selectedCiudadId: number | null = null;
  selectedBarrioId: number | null = null;
  selectedAgenciaId: number | null = null;

  form: CheckoutRequest = {
    delivery: true,
    pagarEnLaEntrega: true,
    razonSocial: environment.demoPrefill ? 'Consumidor final' : '',
    numeroDocumento: environment.demoPrefill ? '0' : '',
    nombre: environment.demoPrefill ? 'Demo' : '',
    apellido: environment.demoPrefill ? 'Cliente' : '',
    email: environment.demoPrefill ? 'demo@datamex.local' : '',
    nroTelefono: environment.demoPrefill ? '0981000000' : '',
    lugarEntrega: '',
    observacion: '',
    latitud: undefined,
    longitud: undefined,
    tipoComprobante: null,
  };

  private map: any;
  private marker: any;
  private pickupMap: any;
  private pickupMarker: any;
  private mapReady = false;
  private mapDefault = { lat: -25.3106, lng: -57.5901 };
  private brandingDefaultsApplied = false;
  private pendingAutoCupon: string | null = null;

  constructor() {
    effect(() => {
      if (this.branding.loaded()) {
        this.applyBrandingDefaults();
      }
    });
  }

  ngOnInit(): void {
    this.restoreDraft();
    this.reloadCart();
    this.api.getCiudadesDelivery().subscribe({
      next: (rows) => this.ciudades.set(rows),
      error: (e) => this.block(e?.error?.error || 'Error ciudades'),
    });
    this.api.getAgencias().subscribe({
      next: (rows) => this.agencias.set(rows),
      error: (e) => this.block(e?.error?.error || 'Error sucursales'),
    });
    this.api.getMapDefaults().subscribe({
      next: (d) => {
        this.mapDefault = { lat: d.lat, lng: d.lng };
        if (this.mapReady && !this.mapPinned()) {
          this.map?.setView([d.lat, d.lng], 14);
          this.setMarker(d.lat, d.lng, false);
        }
      },
    });
    if (isLoggedIn()) {
      this.api.me().subscribe({
        next: (p) => {
          this.form.nombre = p.nombre || this.form.nombre;
          this.form.apellido = p.apellido || this.form.apellido;
          this.form.email = p.email || this.form.email;
          this.form.nroTelefono = p.telefono || this.form.nroTelefono;
          this.form.numeroDocumento = p.numeroDocumento || this.form.numeroDocumento;
          const full = `${p.nombre || ''} ${p.apellido || ''}`.trim();
          if (full) this.form.razonSocial = full;
          this.persistDraft();
        },
      });
    }
    try {
      const pending = (localStorage.getItem(PENDING_COUPON_KEY) || '').trim();
      if (pending) {
        this.cuponCodigo = pending;
        this.pendingAutoCupon = pending;
        localStorage.removeItem(PENDING_COUPON_KEY);
      }
    } catch {
      /* ignore */
    }
  }

  private restoreDraft(): void {
    if (environment.demoPrefill) return;
    try {
      const raw = localStorage.getItem(CHECKOUT_DRAFT_KEY);
      if (!raw) return;
      const d = JSON.parse(raw) as CheckoutDraft;
      if (typeof d.delivery === 'boolean') this.form.delivery = d.delivery;
      if (typeof d.pagarEnLaEntrega === 'boolean') this.form.pagarEnLaEntrega = d.pagarEnLaEntrega;
      if (d.razonSocial) this.form.razonSocial = d.razonSocial;
      if (d.numeroDocumento) this.form.numeroDocumento = d.numeroDocumento;
      if (d.nombre) this.form.nombre = d.nombre;
      if (d.apellido) this.form.apellido = d.apellido;
      if (d.email) this.form.email = d.email;
      if (d.nroTelefono) this.form.nroTelefono = d.nroTelefono;
      if (d.lugarEntrega) this.form.lugarEntrega = d.lugarEntrega;
      if (d.observacion) this.form.observacion = d.observacion;
      if (d.tipoComprobante === 'factura' || d.tipoComprobante === 'ticket' || d.tipoComprobante === null) {
        this.form.tipoComprobante = d.tipoComprobante;
      }
      if (d.selectedCiudadId != null) this.selectedCiudadId = d.selectedCiudadId;
      if (d.selectedBarrioId != null) this.selectedBarrioId = d.selectedBarrioId;
      if (d.selectedAgenciaId != null) this.selectedAgenciaId = d.selectedAgenciaId;
    } catch {
      /* ignore */
    }
  }

  persistDraft(): void {
    if (environment.demoPrefill) return;
    const draft: CheckoutDraft = {
      delivery: this.form.delivery,
      pagarEnLaEntrega: this.form.pagarEnLaEntrega,
      razonSocial: this.form.razonSocial,
      numeroDocumento: this.form.numeroDocumento,
      nombre: this.form.nombre,
      apellido: this.form.apellido,
      email: this.form.email,
      nroTelefono: this.form.nroTelefono,
      lugarEntrega: this.form.lugarEntrega,
      observacion: this.form.observacion,
      tipoComprobante: this.form.tipoComprobante,
      selectedCiudadId: this.selectedCiudadId,
      selectedBarrioId: this.selectedBarrioId,
      selectedAgenciaId: this.selectedAgenciaId,
    };
    try {
      localStorage.setItem(CHECKOUT_DRAFT_KEY, JSON.stringify(draft));
    } catch {
      /* ignore */
    }
  }

  ngAfterViewInit(): void {
    if (this.step() === 2 && this.form.delivery) {
      setTimeout(() => this.initMap(), 0);
    }
  }

  ngOnDestroy(): void {
    this.destroyMap();
  }

  deliveryModes(): { value: boolean; label: string }[] {
    const b = this.branding.branding();
    const modes: { value: boolean; label: string }[] = [];
    if (!b || b.realizaDelivery !== false) {
      modes.push({ value: true, label: 'Delivery' });
    }
    if (!b || b.realizaPickup !== false) {
      modes.push({ value: false, label: 'Pickup' });
    }
    return modes.length
      ? modes
      : [
          { value: true, label: 'Delivery' },
          { value: false, label: 'Pickup' },
        ];
  }

  paymentModes(): { value: boolean; label: string }[] {
    // UAT: solo contra entrega. Bancard se reactivará después.
    return [{ value: true, label: 'Pagar en la entrega' }];
  }

  setPago(value: boolean): void {
    this.form.pagarEnLaEntrega = value;
    this.persistDraft();
  }

  comprobanteOptions(): { value: 'factura' | 'ticket'; label: string }[] {
    const b = this.branding.branding();
    const opts: { value: 'factura' | 'ticket'; label: string }[] = [];
    if (this.form.delivery) {
      if (!b || b.deliveryEmiteFactura !== false) {
        opts.push({ value: 'factura', label: 'Factura' });
      }
      if (b?.deliveryEmiteTicket) {
        opts.push({ value: 'ticket', label: 'Ticket' });
      }
    } else {
      if (!b || b.pickupEmiteFactura !== false) {
        opts.push({ value: 'factura', label: 'Factura' });
      }
      if (b?.pickupEmiteTicket) {
        opts.push({ value: 'ticket', label: 'Ticket' });
      }
    }
    return opts;
  }

  syncComprobante(): void {
    const opts = this.comprobanteOptions();
    if (!opts.length) {
      this.form.tipoComprobante = null;
      return;
    }
    if (opts.length === 1) {
      this.form.tipoComprobante = opts[0].value;
      return;
    }
    if (!opts.some((o) => o.value === this.form.tipoComprobante)) {
      this.form.tipoComprobante = opts[0].value;
    }
  }

  applyBrandingDefaults(): void {
    const delivery = this.deliveryModes();
    if (delivery.length && !delivery.some((m) => m.value === this.form.delivery)) {
      const next = delivery[0].value;
      if (this.brandingDefaultsApplied) {
        this.onDeliveryChange(next);
      } else {
        this.form.delivery = next;
      }
    }

    const pay = this.paymentModes();
    if (pay.length && !pay.some((m) => m.value === this.form.pagarEnLaEntrega)) {
      this.form.pagarEnLaEntrega = pay[0].value;
    } else if (pay.length === 1) {
      this.form.pagarEnLaEntrega = pay[0].value;
    }

    this.syncComprobante();
    this.brandingDefaultsApplied = true;
  }

  canLeaveStep1(): boolean {
    const f = this.form;
    if (
      !f.razonSocial?.trim() ||
      !f.numeroDocumento?.trim() ||
      !f.nombre?.trim() ||
      !f.apellido?.trim() ||
      !f.email?.trim() ||
      !f.nroTelefono?.trim()
    ) {
      return false;
    }
    return this.validateDocumento(true);
  }

  canLeaveStep2(): boolean {
    if (!this.deliveryModes().some((m) => m.value === this.form.delivery)) {
      return false;
    }
    if (!this.paymentModes().some((m) => m.value === this.form.pagarEnLaEntrega)) {
      return false;
    }

    if (this.form.delivery) {
      if (this.selectedBarrioId == null) return false;
      if (!this.form.lugarEntrega?.trim()) return false;
      if (this.form.latitud == null || this.form.longitud == null || !this.mapPinned()) return false;
    } else {
      if (this.selectedAgenciaId == null) return false;
      if (this.pickupBlocked()) return false;
    }

    const opts = this.comprobanteOptions();
    if (opts.length > 1 && !opts.some((o) => o.value === this.form.tipoComprobante)) {
      return false;
    }
    return true;
  }

  goStep(n: 1 | 2 | 3): void {
    if (n === this.step()) return;

    if (n > 1 && !this.canLeaveStep1()) {
      this.toast.error(this.docWarning() || 'Completá los datos del cliente.');
      return;
    }
    if (n > 2 && !this.canLeaveStep2()) {
      this.toast.error(this.step2BlockMessage());
      return;
    }

    const prev = this.step();
    if (prev === 2 && n !== 2) {
      this.destroyMap();
    }

    this.step.set(n);
    this.error.set(null);
    this.persistDraft();

    if (n === 2 && this.form.delivery) {
      setTimeout(() => this.initMap(), 50);
    }
  }

  nextFrom1(): void {
    if (!this.canLeaveStep1()) {
      this.toast.error(this.docWarning() || 'Completá los datos del cliente.');
      return;
    }
    this.step.set(2);
    this.error.set(null);
    this.persistDraft();
    if (this.form.delivery) {
      setTimeout(() => this.initMap(), 50);
    }
  }

  nextFrom2(): void {
    if (!this.canLeaveStep2()) {
      this.toast.error(this.step2BlockMessage());
      return;
    }
    this.destroyMap();
    this.syncComprobante();
    this.step.set(3);
    this.error.set(null);
  }

  onFormSubmit(): void {
    if (this.step() === 1) {
      this.nextFrom1();
      return;
    }
    if (this.step() === 2) {
      this.nextFrom2();
      return;
    }
    this.submit();
  }

  onDeliveryChange(delivery: boolean): void {
    this.form.delivery = delivery;
    this.syncComprobante();

    if (!delivery) {
      this.selectedCiudadId = null;
      this.selectedBarrioId = null;
      this.form.lugarEntrega = '';
      this.form.latitud = undefined;
      this.form.longitud = undefined;
      this.mapPinned.set(false);
      this.destroyMap();
      this.api.clearShipping().subscribe({ next: () => this.reloadCart(), error: () => this.reloadCart() });
      if (this.selectedAgenciaId != null) this.onAgenciaChange(this.selectedAgenciaId);
    } else {
      this.selectedAgenciaId = null;
      this.pickupHoursMsg.set(null);
      this.pickupBlocked.set(false);
      this.mapPinned.set(false);
      this.form.latitud = undefined;
      this.form.longitud = undefined;
      this.destroyMap();
      if (this.step() === 2) {
        setTimeout(() => this.initMap(), 50);
      }
    }
  }

  onAgenciaChange(id: number | null): void {
    if (id == null) {
      this.pickupHoursMsg.set(null);
      this.pickupBlocked.set(false);
      this.destroyPickupMap();
      return;
    }
    const a = this.agencias().find((x) => x.id === id);
    if (a) this.form.lugarEntrega = a.direccion ? `${a.nombre} — ${a.direccion}` : a.nombre;
    this.busy.set(true);
    this.error.set(null);
    this.api.setPickup(id).subscribe({
      next: (c) => {
        this.busy.set(false);
        this.cart.set(c);
        setTimeout(() => this.initPickupMap(), 50);
      },
      error: (e) => {
        this.busy.set(false);
        this.selectedAgenciaId = null;
        this.block(e?.error?.error || 'No se pudo asignar la sucursal (stock).');
      },
    });
    this.api.getAgenciaHorarios(id).subscribe({
      next: (horarios) => this.evaluatePickupHours(horarios),
      error: () => {
        this.pickupHoursMsg.set(null);
        this.pickupBlocked.set(false);
      },
    });
  }

  private evaluatePickupHours(horarios: HorarioAtencion[]): void {
    if (!horarios.length) {
      this.pickupHoursMsg.set(null);
      this.pickupBlocked.set(false);
      return;
    }

    const now = new Date();
    const dia = now.getDay();
    const today = horarios.filter((h) => h.diaSemana === dia);
    const fmt = (h: HorarioAtencion) => {
      let s = `${h.apertura}–${h.cierre}`;
      if (h.apertura2 && h.cierre2) s += ` y ${h.apertura2}–${h.cierre2}`;
      return s;
    };

    if (!today.length) {
      this.pickupHoursMsg.set('La sucursal no atiende hoy. Elegí otra sucursal o volvé en un día hábil.');
      this.pickupBlocked.set(true);
      return;
    }

    const mins = now.getHours() * 60 + now.getMinutes();
    const open = today.some((h) => this.isWithinHours(mins, h));
    const hoursText = today.map(fmt).join(', ');

    if (!open) {
      this.pickupHoursMsg.set(`Fuera del horario de atención. Hoy: ${hoursText}.`);
      this.pickupBlocked.set(true);
    } else {
      this.pickupHoursMsg.set(`Horario de atención hoy: ${hoursText}.`);
      this.pickupBlocked.set(false);
    }
  }

  private isWithinHours(mins: number, h: HorarioAtencion): boolean {
    if (this.inRange(mins, h.apertura, h.cierre)) return true;
    if (h.apertura2 && h.cierre2 && this.inRange(mins, h.apertura2, h.cierre2)) return true;
    return false;
  }

  private inRange(mins: number, open: string, close: string): boolean {
    const toMins = (t: string) => {
      const [hh, mm] = t.split(':').map(Number);
      return hh * 60 + mm;
    };
    const a = toMins(open);
    const b = toMins(close);
    return mins >= a && mins < b;
  }

  selectedPickupHasMap(): boolean {
    const a = this.agencias().find((x) => x.id === this.selectedAgenciaId);
    return !!a && isValidLatLng(a.latitud, a.longitud);
  }

  pickupMapUrl(): string | null {
    const a = this.agencias().find((x) => x.id === this.selectedAgenciaId);
    if (!a || !isValidLatLng(a.latitud, a.longitud)) return null;
    return mapsPinUrl(a.latitud!, a.longitud!, a.nombre);
  }

  pickupDirectionsUrl(): string | null {
    const a = this.agencias().find((x) => x.id === this.selectedAgenciaId);
    if (!a || !isValidLatLng(a.latitud, a.longitud)) return null;
    return mapsDirectionsUrl(a.latitud!, a.longitud!);
  }

  useMyLocation(): void {
    if (!navigator.geolocation) {
      this.toast.error('Tu navegador no permite geolocalización.');
      return;
    }
    this.locating.set(true);
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        this.locating.set(false);
        const { latitude: lat, longitude: lng } = pos.coords;
        if (!this.map) this.initMap();
        this.map?.setView([lat, lng], 16);
        this.setMarker(lat, lng, true);
        this.toast.success('Ubicación marcada');
      },
      () => {
        this.locating.set(false);
        this.toast.error('No se pudo obtener tu ubicación. Tocá el mapa manualmente.');
      },
      { enableHighAccuracy: true, timeout: 12000 },
    );
  }

  subtotal(c: Cart): number {
    const lines = c.items?.reduce((s, l) => s + (l.montoTotal || 0), 0) || 0;
    return lines;
  }

  shippingFee(): number | null {
    if (!this.form.delivery || this.selectedBarrioId == null) return null;
    const b = this.barrios().find((x) => x.id === this.selectedBarrioId);
    return b?.importeDelivery ?? null;
  }

  onCiudadChange(id: number | null): void {
    this.selectedBarrioId = null;
    this.barrios.set([]);
    if (id == null) return;
    this.api.getBarriosDelivery(id).subscribe({
      next: (rows) => this.barrios.set(rows),
      error: (e) => this.error.set(e?.error?.error || 'Error barrios'),
    });
  }

  onBarrioChange(id: number | null): void {
    if (id == null) return;
    const b = this.barrios().find((x) => x.id === id);
    const c = this.ciudades().find((x) => x.id === this.selectedCiudadId);
    if (b && c) this.form.lugarEntrega = `${b.nombre}, ${c.nombre}`;
    this.busy.set(true);
    this.api.applyShippingByBarrio(id).subscribe({
      next: () => {
        this.busy.set(false);
        this.reloadCart();
      },
      error: (e) => {
        this.busy.set(false);
        this.error.set(e?.error?.error || 'No se pudo aplicar tarifa');
      },
    });
  }

  private block(msg: string): void {
    this.error.set(msg);
    this.toast.error(msg);
  }

  private step2BlockMessage(): string {
    if (this.form.delivery) {
      if (this.selectedBarrioId == null) return 'Seleccioná ciudad y barrio.';
      if (!this.form.lugarEntrega?.trim()) return 'Indicá la dirección / referencia.';
      if (this.form.latitud == null || this.form.longitud == null || !this.mapPinned()) {
        return 'Marcá la ubicación en el mapa (tocá el punto de entrega).';
      }
    } else if (this.selectedAgenciaId == null) {
      return 'Seleccioná la sucursal de retiro.';
    } else if (this.pickupBlocked()) {
      return this.pickupHoursMsg() || 'La sucursal está fuera de horario.';
    }
    if (this.comprobanteOptions().length > 1 && !this.form.tipoComprobante) {
      return 'Seleccioná el tipo de comprobante.';
    }
    return 'Completá entrega y pago para continuar.';
  }

  submit(): void {
    if (!this.canLeaveStep1()) {
      this.toast.error(this.docWarning() || 'Revisá los datos del cliente.');
      this.step.set(1);
      return;
    }
    if (!this.canLeaveStep2()) {
      this.toast.error(this.step2BlockMessage());
      this.step.set(2);
      if (this.form.delivery) setTimeout(() => this.initMap(), 50);
      return;
    }

    const cart = this.cart();
    if (!cart || cart.items.length === 0) {
      this.block('El carrito está vacío.');
      return;
    }
    if (cart.items.some((i) => !i.stockOk)) {
      this.block('Hay ítems sin stock suficiente. Volvé al carrito y ajustá cantidades.');
      return;
    }

    this.syncComprobante();
    this.busy.set(true);
    this.error.set(null);
    this.api.checkout(this.form).subscribe({
      next: (r) => {
        this.busy.set(false);
        this.toast.success('Pedido confirmado');
        if (r.requierePagoOnline) {
          void this.router.navigate(['/pago', r.nroPedido]);
        } else {
          void this.router.navigate(['/pedido', r.nroPedido]);
        }
      },
      error: (e) => {
        this.busy.set(false);
        this.block(e?.error?.error || e.message || 'No se pudo finalizar');
      },
    });
  }

  aplicarCupon(): void {
    const codigo = this.cuponCodigo.trim();
    if (!codigo) return;
    this.cuponBusy.set(true);
    this.cuponMsg.set(null);
    this.cuponErr.set(false);
    this.api.aplicarCupon(codigo).subscribe({
      next: (r) => {
        this.cuponBusy.set(false);
        if (r.ok) {
          this.cuponMsg.set(r.mensaje || 'Cupón aplicado');
          this.cuponErr.set(false);
          this.reloadCart();
        } else {
          this.cuponMsg.set(r.mensaje || 'Cupón no válido');
          this.cuponErr.set(true);
        }
      },
      error: (e) => {
        this.cuponBusy.set(false);
        this.cuponMsg.set(e?.error?.error || e.message || 'No se pudo aplicar el cupón');
        this.cuponErr.set(true);
      },
    });
  }

  quitarCupon(): void {
    this.cuponBusy.set(true);
    this.cuponMsg.set(null);
    this.api.quitarCupon().subscribe({
      next: () => {
        this.cuponBusy.set(false);
        this.cuponCodigo = '';
        this.cuponMsg.set('Cupón quitado');
        this.cuponErr.set(false);
        this.reloadCart();
      },
      error: (e) => {
        this.cuponBusy.set(false);
        this.cuponMsg.set(e?.error?.error || e.message);
        this.cuponErr.set(true);
      },
    });
  }

  validateDocumento(block = false): boolean {
    const doc = (this.form.numeroDocumento || '').trim();
    if (environment.demoPrefill && /^0+$/.test(doc)) {
      this.docWarning.set(null);
      return true;
    }
    if (doc.length < 5) {
      const msg = 'El documento debe tener al menos 5 caracteres.';
      this.docWarning.set(msg);
      return !block;
    }
    if (/^0+$/.test(doc)) {
      const msg = 'El documento no puede ser solo ceros.';
      this.docWarning.set(msg);
      return false;
    }
    const looksRuc = doc.includes('-');
    const looksCi = /^\d{6,8}$/.test(doc.replace(/\D/g, '')) && doc.replace(/\D/g, '').length >= 6;
    if (!looksRuc && !looksCi) {
      this.docWarning.set('Verificá el RUC (con guión) o CI (6–8 dígitos).');
      return !block;
    }
    this.docWarning.set(null);
    return true;
  }

  private reloadCart(): void {
    this.api.getCart().subscribe({
      next: (c) => {
        this.cart.set(c);
        if (this.step() === 2 && this.form.delivery) {
          setTimeout(() => this.initMap(), 0);
        }
        this.tryAutoApplyPendingCupon(c);
      },
      error: (e) => this.block(e?.error?.error || e.message || 'Error carrito'),
    });
  }

  private tryAutoApplyPendingCupon(c: Cart): void {
    const code = this.pendingAutoCupon;
    if (!code) return;
    if (!c.items?.length) return;
    this.pendingAutoCupon = null;
    if (c.codigoCupon?.trim()) return;
    queueMicrotask(() => this.aplicarCupon());
  }

  private destroyMap(): void {
    if (this.map) {
      this.map.remove();
      this.map = null;
    }
    this.mapReady = false;
    this.marker = null;
    this.destroyPickupMap();
  }

  private destroyPickupMap(): void {
    if (this.pickupMap) {
      this.pickupMap.remove();
      this.pickupMap = null;
    }
    this.pickupMarker = null;
  }

  private initMap(): void {
    if (this.step() !== 2 || !this.form.delivery || !hasLeaflet()) return;
    const el = this.mapHost?.nativeElement;
    if (!el) return;

    if (this.map) {
      this.map.invalidateSize();
      return;
    }

    const lat = this.form.latitud ?? this.mapDefault.lat;
    const lng = this.form.longitud ?? this.mapDefault.lng;
    this.map = L.map(el, { scrollWheelZoom: true }).setView([lat, lng], 14);
    addOsmTiles(this.map);

    this.setMarker(lat, lng, false);
    this.map.on('click', (e: any) => this.setMarker(e.latlng.lat, e.latlng.lng, true));
    this.mapReady = true;
    setTimeout(() => this.map?.invalidateSize(), 100);
  }

  private initPickupMap(): void {
    if (this.step() !== 2 || this.form.delivery || !hasLeaflet()) return;
    const a = this.agencias().find((x) => x.id === this.selectedAgenciaId);
    if (!a || !isValidLatLng(a.latitud, a.longitud)) {
      this.destroyPickupMap();
      return;
    }
    const el = this.pickupMapHost?.nativeElement;
    if (!el) return;

    if (this.pickupMap) {
      this.pickupMap.setView([a.latitud!, a.longitud!], 15);
      if (this.pickupMarker) this.pickupMap.removeLayer(this.pickupMarker);
      this.pickupMarker = L.marker([a.latitud!, a.longitud!], { icon: pinIcon('store') })
        .addTo(this.pickupMap)
        .bindPopup(`<strong>${a.nombre}</strong>`);
      this.pickupMap.invalidateSize();
      return;
    }

    this.pickupMap = L.map(el, { scrollWheelZoom: false, dragging: true }).setView(
      [a.latitud!, a.longitud!],
      15,
    );
    addOsmTiles(this.pickupMap);
    this.pickupMarker = L.marker([a.latitud!, a.longitud!], { icon: pinIcon('store') })
      .addTo(this.pickupMap)
      .bindPopup(`<strong>${a.nombre}</strong>${a.direccion ? `<br/>${a.direccion}` : ''}`)
      .openPopup();
    setTimeout(() => this.pickupMap?.invalidateSize(), 100);
  }

  private setMarker(lat: number, lng: number, fromClick: boolean): void {
    if (fromClick) {
      this.form.latitud = lat;
      this.form.longitud = lng;
      this.mapPinned.set(true);
    }
    if (!this.map) return;
    if (this.marker) this.map.removeLayer(this.marker);
    this.marker = L.marker([lat, lng], { icon: pinIcon('delivery'), draggable: true })
      .addTo(this.map)
      .bindPopup(fromClick || this.mapPinned() ? 'Tu punto de entrega' : 'Tocá el mapa o arrastrá el pin');
    this.marker.on('dragend', () => {
      const p = this.marker.getLatLng();
      this.form.latitud = p.lat;
      this.form.longitud = p.lng;
      this.mapPinned.set(true);
      this.marker.bindPopup('Tu punto de entrega').openPopup();
    });
    if (fromClick) {
      this.map.panTo([lat, lng]);
      this.marker.openPopup();
    }
  }
}
