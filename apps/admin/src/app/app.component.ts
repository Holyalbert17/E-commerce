import { Component, HostListener, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgClass } from '@angular/common';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs/operators';
import { AdminSessionService } from './core/admin-session.service';
import { CrmBadgeService } from './core/crm-badge.service';
import { ContactoBadgeService } from './core/contacto-badge.service';
import { openStorefront } from './core/storefront-url';

const MOBILE_BREAKPOINT = 992;

interface JumpItem {
  label: string;
  path: string;
  icon: string;
  keywords: string;
  permissions: string[];
}

const JUMP_ITEMS: JumpItem[] = [
  { label: 'Dashboard', path: '/', icon: 'fa-tachometer-alt', keywords: 'inicio home', permissions: ['Site_App_Ecommerce', 'manager:all'] },
  { label: 'Catálogo', path: '/catalogo', icon: 'fa-cubes', keywords: 'productos articulos', permissions: ['Site_App_Ecommerce_Productos', 'manager:all'] },
  { label: 'Activar productos', path: '/catalogo/activar', icon: 'fa-toggle-on', keywords: 'vitrina publicar', permissions: ['Site_App_Ecommerce_Productos_ActivarInactivar', 'manager:all'] },
  { label: 'Estado ETL', path: '/sync', icon: 'fa-exchange-alt', keywords: 'stock sincronizar etl cursors ecom-etl', permissions: ['Site_App_Ecommerce', 'Site_App_Ecommerce_Productos', 'manager:tenants', 'manager:all'] },
  { label: 'Pedidos', path: '/pedidos', icon: 'fa-shopping-bag', keywords: 'ordenes erp facturar', permissions: ['Site_App_Ecommerce_Pedidos', 'Site_App_Ecommerce_Pedidos_View', 'manager:all'] },
  { label: 'Carritos', path: '/carritos', icon: 'fa-shopping-cart', keywords: 'abandonados checkout', permissions: ['Site_App_Ecommerce_CarritoCompras', 'manager:all'] },
  { label: 'Clientes', path: '/clientes', icon: 'fa-user-friends', keywords: 'mayorista', permissions: ['Site_App_Ecommerce_Clientes', 'manager:all'] },
  { label: 'Choferes', path: '/choferes', icon: 'fa-truck', keywords: 'delivery repartidor', permissions: ['Site_App_Ecommerce_Pedidos', 'Site_App_Ecommerce_Pedidos_View', 'manager:all'] },
  { label: 'Listas de precios', path: '/listas-precios', icon: 'fa-tags', keywords: 'precios', permissions: ['Site_App_Ecommerce_ListasPrecios', 'manager:all'] },
  { label: 'Tarifas delivery', path: '/tarifas', icon: 'fa-map-marked-alt', keywords: 'envio barrio ciudad', permissions: ['Site_App_Ecommerce_TarifaDelivery', 'manager:all'] },
  { label: 'Unidades', path: '/unidades', icon: 'fa-balance-scale', keywords: 'medida embalaje', permissions: ['Site_App_Ecommerce_Productos', 'manager:all'] },
  { label: 'Branding / diseño', path: '/branding', icon: 'fa-palette', keywords: 'logo colores', permissions: ['Site_App_Ecommerce_Empresa', 'manager:all'] },
  { label: 'Personalización', path: '/personalizacion', icon: 'fa-sliders-h', keywords: 'tema layout home', permissions: ['Site_App_Ecommerce_Personalizacion', 'Site_App_Ecommerce_Empresa', 'Site_App_Ecommerce', 'manager:all'] },
  { label: 'Contenido CMS', path: '/contenido', icon: 'fa-newspaper', keywords: 'cms bloques', permissions: ['Site_App_Ecommerce_Personalizacion', 'Site_App_Ecommerce_Empresa', 'Site_App_Ecommerce', 'manager:all'] },
  { label: 'Promociones', path: '/promociones', icon: 'fa-bullhorn', keywords: 'flyers ofertas publicidad', permissions: ['Site_App_Ecommerce_GrupoArticulo', 'manager:all'] },
  { label: 'Marketplace', path: '/marketplace', icon: 'fa-store', keywords: 'reseñas cupones devoluciones', permissions: ['Site_App_Ecommerce_Resenas', 'Site_App_Ecommerce_Cupones', 'Site_App_Ecommerce_Devoluciones', 'manager:all'] },
  { label: 'Configuración', path: '/configuracion', icon: 'fa-cog', keywords: 'empresa agencias erp stock', permissions: ['Site_App_Ecommerce_Empresa', 'Site_App_Ecommerce_Empresa_Agencia', 'Site_App_Ecommerce_Empresa_HorarioAtencion', 'manager:all'] },
  { label: 'Atención al cliente', path: '/crm', icon: 'fa-headset', keywords: 'mensajes inbox crm chat soporte', permissions: ['Site_App_Ecommerce_Chat', 'manager:all'] },
  { label: 'Contacto', path: '/contacto', icon: 'fa-envelope', keywords: 'formulario mensajes newsletter', permissions: ['Site_App_Ecommerce', 'manager:all'] },
  { label: 'Reportes', path: '/reportes', icon: 'fa-chart-line', keywords: 'ventas', permissions: ['Site_App_Ecommerce', 'Site_App_Ecommerce_Pedidos', 'Site_App_Ecommerce_Pedidos_View', 'Site_App_Ecommerce_CarritoCompras', 'manager:all'] },
  { label: 'Seguridad', path: '/manager', icon: 'fa-user-shield', keywords: 'roles usuarios', permissions: ['manager:roles', 'manager:usuarios', 'manager:funcionalidades', 'manager:all'] },
];

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, FormsModule, NgClass],
  template: `
    @if (isLoginRoute) {
      <router-outlet />
    } @else {
      <div class="wrapper">
        <nav class="main-header navbar navbar-expand navbar-white navbar-light border-bottom">
          <ul class="navbar-nav">
            <li class="nav-item">
              <a class="nav-link" href="#" (click)="toggleSidebar($event)" role="button"
                 aria-label="Abrir o cerrar menú">
                <i class="fas fa-bars"></i>
              </a>
            </li>
            <li class="nav-item d-none d-md-flex align-items-center pl-2">
              <span class="adm-pill">
                <i class="fas fa-store"></i>
                {{ session.tenantSlug() }}
                @if (session.idEmpresa(); as id) {
                  <span>· #{{ id }}</span>
                }
              </span>
            </li>
          </ul>

          @if (session.isLoggedIn()) {
            <div class="mx-auto adm-jump">
              <i class="fas fa-search"></i>
              <input
                type="search"
                placeholder="Ir a… catálogo, sync, branding (Ctrl+/)"
                [(ngModel)]="jumpQ"
                (ngModelChange)="onJumpQuery($event)"
                (keydown)="onJumpKey($event)"
                aria-label="Navegación rápida"
              />
              @if (jumpOpen() && jumpHits().length) {
                <div class="adm-jump-menu" role="listbox">
                  @for (h of jumpHits(); track h.path; let i = $index) {
                    <button
                      type="button"
                      class="adm-jump-item"
                      [class.on]="i === jumpIndex()"
                      role="option"
                      (click)="goJump(h.path)"
                    >
                      <i class="fas" [ngClass]="h.icon"></i>
                      <span>{{ h.label }}</span>
                    </button>
                  }
                </div>
              }
            </div>
          }

          <ul class="navbar-nav ml-auto">
            @if (session.isLoggedIn()) {
              <li class="nav-item dropdown adm-notifications">
                <button
                  type="button"
                  class="nav-link adm-notification-toggle"
                  (click)="toggleNotifications($event)"
                  [attr.aria-expanded]="notificationsOpen()"
                  aria-controls="adm-notification-menu"
                  title="Pendientes de atención"
                >
                  <i class="far fa-bell"></i>
                  @if (pendingNotifications() > 0) {
                    <span class="adm-notification-count">
                      {{ pendingNotifications() > 99 ? '99+' : pendingNotifications() }}
                    </span>
                  }
                </button>
                @if (notificationsOpen()) {
                  <div id="adm-notification-menu" class="adm-notification-menu" role="menu" (click)="$event.stopPropagation()">
                    <div class="adm-notification-head">
                      <strong>Pendientes</strong>
                      @if (pendingNotifications() > 0) {
                        <span>{{ pendingNotifications() }} por revisar</span>
                      } @else {
                        <span>Todo al día</span>
                      }
                    </div>
                    @if (session.can('Site_App_Ecommerce_Chat', 'manager:all')) {
                      <button type="button" class="adm-notification-item" (click)="goNotification('/crm')">
                        <i class="fas fa-headset text-success"></i>
                        <span>
                          <strong>Atención al cliente</strong>
                          <small>
                            {{ crm.unread() > 0 ? crm.unread() + ' mensaje(s) sin leer' : 'No hay mensajes pendientes' }}
                          </small>
                        </span>
                        @if (crm.unread() > 0) { <b>{{ crm.unread() > 99 ? '99+' : crm.unread() }}</b> }
                      </button>
                    }
                    @if (session.can('Site_App_Ecommerce', 'manager:all')) {
                      <button type="button" class="adm-notification-item" (click)="goNotification('/contacto')">
                        <i class="far fa-envelope text-primary"></i>
                        <span>
                          <strong>Contacto</strong>
                          <small>
                            {{ contacto.unread() > 0 ? contacto.unread() + ' formulario(s) sin leer' : 'No hay formularios pendientes' }}
                          </small>
                        </span>
                        @if (contacto.unread() > 0) { <b>{{ contacto.unread() > 99 ? '99+' : contacto.unread() }}</b> }
                      </button>
                    }
                  </div>
                }
              </li>
              <li class="nav-item">
                <a class="nav-link text-success" href="#" (click)="openShop($event)" title="Abrir storefront">
                  <i class="fas fa-external-link-alt"></i>
                  <span class="d-none d-md-inline"> Tienda</span>
                </a>
              </li>
              @if (session.can('Site_App_Ecommerce', 'Site_App_Ecommerce_Productos', 'manager:tenants', 'manager:all')) {
                <li class="nav-item">
                  <a class="nav-link" routerLink="/sync" title="Estado de sincronización ecom-etl">
                    <i class="fas fa-exchange-alt"></i>
                    <span class="d-none d-md-inline"> ETL</span>
                  </a>
                </li>
              }
              <li class="nav-item d-none d-md-inline-block">
                <span class="nav-link">
                  <i class="far fa-user mr-1"></i>{{ session.displayName() }}
                </span>
              </li>
              <li class="nav-item">
                <a class="nav-link" href="#" (click)="logout($event)">
                  <i class="fas fa-sign-out-alt"></i>
                  <span class="d-none d-sm-inline"> Salir</span>
                </a>
              </li>
            } @else {
              <li class="nav-item">
                <a class="nav-link" routerLink="/login">Login</a>
              </li>
            }
          </ul>
        </nav>

        <aside class="main-sidebar sidebar-dark-primary elevation-4">
          <a routerLink="/" class="brand-link" (click)="closeSidebarOnNavigate()">
            <span class="brand-mark brand-image mr-2">{{ brandMark() }}</span>
            <span class="brand-text">{{ brandTitle() }}</span>
          </a>

          <div class="sidebar">
            <div class="user-panel mt-3 pb-3 mb-3 d-flex">
              <div class="adm-avatar">{{ sessionInitials() }}</div>
              <div class="info">
                <a href="javascript:void(0)" class="d-block">
                  {{ session.isLoggedIn() ? session.displayName() : 'Sin sesión' }}
                </a>
                <small>{{ session.tenantSlug() }}</small>
              </div>
            </div>

            <nav class="mt-2">
              <ul class="nav nav-pills nav-sidebar flex-column nav-child-indent" role="menu">
                @if (session.isLoggedIn()) {
                  <li class="nav-header">E-COMMERCE (ERP)</li>
                  @if (session.can('Site_App_Ecommerce', 'manager:all')) {
                    <li class="nav-item">
                      <a routerLink="/" class="nav-link" routerLinkActive="active"
                         [routerLinkActiveOptions]="{ exact: true }" (click)="closeSidebarOnNavigate()">
                        <i class="nav-icon fas fa-tachometer-alt"></i>
                        <p>Dashboard</p>
                      </a>
                    </li>
                  }

                  @if (session.can('Site_App_Ecommerce_Empresa', 'Site_App_Ecommerce_Empresa_Agencia', 'Site_App_Ecommerce_Empresa_HorarioAtencion', 'Site_App_Ecommerce_Personalizacion', 'Site_App_Ecommerce', 'manager:all')) {
                    <li class="nav-item has-treeview" [class.menu-open]="empresaOpen()">
                      <a href="#" class="nav-link" (click)="toggleEmpresa($event)"
                         [class.active]="empresaOpen()">
                        <i class="nav-icon fas fa-building"></i>
                        <p>Empresa <i class="right fas fa-angle-left"></i></p>
                      </a>
                      <ul class="nav nav-treeview">
                        @if (session.can('Site_App_Ecommerce_Empresa', 'Site_App_Ecommerce_Empresa_Agencia', 'Site_App_Ecommerce_Empresa_HorarioAtencion', 'manager:all')) {
                          <li class="nav-item">
                            <a routerLink="/configuracion" class="nav-link" routerLinkActive="active"
                               (click)="closeSidebarOnNavigate()">
                              <i class="far fa-circle nav-icon"></i><p title="Empresa, agencias y horarios">Empresa y agencias</p>
                            </a>
                          </li>
                        }
                        @if (session.can('Site_App_Ecommerce_Empresa', 'manager:all')) {
                          <li class="nav-item">
                            <a routerLink="/branding" class="nav-link" routerLinkActive="active"
                               (click)="closeSidebarOnNavigate()">
                              <i class="far fa-circle nav-icon"></i><p>Diseño / logo</p>
                            </a>
                          </li>
                        }
                        @if (session.can('Site_App_Ecommerce_Personalizacion', 'Site_App_Ecommerce_Empresa', 'Site_App_Ecommerce', 'manager:all')) {
                          <li class="nav-item">
                            <a routerLink="/personalizacion" class="nav-link" routerLinkActive="active"
                              (click)="closeSidebarOnNavigate()">
                              <i class="far fa-circle nav-icon"></i><p>Personalización</p>
                            </a>
                          </li>
                          <li class="nav-item">
                            <a routerLink="/contenido" class="nav-link" routerLinkActive="active"
                              (click)="closeSidebarOnNavigate()">
                              <i class="far fa-circle nav-icon"></i><p>Contenido / CMS</p>
                            </a>
                          </li>
                        }
                      </ul>
                    </li>
                  }

                  @if (session.can('Site_App_Ecommerce_Productos', 'manager:all')) {
                    <li class="nav-item has-treeview" [class.menu-open]="productosOpen()">
                      <a href="#" class="nav-link" (click)="toggleProductos($event)"
                         [class.active]="productosOpen()">
                        <i class="nav-icon fas fa-cubes"></i>
                        <p>Productos <i class="right fas fa-angle-left"></i></p>
                      </a>
                      <ul class="nav nav-treeview">
                        <li class="nav-item">
                          <a routerLink="/parametros-producto" class="nav-link" routerLinkActive="active"
                             (click)="closeSidebarOnNavigate()">
                            <i class="far fa-circle nav-icon"></i><p>Parámetros</p>
                          </a>
                        </li>
                        <li class="nav-item">
                          <a routerLink="/catalogo" class="nav-link" routerLinkActive="active"
                             [routerLinkActiveOptions]="{ exact: true }" (click)="closeSidebarOnNavigate()">
                            <i class="far fa-circle nav-icon"></i><p>Todos los artículos</p>
                          </a>
                        </li>
                        @if (session.can('Site_App_Ecommerce_Productos_Edit', 'manager:all')) {
                          <li class="nav-item">
                            <a routerLink="/catalogo/nuevo" class="nav-link" routerLinkActive="active"
                               (click)="closeSidebarOnNavigate()">
                              <i class="far fa-circle nav-icon"></i><p>Nuevo artículo</p>
                            </a>
                          </li>
                        }
                        @if (session.can('Site_App_Ecommerce_Productos_ActivarInactivar', 'manager:all')) {
                          <li class="nav-item">
                            <a routerLink="/catalogo/activar" class="nav-link" routerLinkActive="active"
                               (click)="closeSidebarOnNavigate()">
                              <i class="far fa-circle nav-icon"></i><p>Activar / inactivar</p>
                            </a>
                          </li>
                        }
                        @if (session.can('Site_App_Ecommerce_GrupoArticulo', 'manager:all')) {
                          <li class="nav-item">
                            <a routerLink="/promociones" class="nav-link" routerLinkActive="active"
                               (click)="closeSidebarOnNavigate()">
                              <i class="far fa-circle nav-icon"></i><p>Grupo de productos</p>
                            </a>
                          </li>
                        }
                        <li class="nav-item">
                          <a routerLink="/unidades" class="nav-link" routerLinkActive="active"
                             (click)="closeSidebarOnNavigate()">
                            <i class="far fa-circle nav-icon"></i><p>Unidades</p>
                          </a>
                        </li>
                        <li class="nav-item">
                          <a routerLink="/catalogo" [queryParams]="{ estado: 'agotado' }" class="nav-link"
                             (click)="closeSidebarOnNavigate()">
                            <i class="far fa-circle nav-icon"></i><p title="Artículos sin stock según ERP">Agotados</p>
                          </a>
                        </li>
                      </ul>
                    </li>
                  }

                  @if (session.can('Site_App_Ecommerce_ListasPrecios', 'manager:all')) {
                    <li class="nav-item">
                      <a routerLink="/listas-precios" class="nav-link" routerLinkActive="active"
                         (click)="closeSidebarOnNavigate()">
                        <i class="nav-icon fas fa-list-alt"></i>
                        <p>Listas de precios</p>
                      </a>
                    </li>
                  }
                  @if (session.can('Site_App_Ecommerce_TarifaDelivery', 'manager:all')) {
                    <li class="nav-item">
                      <a routerLink="/tarifas" class="nav-link" routerLinkActive="active"
                         (click)="closeSidebarOnNavigate()">
                        <i class="nav-icon fas fa-truck"></i>
                        <p>Tarifas delivery</p>
                      </a>
                    </li>
                  }
                  @if (session.can('Site_App_Ecommerce_Clientes', 'manager:all')) {
                    <li class="nav-item">
                      <a routerLink="/clientes" class="nav-link" routerLinkActive="active"
                         (click)="closeSidebarOnNavigate()">
                        <i class="nav-icon fas fa-user-friends"></i>
                        <p>Clientes</p>
                      </a>
                    </li>
                  }
                  @if (session.can('Site_App_Ecommerce_CarritoCompras', 'manager:all')) {
                    <li class="nav-item">
                      <a routerLink="/carritos" class="nav-link" routerLinkActive="active"
                         (click)="closeSidebarOnNavigate()">
                        <i class="nav-icon fas fa-shopping-cart"></i>
                        <p title="Carritos: export y trazabilidad">Carritos</p>
                      </a>
                    </li>
                  }
                  @if (session.can('Site_App_Ecommerce_Pedidos', 'Site_App_Ecommerce_Pedidos_View', 'manager:all')) {
                    <li class="nav-item">
                      <a routerLink="/pedidos" class="nav-link" routerLinkActive="active"
                         (click)="closeSidebarOnNavigate()">
                        <i class="nav-icon fas fa-shopping-bag"></i>
                        <p>Pedidos</p>
                      </a>
                    </li>
                    <li class="nav-item">
                      <a routerLink="/choferes" class="nav-link" routerLinkActive="active"
                         (click)="closeSidebarOnNavigate()">
                        <i class="nav-icon fas fa-id-badge"></i>
                        <p>Choferes</p>
                      </a>
                    </li>
                  }

                  <li class="nav-header">OPS / EXTRA</li>
                  @if (session.can('Site_App_Ecommerce', 'Site_App_Ecommerce_Productos', 'manager:tenants', 'manager:all')) {
                    <li class="nav-item">
                      <a routerLink="/sync" class="nav-link" routerLinkActive="active"
                         (click)="closeSidebarOnNavigate()">
                        <i class="nav-icon fas fa-exchange-alt"></i>
                        <p title="Estado de sincronización ecom-etl">Estado ETL</p>
                      </a>
                    </li>
                  }
                  @if (session.can('Site_App_Ecommerce_Resenas', 'Site_App_Ecommerce_Cupones', 'Site_App_Ecommerce_Devoluciones', 'manager:all')) {
                    <li class="nav-item">
                      <a routerLink="/marketplace" class="nav-link" routerLinkActive="active"
                         (click)="closeSidebarOnNavigate()">
                        <i class="nav-icon fas fa-star"></i>
                        <p>Marketplace</p>
                      </a>
                    </li>
                  }
                  @if (session.can('Site_App_Ecommerce_Chat', 'manager:all')) {
                    <li class="nav-item">
                      <a routerLink="/crm" class="nav-link" routerLinkActive="active"
                         (click)="closeSidebarOnNavigate()">
                        <i class="nav-icon fas fa-headset"></i>
                        <p>
                          Atención al cliente
                          @if (crm.unread() > 0) {
                            <span class="badge badge-danger right">{{ crm.unread() > 99 ? '99+' : crm.unread() }}</span>
                          }
                        </p>
                      </a>
                    </li>
                  }
                  @if (session.can('Site_App_Ecommerce', 'manager:all')) {
                    <li class="nav-item">
                      <a routerLink="/contacto" class="nav-link" routerLinkActive="active"
                         (click)="closeSidebarOnNavigate()">
                        <i class="nav-icon fas fa-envelope"></i>
                        <p>
                          Contacto
                          @if (contacto.unread() > 0) {
                            <span class="badge badge-danger right">{{ contacto.unread() > 99 ? '99+' : contacto.unread() }}</span>
                          }
                        </p>
                      </a>
                    </li>
                  }
                  @if (session.can('Site_App_Ecommerce', 'Site_App_Ecommerce_Pedidos', 'Site_App_Ecommerce_Pedidos_View', 'Site_App_Ecommerce_CarritoCompras', 'manager:all')) {
                    <li class="nav-item">
                      <a routerLink="/reportes" class="nav-link" routerLinkActive="active"
                         (click)="closeSidebarOnNavigate()">
                        <i class="nav-icon fas fa-chart-line"></i>
                        <p>Reportes</p>
                      </a>
                    </li>
                  }

                  <li class="nav-header">PLATAFORMA</li>
                  @if (session.can('manager:tenants', 'manager:all')) {
                    <li class="nav-item">
                      <a routerLink="/empresas" class="nav-link" routerLinkActive="active"
                         (click)="closeSidebarOnNavigate()">
                        <i class="nav-icon fas fa-building"></i>
                        <p>Empresas</p>
                      </a>
                    </li>
                  }
                  @if (session.can('manager:usuarios', 'manager:roles', 'manager:funcionalidades', 'manager:all')) {
                    <li class="nav-item">
                      <a routerLink="/manager" class="nav-link" routerLinkActive="active"
                         (click)="closeSidebarOnNavigate()">
                        <i class="nav-icon fas fa-user-shield"></i>
                        <p>Seguridad</p>
                      </a>
                    </li>
                  }
                } @else {
                  <li class="nav-item">
                    <a routerLink="/login" class="nav-link" routerLinkActive="active">
                      <i class="nav-icon fas fa-sign-in-alt"></i>
                      <p>Iniciar sesión</p>
                    </a>
                  </li>
                }
              </ul>
            </nav>
          </div>
        </aside>

        <!-- Overlay móvil: cierra el menú al tocar fuera -->
        <div id="sidebar-overlay" (click)="closeMobileSidebar()"></div>

        <div class="content-wrapper">
          <section class="content pt-3">
            <div class="container-fluid">
              <router-outlet />
            </div>
          </section>
        </div>

        <footer class="main-footer text-sm">
          <strong>{{ brandTitle() }}</strong> · backoffice e-commerce
          <span class="text-muted ml-1">({{ session.tenantSlug() || '—' }})</span>
          <div class="float-right d-none d-sm-inline-block text-muted">
            {{ session.displayName() }}
          </div>
        </footer>
      </div>
    }
  `,
})
export class AppComponent implements OnInit, OnDestroy {
  readonly session = inject(AdminSessionService);
  readonly crm = inject(CrmBadgeService);
  readonly contacto = inject(ContactoBadgeService);
  private readonly router = inject(Router);

  isLoginRoute = false;
  jumpQ = '';
  readonly jumpOpen = signal(false);
  readonly jumpHits = signal<JumpItem[]>([]);
  readonly jumpIndex = signal(0);
  readonly empresaOpen = signal(false);
  readonly productosOpen = signal(false);
  readonly notificationsOpen = signal(false);

  ngOnInit(): void {
    this.syncRoute(this.router.url);
    this.syncBodyLayoutClasses();
    this.router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .subscribe((e) => {
        this.syncRoute(e.urlAfterRedirects);
        this.closeJump();
        this.notificationsOpen.set(false);
      });
    this.syncCrmBadge();
  }

  ngOnDestroy(): void {
    this.crm.stop();
    this.contacto.stop();
  }

  brandTitle(): string {
    const slug = this.session.tenantSlug();
    if (!slug) return 'E-commerce';
    return slug
      .split('-')
      .filter(Boolean)
      .map((p) => p.charAt(0).toUpperCase() + p.slice(1))
      .join(' ');
  }

  brandMark(): string {
    const t = this.brandTitle();
    const parts = t.split(' ').filter(Boolean);
    if (parts.length >= 2) return (parts[0][0] + parts[1][0]).toUpperCase();
    return t.slice(0, 2).toUpperCase() || 'DX';
  }

  sessionInitials(): string {
    const n = this.session.displayName() || 'Admin';
    const parts = n.split(' ').filter(Boolean);
    if (parts.length >= 2) return (parts[0][0] + parts[1][0]).toUpperCase();
    return n.slice(0, 2).toUpperCase();
  }

  onJumpQuery(q: string): void {
    const t = q.trim().toLowerCase();
    if (!t) {
      this.jumpHits.set([]);
      this.jumpOpen.set(false);
      return;
    }
    const hits = JUMP_ITEMS.filter(
      (i) =>
        this.session.can(...i.permissions) &&
        (i.label.toLowerCase().includes(t) || i.keywords.includes(t) || i.path.includes(t)),
    ).slice(0, 8);
    this.jumpHits.set(hits);
    this.jumpIndex.set(0);
    this.jumpOpen.set(hits.length > 0);
  }

  onJumpKey(ev: KeyboardEvent): void {
    const hits = this.jumpHits();
    if (!hits.length) return;
    if (ev.key === 'ArrowDown') {
      ev.preventDefault();
      this.jumpIndex.set((this.jumpIndex() + 1) % hits.length);
    } else if (ev.key === 'ArrowUp') {
      ev.preventDefault();
      this.jumpIndex.set((this.jumpIndex() - 1 + hits.length) % hits.length);
    } else if (ev.key === 'Enter') {
      ev.preventDefault();
      this.goJump(hits[this.jumpIndex()].path);
    } else if (ev.key === 'Escape') {
      this.closeJump();
    }
  }

  goJump(path: string): void {
    this.closeJump();
    void this.router.navigateByUrl(path);
    this.closeSidebarOnNavigate();
  }

  pendingNotifications(): number {
    return this.crm.unread() + this.contacto.unread();
  }

  toggleNotifications(event: Event): void {
    event.stopPropagation();
    this.notificationsOpen.update((open) => !open);
  }

  goNotification(path: string): void {
    this.notificationsOpen.set(false);
    void this.router.navigateByUrl(path);
    this.closeSidebarOnNavigate();
  }

  closeJump(): void {
    this.jumpQ = '';
    this.jumpOpen.set(false);
    this.jumpHits.set([]);
  }

  @HostListener('document:keydown', ['$event'])
  onGlobalKey(ev: KeyboardEvent): void {
    if ((ev.ctrlKey || ev.metaKey) && ev.key === '/') {
      ev.preventDefault();
      const el = document.querySelector('.adm-jump input') as HTMLInputElement | null;
      el?.focus();
    }
  }

  @HostListener('document:click')
  onDocumentClick(): void {
    this.notificationsOpen.set(false);
  }

  private syncCrmBadge(): void {
    if (this.session.isLoggedIn() && !this.isLoginRoute) {
      this.crm.start();
      this.contacto.start();
    } else {
      this.crm.stop();
      this.contacto.stop();
    }
  }

  private syncRoute(url: string): void {
    this.isLoginRoute = url.startsWith('/login');
    this.syncBodyLayoutClasses();
    this.syncCrmBadge();
    this.syncTreeOpen(url);
    if (this.isLoginRoute) {
      document.body.classList.remove('sidebar-open', 'sidebar-collapse');
    }
  }

  private syncTreeOpen(url: string): void {
    const path = url.split('?')[0];
    if (['/configuracion', '/branding', '/personalizacion', '/contenido'].some((p) => path === p || path.startsWith(p + '/'))) {
      this.empresaOpen.set(true);
    }
    if (
      path.startsWith('/catalogo') ||
      path.startsWith('/parametros-producto') ||
      path.startsWith('/promociones') ||
      path.startsWith('/unidades')
    ) {
      this.productosOpen.set(true);
    }
  }

  private syncBodyLayoutClasses(): void {
    document.body.classList.add('hold-transition');
    document.body.classList.toggle('sidebar-mini', !this.isLoginRoute);
    document.body.classList.toggle('layout-fixed', !this.isLoginRoute);
    document.body.classList.toggle('login-page', this.isLoginRoute);
  }

  private isMobile(): boolean {
    return window.innerWidth < MOBILE_BREAKPOINT;
  }

  toggleSidebar(ev: Event): void {
    ev.preventDefault();
    ev.stopPropagation();
    if (this.isMobile()) {
      // AdminLTE móvil: sidebar-open desliza el panel desde la izquierda
      document.body.classList.toggle('sidebar-open');
      document.body.classList.remove('sidebar-collapse');
    } else {
      // Desktop: sidebar-collapse contrae a iconos
      document.body.classList.toggle('sidebar-collapse');
      document.body.classList.remove('sidebar-open');
    }
  }

  closeMobileSidebar(): void {
    document.body.classList.remove('sidebar-open');
  }

  closeSidebarOnNavigate(): void {
    if (this.isMobile()) {
      this.closeMobileSidebar();
    }
  }

  toggleEmpresa(ev: Event): void {
    ev.preventDefault();
    ev.stopPropagation();
    this.empresaOpen.update((v) => !v);
  }

  toggleProductos(ev: Event): void {
    ev.preventDefault();
    ev.stopPropagation();
    this.productosOpen.update((v) => !v);
  }

  logout(ev: Event): void {
    ev.preventDefault();
    this.crm.stop();
    this.contacto.stop();
    this.session.logout();
    document.body.classList.remove('sidebar-open', 'sidebar-collapse');
    void this.router.navigateByUrl('/login');
  }

  openShop(ev: Event): void {
    ev.preventDefault();
    openStorefront(this.session.tenantSlug());
  }

  @HostListener('window:resize')
  onResize(): void {
    if (!this.isMobile()) {
      document.body.classList.remove('sidebar-open');
    }
  }
}
