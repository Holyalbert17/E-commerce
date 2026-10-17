import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { getAccessToken, setAccessToken, setTenantSlug } from './tenant.interceptor';
import { SessionService } from './session.service';

export type Canal = 'B2C' | 'B2B';

const CANAL_KEY = 'ecom_canal';

export function getCanal(): Canal {
  const v = localStorage.getItem(CANAL_KEY);
  const mayorista = localStorage.getItem('ecom_mayorista') === '1';
  return v === 'B2B' && mayorista ? 'B2B' : 'B2C';
}

export function setCanal(canal: Canal): void {
  if (canal === 'B2B' && localStorage.getItem('ecom_mayorista') !== '1') {
    canal = 'B2C';
  }
  localStorage.setItem(CANAL_KEY, canal);
}

export function isLoggedIn(): boolean {
  return !!getAccessToken();
}

/** Muestra la factura en un visor de la misma página e invita a imprimir. */
export function openPdfBlob(blob: Blob, fileName: string, preview?: Window | null): boolean {
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

export async function readPdfError(blob: Blob | null | undefined): Promise<string> {
  if (!blob) return 'No se pudo cargar la factura';
  try {
    const text = await blob.text();
    const j = JSON.parse(text) as { error?: string };
    return j.error || 'No se pudo cargar la factura';
  } catch {
    return 'No se pudo cargar la factura';
  }
}

export interface TokenResponse {
  accessToken: string;
  expiresAtUtc: string;
  idEmpresa: number;
  idUsuarioEcommerce: number;
  nombre: string;
  canalPreferido: string;
}

export interface SignUpRequest {
  nombre: string;
  apellido: string;
  idTipoDocumento: number;
  numeroDocumento: string;
  telefono: string;
  movil: string;
  email: string;
  password: string;
  passwordConfirmation: string;
  tenantSlug?: string;
}

export interface SignUpResult {
  idUsuarioEcommerce: number;
  email: string;
  mensaje: string;
  activationUrlDev: string | null;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface ProductListItem {
  id: number;
  sku: string | null;
  nombre: string;
  nombreCorto: string;
  marca: string | null;
  estadoEcommerce: string;
  imagenPrincipalUrl: string | null;
  precioDesde: number | null;
  embalajesCount: number;
  canal: string;
  listaPrecio: string | null;
  stockLibre: number | null;
  stockOk: boolean;
  slugSeo?: string | null;
  precioLista?: number | null;
  porcentajeDescuento?: number | null;
}

export interface SearchDoc {
  id: number;
  sku?: string | null;
  nombre: string;
  nombreCorto?: string | null;
  marca?: string | null;
  imagenPrincipalUrl?: string | null;
  precioDesde?: number | null;
  stockLibre?: number | null;
  stockOk?: boolean;
  slugSeo?: string | null;
  estadoEcommerce?: string | null;
}

export interface SearchSuggestResult {
  query: string;
  items: SearchDoc[];
  source: string;
  tookMs: number;
  fromCache: boolean;
}

export interface SearchSnapshot {
  idEmpresa: number;
  tenantSlug: string | null;
  generatedUtc: string;
  totalDocs: number;
  docs: SearchDoc[];
  engine: string;
}

export interface CollectionItem {
  id: number;
  codigo: number;
  nombre: string;
  imagenUrl: string | null;
  articulosCount: number;
}

export interface ProductDetail {
  id: number;
  sku: string | null;
  nombre: string;
  nombreCorto: string;
  descripcionExtensa: string;
  codReferenciaProveedor: string | null;
  marca: string | null;
  estadoEcommerce: string;
  fichaTecnicaUrl: string | null;
  videoUrl: string | null;
  canal: string;
  listaPrecio: string | null;
  stockLibre: number | null;
  stockOk: boolean;
  depositoNombre: string | null;
  promedioCalificacion?: number;
  totalResenas?: number;
  slugSeo?: string | null;
  embalajes: {
    id: number;
    descripcion: string | null;
    cantidadPorEmbalaje: number;
    codigoBarras: string | null;
    precio: number | null;
    precioCredito: number | null;
    cantidadMinimaMayorista: number;
    paraFacturacion: boolean;
    codigoUnidadMedida?: string;
    unidadSimbolo?: string | null;
    unidadNombre?: string | null;
    permiteFraccion?: boolean;
    incrementoVenta?: number;
    cantidadMinimaVenta?: number;
    precioLista?: number | null;
    porcentajeDescuento?: number | null;
  }[];
  imagenes: {
    id: number;
    url: string;
    titulo: string | null;
    orden: number;
    esPrincipal: boolean;
  }[];
}

export interface MarketingSlot {
  id: number;
  tipoGrupo: number;
  tipoNombre: string;
  titulo: string;
  subtitulo: string | null;
  imagenUrl: string | null;
  urlDestino: string | null;
  orden: number;
}

export interface CartLine {
  id: number;
  idArticulo: number;
  sku: string | null;
  nombre: string;
  imagenUrl: string | null;
  idArticuloEmbalaje: number | null;
  embalaje: string | null;
  cantidad: number;
  precioUnitario: number;
  montoTotal: number;
  stockLibre: number;
  stockOk: boolean;
}

export interface Cart {
  id: number;
  idSesion: string | null;
  canal: string;
  idDeposito: number;
  idAgenciaFacturacion?: number | null;
  estado: number;
  montoTotal: number;
  montoIVA: number;
  itemsCount: number;
  controlaStock: boolean;
  items: CartLine[];
  montoDescuento?: number;
  codigoCupon?: string | null;
}

export interface Resena {
  id: number;
  idArticulo: number;
  email: string;
  canalEcommerce: string;
  puntuacion: number;
  titulo: string | null;
  comentario: string | null;
  fechaResena: string;
  nombreAutor: string | null;
  esMia: boolean;
}

export interface ResumenCalificacion {
  idArticulo: number;
  promedio: number;
  totalResenas: number;
  estrellas5: number;
  estrellas4: number;
  estrellas3: number;
  estrellas2: number;
  estrellas1: number;
}

export interface ProductoElegibleResena {
  idArticulo: number;
  nombre: string;
  sku: string | null;
  imagenUrl: string | null;
  idPedidoMovil: number | null;
  idCarritoCompra: number | null;
  nroPedido: number;
  yaReseno: boolean;
}

export interface FavoritoItem {
  idDetalle: number;
  idArticulo: number;
  sku: string | null;
  nombre: string;
  imagenUrl: string | null;
  precioDesde: number | null;
  fechaAgregado: string;
  nota: string | null;
}

export interface FavoritoSeccion {
  id: number;
  nombreLista: string;
  canalEcommerce: string;
  email: string;
  idCuenta: number | null;
  orden: number;
  color: string | null;
  itemsCount: number;
  items: FavoritoItem[];
}

export interface Recomendacion {
  id: number;
  sku: string | null;
  nombre: string;
  imagenUrl: string | null;
  precioDesde: number | null;
  promedioCalificacion: number | null;
  totalResenas: number;
  motivo: string;
  slugSeo?: string | null;
}

export interface Seguimiento {
  id: number;
  idPedidoMovil: number;
  estado: string;
  mensaje: string | null;
  fechaEstado: string;
  codigoTracking: string | null;
}

export interface TrackingPublico {
  nroPedido: number;
  estadoActual: string;
  mensajeActual: string | null;
  fechaEstadoActual: string | null;
  eventos: Seguimiento[];
}

export interface SeguidosItem {
  idArticulo: number;
  nombre: string;
  imagenUrl: string | null;
  fechaSeguimiento: string;
  sku?: string | null;
  precioDesde?: number | null;
  stockOk?: boolean;
  stockLibre?: number | null;
}

export interface Devolucion {
  id: number;
  idPedidoMovil: number;
  idArticulo: number | null;
  motivo: string;
  detalle: string | null;
  estado: string;
  fechaSolicitud: string;
  nroPedido?: number | null;
  email?: string | null;
  articuloNombre?: string | null;
  urlAdjunto?: string | null;
  idNotaCreditoErp?: number | null;
  nroNotaCredito?: number | null;
  puedeDescargarNotaCredito?: boolean;
  erpMensaje?: string | null;
}

export interface ChatMensaje {
  id: number;
  idConversacion?: number | null;
  idPedidoMovil: number | null;
  email: string;
  origen: string;
  tipoMensaje?: string;
  mensaje: string;
  fechaMensaje: string;
  leido: boolean;
  adjunto?: { url: string | null; mimeType: string | null; nombreArchivo: string | null } | null;
  nombreAutor?: string | null;
}

export interface ChatConversacion {
  id: number;
  idUsuarioEcommerce: number;
  email: string;
  nombreCliente: string | null;
  asunto: string;
  estado: string;
  idPedidoMovil: number | null;
  idAdminAsignado?: number | null;
  nombreAdminAsignado?: string | null;
  fechaUltimoMensaje: string;
  previewUltimoMensaje: string | null;
  noLeidosCliente: number;
  noLeidosSoporte: number;
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

export interface CheckoutRequest {
  delivery: boolean;
  pagarEnLaEntrega: boolean;
  razonSocial: string;
  numeroDocumento: string;
  nombre: string;
  apellido: string;
  email: string;
  nroTelefono: string;
  lugarEntrega?: string;
  observacion?: string;
  latitud?: number;
  longitud?: number;
  tipoComprobante?: 'factura' | 'ticket' | null;
}

export interface CheckoutResult {
  idCarrito: number;
  nroPedido: number;
  idPedidoMovil: number | null;
  estadoCarrito: number;
  tipoEntrega: number;
  tipoPago: number;
  montoTotal: number;
  fechaPrevistaEntrega: string;
  razonSocial: string;
  lugarEntrega: string | null;
  requierePagoOnline: boolean;
  mensaje: string;
  processId?: string | null;
  bancardMode?: string | null;
  nroFactura?: number | null;
  facturado?: boolean;
  puedeDescargarFactura?: boolean;
  items: {
    idArticulo: number;
    sku: string | null;
    nombre: string;
    cantidad: number;
    precioUnitario: number;
    montoTotal: number;
  }[];
}

export interface PaymentSession {
  idCarrito: number;
  nroPedido: number;
  shopProcessId: number;
  processId: string | null;
  montoTotal: number;
  currency: string;
  requierePagoOnline: boolean;
  pagoCompletado: boolean;
  bancardMode: string;
  checkoutScriptUrl: string | null;
  mensaje: string;
}

export interface CiudadDelivery {
  id: number;
  nombre: string;
  divisionPolitica: string | null;
}

export interface BarrioDelivery {
  id: number;
  idCiudad: number;
  nombre: string;
  importeDelivery: number | null;
}

export interface TipoDocumento {
  id: number;
  codigo: number;
  nombre: string;
  esRuc: boolean;
}

export interface Agencia {
  id: number;
  nombre: string;
  direccion: string | null;
  idCiudad: number | null;
  idDeposito?: number | null;
  latitud: number | null;
  longitud: number | null;
}

export interface HorarioAtencion {
  diaSemana: number;
  apertura: string;
  cierre: string;
  apertura2: string | null;
  cierre2: string | null;
}

export interface MapDefaults {
  lat: number;
  lng: number;
}

export interface AccountProfile {
  idUsuarioEcommerce: number;
  usuario: string;
  nombre: string;
  apellido: string;
  email: string | null;
  telefono: string | null;
  numeroDocumento: string | null;
  canalPreferido: string;
  idCuentaPrincipal: number | null;
  estadoMayorista: string;
  montoComprasAcumulado: number;
  montoUmbralMayorista: number;
  idListaPrecio: number | null;
  listaPrecioDescripcion: string | null;
}

export interface AccountOrder {
  idCarrito: number;
  nroPedido: number;
  idPedidoMovil: number | null;
  fechaPedido: string;
  razonSocial: string;
  numeroDocumento: string;
  tipoEntrega: number;
  tipoPago: number;
  direccion: string | null;
  montoTotal: number;
  estado: number;
  migrado: boolean;
  cancelado: boolean;
  nroFactura?: number | null;
  facturado?: boolean;
  puedeDescargarFactura?: boolean;
  puedeCancelar?: boolean;
  idNotaCreditoErp?: number | null;
  nroNotaCredito?: number | null;
  puedeDescargarNotaCredito?: boolean;
}

export interface AccountInvoice {
  nroPedido: number;
  idPedidoMovil: number | null;
  nroFactura: number | null;
  idPedidoErp: number | null;
  fechaPedido: string;
  montoTotal: number;
  razonSocial: string | null;
  facturado: boolean;
  puedeDescargarPdf: boolean;
  tipoReporte: string;
  mensaje: string | null;
}

export interface ProductQuery {
  search?: string;
  estado?: string;
  grupoId?: number;
  marcaId?: number;
  lineaId?: number;
  tipoId?: number;
  sort?: string;
  canal?: Canal;
  page?: number;
  pageSize?: number;
}

export interface CatalogMarca {
  id: number;
  nombre: string;
}

export interface CatalogFacet {
  id: number;
  nombre: string;
}

export interface ContactoRequest {
  nombre: string;
  email: string;
  telefono?: string;
  mensaje: string;
}

export interface ContactoResult {
  ok: boolean;
  mensaje: string;
  id?: number | null;
}

export interface ForgotPasswordResult {
  mensaje: string;
  resetUrlDev: string | null;
}

export interface StorefrontCmsBlockItem {
  id: number;
  itemType: string;
  title: string | null;
  subtitle: string | null;
  description: string | null;
  imageUrl: string | null;
  linkUrl: string | null;
  linkTarget: string | null;
  badgeText: string | null;
  displayOrder: number;
}

export interface StorefrontCmsBlock {
  id: number;
  pageZone: string | null;
  blockType: string;
  title: string | null;
  subtitle: string | null;
  description: string | null;
  displayOrder: number;
  items: StorefrontCmsBlockItem[];
}

export interface StorefrontNavMenu {
  id: number;
  code: string;
  menuType: string;
  items: { id: number; label: string; url: string | null; target: string | null; badgeText: string | null }[];
}

export interface StorefrontHomeContent {
  blocks: StorefrontCmsBlock[];
  menus: StorefrontNavMenu[];
}

export interface UpdateProfileRequest {
  nombre: string;
  apellido: string;
  telefono?: string;
  numeroDocumento?: string;
}

export interface ResetPasswordRequest {
  codigo: string;
  checksum: string;
  password: string;
  passwordConfirmation: string;
  tenantSlug?: string;
}

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly session = inject(SessionService);
  private readonly base = environment.apiBaseUrl;

  products(query: ProductQuery = {}): Observable<PagedResult<ProductListItem>> {
    let params = new HttpParams();
    if (query.search) params = params.set('search', query.search);
    if (query.estado) params = params.set('estado', query.estado);
    if (query.grupoId != null) params = params.set('grupoId', query.grupoId);
    if (query.marcaId != null) params = params.set('marcaId', query.marcaId);
    if (query.lineaId != null) params = params.set('lineaId', query.lineaId);
    if (query.tipoId != null) params = params.set('tipoId', query.tipoId);
    if (query.sort) params = params.set('sort', query.sort);
    params = params.set('canal', query.canal ?? getCanal());
    params = params.set('page', String(query.page ?? 1));
    params = params.set('pageSize', String(query.pageSize ?? 12));
    return this.http.get<PagedResult<ProductListItem>>(`${this.base}/api/catalog/products`, { params });
  }

  getMasVendidos(take = 10, canal?: Canal): Observable<ProductListItem[]> {
    const params = new HttpParams()
      .set('take', String(take))
      .set('canal', canal ?? getCanal());
    return this.http.get<ProductListItem[]>(`${this.base}/api/catalog/mas-vendidos`, { params });
  }

  getOfertas(take = 12, canal?: Canal): Observable<ProductListItem[]> {
    const params = new HttpParams()
      .set('take', String(take))
      .set('canal', canal ?? getCanal());
    return this.http.get<ProductListItem[]>(`${this.base}/api/catalog/ofertas`, { params });
  }

  getLineas(): Observable<CatalogFacet[]> {
    return this.http.get<CatalogFacet[]>(`${this.base}/api/catalog/lineas`);
  }

  getTipos(): Observable<CatalogFacet[]> {
    return this.http.get<CatalogFacet[]>(`${this.base}/api/catalog/tipos`);
  }

  enviarContacto(body: ContactoRequest): Observable<ContactoResult> {
    return this.http.post<ContactoResult>(`${this.base}/api/contacto`, body);
  }

  guardarCarritoComoLista(nombreLista?: string, canal?: Canal): Observable<FavoritoSeccion> {
    return this.http.post<FavoritoSeccion>(`${this.base}/api/favoritos/desde-carrito`, {
      nombreLista: nombreLista ?? null,
      canal: canal ?? getCanal(),
    });
  }

  searchSuggest(q: string, canal = 'B2C', take = 8): Observable<SearchSuggestResult> {
    const params = new HttpParams()
      .set('q', q)
      .set('canal', canal)
      .set('take', String(take));
    return this.http.get<SearchSuggestResult>(`${this.base}/api/search`, { params });
  }

  searchResults(q: string, canal = 'B2C', page = 1, pageSize = 24): Observable<SearchSuggestResult> {
    const params = new HttpParams()
      .set('q', q)
      .set('canal', canal)
      .set('page', String(page))
      .set('pageSize', String(pageSize));
    return this.http.get<SearchSuggestResult>(`${this.base}/api/search/results`, { params });
  }

  searchSnapshot(canal = 'B2C', max = 2000): Observable<SearchSnapshot> {
    const params = new HttpParams().set('canal', canal).set('max', String(max));
    return this.http.get<SearchSnapshot>(`${this.base}/api/search/snapshot`, { params });
  }

  getMarcas(): Observable<CatalogMarca[]> {
    return this.http.get<CatalogMarca[]>(`${this.base}/api/catalog/marcas`);
  }

  getBranding(): Observable<StorefrontBranding> {
    return this.http.get<StorefrontBranding>(`${this.base}/api/catalog/branding`);
  }

  getPersonalizacionPublica(): Observable<{ claves: Record<string, string>; Claves?: Record<string, string> }> {
    return this.http.get<{ claves: Record<string, string>; Claves?: Record<string, string> }>(
      `${this.base}/api/storefront/personalizacion`,
    );
  }

  getHomeContent(): Observable<StorefrontHomeContent> {
    return this.http.get<StorefrontHomeContent>(`${this.base}/api/storefront/content/home`);
  }

  resendActivation(email: string, tenantSlug?: string): Observable<{ mensaje: string; activationUrlDev: string | null }> {
    return this.http.post<{ mensaje: string; activationUrlDev: string | null }>(
      `${this.base}/api/auth/resend-activation`,
      { email, tenantSlug: tenantSlug ?? null },
    );
  }

  product(id: number, canal?: Canal): Observable<ProductDetail> {
    const params = new HttpParams().set('canal', canal ?? getCanal());
    return this.http.get<ProductDetail>(`${this.base}/api/catalog/products/${id}`, { params });
  }

  productBySlug(slug: string, canal?: Canal): Observable<ProductDetail> {
    const params = new HttpParams().set('canal', canal ?? getCanal());
    return this.http.get<ProductDetail>(
      `${this.base}/api/catalog/products/slug/${encodeURIComponent(slug)}`,
      { params },
    );
  }

  /** Resuelve PDP por id numérico o slug SEO. */
  productByKey(key: string, canal?: Canal): Observable<ProductDetail> {
    if (/^\d+$/.test(key)) return this.product(Number(key), canal);
    return this.productBySlug(key, canal);
  }

  collections(): Observable<CollectionItem[]> {
    return this.http.get<CollectionItem[]>(`${this.base}/api/catalog/collections`);
  }

  getMarketingSlots(): Observable<MarketingSlot[]> {
    return this.http.get<MarketingSlot[]>(`${this.base}/api/catalog/marketing`);
  }

  getCart(canal?: Canal): Observable<Cart> {
    const params = new HttpParams().set('canal', canal ?? getCanal());
    return this.http.get<Cart>(`${this.base}/api/cart`, { params });
  }

  addToCart(idArticulo: number, cantidad: number, idArticuloEmbalaje?: number, canal?: Canal): Observable<Cart> {
    return this.http.post<Cart>(`${this.base}/api/cart/items`, {
      idArticulo,
      cantidad,
      idArticuloEmbalaje: idArticuloEmbalaje ?? null,
      canal: canal ?? getCanal(),
    });
  }

  updateCartItem(detalleId: number, cantidad: number, canal?: Canal): Observable<Cart> {
    const params = new HttpParams().set('canal', canal ?? getCanal());
    return this.http.put<Cart>(`${this.base}/api/cart/items/${detalleId}`, { cantidad }, { params });
  }

  removeCartItem(detalleId: number, canal?: Canal): Observable<Cart> {
    const params = new HttpParams().set('canal', canal ?? getCanal());
    return this.http.delete<Cart>(`${this.base}/api/cart/items/${detalleId}`, { params });
  }

  clearCart(): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/cart`);
  }

  setPickup(idAgencia: number): Observable<Cart> {
    return this.http.put<Cart>(`${this.base}/api/cart/pickup`, { idAgencia });
  }

  reorder(nroPedido: number, canal?: Canal): Observable<Cart> {
    const params = new HttpParams().set('canal', canal ?? getCanal());
    return this.http.post<Cart>(`${this.base}/api/cart/reorder/${nroPedido}`, null, { params });
  }

  changePassword(passwordActual: string, passwordNueva: string, passwordConfirmacion: string): Observable<{ mensaje: string }> {
    return this.http.post<{ mensaje: string }>(`${this.base}/api/auth/change-password`, {
      passwordActual,
      passwordNueva,
      passwordConfirmacion,
    });
  }

  getCiudadesDelivery(): Observable<CiudadDelivery[]> {
    return this.http.get<CiudadDelivery[]>(`${this.base}/api/geography/ciudades-delivery`);
  }

  getBarriosDelivery(ciudadId?: number): Observable<BarrioDelivery[]> {
    let params = new HttpParams();
    if (ciudadId != null) params = params.set('ciudadId', ciudadId);
    return this.http.get<BarrioDelivery[]>(`${this.base}/api/geography/barrios-delivery`, { params });
  }

  getTiposDocumento(): Observable<TipoDocumento[]> {
    return this.http.get<TipoDocumento[]>(`${this.base}/api/geography/tipos-documento`);
  }

  getAgencias(): Observable<Agencia[]> {
    return this.http.get<Agencia[]>(`${this.base}/api/geography/agencias`);
  }

  getAgenciaHorarios(idAgencia: number): Observable<HorarioAtencion[]> {
    return this.http.get<HorarioAtencion[]>(`${this.base}/api/geography/agencias/${idAgencia}/horarios`);
  }

  getMapDefaults(): Observable<MapDefaults> {
    return this.http.get<MapDefaults>(`${this.base}/api/geography/map-defaults`);
  }

  applyShippingByBarrio(idBarrio: number): Observable<{ ok: boolean }> {
    return this.http.post<{ ok: boolean }>(`${this.base}/api/shipping/apply`, { idBarrio });
  }

  clearShipping(): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/shipping`);
  }

  checkout(body: CheckoutRequest): Observable<CheckoutResult> {
    return this.http.post<CheckoutResult>(`${this.base}/api/checkout`, body);
  }

  getOrder(nroPedido: number): Observable<CheckoutResult> {
    return this.http.get<CheckoutResult>(`${this.base}/api/checkout/orders/${nroPedido}`);
  }

  getPaymentSession(nroPedido: number): Observable<PaymentSession> {
    return this.http.get<PaymentSession>(`${this.base}/api/payments/bancard/session/${nroPedido}`);
  }

  simulateBancardSuccess(shopProcessId: number): Observable<CheckoutResult> {
    return this.http.post<CheckoutResult>(`${this.base}/api/payments/bancard/simulate-success`, {
      shopProcessId,
    });
  }

  rollbackPayment(shopProcessId: number): Observable<{ status: string }> {
    return this.http.post<{ status: string }>(`${this.base}/api/payments/bancard/rollback`, {
      shopProcessId,
    });
  }

  getMyOrders(): Observable<AccountOrder[]> {
    return this.http.get<AccountOrder[]>(`${this.base}/api/account/orders`);
  }

  getMyInvoices(): Observable<AccountInvoice[]> {
    return this.http.get<AccountInvoice[]>(`${this.base}/api/account/invoices`);
  }

  /** PDF factura vía Pentaho (mismo contrato que ERP). */
  downloadInvoicePdf(nroPedido: number): Observable<Blob> {
    const token = getAccessToken();
    return this.http.get(`${this.base}/api/account/orders/${nroPedido}/factura.pdf`, {
      responseType: 'blob',
      headers: token ? { Authorization: `Bearer ${token}` } : {},
    });
  }

  /** PDF nota de crédito (devolución), original a media hoja. */
  downloadCreditNotePdf(nroPedido: number): Observable<Blob> {
    const token = getAccessToken();
    return this.http.get(`${this.base}/api/account/orders/${nroPedido}/nota-credito.pdf`, {
      responseType: 'blob',
      headers: token ? { Authorization: `Bearer ${token}` } : {},
    });
  }

  cancelOrder(nroPedido: number, motivo?: string): Observable<{ ok: boolean }> {
    return this.http.post<{ ok: boolean }>(`${this.base}/api/account/orders/${nroPedido}/cancelar`, {
      motivo: motivo ?? null,
    });
  }

  me(): Observable<AccountProfile> {
    return this.http.get<AccountProfile>(`${this.base}/api/account/me`);
  }

  updateProfile(body: UpdateProfileRequest): Observable<AccountProfile> {
    return this.http.put<AccountProfile>(`${this.base}/api/account/me`, body);
  }

  solicitarMayorista(motivo?: string): Observable<AccountProfile> {
    return this.http.post<AccountProfile>(`${this.base}/api/account/solicitar-mayorista`, {
      motivo: motivo ?? null,
    });
  }

  getResenas(idArticulo: number): Observable<Resena[]> {
    return this.http.get<Resena[]>(`${this.base}/api/reviews/product/${idArticulo}`);
  }

  getResumenCalificacion(idArticulo: number): Observable<ResumenCalificacion> {
    return this.http.get<ResumenCalificacion>(`${this.base}/api/reviews/product/${idArticulo}/summary`);
  }

  getElegiblesResena(): Observable<ProductoElegibleResena[]> {
    return this.http.get<ProductoElegibleResena[]>(`${this.base}/api/reviews/elegibles`);
  }

  crearResena(body: {
    idArticulo: number;
    puntuacion: number;
    titulo?: string;
    comentario?: string;
    idPedidoMovil?: number | null;
    idCarritoCompra?: number | null;
  }): Observable<Resena> {
    return this.http.post<Resena>(`${this.base}/api/reviews`, body);
  }

  getFavoritos(canal?: Canal): Observable<FavoritoSeccion[]> {
    let params = new HttpParams();
    if (canal) params = params.set('canal', canal);
    return this.http.get<FavoritoSeccion[]>(`${this.base}/api/favoritos`, { params });
  }

  crearSeccionFavorito(nombreLista: string, color?: string): Observable<FavoritoSeccion> {
    return this.http.post<FavoritoSeccion>(`${this.base}/api/favoritos/secciones`, {
      nombreLista,
      color: color ?? null,
      canal: getCanal(),
    });
  }

  eliminarSeccionFavorito(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/favoritos/secciones/${id}`);
  }

  agregarFavorito(idArticulo: number, idLista?: number, nombreLista?: string): Observable<FavoritoSeccion> {
    return this.http.post<FavoritoSeccion>(`${this.base}/api/favoritos/items`, {
      idArticulo,
      idLista: idLista ?? null,
      nombreLista: nombreLista ?? null,
    });
  }

  quitarFavorito(idDetalle: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/favoritos/items/${idDetalle}`);
  }

  quitarFavoritoPorArticulo(idArticulo: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/favoritos/items/by-articulo/${idArticulo}`);
  }

  favoritoContains(idArticulo: number): Observable<{ enFavoritos: boolean }> {
    return this.http.get<{ enFavoritos: boolean }>(`${this.base}/api/favoritos/contains/${idArticulo}`);
  }

  track(idArticulo: number, tipoInteraccion: string): Observable<void> {
    return this.http.post<void>(`${this.base}/api/engagement/track`, {
      idArticulo,
      tipoInteraccion,
      canal: getCanal(),
    });
  }

  recomendaciones(idArticulo?: number): Observable<Recomendacion[]> {
    let params = new HttpParams().set('canal', getCanal());
    if (idArticulo != null) params = params.set('idArticulo', idArticulo);
    return this.http.get<Recomendacion[]>(`${this.base}/api/engagement/recomendaciones`, { params });
  }

  aplicarCupon(codigo: string): Observable<{ ok: boolean; mensaje: string; montoDescuento: number; codigo: string | null }> {
    return this.http.post<{ ok: boolean; mensaje: string; montoDescuento: number; codigo: string | null }>(
      `${this.base}/api/engagement/cupones/aplicar`,
      { codigo, canal: getCanal() },
    );
  }

  quitarCupon(): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/engagement/cupones`);
  }

  seguirProducto(idArticulo: number): Observable<void> {
    return this.http.post<void>(`${this.base}/api/engagement/seguir/${idArticulo}`, {});
  }

  dejarSeguir(idArticulo: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/engagement/seguir/${idArticulo}`);
  }

  getTracking(idPedidoMovil: number): Observable<Seguimiento[]> {
    return this.http.get<Seguimiento[]>(`${this.base}/api/engagement/tracking/${idPedidoMovil}`);
  }

  shareTracking(idPedidoMovil: number): Observable<{ token: string; path: string }> {
    return this.http.post<{ token: string; path: string }>(
      `${this.base}/api/engagement/tracking/${idPedidoMovil}/share`,
      {},
    );
  }

  getTrackingPublico(token: string): Observable<TrackingPublico> {
    return this.http.get<TrackingPublico>(
      `${this.base}/api/engagement/tracking/publico/${encodeURIComponent(token)}`,
    );
  }

  getSeguidos(): Observable<SeguidosItem[]> {
    return this.http.get<SeguidosItem[]>(`${this.base}/api/engagement/seguidos`);
  }

  seguidoContains(idArticulo: number): Observable<{ seguido: boolean }> {
    return this.http.get<{ seguido: boolean }>(`${this.base}/api/engagement/seguidos/contains/${idArticulo}`);
  }

  crearDevolucion(body: {
    idPedidoMovil: number;
    idArticulo?: number | null;
    motivo: string;
    detalle?: string;
    file?: File | null;
  }): Observable<Devolucion> {
    const fd = new FormData();
    fd.append('idPedidoMovil', String(body.idPedidoMovil));
    fd.append('motivo', body.motivo);
    if (body.detalle) fd.append('detalle', body.detalle);
    if (body.idArticulo != null) fd.append('idArticulo', String(body.idArticulo));
    if (body.file) fd.append('file', body.file);
    return this.http.post<Devolucion>(`${this.base}/api/engagement/devoluciones/upload`, fd);
  }

  getDevoluciones(): Observable<Devolucion[]> {
    return this.http.get<Devolucion[]>(`${this.base}/api/engagement/devoluciones`);
  }

  getChat(idPedidoMovil?: number, idConversacion?: number): Observable<ChatMensaje[]> {
    let params = new HttpParams();
    if (idPedidoMovil != null) params = params.set('idPedidoMovil', idPedidoMovil);
    if (idConversacion != null) params = params.set('idConversacion', idConversacion);
    return this.http.get<ChatMensaje[]>(`${this.base}/api/engagement/chat`, { params });
  }

  getChatConversaciones(): Observable<ChatConversacion[]> {
    return this.http.get<ChatConversacion[]>(`${this.base}/api/engagement/chat/conversaciones`);
  }

  enviarChat(mensaje: string, idPedidoMovil?: number, asunto = 'consulta', idConversacion?: number, forzarNueva = false): Observable<ChatMensaje> {
    return this.http.post<ChatMensaje>(`${this.base}/api/engagement/chat`, {
      mensaje,
      idPedidoMovil: idPedidoMovil ?? null,
      asunto,
      idConversacion: idConversacion ?? null,
      forzarNueva,
    });
  }

  enviarChatFoto(
    file: File,
    mensaje?: string,
    asunto = 'consulta',
    idConversacion?: number,
    idPedidoMovil?: number,
    forzarNueva = false,
  ): Observable<ChatMensaje> {
    const fd = new FormData();
    fd.append('file', file);
    if (mensaje) fd.append('mensaje', mensaje);
    fd.append('asunto', asunto);
    if (idConversacion != null) fd.append('idConversacion', String(idConversacion));
    if (idPedidoMovil != null) fd.append('idPedidoMovil', String(idPedidoMovil));
    if (forzarNueva) fd.append('forzarNueva', 'true');
    return this.http.post<ChatMensaje>(`${this.base}/api/engagement/chat/upload`, fd);
  }

  marcarChatLeidos(idConversacion: number): Observable<void> {
    return this.http.post<void>(`${this.base}/api/engagement/chat/conversaciones/${idConversacion}/leidos`, {});
  }

  cerrarChat(idConversacion: number): Observable<void> {
    return this.http.post<void>(`${this.base}/api/engagement/chat/conversaciones/${idConversacion}/cerrar`, {});
  }

  /** Resuelve URLs de media relativas o con puerto viejo (5081/5082). */
  resolveMediaUrl(url: string | null | undefined): string | null {
    if (!url) return null;
    let u = url.trim();
    u = u.replace(/https?:\/\/(localhost|127\.0\.0\.1):(5081|5082)/gi, this.base);
    if (!u.includes('://') && !u.startsWith('/') && /\.(png|jpe?g|gif|webp|avif|ico|bmp)$/i.test(u)) {
      u = `/media/erp/${u.replace(/^\/+/, '')}`;
    }
    if (u.startsWith('/')) return `${this.base}${u}`;
    return u;
  }

  login(usuario: string, password: string, tenantSlug: string): Observable<TokenResponse> {
    setTenantSlug(tenantSlug);
    return this.http.post<TokenResponse>(`${this.base}/api/auth/login`, {
      usuario,
      password,
      tenantSlug,
    });
  }

  register(body: SignUpRequest): Observable<SignUpResult> {
    if (body.tenantSlug) setTenantSlug(body.tenantSlug);
    return this.http.post<SignUpResult>(`${this.base}/api/auth/register`, body);
  }

  activate(c: string, k: string, tenantSlug: string): Observable<TokenResponse> {
    setTenantSlug(tenantSlug);
    return this.http.post<TokenResponse>(`${this.base}/api/auth/activate`, {
      codigo: c,
      checksum: k,
      tenantSlug,
    });
  }

  forgotPassword(email: string, tenantSlug: string): Observable<ForgotPasswordResult> {
    if (tenantSlug) setTenantSlug(tenantSlug);
    return this.http.post<ForgotPasswordResult>(`${this.base}/api/auth/forgot-password`, {
      email,
      tenantSlug: tenantSlug || null,
    });
  }

  resetPassword(body: ResetPasswordRequest): Observable<{ mensaje: string }> {
    if (body.tenantSlug) setTenantSlug(body.tenantSlug);
    return this.http.post<{ mensaje: string }>(`${this.base}/api/auth/reset-password`, body);
  }

  persistSession(token: TokenResponse, from: 'login' | 'activate' | 'register' = 'login'): void {
    this.session.persist(
      {
        accessToken: token.accessToken,
        idUsuarioEcommerce: token.idUsuarioEcommerce,
        nombre: token.nombre,
        canalPreferido: token.canalPreferido,
        expiresAtUtc: token.expiresAtUtc,
      },
      { welcome: true, from },
    );
  }

  logout(): void {
    this.session.clear();
  }
}
