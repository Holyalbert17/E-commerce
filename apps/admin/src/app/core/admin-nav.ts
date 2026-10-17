import { AdminSessionService } from './admin-session.service';

/** Rutas de aterrizaje ordenadas — primera que el usuario pueda abrir. */
export const ADMIN_LANDING_ROUTES: ReadonlyArray<{ path: string; permissions: string[] }> = [
  { path: '/', permissions: ['Site_App_Ecommerce', 'manager:all'] },
  { path: '/catalogo', permissions: ['Site_App_Ecommerce_Productos', 'manager:all'] },
  { path: '/pedidos', permissions: ['Site_App_Ecommerce_Pedidos', 'Site_App_Ecommerce_Pedidos_View', 'manager:all'] },
  { path: '/clientes', permissions: ['Site_App_Ecommerce_Clientes', 'manager:all'] },
  { path: '/configuracion', permissions: ['Site_App_Ecommerce_Empresa', 'Site_App_Ecommerce_Empresa_Agencia', 'Site_App_Ecommerce_Empresa_HorarioAtencion', 'manager:all'] },
  { path: '/crm', permissions: ['Site_App_Ecommerce_Chat', 'manager:all'] },
  { path: '/manager', permissions: ['manager:roles', 'manager:usuarios', 'manager:funcionalidades', 'manager:all'] },
  { path: '/empresas', permissions: ['manager:tenants', 'manager:all'] },
  { path: '/sync', permissions: ['Site_App_Ecommerce', 'Site_App_Ecommerce_Productos', 'manager:tenants', 'manager:all'] },
];

export function firstAllowedAdminPath(session: AdminSessionService): string {
  for (const r of ADMIN_LANDING_ROUTES) {
    if (session.can(...r.permissions)) return r.path;
  }
  return '/login';
}
