import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { environment } from '../../environments/environment';

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface AdminDashboard {
  articulosEcommerce: number;
  articulosAgotados: number;
  pedidosHoy: number;
  pedidosPendientesMigracion: number;
  montoPedidosHoy: number;
}

export interface AdminProduct {
  id: number;
  sku: string | null;
  nombre: string;
  marca: string | null;
  activo: boolean;
  activoEcommerce: boolean;
  estadoEcommerce: string;
  precioDesdeB2C: number | null;
  stockExistencia: number;
  idDeposito: number | null;
  origenDatos?: string;
  isReadOnly?: boolean;
  slugSeo?: string | null;
}

export interface InventarioMovimientoItem {
  id: number;
  idArticulo: number;
  idDeposito: number | null;
  tipoMovimiento: string;
  cantidad: number;
  existenciaAntes: number;
  existenciaDespues: number;
  referencia: string | null;
  fecha: string;
  userName: string | null;
}

export interface AdminArticuloDetail {
  id: number;
  nroArticulo: number;
  sku: string | null;
  descripcion: string;
  descripcionAbreviada: string | null;
  descripcionExtensa: string | null;
  codReferenciaProveedor: string | null;
  idMarca: number;
  nombreMarca: string | null;
  idLinea: number | null;
  nombreLinea: string | null;
  idTipoArticulo: number | null;
  nombreTipoArticulo: string | null;
  idProveedor: number | null;
  nombreProveedor: string | null;
  idPais: number | null;
  nombrePais: string | null;
  idRegimenImpositivo: number | null;
  nombreRegimenImpositivo: string | null;
  activo: boolean;
  activoEcommerce: boolean;
  estadoEcommerce: string;
  codigoBarra: string | null;
  idEmbalajeParametro: number | null;
  descripcionEmbalaje: string | null;
  codigoUnidadMedida: string;
  cantidadPorEmbalaje: number;
  idEmbalajeArticulo: number | null;
  imagenUrl: string | null;
  stockExistencia: number;
  precioDesdeB2C: number | null;
  fichaTecnicaUrl: string | null;
  videoUrl: string | null;
  slugSeo?: string | null;
  origenDatos?: string;
  isReadOnly?: boolean;
}

export interface CreateAdminArticuloBody {
  sku: string;
  descripcion: string;
  codReferenciaProveedor?: string | null;
  idMarca: number;
  idLinea: number;
  idTipoArticulo: number;
  idProveedor: number;
  idEmbalaje: number;
  idPais?: number | null;
  idRegimenImpositivo?: number | null;
  codigoUnidadMedida: string;
  cantidadPorEmbalaje: number;
  nroArticulo?: number | null;
  codigoBarra?: string | null;
  imagenUrl?: string | null;
  descripcionExtensa?: string | null;
  fichaTecnicaUrl?: string | null;
  videoUrl?: string | null;
  slugSeo?: string | null;
  stockInicial?: number | null;
}

export interface CatalogoParametro {
  id: number;
  tipo: string;
  codigo: number;
  descripcion: string;
  activo: boolean;
}

export interface AdminOrderListItem {
  idPedidoMovil: number;
  nroPedido: number;
  fechaPedido: string;
  razonSocial: string;
  numeroDocumento: string;
  montoTotal: number;
  tipoEntrega: number;
  tipoPago: number;
  estado: number;
  migrado: boolean;
  itemsCount: number;
  lugarEntrega?: string | null;
  estadoFulfillment?: string | null;
  nroFactura?: number | null;
  facturado?: boolean;
  puedeDescargarFactura?: boolean;
}

export interface AdminOrderDetail {
  idPedidoMovil: number;
  nroPedido: number;
  idCarrito: number | null;
  fechaPedido: string;
  fechaPrevistaEntrega: string | null;
  razonSocial: string;
  numeroDocumento: string;
  observacion: string | null;
  lugarEntrega: string | null;
  tipoEntrega: number;
  tipoPago: number;
  estado: number;
  migrado: boolean;
  origenPedido: number;
  montoTotal: number;
  montoIVA: number;
  idDeposito: number;
  latitud?: number | null;
  longitud?: number | null;
  direccionEntrega?: string | null;
  idChofer?: number | null;
  nombreChofer?: string | null;
  estadoFulfillment?: string | null;
  nroFactura?: number | null;
  facturado?: boolean;
  puedeDescargarFactura?: boolean;
  items: {
    idArticulo: number;
    sku: string | null;
    nombre: string;
    cantidadEmbalada: number;
    precioUnitario: number;
    montoLinea: number;
  }[];
}

export interface AdminChofer {
  id: number;
  nroChofer: number;
  nombre: string;
  documento: string | null;
  telefono: string | null;
  observacion: string | null;
  activo: boolean;
}

export interface AdminTrackingEvent {
  id: number;
  idPedidoMovil: number;
  estado: string;
  mensaje: string | null;
  fechaEstado: string;
  codigoTracking: string | null;
}

export interface AdminTokenResponse {
  accessToken: string;
  expiresAtUtc: string;
  idEmpresa: number;
  idUsuarioEcommerce: number;
  nombre: string;
  canalPreferido: string;
  permissions?: string[];
  isSuper?: boolean;
}

@Injectable({ providedIn: 'root' })
export class AdminApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  dashboard(): Observable<AdminDashboard> {
    return this.http.get<AdminDashboard>(`${this.base}/api/admin/dashboard`);
  }

  products(opts: {
    search?: string;
    estado?: string;
    activo?: boolean;
    activoEcommerce?: boolean;
    calidad?: string;
    page?: number;
    pageSize?: number;
  } = {}): Observable<PagedResult<AdminProduct>> {
    let params = new HttpParams()
      .set('page', String(opts.page ?? 1))
      .set('pageSize', String(opts.pageSize ?? 20));
    if (opts.search) params = params.set('search', opts.search);
    if (opts.estado) params = params.set('estado', opts.estado);
    if (opts.activo != null) params = params.set('activo', String(opts.activo));
    if (opts.activoEcommerce != null) params = params.set('activoEcommerce', String(opts.activoEcommerce));
    if (opts.calidad) params = params.set('calidad', opts.calidad);
    return this.http.get<PagedResult<AdminProduct>>(`${this.base}/api/admin/products`, { params });
  }

  product(id: number): Observable<AdminProduct> {
    return this.http.get<AdminProduct>(`${this.base}/api/admin/products/${id}`);
  }

  productDetail(id: number): Observable<AdminArticuloDetail> {
    return this.http.get<AdminArticuloDetail>(`${this.base}/api/admin/products/${id}/detail`);
  }

  createProduct(body: CreateAdminArticuloBody): Observable<AdminArticuloDetail> {
    return this.http.post<AdminArticuloDetail>(`${this.base}/api/admin/products`, body);
  }

  upsertProductMaster(id: number, body: CreateAdminArticuloBody & {
    descripcionAbreviada?: string | null;
    activo?: boolean;
    activoEcommerce?: boolean;
    estadoEcommerce?: string;
  }): Observable<AdminArticuloDetail> {
    return this.http.put<AdminArticuloDetail>(`${this.base}/api/admin/products/${id}/master`, body);
  }

  deleteProduct(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/admin/products/${id}`);
  }

  listParametrosProducto(tipo: string, incluirInactivos = false): Observable<CatalogoParametro[]> {
    let params = new HttpParams().set('tipo', tipo);
    if (incluirInactivos) params = params.set('incluirInactivos', 'true');
    return this.http.get<CatalogoParametro[]>(`${this.base}/api/admin/parametros-producto`, { params });
  }

  createParametroProducto(body: { tipo: string; descripcion: string; codigo?: number; activo?: boolean }): Observable<CatalogoParametro> {
    return this.http.post<CatalogoParametro>(`${this.base}/api/admin/parametros-producto`, body);
  }

  updateParametroProducto(id: number, body: { tipo: string; descripcion: string; codigo?: number; activo?: boolean }): Observable<CatalogoParametro> {
    return this.http.put<CatalogoParametro>(`${this.base}/api/admin/parametros-producto/${id}`, body);
  }

  deleteParametroProducto(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/admin/parametros-producto/${id}`);
  }

  updateProduct(id: number, body: { activoEcommerce?: boolean; estadoEcommerce?: string; nombre?: string }): Observable<AdminProduct> {
    return this.http.put<AdminProduct>(`${this.base}/api/admin/products/${id}`, body);
  }

  updateStock(id: number, totalExistencia: number): Observable<AdminProduct> {
    return this.http.put<AdminProduct>(`${this.base}/api/admin/products/${id}/stock`, { totalExistencia });
  }

  listInventarioMovimientos(articuloId?: number, take = 100): Observable<InventarioMovimientoItem[]> {
    let params = new HttpParams().set('take', String(take));
    if (articuloId != null) params = params.set('articuloId', String(articuloId));
    return this.http.get<InventarioMovimientoItem[]>(`${this.base}/api/admin/inventory/movimientos`, { params });
  }

  orders(opts: {
    search?: string;
    migrado?: boolean;
    estadoFulfillment?: string;
    page?: number;
    pageSize?: number;
  } = {}): Observable<PagedResult<AdminOrderListItem>> {
    let params = new HttpParams()
      .set('page', String(opts.page ?? 1))
      .set('pageSize', String(opts.pageSize ?? 20));
    if (opts.search) params = params.set('search', opts.search);
    if (opts.migrado != null) params = params.set('migrado', String(opts.migrado));
    if (opts.estadoFulfillment) params = params.set('estadoFulfillment', opts.estadoFulfillment);
    return this.http.get<PagedResult<AdminOrderListItem>>(`${this.base}/api/admin/orders`, { params });
  }

  order(id: number): Observable<AdminOrderDetail> {
    return this.http.get<AdminOrderDetail>(`${this.base}/api/admin/orders/${id}`);
  }

  exportOrders(opts: { fechaDesde?: string; fechaHasta?: string } = {}): Observable<Blob> {
    let params = new HttpParams();
    if (opts.fechaDesde) params = params.set('fechaDesde', opts.fechaDesde);
    if (opts.fechaHasta) params = params.set('fechaHasta', opts.fechaHasta);
    return this.http.get(`${this.base}/api/admin/orders/export`, {
      params,
      responseType: 'blob',
    });
  }

  downloadOrderFacturaPdf(idPedidoMovil: number): Observable<Blob> {
    return this.http.get(`${this.base}/api/admin/orders/${idPedidoMovil}/factura.pdf`, {
      responseType: 'blob',
    });
  }

  /** Muestra la factura en un visor de la misma página e invita a imprimir. */
  openPdfBlob(blob: Blob, fileName: string, preview?: Window | null): boolean {
    preview?.close();
    if (!blob || blob.size < 80) return false;
    const type = (blob.type || '').toLowerCase();
    if (type.includes('json') || type.includes('text/')) return false;

    document.getElementById('dx-factura-overlay')?.remove();
    const pdf = new Blob([blob], { type: 'application/pdf' });
    const url = URL.createObjectURL(pdf);

    const overlay = document.createElement('div');
    overlay.id = 'dx-factura-overlay';
    overlay.style.cssText =
      'position:fixed;inset:0;z-index:2147483646;background:rgba(15,23,42,.62);display:flex;flex-direction:column;padding:12px;gap:8px;';
    overlay.innerHTML = `
      <div style="display:flex;gap:8px;align-items:center;flex-wrap:wrap">
        <strong style="margin-right:auto;color:#fff;font:600 15px/1.2 system-ui,sans-serif">Factura</strong>
        <button type="button" data-act="print" style="padding:8px 14px;border:0;border-radius:8px;background:#2563eb;color:#fff;cursor:pointer;font-weight:600">Imprimir</button>
        <a data-act="dl" download="${fileName}" style="padding:8px 14px;border-radius:8px;background:#fff;color:#0f172a;text-decoration:none;font-weight:600">Descargar</a>
        <button type="button" data-act="close" style="padding:8px 14px;border:0;border-radius:8px;background:#e2e8f0;cursor:pointer;font-weight:600">Cerrar</button>
      </div>
      <iframe title="Factura" style="flex:1;width:100%;border:0;border-radius:10px;background:#fff"></iframe>
    `;
    const iframe = overlay.querySelector('iframe')!;
    iframe.src = url;
    overlay.querySelector<HTMLAnchorElement>('[data-act="dl"]')!.href = url;
    const close = () => {
      overlay.remove();
      URL.revokeObjectURL(url);
    };
    overlay.querySelector('[data-act="close"]')!.addEventListener('click', close);
    overlay.querySelector('[data-act="print"]')!.addEventListener('click', () => {
      try {
        iframe.contentWindow?.focus();
        iframe.contentWindow?.print();
      } catch {
        window.open(url, '_blank', 'noopener');
      }
    });
    document.body.appendChild(overlay);
    return true;
  }

  asignarFactura(idPedidoMovil: number, nroFactura?: number | null): Observable<{
    idPedidoMovil: number;
    nroPedido: number;
    nroFactura: number;
    yaFacturado: boolean;
  }> {
    return this.http.post<{
      idPedidoMovil: number;
      nroPedido: number;
      nroFactura: number;
      yaFacturado: boolean;
    }>(`${this.base}/api/admin/orders/${idPedidoMovil}/factura`, {
      nroFactura: nroFactura ?? null,
    });
  }

  syncFacturaErp(idPedidoMovil: number): Observable<{
    ok: boolean;
    mensaje: string;
    nroFactura: number | null;
    idPedidoErp: number | null;
    esFiscalErp: boolean;
  }> {
    return this.http.post<{
      ok: boolean;
      mensaje: string;
      nroFactura: number | null;
      idPedidoErp: number | null;
      esFiscalErp: boolean;
    }>(`${this.base}/api/admin/orders/${idPedidoMovil}/factura/sync-erp`, {});
  }

  facturarPedidos(ids: number[]): Observable<{
    procesados: number;
    exitosos: number;
    errores: number;
  }> {
    return this.http.post<{ procesados: number; exitosos: number; errores: number }>(
      `${this.base}/api/admin/orders/facturar`,
      { ids },
    );
  }

  listChoferes(): Observable<AdminChofer[]> {
    return this.http.get<AdminChofer[]>(`${this.base}/api/admin/choferes`);
  }

  createChofer(body: {
    nombre: string;
    nroChofer?: number;
    documento?: string | null;
    telefono?: string | null;
    observacion?: string | null;
    activo?: boolean;
  }): Observable<AdminChofer> {
    return this.http.post<AdminChofer>(`${this.base}/api/admin/choferes`, body);
  }

  updateChofer(id: number, body: {
    nombre: string;
    nroChofer?: number;
    documento?: string | null;
    telefono?: string | null;
    observacion?: string | null;
    activo?: boolean;
  }): Observable<AdminChofer> {
    return this.http.put<AdminChofer>(`${this.base}/api/admin/choferes/${id}`, body);
  }

  deleteChofer(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/admin/choferes/${id}`);
  }

  asignarChoferPedido(idPedidoMovil: number, idChofer: number | null): Observable<void> {
    return this.http.post<void>(`${this.base}/api/admin/pedidos/${idPedidoMovil}/asignar-chofer`, { idChofer });
  }

  addPedidoTracking(
    idPedidoMovil: number,
    body: { estado: string; mensaje?: string; codigoTracking?: string },
  ): Observable<void> {
    return this.http.post<void>(`${this.base}/api/admin/pedidos/${idPedidoMovil}/tracking`, body);
  }

  listPedidoTracking(idPedidoMovil: number): Observable<AdminTrackingEvent[]> {
    return this.http.get<AdminTrackingEvent[]>(`${this.base}/api/engagement/tracking/${idPedidoMovil}`);
  }

  listTenants(): Observable<TenantSummary[]> {
    return this.http.get<TenantSummary[]>(`${this.base}/api/admin/tenants`);
  }

  provisionManual(body: ManualTenantRequest): Observable<ProvisionResult> {
    return this.http.post<ProvisionResult>(`${this.base}/api/admin/tenants/manual`, body);
  }

  provisionFromErp(body: ErpTenantRequest): Observable<ProvisionResult> {
    return this.http.post<ProvisionResult>(`${this.base}/api/admin/tenants/from-erp`, body);
  }

  adminLogin(usuario: string, password: string): Observable<AdminTokenResponse> {
    return this.http.post<AdminTokenResponse>(`${this.base}/api/admin/auth/login`, { usuario, password });
  }

  listFunctionalities(): Observable<AdminFuncionalidadDto[]> {
    return this.http.get<AdminFuncionalidadDto[]>(`${this.base}/api/admin/manager/functionalidades`);
  }

  listRoles(includeInactive = true): Observable<AdminRoleDto[]> {
    const params = new HttpParams().set('includeInactive', String(includeInactive));
    return this.http.get<AdminRoleDto[]>(`${this.base}/api/admin/manager/roles`, { params });
  }

  getRole(idRol: number): Observable<AdminRoleDetailDto> {
    return this.http.get<AdminRoleDetailDto>(`${this.base}/api/admin/manager/roles/${idRol}`);
  }

  createRole(body: { codigoRol: string; nombreRol: string }): Observable<AdminRoleDto> {
    return this.http.post<AdminRoleDto>(`${this.base}/api/admin/manager/roles`, body);
  }

  updateRole(idRol: number, body: { nombreRol?: string; activo?: boolean }): Observable<AdminRoleDetailDto> {
    return this.http.put<AdminRoleDetailDto>(`${this.base}/api/admin/manager/roles/${idRol}`, body);
  }

  deleteRole(idRol: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/admin/manager/roles/${idRol}`);
  }

  /** SaveTree: reemplaza el set completo de PermissionCodes del rol. */
  saveRolePermissions(idRol: number, permissionCodes: string[]): Observable<AdminRoleDetailDto> {
    return this.http.put<AdminRoleDetailDto>(`${this.base}/api/admin/manager/roles/${idRol}/functionalidades`, {
      idRol,
      permissionCodes,
    });
  }

  assignPermissionsToRole(idRol: number, permissionCodes: string[]): Observable<AdminRoleDetailDto> {
    return this.saveRolePermissions(idRol, permissionCodes);
  }

  copyRolePermissions(idRolDestino: number, idRolOrigen: number): Observable<AdminRoleDetailDto> {
    return this.http.post<AdminRoleDetailDto>(
      `${this.base}/api/admin/manager/roles/${idRolDestino}/copiar-permisos`,
      { idRolOrigen },
    );
  }

  listUsers(includeInactive = true): Observable<AdminUserDto[]> {
    const params = new HttpParams().set('includeInactive', String(includeInactive));
    return this.http.get<AdminUserDto[]>(`${this.base}/api/admin/manager/usuarios`, { params });
  }

  createUser(body: { usuario: string; password: string; nombre?: string | null; email?: string | null }): Observable<AdminUserDto> {
    return this.http.post<AdminUserDto>(`${this.base}/api/admin/manager/usuarios`, body);
  }

  updateUser(
    idUsuario: number,
    body: { nombre?: string | null; email?: string | null; password?: string | null; activo?: boolean },
  ): Observable<AdminUserDto> {
    return this.http.put<AdminUserDto>(`${this.base}/api/admin/manager/usuarios/${idUsuario}`, body);
  }

  deleteUser(idUsuario: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/admin/manager/usuarios/${idUsuario}`);
  }

  assignRoleToUser(idUsuario: number, idRol: number): Observable<void> {
    return this.http.post<void>(`${this.base}/api/admin/manager/usuarios/${idUsuario}/roles`, { idRol });
  }

  unassignRoleFromUser(idUsuario: number, idRol: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/admin/manager/usuarios/${idUsuario}/roles/${idRol}`);
  }

  getUserPermissionTree(idUsuario: number): Observable<AdminUserPermissionTreeDto> {
    return this.http.get<AdminUserPermissionTreeDto>(
      `${this.base}/api/admin/manager/usuarios/${idUsuario}/operaciones-tree`,
    );
  }

  saveUserPermissions(idUsuario: number, idFuncionalidades: number[]): Observable<AdminUserPermissionTreeDto> {
    return this.http.put<AdminUserPermissionTreeDto>(
      `${this.base}/api/admin/manager/usuarios/${idUsuario}/operaciones`,
      { idFuncionalidades },
    );
  }

  getOperationsTree(): Observable<AdminPermissionTreeNodeDto[]> {
    return this.http.get<AdminPermissionTreeNodeDto[]>(`${this.base}/api/admin/manager/operaciones-tree`);
  }

  listListasPrecio(): Observable<AdminListaPrecio[]> {
    return this.http.get<AdminListaPrecio[]>(`${this.base}/api/admin/listas-precios`);
  }

  getListaPrecio(id: number): Observable<AdminListaPrecio> {
    return this.http.get<AdminListaPrecio>(`${this.base}/api/admin/listas-precios/${id}`);
  }

  createListaPrecio(body: UpsertListaPrecioRequest): Observable<AdminListaPrecio> {
    return this.http.post<AdminListaPrecio>(`${this.base}/api/admin/listas-precios`, body);
  }

  updateListaPrecio(id: number, body: UpsertListaPrecioRequest): Observable<AdminListaPrecio> {
    return this.http.put<AdminListaPrecio>(`${this.base}/api/admin/listas-precios/${id}`, body);
  }

  deleteListaPrecio(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/admin/listas-precios/${id}`);
  }

  listDetallesLista(idLista: number): Observable<AdminDetalleLista[]> {
    return this.http.get<AdminDetalleLista[]>(`${this.base}/api/admin/listas-precios/${idLista}/detalles`);
  }

  upsertDetalleLista(idLista: number, body: UpsertDetalleListaRequest): Observable<AdminDetalleLista> {
    return this.http.post<AdminDetalleLista>(`${this.base}/api/admin/listas-precios/${idLista}/detalles`, body);
  }

  updateDetalleLista(
    idLista: number,
    detalleId: number,
    body: { precioContado?: number; precioCheque?: number; precioCredito?: number; activo?: boolean },
  ): Observable<AdminDetalleLista> {
    return this.http.put<AdminDetalleLista>(`${this.base}/api/admin/listas-precios/${idLista}/detalles/${detalleId}`, body);
  }

  deleteDetalleLista(idLista: number, detalleId: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/admin/listas-precios/${idLista}/detalles/${detalleId}`);
  }

  searchEmbalajes(search?: string): Observable<AdminEmbalajeOption[]> {
    let params = new HttpParams();
    if (search) params = params.set('search', search);
    return this.http.get<AdminEmbalajeOption[]>(`${this.base}/api/admin/listas-precios/embalajes`, { params });
  }

  listClientes(opts: { search?: string; page?: number; pageSize?: number } = {}): Observable<PagedResult<AdminCliente>> {
    let params = new HttpParams()
      .set('page', String(opts.page ?? 1))
      .set('pageSize', String(opts.pageSize ?? 20));
    if (opts.search) params = params.set('search', opts.search);
    return this.http.get<PagedResult<AdminCliente>>(`${this.base}/api/admin/clientes`, { params });
  }

  getCliente(id: number): Observable<AdminCliente> {
    return this.http.get<AdminCliente>(`${this.base}/api/admin/clientes/${id}`);
  }

  createCliente(body: UpsertClienteRequest): Observable<AdminCliente> {
    return this.http.post<AdminCliente>(`${this.base}/api/admin/clientes`, body);
  }

  updateCliente(id: number, body: UpsertClienteRequest): Observable<AdminCliente> {
    return this.http.put<AdminCliente>(`${this.base}/api/admin/clientes/${id}`, body);
  }

  deleteCliente(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/admin/clientes/${id}`);
  }

  asignarListaCliente(id: number, idListaPrecio: number): Observable<AdminCliente> {
    return this.http.put<AdminCliente>(`${this.base}/api/admin/clientes/${id}/lista-precio`, { idListaPrecio });
  }

  historialListasCliente(id: number): Observable<AdminListaPrecioCuenta[]> {
    return this.http.get<AdminListaPrecioCuenta[]>(`${this.base}/api/admin/clientes/${id}/listas-precio`);
  }

  listSolicitudesMayorista(): Observable<AdminMayoristaSolicitud[]> {
    return this.http.get<AdminMayoristaSolicitud[]>(`${this.base}/api/admin/clientes/solicitudes-mayorista`);
  }

  aprobarMayorista(idUsuario: number, idListaPrecio?: number | null): Observable<{ ok: boolean }> {
    return this.http.post<{ ok: boolean }>(
      `${this.base}/api/admin/clientes/mayorista/${idUsuario}/aprobar`,
      { idListaPrecio: idListaPrecio ?? null },
    );
  }

  rechazarMayorista(idUsuario: number): Observable<{ ok: boolean }> {
    return this.http.post<{ ok: boolean }>(
      `${this.base}/api/admin/clientes/mayorista/${idUsuario}/rechazar`,
      {},
    );
  }

  listasVigentesCliente(): Observable<AdminListaPrecio[]> {
    return this.http.get<AdminListaPrecio[]>(`${this.base}/api/admin/clientes/listas-vigentes`);
  }

  listCarritos(opts: {
    search?: string;
    estado?: number;
    migrado?: boolean;
    fechaDesde?: string;
    fechaHasta?: string;
    page?: number;
    pageSize?: number;
  } = {}): Observable<PagedResult<AdminCarritoListItem>> {
    let params = new HttpParams()
      .set('page', String(opts.page ?? 1))
      .set('pageSize', String(opts.pageSize ?? 20));
    if (opts.search) params = params.set('search', opts.search);
    if (opts.estado != null) params = params.set('estado', String(opts.estado));
    if (opts.migrado != null) params = params.set('migrado', String(opts.migrado));
    if (opts.fechaDesde) params = params.set('fechaDesde', opts.fechaDesde);
    if (opts.fechaHasta) params = params.set('fechaHasta', opts.fechaHasta);
    return this.http.get<PagedResult<AdminCarritoListItem>>(`${this.base}/api/admin/carritos`, { params });
  }

  getCarrito(id: number): Observable<AdminCarritoDetail> {
    return this.http.get<AdminCarritoDetail>(`${this.base}/api/admin/carritos/${id}`);
  }

  migrarCarritos(ids: number[]): Observable<{ updatedCount: number }> {
    return this.http.post<{ updatedCount: number }>(`${this.base}/api/admin/carritos/migrar`, { ids });
  }

  exportarCarritos(body: {
    ids?: number[];
    format?: 'csv' | 'json';
    marcarMigrado?: boolean;
  } = {}): Observable<Blob> {
    return this.http.post(`${this.base}/api/admin/carritos/exportar`, {
      ids: body.ids ?? null,
      format: body.format ?? 'csv',
      marcarMigrado: body.marcarMigrado ?? true,
    }, { responseType: 'blob' });
  }

  migrarPedidos(ids: number[]): Observable<{ updatedCount: number }> {
    return this.http.post<{ updatedCount: number }>(`${this.base}/api/admin/orders/migrar`, { ids });
  }

  listPromotions(): Observable<AdminPromotionListItem[]> {
    return this.http.get<AdminPromotionListItem[]>(`${this.base}/api/admin/promotions`);
  }

  getPromotion(id: number): Observable<AdminPromotionDetail> {
    return this.http.get<AdminPromotionDetail>(`${this.base}/api/admin/promotions/${id}`);
  }

  createPromotion(body: UpsertPromotionRequest): Observable<AdminPromotionDetail> {
    return this.http.post<AdminPromotionDetail>(`${this.base}/api/admin/promotions`, body);
  }

  uploadPromotionImage(file: File, id?: number | null): Observable<{ url: string; idPromocion?: number | null }> {
    const fd = new FormData();
    fd.append('file', file, file.name);
    let url = `${this.base}/api/admin/promotions/imagen/upload`;
    if (id) url += `?id=${id}`;
    return this.http.post<{ url: string; idPromocion?: number | null }>(url, fd);
  }

  updatePromotion(id: number, body: UpsertPromotionRequest): Observable<AdminPromotionDetail> {
    return this.http.put<AdminPromotionDetail>(`${this.base}/api/admin/promotions/${id}`, body);
  }

  deletePromotion(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/admin/promotions/${id}`);
  }

  listTarifas(): Observable<AdminTarifaDelivery[]> {
    return this.http.get<AdminTarifaDelivery[]>(`${this.base}/api/admin/shipping/tarifas`);
  }

  updateTarifa(id: number, body: { importeDelivery?: number; activo?: boolean; estado?: number }): Observable<AdminTarifaDelivery> {
    return this.http.put<AdminTarifaDelivery>(`${this.base}/api/admin/shipping/tarifas/${id}`, body);
  }

  autorizarTarifas(ids: number[]): Observable<{ updatedCount: number }> {
    return this.http.post<{ updatedCount: number }>(`${this.base}/api/admin/shipping/tarifas/autorizar`, { ids });
  }

  anularTarifas(ids: number[]): Observable<{ updatedCount: number }> {
    return this.http.post<{ updatedCount: number }>(`${this.base}/api/admin/shipping/tarifas/anular`, { ids });
  }

  cancelarTarifas(ids: number[]): Observable<{ updatedCount: number }> {
    return this.http.post<{ updatedCount: number }>(`${this.base}/api/admin/shipping/tarifas/cancelar`, { ids });
  }

  autorizarListaPrecio(id: number): Observable<AdminListaPrecio> {
    return this.http.post<AdminListaPrecio>(`${this.base}/api/admin/listas-precios/${id}/autorizar`, {});
  }

  anularListaPrecio(id: number): Observable<AdminListaPrecio> {
    return this.http.post<AdminListaPrecio>(`${this.base}/api/admin/listas-precios/${id}/anular`, {});
  }

  cancelarListaPrecio(id: number): Observable<AdminListaPrecio> {
    return this.http.post<AdminListaPrecio>(`${this.base}/api/admin/listas-precios/${id}/cancelar`, {});
  }

  getEmpresaConfig(): Observable<AdminEmpresaConfig> {
    return this.http.get<AdminEmpresaConfig>(`${this.base}/api/admin/empresa/config`);
  }

  testErpConnection(draft?: Partial<{
    erpHost: string | null;
    erpPort: number | null;
    erpDatabase: string | null;
    erpUsername: string | null;
    erpPassword: string | null;
    erpSslMode: string | null;
    useSavedPasswordIfEmpty: boolean;
  }>): Observable<ErpConnectionTestResult> {
    return this.http.post<ErpConnectionTestResult>(
      `${this.base}/api/admin/empresa/config/test-erp`,
      draft ?? {},
    );
  }

  getErpHealth(): Observable<AdminErpHealth> {
    return this.http.get<AdminErpHealth>(`${this.base}/api/admin/empresa/config/erp-health`);
  }

  flushPendingErpOrders(): Observable<{ procesados: number; exitosos: number; errores: number; logs: unknown[] }> {
    return this.http.post<{ procesados: number; exitosos: number; errores: number; logs: unknown[] }>(
      `${this.base}/api/admin/empresa/config/erp-flush-pending`,
      {},
    );
  }

  listUnidadesMedida(): Observable<AdminUnidadMedida[]> {
    return this.http.get<AdminUnidadMedida[]>(`${this.base}/api/admin/unidades-medida`);
  }

  createUnidadMedida(body: Partial<AdminUnidadMedida>): Observable<AdminUnidadMedida> {
    return this.http.post<AdminUnidadMedida>(`${this.base}/api/admin/unidades-medida`, body);
  }

  updateUnidadMedida(id: number, body: Partial<AdminUnidadMedida>): Observable<AdminUnidadMedida> {
    return this.http.put<AdminUnidadMedida>(`${this.base}/api/admin/unidades-medida/${id}`, body);
  }

  listProductEmbalajes(idArticulo: number): Observable<AdminProductEmbalaje[]> {
    return this.http.get<AdminProductEmbalaje[]>(`${this.base}/api/admin/products/${idArticulo}/embalajes`);
  }

  createProductEmbalaje(idArticulo: number, body: Partial<AdminProductEmbalaje>): Observable<AdminProductEmbalaje> {
    return this.http.post<AdminProductEmbalaje>(`${this.base}/api/admin/products/${idArticulo}/embalajes`, body);
  }

  listCodigosBarra(idEmbalaje: number): Observable<AdminCodigoBarra[]> {
    return this.http.get<AdminCodigoBarra[]>(`${this.base}/api/admin/embalajes/${idEmbalaje}/codigos-barra`);
  }

  createCodigoBarra(
    idEmbalaje: number,
    body: { codigoBarra: string; paraSupermercado?: boolean; activo?: boolean },
  ): Observable<AdminCodigoBarra> {
    return this.http.post<AdminCodigoBarra>(`${this.base}/api/admin/embalajes/${idEmbalaje}/codigos-barra`, body);
  }

  deleteCodigoBarra(idEmbalaje: number, id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/admin/embalajes/${idEmbalaje}/codigos-barra/${id}`);
  }

  updateProductEmbalaje(
    idArticulo: number,
    id: number,
    body: Partial<AdminProductEmbalaje>,
  ): Observable<AdminProductEmbalaje> {
    return this.http.put<AdminProductEmbalaje>(`${this.base}/api/admin/products/${idArticulo}/embalajes/${id}`, body);
  }

  updateEmpresaConfig(body: Partial<AdminEmpresaConfig>): Observable<AdminEmpresaConfig> {
    return this.http.put<AdminEmpresaConfig>(`${this.base}/api/admin/empresa/config`, body);
  }

  getBranding(): Observable<StorefrontBranding> {
    return this.http.get<StorefrontBranding>(`${this.base}/api/admin/empresa/branding`);
  }

  updateBranding(body: Partial<UpdateBrandingRequest>): Observable<StorefrontBranding> {
    return this.http.put<StorefrontBranding>(`${this.base}/api/admin/empresa/branding`, body);
  }

  uploadBrandingAsset(kind: 'logo' | 'favicon' | 'banner', file: File): Observable<StorefrontBranding> {
    const fd = new FormData();
    fd.append('file', file, file.name);
    return this.http.post<StorefrontBranding>(`${this.base}/api/admin/empresa/branding/upload/${kind}`, fd);
  }

  listAgencias(): Observable<AdminAgencia[]> {
    return this.http.get<AdminAgencia[]>(`${this.base}/api/admin/empresa/agencias`);
  }

  listDepositos(): Observable<AdminDeposito[]> {
    return this.http.get<AdminDeposito[]>(`${this.base}/api/admin/empresa/depositos`);
  }

  createAgencia(body: UpsertAgenciaRequest): Observable<AdminAgencia> {
    return this.http.post<AdminAgencia>(`${this.base}/api/admin/empresa/agencias`, body);
  }

  updateAgencia(id: number, body: UpsertAgenciaRequest): Observable<AdminAgencia> {
    return this.http.put<AdminAgencia>(`${this.base}/api/admin/empresa/agencias/${id}`, body);
  }

  deleteAgencia(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/admin/empresa/agencias/${id}`);
  }

  listHorarios(idAgencia?: number): Observable<AdminHorario[]> {
    let params = new HttpParams();
    if (idAgencia) params = params.set('idAgencia', String(idAgencia));
    return this.http.get<AdminHorario[]>(`${this.base}/api/admin/empresa/horarios`, { params });
  }

  createHorario(body: UpsertHorarioRequest): Observable<AdminHorario> {
    return this.http.post<AdminHorario>(`${this.base}/api/admin/empresa/horarios`, body);
  }

  updateHorario(id: number, body: UpsertHorarioRequest): Observable<AdminHorario> {
    return this.http.put<AdminHorario>(`${this.base}/api/admin/empresa/horarios/${id}`, body);
  }

  deleteHorario(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/admin/empresa/horarios/${id}`);
  }

  listProductImages(productId: number): Observable<AdminProductImage[]> {
    return this.http.get<AdminProductImage[]>(`${this.base}/api/admin/products/${productId}/images`);
  }

  createProductImage(productId: number, body: UpsertProductImageRequest): Observable<AdminProductImage> {
    return this.http.post<AdminProductImage>(`${this.base}/api/admin/products/${productId}/images`, body);
  }

  uploadProductImage(
    productId: number,
    file: File,
    opts: { titulo?: string; esPrincipal?: boolean } = {},
  ): Observable<AdminProductImage> {
    const fd = new FormData();
    fd.append('file', file, file.name);
    if (opts.titulo) fd.append('titulo', opts.titulo);
    fd.append('esPrincipal', String(!!opts.esPrincipal));
    return this.http.post<AdminProductImage>(
      `${this.base}/api/admin/products/${productId}/images/upload`,
      fd,
    );
  }

  setPrincipalProductImage(productId: number, imageId: number): Observable<AdminProductImage> {
    return this.http.post<AdminProductImage>(
      `${this.base}/api/admin/products/${productId}/images/${imageId}/principal`,
      {},
    );
  }

  reorderProductImages(productId: number, orderedIds: number[]): Observable<void> {
    return this.http.post<void>(`${this.base}/api/admin/products/${productId}/images/reorder`, {
      orderedIds,
    });
  }

  updateProductImage(productId: number, imageId: number, body: UpsertProductImageRequest): Observable<AdminProductImage> {
    return this.http.put<AdminProductImage>(`${this.base}/api/admin/products/${productId}/images/${imageId}`, body);
  }

  deleteProductImage(productId: number, imageId: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/admin/products/${productId}/images/${imageId}`);
  }

  bulkActivoEcommerce(body: { ids: number[]; activoEcommerce: boolean; estadoEcommerce?: string }): Observable<{ updatedCount: number }> {
    return this.http.put<{ updatedCount: number }>(`${this.base}/api/admin/products/bulk-activo`, body);
  }

  listResenas(): Observable<any[]> {
    return this.http.get<any[]>(`${this.base}/api/admin/resenas`);
  }

  aprobarResena(id: number, aprobar: boolean): Observable<void> {
    return this.http.post<void>(`${this.base}/api/admin/resenas/${id}/aprobar?aprobar=${aprobar}`, {});
  }

  listCupones(): Observable<AdminCupon[]> {
    return this.http.get<AdminCupon[]>(`${this.base}/api/admin/cupones`);
  }

  crearCupon(body: {
    codigo: string;
    descripcion: string;
    tipoDescuento: string;
    valorDescuento: number;
    montoMinimo?: number;
    montoMaximoDescuento?: number | null;
    canalEcommerce?: string;
    fechaInicio?: string | null;
    fechaFin?: string | null;
    usosMaximos?: number | null;
    usosPorUsuario?: number;
  }): Observable<AdminCupon> {
    return this.http.post<AdminCupon>(`${this.base}/api/admin/cupones`, body);
  }

  toggleCupon(id: number, activo: boolean): Observable<void> {
    return this.http.post<void>(`${this.base}/api/admin/cupones/${id}/toggle?activo=${activo}`, {});
  }

  listContacto(leido?: boolean): Observable<AdminContactoMensaje[]> {
    let params = new HttpParams();
    if (leido === true) params = params.set('leido', 'true');
    if (leido === false) params = params.set('leido', 'false');
    return this.http.get<AdminContactoMensaje[]>(`${this.base}/api/contacto`, { params });
  }

  marcarContactoLeido(id: number, leido = true): Observable<void> {
    return this.http.post<void>(`${this.base}/api/contacto/${id}/leido?leido=${leido}`, {});
  }

  listDevoluciones(): Observable<any[]> {
    return this.http.get<any[]>(`${this.base}/api/admin/devoluciones`);
  }

  updateDevolucion(id: number, estado: string): Observable<void> {
    return this.http.post<void>(`${this.base}/api/admin/devoluciones/${id}/estado`, { estado });
  }

  listChats(): Observable<any[]> {
    return this.http.get<any[]>(`${this.base}/api/admin/chat`);
  }

  responderChat(idMensaje: number, mensaje: string): Observable<void> {
    return this.http.post<void>(`${this.base}/api/admin/chat/${idMensaje}/responder`, { mensaje });
  }

  listChatConversaciones(estado?: string, q?: string): Observable<AdminChatConversacion[]> {
    let params = new HttpParams();
    if (estado) params = params.set('estado', estado);
    if (q) params = params.set('q', q);
    return this.http.get<AdminChatConversacion[]>(`${this.base}/api/admin/chat/conversaciones`, { params });
  }

  getChatConversacion(id: number): Observable<AdminChatMensaje[]> {
    return this.http.get<AdminChatMensaje[]>(`${this.base}/api/admin/chat/conversaciones/${id}`);
  }

  responderConversacion(id: number, mensaje: string, estado?: string): Observable<AdminChatMensaje> {
    return this.http.post<AdminChatMensaje>(`${this.base}/api/admin/chat/conversaciones/${id}/responder`, {
      mensaje,
      estado: estado ?? null,
    });
  }

  responderConversacionFoto(id: number, file: File, mensaje?: string): Observable<AdminChatMensaje> {
    const fd = new FormData();
    fd.append('file', file);
    if (mensaje) fd.append('mensaje', mensaje);
    return this.http.post<AdminChatMensaje>(`${this.base}/api/admin/chat/conversaciones/${id}/upload`, fd);
  }

  asignarConversacion(id: number, body?: { idAdmin?: number | null; nombreAdmin?: string | null }): Observable<void> {
    return this.http.post<void>(`${this.base}/api/admin/chat/conversaciones/${id}/asignar`, body ?? {});
  }

  cambiarEstadoConversacion(id: number, estado: string): Observable<void> {
    return this.http.post<void>(`${this.base}/api/admin/chat/conversaciones/${id}/estado`, { estado });
  }

  resolveMediaUrl(url: string | null | undefined): string | null {
    if (!url) return null;
    let u = url.trim();
    u = u.replace(/https?:\/\/(localhost|127\.0\.0\.1):(5081|5082)/gi, this.base);
    // Flyers ERP: "GUID.JPG" (sin ruta) → /media/erp/GUID.JPG
    if (!u.includes('://') && !u.startsWith('/') && /\.(png|jpe?g|gif|webp|avif|ico|bmp)$/i.test(u)) {
      u = `/media/erp/${u.replace(/^\/+/, '')}`;
    }
    if (u.startsWith('/')) return `${this.base}${u}`;
    return u;
  }

  createTarifa(body: { idCiudad: number; idBarrio: number; importeDelivery: number; idAgencia?: number | null }): Observable<AdminTarifaDelivery> {
    return this.http.post<AdminTarifaDelivery>(`${this.base}/api/admin/shipping/tarifas`, body);
  }

  migrarPedidosErp(ids: number[]): Observable<{ procesados: number; exitosos: number; errores: number; logs: any[] }> {
    return this.http.post<{ procesados: number; exitosos: number; errores: number; logs: any[] }>(
      `${this.base}/api/admin/orders/migrar-erp`,
      { ids },
    );
  }

  reintentarMigracionPedido(id: number): Observable<{ procesados: number; exitosos: number; errores: number; logs: any[] }> {
    return this.http.post<{ procesados: number; exitosos: number; errores: number; logs: any[] }>(
      `${this.base}/api/admin/orders/${id}/reintentar-migracion`,
      {},
    );
  }

  listMigracionLogs(): Observable<any[]> {
    return this.http.get<any[]>(`${this.base}/api/admin/orders/migracion-logs`);
  }

  syncStatus(): Observable<any> {
    return this.http.get<any>(`${this.base}/api/admin/sync/status`);
  }

  /** @deprecated Inbound solo vía ecom-etl — POST sync/run responde 410. */
  syncRun(_entityKeys?: string[]): Observable<any> {
    return throwError(() => ({
      error: {
        error:
          'El pull ERP→ecom lo hace ecom-etl. La API ya no ejecuta sync/run.',
      },
      status: 410,
    }));
  }

  exportPlanilla(
    kind: 'clientes' | 'articulos' | 'lista',
    listaId?: number,
    format: 'csv' | 'xlsx' = 'csv',
  ): Observable<Blob> {
    const base =
      kind === 'clientes'
        ? `${this.base}/api/admin/planillas/clientes/export`
        : kind === 'articulos'
          ? `${this.base}/api/admin/planillas/articulos/export`
          : `${this.base}/api/admin/planillas/listas-precios/${listaId}/export`;
    const url = `${base}?format=${format}`;
    return this.http.get(url, { responseType: 'blob' });
  }

  importPlanilla(kind: 'clientes' | 'articulos' | 'lista', file: File, listaId?: number): Observable<any> {
    const fd = new FormData();
    fd.append('file', file);
    const url =
      kind === 'clientes'
        ? `${this.base}/api/admin/planillas/clientes/import`
        : kind === 'articulos'
          ? `${this.base}/api/admin/planillas/articulos/import-activo`
          : `${this.base}/api/admin/planillas/listas-precios/${listaId}/import`;
    return this.http.post(url, fd);
  }

  exportAbandonados(hours = 24): Observable<Blob> {
    return this.http.get(`${this.base}/api/admin/carritos/abandonados/export?hours=${hours}`, {
      responseType: 'blob',
    });
  }

  reportVentas(from: string, to: string): Observable<VentasResumenDto> {
    return this.http.get<VentasResumenDto>(`${this.base}/api/admin/reports/ventas`, {
      params: new HttpParams().set('from', from).set('to', to),
    });
  }

  personalizacionSchemas(): Observable<Record<string, string>> {
    return this.http.get<Record<string, string>>(`${this.base}/api/admin/personalizacion/schemas`);
  }

  listCmsBlocks(pageCode?: string): Observable<CmsBlock[]> {
    let params = new HttpParams();
    if (pageCode) params = params.set('pageCode', pageCode);
    return this.http.get<CmsBlock[]>(`${this.base}/api/admin/content/blocks`, { params });
  }

  createCmsBlock(body: UpsertCmsBlockRequest): Observable<CmsBlock> {
    return this.http.post<CmsBlock>(`${this.base}/api/admin/content/blocks`, body);
  }

  ensureHomeCmsBlocks(): Observable<{ added: number; mensaje: string }> {
    return this.http.post<{ added: number; mensaje: string }>(
      `${this.base}/api/admin/content/blocks/ensure-home`,
      {},
    );
  }

  updateCmsBlock(id: number, body: UpsertCmsBlockRequest): Observable<CmsBlock> {
    return this.http.put<CmsBlock>(`${this.base}/api/admin/content/blocks/${id}`, body);
  }

  uploadCmsImage(file: File): Observable<{ url: string; originalFileName: string; sizeBytes: number }> {
    const fd = new FormData();
    fd.append('file', file, file.name);
    return this.http.post<{ url: string; originalFileName: string; sizeBytes: number }>(
      `${this.base}/api/admin/content/media/upload`,
      fd,
    );
  }

  listCmsMenus(): Observable<CmsNavMenu[]> {
    return this.http.get<CmsNavMenu[]>(`${this.base}/api/admin/content/menus`);
  }

  createCmsMenu(body: UpsertCmsNavMenuRequest): Observable<CmsNavMenu> {
    return this.http.post<CmsNavMenu>(`${this.base}/api/admin/content/menus`, body);
  }

  updateCmsMenu(id: number, body: UpsertCmsNavMenuRequest): Observable<CmsNavMenu> {
    return this.http.put<CmsNavMenu>(`${this.base}/api/admin/content/menus/${id}`, body);
  }

  deleteCmsMenu(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/admin/content/menus/${id}`);
  }

  listAudit(take = 100): Observable<AdminAuditItem[]> {
    return this.http.get<AdminAuditItem[]>(`${this.base}/api/admin/content/audit`, {
      params: new HttpParams().set('take', String(take)),
    });
  }

  getPersonalizacion(clave: string): Observable<PersonalizacionItem> {
    return this.http.get<PersonalizacionItem>(
      `${this.base}/api/admin/personalizacion/${encodeURIComponent(clave)}`,
    );
  }

  putPersonalizacion(
    clave: string,
    body: {
      valorJson: string;
      fechaInicioVigencia?: string | null;
      fechaFinVigencia?: string | null;
      nota?: string | null;
      activo?: boolean;
    },
  ): Observable<PersonalizacionItem> {
    return this.http.put<PersonalizacionItem>(
      `${this.base}/api/admin/personalizacion/${encodeURIComponent(clave)}`,
      body,
    );
  }

  programarPersonalizacion(
    clave: string,
    body: {
      valorJson: string;
      fechaInicioVigencia: string;
      fechaFinVigencia: string;
      nota?: string | null;
    },
  ): Observable<PersonalizacionItem> {
    return this.http.post<PersonalizacionItem>(
      `${this.base}/api/admin/personalizacion/${encodeURIComponent(clave)}/programar`,
      body,
    );
  }

  listPersonalizacionVersiones(clave: string): Observable<PersonalizacionItem[]> {
    return this.http.get<PersonalizacionItem[]>(
      `${this.base}/api/admin/personalizacion/${encodeURIComponent(clave)}/versiones`,
    );
  }

  getCiudadesDelivery(): Observable<AdminCiudadDelivery[]> {
    return this.http.get<AdminCiudadDelivery[]>(`${this.base}/api/geography/ciudades-delivery`);
  }

  getBarriosDelivery(ciudadId?: number): Observable<AdminBarrioDelivery[]> {
    let params = new HttpParams();
    if (ciudadId != null) params = params.set('ciudadId', ciudadId);
    return this.http.get<AdminBarrioDelivery[]>(`${this.base}/api/geography/barrios-delivery`, { params });
  }

  listTiposDocumento(): Observable<AdminTipoDocumento[]> {
    return this.http.get<AdminTipoDocumento[]>(`${this.base}/api/geography/tipos-documento`);
  }
}

export interface AdminTipoDocumento {
  id: number;
  codigo: number;
  nombre: string;
  esRuc: boolean;
}

export interface AdminChatConversacion {
  id: number;
  idUsuarioEcommerce: number;
  email: string;
  nombreCliente: string | null;
  asunto: string;
  estado: string;
  idPedidoMovil: number | null;
  idAdminAsignado: number | null;
  nombreAdminAsignado: string | null;
  fechaInicio: string;
  fechaUltimoMensaje: string;
  previewUltimoMensaje: string | null;
  noLeidosCliente: number;
  noLeidosSoporte: number;
}

export interface PersonalizacionItem {
  clave: string;
  valorJson: string;
  version: number;
  fechaInicioVigencia: string | null;
  fechaFinVigencia: string | null;
  activo: boolean;
  fechaCreacion: string;
  creadoPor: string | null;
  nota: string | null;
}

export interface AdminContactoMensaje {
  id: number;
  nombre: string;
  email: string;
  telefono: string | null;
  mensaje: string;
  fechaUtc: string;
  leido: boolean;
}

export interface AdminCupon {
  id: number;
  codigo: string;
  descripcion: string;
  tipoDescuento: string;
  valorDescuento: number;
  montoMinimo: number;
  canalEcommerce: string;
  usosActuales: number;
  usosMaximos: number | null;
  activo: boolean;
}

export interface CmsBlockItem {
  id: number;
  itemType: string;
  title: string | null;
  subtitle: string | null;
  description: string | null;
  imageUrl: string | null;
  mobileImageUrl: string | null;
  linkUrl: string | null;
  linkTarget: string | null;
  badgeText: string | null;
  priceText: string | null;
  displayOrder: number;
  activo: boolean;
}

export interface CmsNavMenuItem {
  id: number;
  itemType: string;
  label: string;
  url: string | null;
  target: string | null;
  badgeText: string | null;
  displayOrder: number;
  idGrupoArticulo: number | null;
  activo: boolean;
}

export interface CmsNavMenu {
  id: number;
  code: string;
  name: string;
  menuType: string;
  pageCode: string | null;
  pageZone: string | null;
  displayOrder: number;
  contentStatus: string;
  activo: boolean;
  items: CmsNavMenuItem[];
}

export interface UpsertCmsNavMenuRequest {
  code: string;
  name: string;
  menuType: string;
  pageCode: string | null;
  pageZone: string | null;
  displayOrder: number;
  contentStatus: string;
  activo: boolean;
  items?: {
    id?: number;
    itemType: string;
    label: string;
    url: string | null;
    target: string | null;
    badgeText: string | null;
    displayOrder: number;
    idGrupoArticulo: number | null;
    activo: boolean;
  }[];
}

export interface CmsBlock {
  id: number;
  pageCode: string;
  pageZone: string | null;
  blockType: string;
  internalName: string;
  title: string | null;
  subtitle: string | null;
  description: string | null;
  displayOrder: number;
  contentStatus: string;
  activo: boolean;
  isReadOnlyWhenIntegrated: boolean;
  startDate: string | null;
  endDate: string | null;
  settingsJson: string | null;
  items: CmsBlockItem[];
}

export interface UpsertCmsBlockRequest {
  pageCode: string;
  pageZone: string | null;
  blockType: string;
  internalName: string;
  title: string | null;
  subtitle: string | null;
  description: string | null;
  displayOrder: number;
  contentStatus: string;
  activo: boolean;
  isReadOnlyWhenIntegrated: boolean;
  startDate: string | null;
  endDate: string | null;
  settingsJson: string | null;
  items?: {
    id?: number;
    itemType: string;
    title: string | null;
    subtitle: string | null;
    description: string | null;
    imageUrl: string | null;
    mobileImageUrl: string | null;
    linkUrl: string | null;
    linkTarget: string | null;
    badgeText: string | null;
    priceText: string | null;
    displayOrder: number;
    activo: boolean;
  }[];
}

export interface AdminAuditItem {
  id: number;
  usuario: string;
  accion: string;
  entidad: string;
  entidadId: string | null;
  detalle: string | null;
  fecha: string;
}

export interface AdminChatMensaje {
  id: number;
  idConversacion: number | null;
  idPedidoMovil: number | null;
  email: string;
  origen: string;
  tipoMensaje: string;
  mensaje: string;
  fechaMensaje: string;
  leido: boolean;
  adjunto?: { url: string | null; mimeType: string | null; nombreArchivo: string | null } | null;
  nombreAutor?: string | null;
}

export interface TenantSummary {
  idEmpresa: number;
  razonSocial: string;
  nombreFantasia: string | null;
  numeroDocumento: string | null;
  slugTienda: string | null;
  dominioCustom: string | null;
  activo: boolean;
  procesaPedidosERP: boolean;
  realizaDelivery: boolean;
  realizaPickup: boolean;
  controlaStockEnCarrito: boolean;
  idDeposito: number;
  idArticuloDelivery: number | null;
  sourceHint: number;
  runtimeMode: number;
}

export interface ProvisionResult {
  idEmpresa: number;
  slugTienda: string;
  source: number;
  mensaje: string;
  adminUsuario: string | null;
  adminPasswordPlainDev: string | null;
}

export interface ManualTenantRequest {
  razonSocial: string;
  nombreFantasia?: string | null;
  numeroDocumento: string;
  slugTienda: string;
  dominioCustom?: string | null;
  realizaDelivery?: boolean;
  realizaPickup?: boolean;
  controlaStockEnCarrito?: boolean;
  montoPedidoMinimo?: number;
  adminUsuario?: string | null;
  adminPassword?: string | null;
  adminEmail?: string | null;
}

export interface ErpTenantRequest {
  idEmpresa: number;
  razonSocial: string;
  nombreFantasia?: string | null;
  numeroDocumento?: string | null;
  slugTienda: string;
  dominioCustom?: string | null;
  idDepositoErp?: number | null;
  idArticuloDeliveryErp?: number | null;
  realizaDelivery?: boolean;
  realizaPickup?: boolean;
  controlaStockEnCarrito?: boolean;
  procesaPedidosERP?: boolean;
  montoPedidoMinimo?: number;
  createLocalDefaultsIfMissing?: boolean;
}

export interface AdminFuncionalidadDto {
  id: number;
  codigoPermission: string;
  descripcion: string;
  idFuncionalidadPadre?: number | null;
  orden?: number;
}

export interface AdminPermissionTreeNodeDto {
  key: string;
  codigoPermission: string;
  title: string;
  folder: boolean;
  selected: boolean;
  unselectable: boolean;
  source: 'P' | 'R' | null;
  children: AdminPermissionTreeNodeDto[];
}

export interface AdminUserPermissionTreeDto {
  idUsuario: number;
  usuario: string;
  roleCodes: string[];
  tree: AdminPermissionTreeNodeDto[];
}

export interface AdminRoleDto {
  id: number;
  idEmpresa: number;
  codigoRol: string;
  nombreRol: string;
  activo: boolean;
  ultModificacion: string;
  permisosCount: number;
  permissionCodes: string[];
}

export interface AdminRoleDetailDto {
  id: number;
  idEmpresa: number;
  codigoRol: string;
  nombreRol: string;
  activo: boolean;
  ultModificacion: string;
  permissionCodes: string[];
}

export interface AdminUserRoleDto {
  idRol: number;
  codigoRol: string;
  nombreRol: string;
}

export interface AdminUserDto {
  id: number;
  idEmpresa: number;
  usuario: string;
  nombre?: string | null;
  email?: string | null;
  activo: boolean;
  ultModificacion: string;
  roles: AdminUserRoleDto[];
}

export interface AdminPromotionListItem {
  id: number;
  codGrupoArticulo: number;
  nombre: string;
  fechaInicioVigencia: string;
  fechaFinVigencia: string;
  tipoGrupo: number;
  estado: number;
  orden: number;
  imagen: string | null;
  activo: boolean;
  articulosCount: number;
}

export interface AdminPromotionDetail {
  id: number;
  codGrupoArticulo: number;
  nombre: string;
  fechaInicioVigencia: string;
  fechaFinVigencia: string;
  tipoGrupo: number;
  estado: number;
  orden: number;
  imagen: string | null;
  subtitulo?: string | null;
  urlDestino?: string | null;
  activo: boolean;
  articulos: { idArticulo: number; sku: string | null; nombre: string }[];
}

export interface UpsertPromotionRequest {
  nombre: string;
  fechaInicioVigencia: string;
  fechaFinVigencia: string;
  tipoGrupo?: number;
  estado?: number;
  orden?: number;
  imagen?: string | null;
  subtitulo?: string | null;
  urlDestino?: string | null;
  activo?: boolean;
  articuloIds?: number[];
}

export interface AdminTarifaDelivery {
  id: number;
  idCiudad: number;
  idBarrio: number;
  nombreCiudad: string;
  nombreBarrio: string;
  importeDelivery: number;
  fechaInicioVigencia: string;
  fechaFinVigencia: string;
  estado: number;
  activo: boolean;
}

export interface AdminEmpresaConfig {
  idEmpresa: number;
  razonSocial: string;
  nombreFantasia: string | null;
  numeroDocumento: string | null;
  slugTienda: string | null;
  dominioCustom: string | null;
  realizaDelivery: boolean;
  realizaPickup: boolean;
  cobraEnLaEntrega: boolean;
  cobraAlHacerPedido: boolean;
  montoPedidoMinimo: number;
  procesaPedidosPorPlanilla: boolean;
  procesaPedidosERP: boolean;
  controlaStockEnCarrito: boolean;
  modoIntegracion: string;
  syncStockContinuo?: boolean;
  migrarPedidosAlGenerar?: boolean;
  syncStockIntervaloMinutos?: number;
  idDeposito: number;
  idArticuloDelivery: number | null;
  direccionURLFacebook: string | null;
  direccionURLTwitter: string | null;
  direccionURLInstagram: string | null;
  direccionURLYoutube: string | null;
  direccionURLWhatsapp: string | null;
  clavePublicaProcesadoraTarjeta?: string | null;
  tieneClavePrivada?: boolean;
  clavePrivadaProcesadoraTarjeta?: string | null;
  direccionURLCheckout?: string | null;
  codigoComercio?: number | null;
  codigoSucursal?: number | null;
  montoUmbralMayorista?: number;
  usarListaPrecioCuentaErp?: boolean;
  idListaPrecioMayoristaDefault?: number | null;
  codEmpresa?: number;
  direccionEmpresa?: string | null;
  telefono?: string | null;
  fax?: string | null;
  eMail?: string | null;
  representanteLegal?: string | null;
  idPais?: number | null;
  logoEmpresa?: string | null;
  tiempoAproxEntregaPickup?: string | null;
  tiempoAproxEntregaDelivery?: string | null;
  nombreServidorCorreoElectronico?: string | null;
  nombreUsuarioCorreoElectronico?: string | null;
  tienePasswordCorreo?: boolean;
  passwordCorreoElectronico?: string | null;
  puertoCorreoElectronico?: number | null;
  nroUltPedidoCarritoCompra?: number;
  ultIDProcesoNegocio?: number;
  redesSociales?: AdminEmpresaRedSocial[];
  contenidoFaqHtml?: string | null;
  contenidoTerminosHtml?: string | null;
  contenidoComoComprarHtml?: string | null;
  deliveryEmiteFactura?: boolean;
  deliveryEmiteTicket?: boolean;
  pickupEmiteFactura?: boolean;
  pickupEmiteTicket?: boolean;
  creditoDesarrolladoPor?: string | null;
  creditoDesarrolladoPorUrl?: string | null;
  erpHost?: string | null;
  erpPort?: number | null;
  erpDatabase?: string | null;
  erpUsername?: string | null;
  /** Solo escritura: no vuelve del GET. */
  erpPassword?: string | null;
  erpSslMode?: string | null;
  erpAllowWriteBack?: boolean;
  tieneErpPassword?: boolean;
  erpConnectionConfigured?: boolean;
  erpConnectionSource?: string;
  erpWriteBackEffective?: boolean;
  clearErpPassword?: boolean;
  clearErpConnection?: boolean;
  activarIntegracionErpCompleta?: boolean;
}

export interface ErpConnectionTestResult {
  ok: boolean;
  message: string;
  serverVersionMajor?: number | null;
  source?: string | null;
  host?: string | null;
  database?: string | null;
  canReadEmpresa?: boolean | null;
  canReadStock?: boolean | null;
  stockRowsForDeposito?: number | null;
  idDepositoProbed?: number | null;
}

export interface AdminErpHealth {
  hasConnection: boolean;
  connectionSource: string;
  writeBackEffective: boolean;
  syncStockContinuo: boolean;
  syncStockIntervaloMinutos: number;
  migrarPedidosAlGenerar: boolean;
  procesaPedidosERP: boolean;
  modoIntegracion: string;
  idDeposito: number;
  lastStockSyncUtc: string | null;
  lastStockStatus: string | null;
  lastStockRows: number;
  lastStockError: string | null;
  pendingOrdersWriteBack: number;
  hint: string | null;
  nextStockDueUtc?: string | null;
  secondsUntilNextStock?: number | null;
  stockDueNow?: boolean;
}

export interface AdminEmpresaRedSocial {
  id: number;
  nombreRedSocial: string;
  direccionURLRedSocial: string;
  logoRedSocial: string;
  activo: boolean;
}

export interface AdminUnidadMedida {
  id: number;
  codigo: string;
  nombre: string;
  simbolo: string | null;
  permiteDecimales: boolean;
  incrementoDefault: number;
  orden: number;
  activo: boolean;
}

export interface AdminProductEmbalaje {
  id: number;
  idArticulo: number;
  descripcion: string | null;
  cantidadPorEmbalaje: number;
  codigoBarras: string | null;
  codigoUnidadMedida: string;
  permiteFraccion: boolean;
  incrementoVenta: number;
  cantidadMinimaVenta: number;
  paraFacturacion: boolean;
  paraStock: boolean;
  activo: boolean;
  precioDemo: number | null;
}

export interface StorefrontBranding {
  idEmpresa: number;
  nombreTienda: string;
  tagline: string | null;
  logoUrl: string | null;
  faviconUrl: string | null;
  bannerUrl: string | null;
  colorPrimario: string | null;
  colorSecundario: string | null;
  colorAcento: string | null;
  logoMark: string;
  facebook: string | null;
  twitter: string | null;
  instagram: string | null;
  youtube: string | null;
  whatsapp: string | null;
  tiktok?: string | null;
  footerDescripcion?: string | null;
  direccionLocal?: string | null;
  ciudadLocal?: string | null;
  telefonoContacto?: string | null;
  emailContacto?: string | null;
  horarioAtencion?: string | null;
  latitud?: number | null;
  longitud?: number | null;
  mostrarMapa?: boolean;
  realizaDelivery?: boolean;
  realizaPickup?: boolean;
  cobraEnLaEntrega?: boolean;
  cobraAlHacerPedido?: boolean;
  tiempoAproxEntregaPickup?: string | null;
  tiempoAproxEntregaDelivery?: string | null;
  contenidoFaqHtml?: string | null;
  contenidoTerminosHtml?: string | null;
  contenidoComoComprarHtml?: string | null;
  deliveryEmiteFactura?: boolean;
  deliveryEmiteTicket?: boolean;
  pickupEmiteFactura?: boolean;
  pickupEmiteTicket?: boolean;
  creditoDesarrolladoPor?: string | null;
  creditoDesarrolladoPorUrl?: string | null;
}

export interface UpdateBrandingRequest {
  nombreTiendaDisplay?: string | null;
  taglineTienda?: string | null;
  logoMark?: string | null;
  colorPrimario?: string | null;
  colorSecundario?: string | null;
  colorAcento?: string | null;
  direccionURLFacebook?: string | null;
  direccionURLTwitter?: string | null;
  direccionURLInstagram?: string | null;
  direccionURLYoutube?: string | null;
  direccionURLWhatsapp?: string | null;
  direccionURLTikTok?: string | null;
  footerDescripcion?: string | null;
  direccionLocal?: string | null;
  ciudadLocal?: string | null;
  telefonoContacto?: string | null;
  emailContacto?: string | null;
  horarioAtencionTexto?: string | null;
  latitudLocal?: number | null;
  longitudLocal?: number | null;
  mostrarMapaFooter?: boolean | null;
  clearCoords?: boolean;
  clearLogo?: boolean;
  clearFavicon?: boolean;
  clearBanner?: boolean;
  contenidoFaqHtml?: string | null;
  contenidoTerminosHtml?: string | null;
  contenidoComoComprarHtml?: string | null;
  creditoDesarrolladoPor?: string | null;
  creditoDesarrolladoPorUrl?: string | null;
}

export interface AdminAgencia {
  id: number;
  codAgencia: number;
  descripcionAgencia: string;
  direccion: string | null;
  idCiudad: number | null;
  idDeposito: number | null;
  latitud: number | null;
  longitud: number | null;
  esSucursalEcommerce: boolean;
  activo: boolean;
}

export interface AdminDeposito {
  id: number;
  codigo: number;
  descripcion: string;
  esDepositoEcommerce: boolean;
}

export interface UpsertAgenciaRequest {
  codAgencia?: number | null;
  descripcionAgencia: string;
  direccion?: string | null;
  idCiudad?: number | null;
  idDeposito?: number | null;
  latitud?: number | null;
  longitud?: number | null;
  esSucursalEcommerce?: boolean;
  activo?: boolean;
}

export interface AdminHorario {
  id: number;
  idAgencia: number;
  nombreAgencia: string | null;
  diaSemana: number;
  horaPrimeraApertura: string;
  horaPrimerCierre: string;
  horaSegundaApertura: string;
  horaSegundoCierre: string;
  activo: boolean;
}

export interface UpsertHorarioRequest {
  idAgencia: number;
  diaSemana: number;
  horaPrimeraApertura: string;
  horaPrimerCierre: string;
  horaSegundaApertura?: string | null;
  horaSegundoCierre?: string | null;
  activo?: boolean;
}

export interface AdminProductImage {
  id: number;
  idArticulo: number;
  url: string;
  titulo: string | null;
  orden: number;
  esPrincipal: boolean;
  activo: boolean;
}

export interface UpsertProductImageRequest {
  url: string;
  titulo?: string | null;
  orden?: number;
  esPrincipal?: boolean;
  activo?: boolean;
}

export interface AdminListaPrecio {
  id: number;
  nroListaPrecio: number;
  descripcion: string;
  esListaPrecioEcommerce: boolean;
  precioConImpuestos: boolean;
  canalEcommerce: string;
  fechaInicioVigencia: string | null;
  fechaFinVigencia: string | null;
  activo: boolean;
  estado: number;
  detalleCount: number;
  cantidadMinimaMayorista?: number;
  porDescuentoMinorista?: number;
  porDescuentoMayorista?: number;
}

export interface UpsertListaPrecioRequest {
  descripcion: string;
  nroListaPrecio?: number | null;
  esListaPrecioEcommerce?: boolean;
  precioConImpuestos?: boolean;
  canalEcommerce?: string;
  fechaInicioVigencia?: string | null;
  fechaFinVigencia?: string | null;
  activo?: boolean;
  cantidadMinimaMayorista?: number;
  porDescuentoMinorista?: number;
  porDescuentoMayorista?: number;
}

export interface AdminDetalleLista {
  id: number;
  idArticuloEmbalaje: number;
  idArticulo: number;
  sku: string | null;
  nombreArticulo: string;
  descripcionEmbalaje: string | null;
  precioContado: number;
  precioCheque: number;
  precioCredito: number;
  cantidadMinimaMayorista: number;
  activo: boolean;
}

export interface UpsertDetalleListaRequest {
  idArticuloEmbalaje: number;
  precioContado: number;
  precioCheque?: number;
  precioCredito?: number;
  cantidadMinimaMayorista?: number;
  activo?: boolean;
}

export interface AdminEmbalajeOption {
  idArticuloEmbalaje: number;
  idArticulo: number;
  sku: string | null;
  nombreArticulo: string;
  descripcionEmbalaje: string | null;
  precioDemo: number | null;
}

export interface AdminCliente {
  id: number;
  nroCuenta: number;
  razonSocial: string;
  nombreCuenta: string | null;
  apellidoCuenta: string | null;
  email: string | null;
  direccion: string | null;
  idListaPrecio: number | null;
  listaDescripcion: string | null;
  idAgencia: number | null;
  activo: boolean;
  ultModificacion: string;
  idTipoDocumento?: number | null;
  numeroDocumento?: string | null;
  telefono?: string | null;
  idUsuarioEcommerce?: number | null;
}

export interface AdminListaPrecioCuenta {
  id: number;
  idCuenta: number;
  idListaPrecio: number;
  listaDescripcion: string | null;
  nroListaPrecio: number | null;
  activo: boolean;
  ultModificacion: string;
  userName: string | null;
}

export interface AdminMayoristaSolicitud {
  idUsuarioEcommerce: number;
  nombre: string;
  email: string | null;
  estadoMayorista: string;
  fechaSolicitud: string | null;
  motivo: string | null;
  montoComprasAcumulado: number;
  idCuenta: number | null;
  razonSocial: string | null;
  idListaPrecio: number | null;
}

export interface UpsertClienteRequest {
  razonSocial: string;
  nombreCuenta?: string | null;
  apellidoCuenta?: string | null;
  email?: string | null;
  direccion?: string | null;
  idListaPrecio?: number | null;
  idAgencia?: number | null;
  activo?: boolean;
  idTipoDocumento?: number | null;
  numeroDocumento?: string | null;
  telefono?: string | null;
}

export interface AdminCarritoListItem {
  id: number;
  nroPedidoCarritoCompra: number;
  fechaPedido: string;
  razonSocial: string;
  numeroDocumento: string;
  montoTotal: number;
  tipoEntrega: number;
  tipoPago: number;
  estado: number;
  migrado: boolean;
  idPedidoMovil: number | null;
  idCuenta: number | null;
  itemsCount: number;
  lugarEntrega: string | null;
}

export interface AdminCarritoDetail {
  id: number;
  nroPedidoCarritoCompra: number;
  fechaPedido: string;
  fechaPrevistaEntrega: string;
  razonSocial: string;
  numeroDocumento: string;
  observacion: string | null;
  lugarEntrega: string | null;
  tipoEntrega: number;
  tipoPago: number;
  estado: number;
  migrado: boolean;
  idPedidoMovil: number | null;
  idCuenta: number | null;
  idListaPrecio: number | null;
  idDeposito: number;
  montoTotal: number;
  montoIVA: number;
  montoDescuento: number;
  latitudEntrega: number | null;
  longitudEntrega: number | null;
  ultModificacion: string;
  items: {
    id: number;
    idArticulo: number;
    sku: string | null;
    nombre: string;
    cantidadEmbalada: number;
    precioUnitario: number;
    montoLinea: number;
  }[];
  trazabilidad: {
    codigo: string;
    etiqueta: string;
    completado: boolean;
    fecha: string | null;
    detalle: string | null;
  }[];
}

export interface VentasDiaDto {
  fecha: string;
  pedidosCount: number;
  montoTotal: number;
}

export interface TopSkuDto {
  idArticulo: number;
  sku: string | null;
  nombre: string;
  cantidadVendida: number;
  montoTotal: number;
}

export interface VentasResumenDto {
  desde: string;
  hasta: string;
  totalPedidos: number;
  montoTotal: number;
  ticketPromedio: number;
  carritosAbiertos: number;
  carritosCerrados: number;
  conversionPct: number;
  porDia: VentasDiaDto[];
  topSkus: TopSkuDto[];
}

export interface AdminCodigoBarra {
  id: number;
  idArticuloEmbalaje: number;
  codigoBarra: string;
  paraSupermercado: boolean;
  activo: boolean;
}

export interface AdminCiudadDelivery {
  id: number;
  nombre: string;
  divisionPolitica: string | null;
}

export interface AdminBarrioDelivery {
  id: number;
  idCiudad: number;
  nombre: string;
  importeDelivery: number | null;
}
