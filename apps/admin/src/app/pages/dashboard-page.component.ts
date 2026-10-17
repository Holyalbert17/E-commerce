import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { AdminApiService, AdminDashboard, AdminEmpresaConfig, AdminErpHealth } from '../core/admin-api.service';
import { AdminSessionService } from '../core/admin-session.service';
import { CrmBadgeService } from '../core/crm-badge.service';
import { ContactoBadgeService } from '../core/contacto-badge.service';
import { openStorefront } from '../core/storefront-url';

interface ChecklistItem {
  id: string;
  label: string;
  done: boolean;
  hint: string;
  link: string;
}

interface SyncCursorLite {
  entityKey: string;
  lastStatus?: string | null;
  lastRunUtc?: string | null;
  rowsRead?: number;
  rowsProcessed?: number;
}

@Component({
  standalone: true,
  imports: [RouterLink, CurrencyPipe, DatePipe, DecimalPipe],
  template: `
    <div class="dx-toolbar">
      <div>
        <p class="adm-greeting">{{ greeting() }}</p>
        <p class="dx-page-sub">
          <span class="badge badge-soft-info mr-1">{{ session.tenantSlug() }}</span>
          @if (session.idEmpresa(); as id) {
            <span class="badge badge-light mr-1">empresa #{{ id }}</span>
          }
          @if (empresa(); as e) {
            <span
              class="badge mr-1"
              [class.badge-soft-success]="e.modoIntegracion === 'Integrated'"
              [class.badge-soft-warning]="e.modoIntegracion !== 'Integrated'"
            >
              {{ e.modoIntegracion === 'Integrated' ? 'ERP integrado' : e.modoIntegracion }}
            </span>
          }
          — lo que cambiás acá se refleja en la vitrina.
        </p>
        @if (checklist().length) {
          <div class="adm-progress" [attr.aria-label]="'Checklist ' + checklistPct() + '%'">
            <span [style.width.%]="checklistPct()"></span>
          </div>
          <small class="text-muted">Checklist tienda: {{ checklistDone() }}/{{ checklist().length }} listo</small>
        }
      </div>
      <div class="dx-filters">
        <button type="button" class="btn btn-success btn-sm" (click)="openShop()">
          <i class="fas fa-store mr-1"></i> Abrir tienda
        </button>
        <a routerLink="/sync" class="btn btn-primary btn-sm">
          <i class="fas fa-exchange-alt mr-1"></i> Estado ETL
        </a>
        <button type="button" class="btn btn-outline-secondary btn-sm" (click)="load()" [disabled]="loading()">
          <i class="fas fa-sync-alt mr-1" [class.fa-spin]="loading()"></i> Refrescar
        </button>
      </div>
    </div>

    @if (error()) {
      <div class="alert alert-danger alert-dismissible">
        <button type="button" class="close" (click)="error.set(null)"><span>&times;</span></button>
        {{ error() }}
      </div>
    }

    @if (erpHealth() || empresa()?.modoIntegracion === 'Integrated') {
      <div class="callout dx-ops-strip"
           [class.callout-success]="erpHealth()?.hasConnection && erpHealth()?.writeBackEffective"
           [class.callout-info]="!erpHealth() || (erpHealth()?.hasConnection && !erpHealth()?.writeBackEffective)"
           [class.callout-warning]="erpHealth() && !erpHealth()?.hasConnection">
        <h5><i class="fas fa-link mr-1"></i> ERP · dual backoffice</h5>
        <p class="mb-2">
          Podés cargar catálogo, precios y clientes acá o en el backoffice ERP (Site → ECommerce).
          Stock espejo del depósito
          <strong>#{{ erpHealth()?.idDeposito ?? empresa()?.idDeposito }}</strong>
          @if (erpHealth()?.syncStockContinuo) {
            · sync automático cada <strong>{{ erpHealth()?.syncStockIntervaloMinutos }} min</strong>
          }
          · pedidos
          @if (erpHealth()?.writeBackEffective && erpHealth()?.migrarPedidosAlGenerar) {
            <strong>caen solos en ERP</strong>
          } @else {
            <span class="text-warning">sin write-back automático</span>
          }
          · CS <code>{{ erpHealth()?.connectionSource || '—' }}</code>
        </p>
        <div class="dx-ops-meta d-flex flex-wrap align-items-center">
          <span class="mr-3">
            Stock:
            @if (erpHealth(); as eh) {
              <span class="badge" [class.badge-success]="eh.lastStockStatus === 'ok'" [class.badge-danger]="eh.lastStockStatus === 'error'">
                {{ eh.lastStockStatus || '—' }}
              </span>
              @if (eh.lastStockSyncUtc) {
                <small class="text-muted ml-1">{{ eh.lastStockSyncUtc | date: 'short' }}</small>
              }
              <small class="text-muted ml-1">· {{ eh.lastStockRows }} filas</small>
            } @else {
              @if (stockCursor(); as sc) {
                <span class="badge" [class.badge-success]="sc.lastStatus === 'ok'" [class.badge-danger]="sc.lastStatus === 'error'">
                  {{ sc.lastStatus || '—' }}
                </span>
              } @else {
                <span class="text-muted">sin corridas</span>
              }
            }
          </span>
          <span class="mr-3">
            Pendientes ERP:
            <strong>{{ erpHealth()?.pendingOrdersWriteBack ?? data()?.pedidosPendientesMigracion ?? 0 }}</strong>
          </span>
          <a routerLink="/configuracion" class="btn btn-xs btn-outline-primary mr-1">Config ERP</a>
          <a routerLink="/sync" class="btn btn-xs btn-outline-secondary mr-1">Sync</a>
          <button type="button" class="btn btn-xs btn-outline-secondary" (click)="pullStock()" [disabled]="stockBusy()"
                  title="Refresca cursors de ecom-etl (no hace pull)">
            {{ stockBusy() ? '…' : 'Ver estado stock' }}
          </button>
        </div>
      </div>
    }

    @if (data(); as d) {
      <div class="row dx-kpi-row">
        <div class="col-lg-3 col-md-6">
          <a routerLink="/catalogo" class="info-box text-decoration-none">
            <span class="info-box-icon bg-info elevation-1"><i class="fas fa-cubes"></i></span>
            <div class="info-box-content">
              <span class="info-box-text">En vitrina</span>
              <span class="info-box-number">{{ d.articulosEcommerce | number }}</span>
              <span class="progress-description">ActivoEcommerce ON</span>
            </div>
          </a>
        </div>
        <div class="col-lg-3 col-md-6">
          <a routerLink="/catalogo" [queryParams]="{ estado: 'agotado' }" class="info-box text-decoration-none">
            <span class="info-box-icon bg-danger elevation-1"><i class="fas fa-box-open"></i></span>
            <div class="info-box-content">
              <span class="info-box-text">Agotados</span>
              <span class="info-box-number">{{ d.articulosAgotados | number }}</span>
              <span class="progress-description">Stock libre = 0</span>
            </div>
          </a>
        </div>
        <div class="col-lg-3 col-md-6">
          <a routerLink="/pedidos" class="info-box text-decoration-none">
            <span class="info-box-icon bg-success elevation-1"><i class="fas fa-shopping-bag"></i></span>
            <div class="info-box-content">
              <span class="info-box-text">Pedidos hoy</span>
              <span class="info-box-number">{{ d.pedidosHoy }}</span>
              <span class="progress-description">
                {{ d.montoPedidosHoy | currency: 'PYG':'symbol-narrow':'1.0-0' }}
              </span>
            </div>
          </a>
        </div>
        <div class="col-lg-3 col-md-6">
          <a routerLink="/pedidos" [queryParams]="{ migrado: 'false' }" class="info-box text-decoration-none">
            <span class="info-box-icon bg-warning elevation-1"><i class="fas fa-exchange-alt"></i></span>
            <div class="info-box-content">
              <span class="info-box-text">Pendientes ETL</span>
              <span class="info-box-number">{{ d.pedidosPendientesMigracion }}</span>
              <span class="progress-description">
                @if (erpHealth()?.writeBackEffective) { Write-back ERP activo }
                @else { Pendientes de caer en ERP }
              </span>
            </div>
          </a>
        </div>
      </div>

      @if (attention().length) {
        <div class="dx-attention">
          @for (a of attention(); track a.id) {
            <a class="dx-attention-item" [class.is-danger]="a.tone === 'danger'" [class.is-info]="a.tone === 'info'"
               [routerLink]="a.link" [queryParams]="a.query">
              <i [class]="a.icon"></i>
              <div>
                <strong>{{ a.count }}</strong>
                <span>{{ a.label }}</span>
              </div>
            </a>
          }
        </div>
      }

      <div class="row">
        <div class="col-lg-4">
          <div class="card card-outline card-success dx-card-tight">
            <div class="card-header">
              <h3 class="card-title"><i class="fas fa-bolt mr-1"></i> Acciones del día</h3>
            </div>
            <div class="card-body p-0">
              <div class="list-group list-group-flush dx-action-list">
                <a routerLink="/catalogo/activar" class="list-group-item list-group-item-action">
                  <i class="fas fa-toggle-on text-warning"></i>
                  <span>Publicar / ocultar productos</span>
                </a>
                <a routerLink="/listas-precios" class="list-group-item list-group-item-action">
                  <i class="fas fa-tags text-info"></i>
                  <span>Revisar precios vitrina</span>
                </a>
                <a routerLink="/sync" class="list-group-item list-group-item-action">
                  <i class="fas fa-warehouse text-primary"></i>
                  <span>Ver estado stock (ecom-etl)</span>
                </a>
                <a routerLink="/branding" class="list-group-item list-group-item-action">
                  <i class="fas fa-palette text-teal"></i>
                  <span>Logo y colores</span>
                </a>
                <a routerLink="/promociones" class="list-group-item list-group-item-action">
                  <i class="fas fa-bullhorn text-danger"></i>
                  <span>Grupos / flyers home</span>
                </a>
                <a routerLink="/crm" class="list-group-item list-group-item-action">
                  <i class="fas fa-headset text-success"></i>
                  <span>
                    Atender mensajes de clientes
                    @if (crm.unread() > 0) {
                      <span class="badge badge-danger ml-1">{{ crm.unread() > 99 ? '99+' : crm.unread() }}</span>
                    }
                  </span>
                </a>
                <button type="button" class="list-group-item list-group-item-action text-left border-0"
                        (click)="openShop()">
                  <i class="fas fa-external-link-alt text-success"></i>
                  <span>Ver cómo quedó la tienda</span>
                </button>
              </div>
            </div>
          </div>
        </div>

        <div class="col-lg-8">
          <div class="card card-outline card-primary dx-card-tight">
            <div class="card-header">
              <h3 class="card-title">Checklist para que el cliente vea la tienda</h3>
            </div>
            <div class="card-body p-0">
              <ul class="dx-checklist">
                @for (c of checklist(); track c.id) {
                  <li>
                    <span class="dx-check-icon">
                      @if (c.done) {
                        <i class="fas fa-check-circle text-success"></i>
                      } @else {
                        <i class="far fa-circle text-muted"></i>
                      }
                    </span>
                    <div class="flex-grow-1">
                      <strong [class.text-muted]="c.done">{{ c.label }}</strong>
                      <div class="small text-muted">{{ c.hint }}</div>
                    </div>
                    <a class="btn btn-xs btn-outline-primary" [routerLink]="c.link">Ir</a>
                  </li>
                }
              </ul>
            </div>
          </div>

          <div class="card card-outline card-secondary dx-card-tight mt-3">
            <div class="card-header"><h3 class="card-title">Atajos</h3></div>
            <div class="card-body dx-shortcuts">
              <a routerLink="/catalogo" class="btn btn-app"><i class="fas fa-cubes"></i> Catálogo</a>
              <a routerLink="/catalogo/activar" class="btn btn-app"><i class="fas fa-toggle-on"></i> Activar</a>
              <a routerLink="/sync" class="btn btn-app"><i class="fas fa-exchange-alt"></i> Estado ETL</a>
              <a routerLink="/pedidos" class="btn btn-app"><i class="fas fa-shopping-bag"></i> Pedidos</a>
              <a routerLink="/clientes" class="btn btn-app"><i class="fas fa-user-friends"></i> Clientes</a>
              <a routerLink="/manager" class="btn btn-app"><i class="fas fa-user-shield"></i> Seguridad</a>
              <a routerLink="/configuracion" class="btn btn-app"><i class="fas fa-cog"></i> Config</a>
            </div>
          </div>
        </div>
      </div>
    } @else if (loading()) {
      <div class="dx-empty py-5">
        <i class="fas fa-spinner fa-spin"></i>
        <p>Cargando…</p>
      </div>
    }
  `,
})
export class DashboardPageComponent implements OnInit {
  readonly session = inject(AdminSessionService);
  readonly crm = inject(CrmBadgeService);
  readonly contacto = inject(ContactoBadgeService);
  private readonly api = inject(AdminApiService);

  readonly data = signal<AdminDashboard | null>(null);
  readonly empresa = signal<AdminEmpresaConfig | null>(null);
  readonly erpHealth = signal<AdminErpHealth | null>(null);
  readonly stockCursor = signal<SyncCursorLite | null>(null);
  readonly error = signal<string | null>(null);
  readonly loading = signal(false);
  readonly stockBusy = signal(false);
  readonly checklist = signal<ChecklistItem[]>([]);

  readonly attention = computed(() => {
    const d = this.data();
    const items: { id: string; count: number; label: string; link: string; query?: Record<string, string>; icon: string; tone: 'warn' | 'danger' | 'info' }[] = [];
    if ((d?.pedidosPendientesMigracion ?? 0) > 0) {
      items.push({
        id: 'erp',
        count: d!.pedidosPendientesMigracion,
        label: 'pedidos pendientes de ERP',
        link: '/pedidos',
        query: { migrado: 'false' },
        icon: 'fas fa-exchange-alt',
        tone: 'warn',
      });
    }
    if ((d?.articulosAgotados ?? 0) > 0) {
      items.push({
        id: 'stock',
        count: d!.articulosAgotados,
        label: 'artículos agotados en vitrina',
        link: '/catalogo',
        query: { estado: 'agotado' },
        icon: 'fas fa-box-open',
        tone: 'danger',
      });
    }
    if (this.crm.unread() > 0) {
      items.push({
        id: 'crm',
        count: this.crm.unread(),
        label: 'chats sin leer',
        link: '/crm',
        icon: 'fas fa-headset',
        tone: 'info',
      });
    }
    if (this.contacto.unread() > 0) {
      items.push({
        id: 'contacto',
        count: this.contacto.unread(),
        label: 'formularios de contacto',
        link: '/contacto',
        icon: 'fas fa-envelope',
        tone: 'info',
      });
    }
    return items;
  });

  ngOnInit(): void {
    this.load();
  }

  openShop(): void {
    openStorefront(this.session.tenantSlug());
  }

  pullStock(): void {
    this.error.set(null);
    this.stockBusy.set(true);
    // Inbound: ecom-etl. Solo refrescar cursors locales.
    this.api.syncStatus().subscribe({
      next: (s) => {
        const cursors = (s?.cursors ?? []) as SyncCursorLite[];
        this.stockCursor.set(cursors.find((c) => c.entityKey === 'stock') ?? null);
        this.stockBusy.set(false);
        this.api.getErpHealth().subscribe({
          next: (h) => this.erpHealth.set(h),
          error: () => undefined,
        });
      },
      error: (e) => {
        this.stockBusy.set(false);
        this.error.set(
          (e?.error?.error || e.message) +
            ' · Stock lo sincroniza ecom-etl (no la API).',
        );
      },
    });
  }

  greeting(): string {
    const h = new Date().getHours();
    const name = this.session.displayName()?.split(' ')[0] || 'equipo';
    const hi = h < 12 ? 'Buenos días' : h < 19 ? 'Buenas tardes' : 'Buenas noches';
    return `${hi}, ${name}`;
  }

  checklistDone(): number {
    return this.checklist().filter((c) => c.done).length;
  }

  checklistPct(): number {
    const n = this.checklist().length;
    if (!n) return 0;
    return Math.round((this.checklistDone() / n) * 100);
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.dashboard().subscribe({
      next: (d) => {
        this.data.set(d);
        this.checklist.set([
          {
            id: 'sync',
            label: 'Datos traídos del ERP (o catálogo cargado)',
            done: false,
            hint: 'El inbound lo hace ecom-etl. Acá solo ves cursors y write-back de pedidos.',
            link: '/sync',
          },
          {
            id: 'vitrina',
            label: 'Productos publicados en vitrina',
            done: d.articulosEcommerce > 0,
            hint: d.articulosEcommerce > 0
              ? `${d.articulosEcommerce} visibles para el comprador.`
              : 'Sin ActivoEcommerce ON el catálogo público queda vacío.',
            link: '/catalogo/activar',
          },
          {
            id: 'precios',
            label: 'Lista e-commerce con precios',
            done: d.articulosEcommerce > 0,
            hint: 'Revisá lista “ecommerce” autorizada y precios por embalaje.',
            link: '/listas-precios',
          },
          {
            id: 'branding',
            label: 'Marca visible (logo / colores)',
            done: false,
            hint: 'El comprador ve logo, colores y footer al instante.',
            link: '/branding',
          },
          {
            id: 'envios',
            label: 'Delivery / pickup configurados',
            done: false,
            hint: 'Montos mínimos, depósito y tarifas afectan el checkout.',
            link: '/configuracion',
          },
        ]);
        this.loading.set(false);
        this.enrichChecklist();
      },
      error: (e) => {
        this.error.set(e?.error?.error || e.message);
        this.loading.set(false);
      },
    });
  }

  private enrichChecklist(): void {
    this.api.getErpHealth().subscribe({
      next: (h) => this.erpHealth.set(h),
      error: () => this.erpHealth.set(null),
    });
    this.api.syncStatus().subscribe({
      next: (s) => {
        const cursors = (s?.cursors ?? []) as SyncCursorLite[];
        const stock = cursors.find((c) => c.entityKey === 'stock') ?? null;
        this.stockCursor.set(stock);
        const anyOk = cursors.some((c) => c.lastStatus === 'ok');
        this.checklist.update((items) =>
          items.map((i) =>
            i.id === 'sync'
              ? {
                  ...i,
                  done: anyOk || (this.data()?.articulosEcommerce ?? 0) > 0,
                  hint: anyOk
                    ? 'ecom-etl ya sincronizó OK. Revisá cursors si necesitás más detalle.'
                    : i.hint,
                }
              : i,
          ),
        );
      },
      error: () => undefined,
    });
    this.api.getBranding().subscribe({
      next: (b) => {
        const hasBrand = !!(b?.logoUrl || b?.nombreTienda || b?.colorPrimario);
        this.checklist.update((items) =>
          items.map((i) => (i.id === 'branding' ? { ...i, done: hasBrand } : i)),
        );
      },
      error: () => undefined,
    });
    this.api.getEmpresaConfig().subscribe({
      next: (c) => {
        this.empresa.set(c);
        const ok = !!(c?.realizaDelivery || c?.realizaPickup);
        this.checklist.update((items) =>
          items.map((i) => (i.id === 'envios' ? { ...i, done: ok } : i)),
        );
      },
      error: () => undefined,
    });
  }
}
