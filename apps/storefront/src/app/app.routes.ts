import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'catalogo' },
  {
    path: 'catalogo',
    loadComponent: () => import('./pages/catalog-page.component').then((m) => m.CatalogPageComponent),
  },
  {
    path: 'buscar',
    loadComponent: () => import('./pages/catalog-page.component').then((m) => m.CatalogPageComponent),
    data: { searchMode: true },
  },
  {
    path: 'producto/:id',
    loadComponent: () => import('./pages/product-page.component').then((m) => m.ProductPageComponent),
  },
  {
    path: 'carrito',
    loadComponent: () => import('./pages/cart-page.component').then((m) => m.CartPageComponent),
  },
  {
    path: 'checkout',
    loadComponent: () => import('./pages/checkout-page.component').then((m) => m.CheckoutPageComponent),
  },
  {
    path: 'pago/:nro',
    loadComponent: () => import('./pages/payment-page.component').then((m) => m.PaymentPageComponent),
  },
  {
    path: 'pedido/:nro',
    loadComponent: () => import('./pages/order-page.component').then((m) => m.OrderPageComponent),
  },
  {
    path: 'seguimiento/:token',
    loadComponent: () =>
      import('./pages/tracking-public-page.component').then((m) => m.TrackingPublicPageComponent),
  },
  { path: 'pedidos', redirectTo: 'mis-pedidos', pathMatch: 'full' },
  {
    path: 'mis-pedidos',
    loadComponent: () => import('./pages/orders-page.component').then((m) => m.OrdersPageComponent),
    canActivate: [authGuard],
  },
  {
    path: 'devoluciones',
    loadComponent: () => import('./pages/returns-page.component').then((m) => m.ReturnsPageComponent),
    canActivate: [authGuard],
  },
  {
    path: 'favoritos',
    loadComponent: () => import('./pages/favorites-page.component').then((m) => m.FavoritesPageComponent),
    canActivate: [authGuard],
  },
  {
    path: 'seguidos',
    loadComponent: () => import('./pages/following-page.component').then((m) => m.FollowingPageComponent),
    canActivate: [authGuard],
  },
  {
    path: 'mi-cuenta',
    loadComponent: () => import('./pages/account-hub-page.component').then((m) => m.AccountHubPageComponent),
    canActivate: [authGuard],
  },
  {
    path: 'login',
    loadComponent: () => import('./pages/login-page.component').then((m) => m.LoginPageComponent),
  },
  {
    path: 'registro',
    loadComponent: () => import('./pages/register-page.component').then((m) => m.RegisterPageComponent),
  },
  {
    path: 'recuperar',
    loadComponent: () => import('./pages/recuperar-page.component').then((m) => m.RecuperarPageComponent),
  },
  {
    path: 'activar',
    loadComponent: () => import('./pages/activate-page.component').then((m) => m.ActivatePageComponent),
  },
  {
    path: 'sucursales',
    loadComponent: () => import('./pages/branches-page.component').then((m) => m.BranchesPageComponent),
  },
  {
    path: 'contacto',
    loadComponent: () => import('./pages/contact-page.component').then((m) => m.ContactPageComponent),
  },
  {
    path: 'faq',
    loadComponent: () => import('./pages/cms-page.component').then((m) => m.CmsPageComponent),
    data: { slug: 'faq' },
  },
  {
    path: 'terminos',
    loadComponent: () => import('./pages/cms-page.component').then((m) => m.CmsPageComponent),
    data: { slug: 'terminos' },
  },
  {
    path: 'como-comprar',
    loadComponent: () => import('./pages/cms-page.component').then((m) => m.CmsPageComponent),
    data: { slug: 'como-comprar' },
  },
  { path: '**', redirectTo: 'catalogo' },
];
