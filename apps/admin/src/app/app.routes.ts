import { Routes } from '@angular/router';
import { adminAuthGuard } from './core/admin-auth.guard';
import { DashboardPageComponent } from './pages/dashboard-page.component';
import { CatalogAdminPageComponent } from './pages/catalog-admin-page.component';
import { OrdersAdminPageComponent } from './pages/orders-admin-page.component';
import { OrderDetailAdminPageComponent } from './pages/order-detail-admin-page.component';
import { TenantsPageComponent } from './pages/tenants-page.component';
import { AdminLoginPageComponent } from './pages/admin-login-page.component';
import { ManagerPageComponent } from './pages/manager-page.component';
import { PromotionsAdminPageComponent } from './pages/promotions-admin-page.component';
import { ShippingAdminPageComponent } from './pages/shipping-admin-page.component';
import { EmpresaConfigPageComponent } from './pages/empresa-config-page.component';
import { BrandingPageComponent } from './pages/branding-page.component';
import { PersonalizacionAdminPageComponent } from './pages/personalizacion-admin-page.component';
import { UnidadesPageComponent } from './pages/unidades-page.component';
import { ProductImagesPageComponent } from './pages/product-images-page.component';
import { ProductActivatePageComponent } from './pages/product-activate-page.component';
import { ListasPreciosPageComponent } from './pages/listas-precios-page.component';
import { ClientesPageComponent } from './pages/clientes-page.component';
import { CarritosAdminPageComponent } from './pages/carritos-admin-page.component';
import { CarritoDetailAdminPageComponent } from './pages/carrito-detail-admin-page.component';
import { MarketplaceAdminPageComponent } from './pages/marketplace-admin-page.component';
import { CrmInboxPageComponent } from './pages/crm-inbox-page.component';
import { ReportsPageComponent } from './pages/reports-page.component';
import { ChoferesAdminPageComponent } from './pages/choferes-admin-page.component';
import { ProductFormPageComponent } from './pages/product-form-page.component';
import { ParametrosProductoPageComponent } from './pages/parametros-producto-page.component';
import { SyncOpsPageComponent } from './pages/sync-ops-page.component';
import { ContentCmsAdminPageComponent } from './pages/content-cms-admin-page.component';
import { ContactoAdminPageComponent } from './pages/contacto-admin-page.component';
import { InventoryMovimientosPageComponent } from './pages/inventory-movimientos-page.component';
import { adminPermissionGuard } from './core/admin-permission.guard';

const auth = [adminAuthGuard, adminPermissionGuard] as const;

export const routes: Routes = [
  { path: 'login', component: AdminLoginPageComponent },
  {
    path: '',
    component: DashboardPageComponent,
    canActivate: [...auth],
    data: { permissions: ['Site_App_Ecommerce', 'manager:all'] },
  },
  {
    path: 'sync',
    component: SyncOpsPageComponent,
    canActivate: [...auth],
    data: {
      permissions: ['Site_App_Ecommerce', 'Site_App_Ecommerce_Productos', 'manager:tenants', 'manager:all'],
    },
  },
  {
    path: 'configuracion',
    component: EmpresaConfigPageComponent,
    canActivate: [...auth],
    data: {
      permissions: [
        'Site_App_Ecommerce_Empresa',
        'Site_App_Ecommerce_Empresa_Agencia',
        'Site_App_Ecommerce_Empresa_HorarioAtencion',
        'manager:all',
      ],
    },
  },
  {
    path: 'branding',
    component: BrandingPageComponent,
    canActivate: [...auth],
    data: { permissions: ['Site_App_Ecommerce_Empresa', 'manager:all'] },
  },
  {
    path: 'personalizacion',
    component: PersonalizacionAdminPageComponent,
    canActivate: [...auth],
    data: {
      permissions: ['Site_App_Ecommerce_Personalizacion', 'Site_App_Ecommerce_Empresa', 'Site_App_Ecommerce', 'manager:all'],
    },
  },
  {
    path: 'contenido',
    component: ContentCmsAdminPageComponent,
    canActivate: [...auth],
    data: {
      permissions: ['Site_App_Ecommerce_Personalizacion', 'Site_App_Ecommerce_Empresa', 'Site_App_Ecommerce', 'manager:all'],
    },
  },
  {
    path: 'unidades',
    component: UnidadesPageComponent,
    canActivate: [...auth],
    data: { permissions: ['Site_App_Ecommerce_Productos', 'manager:all'] },
  },
  {
    path: 'parametros-producto',
    component: ParametrosProductoPageComponent,
    canActivate: [...auth],
    data: { permissions: ['Site_App_Ecommerce_Productos', 'manager:all'] },
  },
  {
    path: 'catalogo/:id/embalajes',
    component: UnidadesPageComponent,
    canActivate: [...auth],
    data: { permissions: ['Site_App_Ecommerce_Productos', 'manager:all'] },
  },
  {
    path: 'catalogo/:id/movimientos',
    component: InventoryMovimientosPageComponent,
    canActivate: [...auth],
    data: { permissions: ['Site_App_Ecommerce_Productos', 'manager:all'] },
  },
  {
    path: 'empresas',
    component: TenantsPageComponent,
    canActivate: [...auth],
    data: { permissions: ['manager:tenants', 'manager:all'] },
  },
  {
    path: 'catalogo/nuevo',
    component: ProductFormPageComponent,
    canActivate: [...auth],
    data: { permissions: ['Site_App_Ecommerce_Productos_Edit', 'manager:all'] },
  },
  {
    path: 'catalogo/activar',
    component: ProductActivatePageComponent,
    canActivate: [...auth],
    data: { permissions: ['Site_App_Ecommerce_Productos_ActivarInactivar', 'manager:all'] },
  },
  {
    path: 'catalogo/:id/editar',
    component: ProductFormPageComponent,
    canActivate: [...auth],
    data: { permissions: ['Site_App_Ecommerce_Productos', 'Site_App_Ecommerce_Productos_Edit', 'manager:all'] },
  },
  {
    path: 'catalogo/:id/imagenes',
    component: ProductImagesPageComponent,
    canActivate: [...auth],
    data: { permissions: ['Site_App_Ecommerce_Productos_Imagenes', 'Site_App_Ecommerce_Productos', 'manager:all'] },
  },
  {
    path: 'catalogo',
    component: CatalogAdminPageComponent,
    canActivate: [...auth],
    data: { permissions: ['Site_App_Ecommerce_Productos', 'manager:all'] },
  },
  {
    path: 'promociones',
    component: PromotionsAdminPageComponent,
    canActivate: [...auth],
    data: { permissions: ['Site_App_Ecommerce_GrupoArticulo', 'manager:all'] },
  },
  {
    path: 'marketplace',
    component: MarketplaceAdminPageComponent,
    canActivate: [...auth],
    data: {
      permissions: [
        'Site_App_Ecommerce_Resenas',
        'Site_App_Ecommerce_Cupones',
        'Site_App_Ecommerce_Devoluciones',
        'manager:all',
      ],
    },
  },
  {
    path: 'crm',
    component: CrmInboxPageComponent,
    canActivate: [...auth],
    data: { permissions: ['Site_App_Ecommerce_Chat', 'manager:all'] },
  },
  {
    path: 'contacto',
    component: ContactoAdminPageComponent,
    canActivate: [...auth],
    data: { permissions: ['Site_App_Ecommerce', 'manager:all'] },
  },
  {
    path: 'choferes',
    component: ChoferesAdminPageComponent,
    canActivate: [...auth],
    data: { permissions: ['Site_App_Ecommerce_Pedidos', 'Site_App_Ecommerce_Pedidos_View', 'manager:all'] },
  },
  {
    path: 'listas-precios',
    component: ListasPreciosPageComponent,
    canActivate: [...auth],
    data: { permissions: ['Site_App_Ecommerce_ListasPrecios', 'manager:all'] },
  },
  {
    path: 'listas-precios/:id',
    component: ListasPreciosPageComponent,
    canActivate: [...auth],
    data: { permissions: ['Site_App_Ecommerce_ListasPrecios', 'manager:all'] },
  },
  {
    path: 'clientes',
    component: ClientesPageComponent,
    canActivate: [...auth],
    data: { permissions: ['Site_App_Ecommerce_Clientes', 'manager:all'] },
  },
  {
    path: 'tarifas',
    component: ShippingAdminPageComponent,
    canActivate: [...auth],
    data: { permissions: ['Site_App_Ecommerce_TarifaDelivery', 'manager:all'] },
  },
  {
    path: 'carritos',
    component: CarritosAdminPageComponent,
    canActivate: [...auth],
    data: { permissions: ['Site_App_Ecommerce_CarritoCompras', 'manager:all'] },
  },
  {
    path: 'carritos/:id',
    component: CarritoDetailAdminPageComponent,
    canActivate: [...auth],
    data: { permissions: ['Site_App_Ecommerce_CarritoCompras', 'manager:all'] },
  },
  {
    path: 'pedidos',
    component: OrdersAdminPageComponent,
    canActivate: [...auth],
    data: { permissions: ['Site_App_Ecommerce_Pedidos', 'Site_App_Ecommerce_Pedidos_View', 'manager:all'] },
  },
  {
    path: 'pedidos/:id',
    component: OrderDetailAdminPageComponent,
    canActivate: [...auth],
    data: { permissions: ['Site_App_Ecommerce_Pedidos', 'Site_App_Ecommerce_Pedidos_View', 'manager:all'] },
  },
  {
    path: 'reportes',
    component: ReportsPageComponent,
    canActivate: [...auth],
    data: {
      permissions: [
        'Site_App_Ecommerce',
        'Site_App_Ecommerce_Pedidos',
        'Site_App_Ecommerce_Pedidos_View',
        'Site_App_Ecommerce_CarritoCompras',
        'manager:all',
      ],
    },
  },
  {
    path: 'manager',
    component: ManagerPageComponent,
    canActivate: [...auth],
    data: { permissions: ['manager:roles', 'manager:usuarios', 'manager:funcionalidades', 'manager:all'] },
  },
  { path: '**', redirectTo: '' },
];
