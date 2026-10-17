import {
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  ViewChild,
  inject,
  signal,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService, Agencia, HorarioAtencion } from '../core/api.service';
import {
  addOsmTiles,
  hasLeaflet,
  isValidLatLng,
  mapsDirectionsUrl,
  mapsPinUrl,
  pinIcon,
} from '../core/map.util';
import { DxIconComponent } from '../shared/dx-icon.component';

declare const L: any;

const DIAS = ['Domingo', 'Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado'];

@Component({
  standalone: true,
  imports: [RouterLink, DxIconComponent],
  template: `
    <section class="branches" aria-labelledby="br-title">
      <header>
        <div>
          <h1 id="br-title"><dx-icon name="map-pin" [size]="28" /> Sucursales</h1>
          <p class="lead">Elegí una sucursal en la lista o tocá el pin en el mapa.</p>
        </div>
        <a routerLink="/catalogo"><dx-icon name="grid" [size]="14" /> Volver al catálogo</a>
      </header>

      @if (error()) {
        <p class="dx-alert dx-alert--error" role="alert">{{ error() }}</p>
      }

      @if (loading()) {
        <p class="loading" role="status">Cargando sucursales…</p>
      } @else {
        <div class="layout" [class.no-map]="!hasMappable()">
          <ul class="list" role="list">
            @for (a of agencias(); track a.id) {
              <li [class.on]="selectedId() === a.id">
                <button type="button" class="card" (click)="selectBranch(a.id)">
                  <span class="card-top">
                    <span class="name">{{ a.nombre }}</span>
                    @if (hasCoords(a)) {
                      <span class="badge">En mapa</span>
                    }
                  </span>
                  @if (a.direccion) {
                    <span class="addr">{{ a.direccion }}</span>
                  }
                </button>
                @if (selectedId() === a.id) {
                  <div class="detail" role="region" [attr.aria-label]="'Detalle de ' + a.nombre">
                    <div class="actions">
                      @if (directionsUrl(a); as dir) {
                        <a class="dx-btn" [href]="dir" target="_blank" rel="noopener">
                          <dx-icon name="locate" [size]="16" /> Cómo llegar
                        </a>
                      }
                      @if (pinUrl(a); as pin) {
                        <a class="dx-btn dx-btn--ghost" [href]="pin" target="_blank" rel="noopener">
                          <dx-icon name="map-pin" [size]="16" /> Abrir en Maps
                        </a>
                      }
                      <button type="button" class="dx-btn dx-btn--ghost" (click)="toggleHours(a.id)">
                        {{ hoursOpenId() === a.id ? 'Ocultar horarios' : 'Ver horarios' }}
                      </button>
                    </div>
                    @if (hoursOpenId() === a.id) {
                      <div class="hours">
                        @if (hoursLoading()) {
                          <p class="mute">Cargando…</p>
                        } @else if (horarios().length) {
                          <ul>
                            @for (h of horarios(); track h.diaSemana + h.apertura) {
                              <li>
                                <strong>{{ diaLabel(h.diaSemana) }}</strong>
                                <span>{{ formatHours(h) }}</span>
                              </li>
                            }
                          </ul>
                        } @else {
                          <p class="mute">Sin horarios publicados.</p>
                        }
                      </div>
                    }
                  </div>
                }
              </li>
            } @empty {
              <li class="empty">No hay sucursales publicadas.</li>
            }
          </ul>

          @if (hasMappable()) {
            <div class="map-pane">
              <div class="map-tools">
                <p class="map-hint">
                  @if (selectedName(); as n) {
                    Seleccionada: <strong>{{ n }}</strong>
                  } @else {
                    Tocá un pin para ver la sucursal
                  }
                </p>
                <button
                  type="button"
                  class="dx-btn dx-btn--ghost"
                  (click)="fitAll()"
                  [disabled]="!mapReady()"
                >
                  Ver todas
                </button>
              </div>
              <div class="map-wrap">
                <div #mapHost class="map" role="application" aria-label="Mapa de sucursales"></div>
                @if (!mapReady()) {
                  <div class="map-overlay" role="status">
                    @if (mapError(); as err) {
                      <p>{{ err }}</p>
                      <button type="button" class="dx-btn" (click)="retryMap()">Reintentar mapa</button>
                    } @else {
                      <p>Cargando mapa…</p>
                    }
                  </div>
                }
              </div>
              @if (!selectedHasCoords()) {
                <p class="map-warn">Esta sucursal no tiene coordenadas; el mapa muestra las demás.</p>
              }
            </div>
          } @else {
            <p class="map-warn solo">Las sucursales aún no tienen ubicación en el mapa. Usá la dirección o contactá a la tienda.</p>
          }
        </div>
      }
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
        display: inline-flex;
        align-items: center;
        gap: 0.45rem;
      }
      .lead {
        margin: 0.35rem 0 0;
        color: var(--dx-mute);
      }
      header > a {
        color: var(--dx-red);
        font-weight: 700;
        text-decoration: none;
        display: inline-flex;
        align-items: center;
        gap: 0.35rem;
      }
      .layout {
        display: grid;
        gap: 1rem;
        grid-template-columns: minmax(260px, 380px) 1fr;
        align-items: start;
        min-height: 420px;
      }
      .layout.no-map {
        grid-template-columns: 1fr;
      }
      @media (max-width: 900px) {
        .layout {
          grid-template-columns: 1fr;
        }
        .map {
          height: 320px !important;
        }
      }
      .list {
        list-style: none;
        margin: 0;
        padding: 0;
        display: grid;
        gap: 0.55rem;
        max-height: min(70vh, 640px);
        overflow: auto;
      }
      .list > li {
        background: #fff;
        border: 1px solid var(--dx-line);
        border-radius: 14px;
        overflow: hidden;
        transition: border-color 0.15s ease, box-shadow 0.15s ease;
      }
      .list > li.on {
        border-color: #f0b4b4;
        box-shadow: 0 4px 16px rgba(198, 40, 40, 0.1);
      }
      .card {
        width: 100%;
        text-align: left;
        background: transparent;
        border: none;
        padding: 0.95rem 1.05rem;
        cursor: pointer;
        font: inherit;
        color: inherit;
        display: grid;
        gap: 0.3rem;
      }
      .card:hover .name {
        color: var(--dx-red);
      }
      .card-top {
        display: flex;
        justify-content: space-between;
        gap: 0.5rem;
        align-items: baseline;
      }
      .name {
        font-family: var(--dx-display);
        font-size: 1.05rem;
        font-weight: 700;
      }
      .badge {
        flex-shrink: 0;
        font-size: 0.68rem;
        font-weight: 800;
        text-transform: uppercase;
        letter-spacing: 0.04em;
        color: var(--dx-red);
        background: var(--dx-red-soft);
        padding: 0.15rem 0.45rem;
        border-radius: 999px;
      }
      .addr {
        color: var(--dx-ink-soft);
        font-size: 0.92rem;
      }
      .detail {
        border-top: 1px solid var(--dx-line);
        padding: 0.75rem 1.05rem 1rem;
        display: grid;
        gap: 0.65rem;
      }
      .actions {
        display: flex;
        flex-wrap: wrap;
        gap: 0.4rem;
      }
      .actions .dx-btn {
        text-decoration: none;
      }
      .hours ul {
        list-style: none;
        margin: 0;
        padding: 0;
        display: grid;
        gap: 0.3rem;
      }
      .hours li {
        display: flex;
        justify-content: space-between;
        gap: 1rem;
        font-size: 0.9rem;
      }
      .map-pane {
        position: sticky;
        top: 5.5rem;
        display: grid;
        gap: 0.55rem;
      }
      .map-tools {
        display: flex;
        justify-content: space-between;
        align-items: center;
        gap: 0.75rem;
        flex-wrap: wrap;
      }
      .map-hint {
        margin: 0;
        color: var(--dx-mute);
        font-size: 0.9rem;
      }
      .map-wrap {
        position: relative;
        min-height: 320px;
      }
      .map {
        height: min(70vh, 560px);
        min-height: 320px;
        border: 1px solid var(--dx-line);
        border-radius: 14px;
        background: #eef2f6;
        z-index: 0;
      }
      .map-overlay {
        position: absolute;
        inset: 0;
        display: grid;
        place-content: center;
        gap: 0.75rem;
        justify-items: center;
        border-radius: 14px;
        background: rgba(238, 242, 246, 0.92);
        color: var(--dx-ink-soft);
        font-weight: 650;
        z-index: 2;
        padding: 1rem;
        text-align: center;
      }
      .map-overlay p {
        margin: 0;
      }
      .map-warn {
        margin: 0;
        font-size: 0.88rem;
        color: #9a3412;
        background: #fff7ed;
        border: 1px solid #fed7aa;
        border-radius: 10px;
        padding: 0.55rem 0.75rem;
      }
      .map-warn.solo {
        grid-column: 1 / -1;
      }
      .mute {
        color: var(--dx-mute);
        margin: 0;
      }
      .empty {
        text-align: center;
        color: var(--dx-mute);
        padding: 1.5rem;
        background: #fff;
        border-radius: 14px;
        border: 1px dashed var(--dx-line);
      }
      .loading {
        color: var(--dx-mute);
        padding: 1.5rem 0;
      }
    `,
  ],
})
export class BranchesPageComponent implements OnInit, OnDestroy {
  private readonly api = inject(ApiService);

  /** Cuando Angular crea el div del mapa (después del loading), arrancamos Leaflet. */
  @ViewChild('mapHost')
  set mapHost(ref: ElementRef<HTMLDivElement> | undefined) {
    this.mapEl = ref?.nativeElement ?? null;
    if (this.mapEl && !this.map) {
      // Esperar un frame para que el layout tenga tamaño real.
      requestAnimationFrame(() => this.initMap(true));
    }
  }

  readonly agencias = signal<Agencia[]>([]);
  readonly horarios = signal<HorarioAtencion[]>([]);
  readonly selectedId = signal<number | null>(null);
  readonly hoursOpenId = signal<number | null>(null);
  readonly loading = signal(true);
  readonly hoursLoading = signal(false);
  readonly error = signal<string | null>(null);
  readonly mapReady = signal(false);
  readonly mapError = signal<string | null>(null);

  private map: any;
  private mapEl: HTMLDivElement | null = null;
  private markers = new Map<number, any>();
  private initAttempts = 0;

  ngOnInit(): void {
    this.api.getAgencias().subscribe({
      next: (rows) => {
        this.agencias.set(rows);
        this.loading.set(false);
        const first = rows.find((a) => this.hasCoords(a)) ?? rows[0];
        if (first) this.selectedId.set(first.id);
      },
      error: (e) => {
        this.loading.set(false);
        this.error.set(e?.error?.error || 'No se pudieron cargar las sucursales');
      },
    });
  }

  ngOnDestroy(): void {
    this.map?.remove();
    this.map = null;
    this.markers.clear();
  }

  hasCoords(a: Agencia): boolean {
    return isValidLatLng(a.latitud, a.longitud);
  }

  hasMappable(): boolean {
    return this.agencias().some((a) => this.hasCoords(a));
  }

  selectedName(): string | null {
    const id = this.selectedId();
    return this.agencias().find((a) => a.id === id)?.nombre ?? null;
  }

  pinUrl(a: Agencia): string | null {
    if (!this.hasCoords(a)) return null;
    return mapsPinUrl(a.latitud!, a.longitud!, a.nombre);
  }

  directionsUrl(a: Agencia): string | null {
    if (!this.hasCoords(a)) return null;
    return mapsDirectionsUrl(a.latitud!, a.longitud!);
  }

  selectBranch(id: number): void {
    this.selectedId.set(id);
    const a = this.agencias().find((x) => x.id === id);
    if (!a || !this.hasCoords(a) || !this.map) return;
    const marker = this.markers.get(id);
    this.map.setView([a.latitud!, a.longitud!], 15, { animate: true });
    marker?.openPopup();
  }

  fitAll(): void {
    if (!this.map) return;
    const pts = this.agencias().filter((a) => this.hasCoords(a));
    if (!pts.length) return;
    if (pts.length === 1) {
      this.map.setView([pts[0].latitud!, pts[0].longitud!], 14);
      return;
    }
    const bounds = L.latLngBounds(pts.map((a) => [a.latitud!, a.longitud!]));
    this.map.fitBounds(bounds.pad(0.2));
  }

  toggleHours(id: number): void {
    if (this.hoursOpenId() === id) {
      this.hoursOpenId.set(null);
      this.horarios.set([]);
      return;
    }
    this.hoursOpenId.set(id);
    this.hoursLoading.set(true);
    this.horarios.set([]);
    this.api.getAgenciaHorarios(id).subscribe({
      next: (h) => {
        this.horarios.set(h);
        this.hoursLoading.set(false);
      },
      error: () => {
        this.horarios.set([]);
        this.hoursLoading.set(false);
      },
    });
  }

  diaLabel(d: number): string {
    return DIAS[d] ?? `Día ${d}`;
  }

  formatHours(h: HorarioAtencion): string {
    let s = `${h.apertura}–${h.cierre}`;
    if (h.apertura2 && h.cierre2) s += ` y ${h.apertura2}–${h.cierre2}`;
    return s;
  }

  selectedHasCoords(): boolean {
    const id = this.selectedId();
    const a = this.agencias().find((x) => x.id === id);
    return !!a && this.hasCoords(a);
  }

  retryMap(): void {
    this.mapError.set(null);
    this.mapReady.set(false);
    if (this.map) {
      this.map.remove();
      this.map = null;
    }
    this.markers.clear();
    this.initAttempts = 0;
    this.initMap(true);
  }

  private initMap(fromHost = false): void {
    if (this.map || this.loading() || !this.hasMappable()) return;

    if (!hasLeaflet()) {
      this.mapError.set('No se cargó Leaflet. Recargá la página.');
      return;
    }

    const el = this.mapEl;
    if (!el) {
      if (fromHost || this.initAttempts > 8) return;
      this.initAttempts += 1;
      setTimeout(() => this.initMap(true), 80);
      return;
    }

    // Contenedor sin tamaño → Leaflet pinta en blanco.
    if (el.clientWidth < 40 || el.clientHeight < 40) {
      this.initAttempts += 1;
      if (this.initAttempts > 20) {
        this.mapError.set('El contenedor del mapa no tiene tamaño. Probá redimensionar la ventana.');
        return;
      }
      setTimeout(() => this.initMap(true), 100);
      return;
    }

    try {
      const pts = this.agencias().filter((a) => this.hasCoords(a));
      if (!pts.length) {
        this.mapError.set('Ninguna sucursal tiene coordenadas válidas.');
        return;
      }

      const first = pts[0];
      this.map = L.map(el, { scrollWheelZoom: true }).setView([first.latitud!, first.longitud!], 13);
      addOsmTiles(this.map);

      for (const a of pts) {
        const marker = L.marker([a.latitud!, a.longitud!], { icon: pinIcon('store') })
          .addTo(this.map)
          .bindPopup(
            `<strong>${escapeHtml(a.nombre)}</strong>${a.direccion ? `<br/><span>${escapeHtml(a.direccion)}</span>` : ''}`,
          );
        marker.on('click', () => this.selectedId.set(a.id));
        this.markers.set(a.id, marker);
      }

      this.fitAll();
      this.mapReady.set(true);
      this.mapError.set(null);
      setTimeout(() => this.map?.invalidateSize(), 50);
      setTimeout(() => this.map?.invalidateSize(), 300);

      const sel = this.selectedId();
      if (sel != null) {
        const a = this.agencias().find((x) => x.id === sel);
        if (a && this.hasCoords(a)) {
          this.map.setView([a.latitud!, a.longitud!], 15);
          this.markers.get(sel)?.openPopup();
        }
      }
    } catch (e) {
      console.error(e);
      this.mapError.set('No se pudo inicializar el mapa.');
      this.map = null;
    }
  }
}

function escapeHtml(s: string): string {
  return s
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}
