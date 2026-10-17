import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  AdminAgencia,
  AdminApiService,
  AdminDeposito,
  AdminEmpresaConfig,
  AdminErpHealth,
  AdminHorario,
  ErpConnectionTestResult,
  UpsertAgenciaRequest,
  UpsertHorarioRequest,
} from '../core/admin-api.service';

const DIAS = [
  { value: 0, label: 'Domingo' },
  { value: 1, label: 'Lunes' },
  { value: 2, label: 'Martes' },
  { value: 3, label: 'Miércoles' },
  { value: 4, label: 'Jueves' },
  { value: 5, label: 'Viernes' },
  { value: 6, label: 'Sábado' },
];

@Component({
  standalone: true,
  imports: [FormsModule, RouterLink, DatePipe],
  template: `
    <div class="dx-toolbar">
      <div>
        <h1 class="dx-page-title">Configuración empresa</h1>
        <p class="dx-page-sub">Datos de la tienda, operación, ERP, pagos, agencias y horarios.</p>
      </div>
      <div class="dx-filters">
        <button type="button" class="btn btn-outline-secondary btn-sm" (click)="reload()" title="Recargar">
          <i class="fas fa-sync-alt"></i>
        </button>
        <a routerLink="/sync" class="btn btn-outline-secondary btn-sm">
          <i class="fas fa-exchange-alt mr-1"></i> Estado ETL
        </a>
        <button type="button" class="btn btn-primary btn-sm" (click)="saveConfig()">
          <i class="fas fa-save mr-1"></i> Guardar
        </button>
      </div>
    </div>

    @if (error()) {
      <div class="alert alert-danger alert-dismissible">
        <button type="button" class="close" (click)="error.set(null)"><span>&times;</span></button>
        {{ error() }}
      </div>
    }
    @if (ok()) {
      <div class="alert alert-success alert-dismissible">
        <button type="button" class="close" (click)="ok.set(null)"><span>&times;</span></button>
        {{ ok() }}
      </div>
    }

    @if (cfg(); as c) {
      <div class="emp-status">
        <span class="emp-chip" [class.on]="form.modoIntegracion === 'Integrated'">{{ form.modoIntegracion || '—' }}</span>
        @if (erpHealth(); as h) {
          <span class="emp-chip" [class.ok]="h.hasConnection" [class.warn]="!h.hasConnection">
            ERP {{ h.hasConnection ? (h.connectionSource || 'OK') : 'sin conexión' }}
          </span>
          <span class="emp-chip" [class.ok]="h.writeBackEffective">Write-back {{ h.writeBackEffective ? 'ON' : 'OFF' }}</span>
          @if (h.pendingOrdersWriteBack > 0) {
            <span class="emp-chip warn">{{ h.pendingOrdersWriteBack }} pendientes ERP</span>
          }
        }
        <span class="emp-chip" [class.on]="form.realizaDelivery">{{ form.realizaDelivery ? 'Delivery' : 'Sin delivery' }}</span>
        <span class="emp-chip" [class.on]="form.realizaPickup">{{ form.realizaPickup ? 'Pickup' : 'Sin pickup' }}</span>
        <span class="emp-chip muted">{{ agenciasActivas().length }} agencia(s)</span>
      </div>

      <nav class="emp-tabs" aria-label="Secciones de configuración">
        <button type="button" [class.on]="tab() === 'tienda'" (click)="tab.set('tienda')">🏪 Tienda</button>
        <button type="button" [class.on]="tab() === 'operacion'" (click)="tab.set('operacion')">🚚 Operación</button>
        <button type="button" [class.on]="tab() === 'erp'" (click)="tab.set('erp')">🔗 ERP</button>
        <button type="button" [class.on]="tab() === 'pagos'" (click)="tab.set('pagos')">💳 Pagos y redes</button>
        <button type="button" [class.on]="tab() === 'agencias'" (click)="tab.set('agencias')">📍 Agencias</button>
        <button type="button" [class.on]="tab() === 'horarios'" (click)="tab.set('horarios')">🕒 Horarios</button>
      </nav>

      @if (tab() === 'tienda') {
        <div class="card card-outline card-primary mb-3">
          <div class="card-header"><h3 class="card-title">Identidad de la tienda</h3></div>
          <div class="card-body">
            <div class="row">
              <div class="col-md-6 form-group">
                <label>Razón social</label>
                <input class="form-control" [(ngModel)]="form.razonSocial" />
              </div>
              <div class="col-md-3 form-group">
                <label>Cód. empresa</label>
                <input class="form-control" type="number" [(ngModel)]="form.codEmpresa" />
              </div>
              <div class="col-md-3 form-group">
                <label>Nombre fantasía</label>
                <input class="form-control" [(ngModel)]="form.nombreFantasia" />
              </div>
              <div class="col-md-4 form-group">
                <label>RUC / documento</label>
                <input class="form-control" [(ngModel)]="form.numeroDocumento" />
              </div>
              <div class="col-md-4 form-group">
                <label>Teléfono</label>
                <input class="form-control" [(ngModel)]="form.telefono" />
              </div>
              <div class="col-md-4 form-group">
                <label>Fax</label>
                <input class="form-control" [(ngModel)]="form.fax" />
              </div>
              <div class="col-md-6 form-group">
                <label>Dirección</label>
                <input class="form-control" [(ngModel)]="form.direccionEmpresa" />
              </div>
              <div class="col-md-6 form-group">
                <label>Email</label>
                <input class="form-control" [(ngModel)]="form.eMail" />
              </div>
              <div class="col-md-6 form-group">
                <label>Representante legal</label>
                <input class="form-control" [(ngModel)]="form.representanteLegal" />
              </div>
              <div class="col-md-3 form-group">
                <label>ID país</label>
                <input class="form-control" type="number" [(ngModel)]="form.idPais" />
              </div>
              <div class="col-md-3 form-group">
                <label>Logo empresa (URL)</label>
                <input class="form-control" [(ngModel)]="form.logoEmpresa" />
                <small class="text-muted">Branding visual en <a routerLink="/branding">Diseño / logo</a>.</small>
              </div>
              <div class="col-md-4 form-group">
                <label>Slug tienda</label>
                <input class="form-control" [(ngModel)]="form.slugTienda" />
              </div>
              <div class="col-md-4 form-group">
                <label>Dominio custom</label>
                <input class="form-control" [(ngModel)]="form.dominioCustom" />
              </div>
            </div>
            <button type="button" class="btn btn-primary" (click)="saveConfig()">
              <i class="fas fa-save mr-1"></i> Guardar
            </button>
          </div>
        </div>
      }

      @if (tab() === 'operacion') {
        <div class="card card-outline card-success mb-3">
          <div class="card-header"><h3 class="card-title">Operación de la tienda</h3></div>
          <div class="card-body">
            <div class="row">
              <div class="col-md-4 form-group">
                <label>Monto pedido mínimo</label>
                <input class="form-control" type="number" min="0" [(ngModel)]="form.montoPedidoMinimo" />
              </div>
              <div class="col-md-4 form-group">
                <label>ETA pickup (HH:mm)</label>
                <input class="form-control" placeholder="01:30" [(ngModel)]="form.tiempoAproxEntregaPickup" />
              </div>
              <div class="col-md-4 form-group">
                <label>ETA delivery (HH:mm)</label>
                <input class="form-control" placeholder="02:00" [(ngModel)]="form.tiempoAproxEntregaDelivery" />
              </div>
              <div class="col-md-4 form-group">
                <label>Umbral auto-mayorista (Gs)</label>
                <input class="form-control" type="number" min="0" [(ngModel)]="form.montoUmbralMayorista" />
              </div>
              <div class="col-md-4 form-group">
                <label>Lista mayorista por defecto (ID)</label>
                <input class="form-control" type="number" [(ngModel)]="form.idListaPrecioMayoristaDefault" />
              </div>
              <div class="col-md-4 form-group">
                <label>ID depósito</label>
                <input class="form-control" type="number" [(ngModel)]="form.idDeposito" />
              </div>
              <div class="col-md-4 form-group">
                <label>ID artículo delivery</label>
                <input class="form-control" type="number" [(ngModel)]="form.idArticuloDelivery" />
              </div>
            </div>
            <div class="emp-switches">
              <label class="emp-switch"><input type="checkbox" [(ngModel)]="form.realizaDelivery" /> Delivery</label>
              <label class="emp-switch"><input type="checkbox" [(ngModel)]="form.realizaPickup" /> Pickup</label>
              <label class="emp-switch"><input type="checkbox" [(ngModel)]="form.usarListaPrecioCuentaErp" /> Precio por lista ERP</label>
              <label class="emp-switch">
                <input type="checkbox" [(ngModel)]="form.controlaStockEnCarrito" [disabled]="form.modoIntegracion === 'Integrated'" />
                Controla stock <small>(solo Standalone)</small>
              </label>
              <label class="emp-switch"><input type="checkbox" [(ngModel)]="form.cobraEnLaEntrega" /> Cobra en entrega</label>
              <label class="emp-switch"><input type="checkbox" [(ngModel)]="form.cobraAlHacerPedido" /> Cobra al pedir</label>
              <label class="emp-switch"><input type="checkbox" [(ngModel)]="form.procesaPedidosPorPlanilla" /> Por planilla</label>
            </div>
            <h5 class="mt-3">Comprobantes por modalidad</h5>
            <div class="emp-switches">
              <label class="emp-switch"><input type="checkbox" [(ngModel)]="form.deliveryEmiteFactura" /> Delivery · factura</label>
              <label class="emp-switch"><input type="checkbox" [(ngModel)]="form.deliveryEmiteTicket" /> Delivery · ticket</label>
              <label class="emp-switch"><input type="checkbox" [(ngModel)]="form.pickupEmiteFactura" /> Pickup · factura</label>
              <label class="emp-switch"><input type="checkbox" [(ngModel)]="form.pickupEmiteTicket" /> Pickup · ticket</label>
            </div>
            <p class="text-muted small mt-2 mb-3">
              FAQ / Términos / Cómo comprar se editan en <a routerLink="/branding">Diseño de tienda</a>.
            </p>
            <button type="button" class="btn btn-primary" (click)="saveConfig()">
              <i class="fas fa-save mr-1"></i> Guardar
            </button>
          </div>
        </div>
      }

      @if (tab() === 'erp') {
        <div class="card card-outline card-warning mb-3">
          <div class="card-header d-flex justify-content-between align-items-center flex-wrap">
            <h3 class="card-title mb-0">Integración ERP</h3>
            <div>
              <button type="button" class="btn btn-success btn-sm mr-1" (click)="activarIntegracionCompleta()">
                <i class="fas fa-bolt mr-1"></i> Activar integración completa
              </button>
              <a routerLink="/sync" class="btn btn-outline-secondary btn-sm">Ir a Sync</a>
            </div>
          </div>
          <div class="card-body">
            <p class="text-muted small">
              Modo Integrated: el artículo se da de alta en el <strong>ERP</strong> (Stock → Artículos).
              ecom-etl lo replica; este admin publica vitrina, imágenes y SEO.
              Las devoluciones de la tienda generan la nota de crédito en el ERP
              (MotivoNDNC tipo DEVOLUCION), igual que Stock → Notas de crédito.
              El <strong>stock</strong> lo manda el depósito ERP.
            </p>
            <div class="row mb-3">
              <div class="col-md-4 form-group">
                <label>Modo integración</label>
                <select class="custom-select" [(ngModel)]="form.modoIntegracion" name="modoIntegracion"
                        (ngModelChange)="onModoChange($event)">
                  <option value="Standalone">Standalone</option>
                  <option value="Integrated">Integrated (ERP)</option>
                  <option value="External">External</option>
                </select>
              </div>
            </div>

            @if (erpHealth(); as h) {
              <div class="callout mb-3"
                   [class.callout-success]="h.hasConnection && h.writeBackEffective && h.syncStockContinuo"
                   [class.callout-warning]="h.hasConnection && !(h.writeBackEffective && h.syncStockContinuo)"
                   [class.callout-danger]="!h.hasConnection">
                <div class="d-flex flex-wrap align-items-center mb-1">
                  <span class="badge mr-1"
                        [class.badge-success]="h.connectionSource==='Tenant'"
                        [class.badge-info]="h.connectionSource==='Global'"
                        [class.badge-secondary]="h.connectionSource==='None'">CS {{ h.connectionSource }}</span>
                  <span class="badge mr-1" [class.badge-success]="h.syncStockContinuo" [class.badge-secondary]="!h.syncStockContinuo">
                    Stock {{ h.syncStockContinuo ? 'cada ' + h.syncStockIntervaloMinutos + ' min' : 'off' }}
                  </span>
                  <span class="badge mr-1" [class.badge-success]="h.writeBackEffective" [class.badge-secondary]="!h.writeBackEffective">
                    Write-back {{ h.writeBackEffective ? 'ON' : 'OFF' }}
                  </span>
                  <span class="badge mr-1" [class.badge-warning]="h.pendingOrdersWriteBack>0" [class.badge-light]="h.pendingOrdersWriteBack===0">
                    Pendientes ERP: {{ h.pendingOrdersWriteBack }}
                  </span>
                  <span class="badge badge-light">Depósito #{{ h.idDeposito }}</span>
                </div>
                <p class="mb-1 small">{{ h.hint }}</p>
                <p class="mb-2 small text-muted">
                  Último stock:
                  @if (h.lastStockSyncUtc) {
                    {{ h.lastStockSyncUtc | date:'dd/MM HH:mm:ss' }} ·
                    <span [class.text-success]="h.lastStockStatus==='ok'" [class.text-danger]="h.lastStockStatus==='error'">
                      {{ h.lastStockStatus || '—' }}
                    </span>
                    · {{ h.lastStockRows }} filas
                  } @else { aún no corrió }
                  @if (h.lastStockError) { · {{ h.lastStockError }} }
                </p>
                <div class="btn-group btn-group-sm">
                  <button type="button" class="btn btn-warning" (click)="pullStockNow()" [disabled]="erpBusy()">
                    {{ erpBusy()==='stock' ? '…' : 'Ver cursor stock (ecom-etl)' }}
                  </button>
                  <button type="button" class="btn btn-outline-primary"
                          (click)="flushPending()" [disabled]="erpBusy() || h.pendingOrdersWriteBack===0">
                    {{ erpBusy()==='flush' ? 'Reintentando…' : 'Reintentar pendientes (' + h.pendingOrdersWriteBack + ')' }}
                  </button>
                  <button type="button" class="btn btn-outline-secondary" (click)="refreshErpHealth()" [disabled]="!!erpBusy()">
                    Salud
                  </button>
                </div>
              </div>
            }

            <div class="emp-switches mb-3">
              <label class="emp-switch"><input type="checkbox" [(ngModel)]="form.procesaPedidosERP" /> Procesa ERP</label>
              <label class="emp-switch"><input type="checkbox" [(ngModel)]="form.syncStockContinuo" /> Lectura continua de stock</label>
              <label class="emp-switch"><input type="checkbox" [(ngModel)]="form.migrarPedidosAlGenerar" /> Migrar pedidos al generar</label>
              <label class="emp-switch"><input type="checkbox" [(ngModel)]="form.erpAllowWriteBack" /> Permitir write-back ERP</label>
            </div>

            <div class="row mb-3">
              <div class="col-md-4 form-group">
                <label>Intervalo sync stock</label>
                <div class="btn-group btn-group-sm btn-block mb-1">
                  <button type="button" class="btn" [class.btn-primary]="form.syncStockIntervaloMinutos===1"
                          [class.btn-outline-secondary]="form.syncStockIntervaloMinutos!==1"
                          (click)="form.syncStockIntervaloMinutos=1">1 min</button>
                  <button type="button" class="btn" [class.btn-primary]="form.syncStockIntervaloMinutos===2"
                          [class.btn-outline-secondary]="form.syncStockIntervaloMinutos!==2"
                          (click)="form.syncStockIntervaloMinutos=2">2 min</button>
                  <button type="button" class="btn" [class.btn-primary]="form.syncStockIntervaloMinutos===5"
                          [class.btn-outline-secondary]="form.syncStockIntervaloMinutos!==5"
                          (click)="form.syncStockIntervaloMinutos=5">5 min</button>
                </div>
                <input class="form-control form-control-sm" type="number" min="1" max="120"
                       [(ngModel)]="form.syncStockIntervaloMinutos" />
              </div>
            </div>

            <h5>Conexión ERP (por empresa)</h5>
            <p class="text-muted small mb-2">
              Credenciales Postgres de esta tienda. Vacío = fallback <code>ConnectionStrings:Erp</code>.
            </p>
            @if (form.erpConnectionConfigured) {
              <div class="alert alert-info py-2">
                Conexión propia · origen: <strong>{{ form.erpConnectionSource || 'Tenant' }}</strong>
                @if (form.tieneErpPassword) { · password guardado }
              </div>
            } @else {
              <div class="alert alert-warning py-2">
                Sin CS propia — origen: <strong>{{ form.erpConnectionSource || 'Global/None' }}</strong>
              </div>
            }
            <div class="row">
              <div class="col-md-4 form-group">
                <label>Host</label>
                <input class="form-control" placeholder="10.99.99.x" [(ngModel)]="form.erpHost" name="erpHost" />
              </div>
              <div class="col-md-2 form-group">
                <label>Puerto</label>
                <input class="form-control" type="number" placeholder="5432" [(ngModel)]="form.erpPort" name="erpPort" />
              </div>
              <div class="col-md-3 form-group">
                <label>Base de datos</label>
                <input class="form-control" [(ngModel)]="form.erpDatabase" name="erpDatabase" />
              </div>
              <div class="col-md-3 form-group">
                <label>Usuario</label>
                <input class="form-control" [(ngModel)]="form.erpUsername" name="erpUsername" />
              </div>
              <div class="col-md-4 form-group">
                <label>Password {{ form.tieneErpPassword ? '(vacío = no cambiar)' : '' }}</label>
                <input class="form-control" type="password" autocomplete="new-password"
                       [(ngModel)]="erpPasswordDraft" name="erpPassword" />
              </div>
              <div class="col-md-3 form-group">
                <label>SSL Mode</label>
                <select class="custom-select" [(ngModel)]="form.erpSslMode" name="erpSslMode">
                  <option [ngValue]="null">Prefer (default)</option>
                  <option value="Prefer">Prefer</option>
                  <option value="Require">Require</option>
                  <option value="Disable">Disable</option>
                </select>
              </div>
              <div class="col-md-5 d-flex align-items-end form-group flex-wrap">
                <div class="custom-control custom-checkbox mr-3">
                  <input type="checkbox" class="custom-control-input" id="flgClearErpPwd"
                         [(ngModel)]="clearErpPassword" name="clearErpPassword" />
                  <label class="custom-control-label" for="flgClearErpPwd">Borrar password</label>
                </div>
                <button type="button" class="btn btn-outline-danger btn-sm" (click)="clearErpConnection()">
                  Limpiar CS propia
                </button>
              </div>
              <div class="col-md-12 form-group">
                <button type="button" class="btn btn-outline-primary btn-sm mr-2"
                        (click)="testErp()" [disabled]="testingErp()">
                  <i class="fas fa-plug mr-1"></i>
                  {{ testingErp() ? 'Probando…' : 'Probar conexión (sin guardar)' }}
                </button>
                <button type="button" class="btn btn-outline-secondary btn-sm mr-2"
                        (click)="refreshErpHealth()" [disabled]="testingErp()">
                  Actualizar salud
                </button>
                @if (erpTest(); as t) {
                  <div class="mt-2 p-2 border rounded small"
                       [class.border-success]="t.ok" [class.border-danger]="!t.ok"
                       [class.text-success]="t.ok" [class.text-danger]="!t.ok">
                    <div>{{ t.message }}</div>
                    @if (t.ok) {
                      <div class="text-muted mt-1">
                        @if (t.source) { origen={{ t.source }} · }
                        @if (t.host) { host={{ t.host }} · }
                        @if (t.database) { db={{ t.database }} · }
                        @if (t.canReadEmpresa === true) { empresa OK · }
                        @if (t.canReadEmpresa === false) { empresa NO encontrada · }
                        @if (t.canReadStock === true) { stock depósito #{{ t.idDepositoProbed }} = {{ t.stockRowsForDeposito }} filas }
                        @if (t.canReadStock === false) { sin lectura dbo.stock }
                      </div>
                    }
                  </div>
                }
              </div>
            </div>
            <button type="button" class="btn btn-primary" (click)="saveConfig()">
              <i class="fas fa-save mr-1"></i> Guardar
            </button>
          </div>
        </div>
      }

      @if (tab() === 'pagos') {
        <div class="card card-outline card-info mb-3">
          <div class="card-header"><h3 class="card-title">Correo, Bancard y redes</h3></div>
          <div class="card-body">
            <h5>Correo (SMTP ERP)</h5>
            <div class="row">
              <div class="col-md-4 form-group">
                <label>Servidor</label>
                <input class="form-control" [(ngModel)]="form.nombreServidorCorreoElectronico" />
              </div>
              <div class="col-md-4 form-group">
                <label>Usuario</label>
                <input class="form-control" [(ngModel)]="form.nombreUsuarioCorreoElectronico" autocomplete="off" />
              </div>
              <div class="col-md-2 form-group">
                <label>Puerto</label>
                <input class="form-control" type="number" [(ngModel)]="form.puertoCorreoElectronico" />
              </div>
              <div class="col-md-4 form-group">
                <label>Password {{ form.tienePasswordCorreo ? '(configurado — vacío = no cambiar)' : '' }}</label>
                <input class="form-control" type="password" [(ngModel)]="form.passwordCorreoElectronico"
                       placeholder="••••••••" autocomplete="new-password" />
              </div>
            </div>
            <h5 class="mt-2">Bancard / pago online</h5>
            <div class="row">
              <div class="col-md-6 form-group">
                <label>Clave pública</label>
                <input class="form-control" [(ngModel)]="form.clavePublicaProcesadoraTarjeta" autocomplete="off" />
              </div>
              <div class="col-md-6 form-group">
                <label>Clave privada {{ form.tieneClavePrivada ? '(configurada — vacío = no cambiar)' : '' }}</label>
                <input class="form-control" type="password" [(ngModel)]="form.clavePrivadaProcesadoraTarjeta"
                       placeholder="••••••••" autocomplete="new-password" />
              </div>
              <div class="col-md-6 form-group">
                <label>URL checkout / return</label>
                <input class="form-control" [(ngModel)]="form.direccionURLCheckout" />
              </div>
              <div class="col-md-3 form-group">
                <label>Código comercio</label>
                <input class="form-control" type="number" [(ngModel)]="form.codigoComercio" />
              </div>
              <div class="col-md-3 form-group">
                <label>Código sucursal</label>
                <input class="form-control" type="number" [(ngModel)]="form.codigoSucursal" />
              </div>
            </div>
            <h5 class="mt-3">Redes sociales</h5>
            <div class="row">
              <div class="col-md-4 form-group">
                <label>Facebook</label>
                <input class="form-control" [(ngModel)]="form.direccionURLFacebook" />
              </div>
              <div class="col-md-4 form-group">
                <label>Instagram</label>
                <input class="form-control" [(ngModel)]="form.direccionURLInstagram" />
              </div>
              <div class="col-md-4 form-group">
                <label>WhatsApp</label>
                <input class="form-control" [(ngModel)]="form.direccionURLWhatsapp" />
              </div>
              <div class="col-md-4 form-group">
                <label>Twitter / X</label>
                <input class="form-control" [(ngModel)]="form.direccionURLTwitter" />
              </div>
              <div class="col-md-4 form-group">
                <label>YouTube</label>
                <input class="form-control" [(ngModel)]="form.direccionURLYoutube" />
              </div>
            </div>
            @if (form.redesSociales?.length) {
              <div class="table-responsive mt-2">
                <table class="table table-sm table-striped mb-0">
                  <thead><tr><th>Red (ERP)</th><th>URL</th><th>Logo</th></tr></thead>
                  <tbody>
                    @for (rs of form.redesSociales!; track rs.id) {
                      <tr>
                        <td>{{ rs.nombreRedSocial }}</td>
                        <td class="text-truncate" style="max-width:220px">{{ rs.direccionURLRedSocial }}</td>
                        <td class="text-truncate" style="max-width:160px">{{ rs.logoRedSocial }}</td>
                      </tr>
                    }
                  </tbody>
                </table>
              </div>
            }
            <button type="button" class="btn btn-primary mt-2" (click)="saveConfig()">
              <i class="fas fa-save mr-1"></i> Guardar
            </button>
          </div>
        </div>
      }
    }

    @if (tab() === 'agencias') {
      <div class="card card-outline card-info mb-3">
        <div class="card-header"><h3 class="card-title">Agencias / sucursales</h3></div>
        <div class="card-body">
          <div class="form-row">
            <div class="col-md-4 form-group">
              <label>Descripción</label>
              <input class="form-control form-control-sm" [(ngModel)]="agenciaForm.descripcionAgencia" />
            </div>
            <div class="col-md-4 form-group">
              <label>Dirección</label>
              <input class="form-control form-control-sm" [(ngModel)]="agenciaForm.direccion" />
            </div>
            <div class="col-md-2 form-group">
              <label>Depósito</label>
              <select class="custom-select custom-select-sm" [(ngModel)]="agenciaForm.idDeposito">
                <option [ngValue]="null">—</option>
                @for (d of depositos(); track d.id) {
                  <option [ngValue]="d.id">{{ d.descripcion }} (#{{ d.codigo }})</option>
                }
              </select>
            </div>
            <div class="col-md-2 form-group">
              <label>Lat</label>
              <input class="form-control form-control-sm" type="number" [(ngModel)]="agenciaForm.latitud" />
            </div>
            <div class="col-md-2 form-group">
              <label>Lng</label>
              <input class="form-control form-control-sm" type="number" [(ngModel)]="agenciaForm.longitud" />
            </div>
            <div class="col-md-2 form-group d-flex align-items-end">
              <div class="custom-control custom-checkbox mb-2">
                <input type="checkbox" class="custom-control-input" id="flgSucEcom"
                       [(ngModel)]="agenciaForm.esSucursalEcommerce" />
                <label class="custom-control-label" for="flgSucEcom">Sucursal e-com</label>
              </div>
            </div>
            <div class="col-md-2 form-group d-flex align-items-end">
              <button class="btn btn-sm btn-success btn-block" type="button" (click)="saveAgencia()">
                <i class="fas fa-save mr-1"></i>{{ editingAgenciaId() ? 'Guardar' : 'Alta' }}
              </button>
            </div>
          </div>
          @if (editingAgenciaId()) {
            <div class="mb-2">
              <button type="button" class="btn btn-xs btn-default" (click)="cancelEditAgencia()">Cancelar edición</button>
            </div>
          }
          <div class="table-responsive">
            <table class="table table-sm table-hover mb-0">
              <thead>
                <tr><th>Cód</th><th>Nombre</th><th>Dirección</th><th>Depósito</th><th>E-com</th><th></th></tr>
              </thead>
              <tbody>
                @for (a of agencias(); track a.id) {
                  <tr [class.table-secondary]="!a.activo">
                    <td>{{ a.codAgencia }}</td>
                    <td><strong>{{ a.descripcionAgencia }}</strong></td>
                    <td class="small">{{ a.direccion || '—' }}</td>
                    <td>{{ a.idDeposito || '—' }}</td>
                    <td>@if (a.esSucursalEcommerce) { <span class="badge badge-soft-success">Sí</span> } @else { — }</td>
                    <td class="text-right">
                      <button class="btn btn-xs btn-outline-primary mr-1" type="button" (click)="editAgencia(a)"><i class="fas fa-edit"></i></button>
                      <button class="btn btn-xs btn-outline-danger" type="button" (click)="deleteAgencia(a)" [disabled]="!a.activo"><i class="fas fa-trash"></i></button>
                    </td>
                  </tr>
                } @empty {
                  <tr><td colspan="6" class="text-center text-muted py-3">Sin agencias.</td></tr>
                }
              </tbody>
            </table>
          </div>
        </div>
      </div>
    }

    @if (tab() === 'horarios') {
      <div class="card card-outline card-warning mb-3">
        <div class="card-header"><h3 class="card-title">Horarios de atención</h3></div>
        <div class="card-body">
          <div class="form-row">
            <div class="col-md-3 form-group">
              <label>Agencia</label>
              <select class="custom-select custom-select-sm" [(ngModel)]="horarioForm.idAgencia">
                @for (a of agenciasActivas(); track a.id) {
                  <option [ngValue]="a.id">{{ a.descripcionAgencia }}</option>
                }
              </select>
            </div>
            <div class="col-md-2 form-group">
              <label>Día</label>
              <select class="custom-select custom-select-sm" [(ngModel)]="horarioForm.diaSemana">
                @for (d of dias; track d.value) {
                  <option [ngValue]="d.value">{{ d.label }}</option>
                }
              </select>
            </div>
            <div class="col-md-2 form-group">
              <label>1ª apertura</label>
              <input class="form-control form-control-sm" [(ngModel)]="horarioForm.horaPrimeraApertura" />
            </div>
            <div class="col-md-2 form-group">
              <label>1º cierre</label>
              <input class="form-control form-control-sm" [(ngModel)]="horarioForm.horaPrimerCierre" />
            </div>
            <div class="col-md-2 form-group">
              <label>2ª apertura</label>
              <input class="form-control form-control-sm" [(ngModel)]="horarioForm.horaSegundaApertura" />
            </div>
            <div class="col-md-2 form-group">
              <label>2º cierre</label>
              <input class="form-control form-control-sm" [(ngModel)]="horarioForm.horaSegundoCierre" />
            </div>
            <div class="col-md-2 form-group d-flex align-items-end">
              <button class="btn btn-sm btn-success btn-block" type="button" (click)="createHorario()">
                <i class="fas fa-plus mr-1"></i> Alta
              </button>
            </div>
          </div>
          <div class="table-responsive">
            <table class="table table-sm table-hover mb-0">
              <thead>
                <tr><th>Agencia</th><th>Día</th><th>Turno 1</th><th>Turno 2</th><th></th></tr>
              </thead>
              <tbody>
                @for (h of horarios(); track h.id) {
                  <tr [class.table-secondary]="!h.activo">
                    <td>{{ h.nombreAgencia }}</td>
                    <td>{{ diaLabel(h.diaSemana) }}</td>
                    <td>{{ h.horaPrimeraApertura }} – {{ h.horaPrimerCierre }}</td>
                    <td>{{ h.horaSegundaApertura || '—' }} – {{ h.horaSegundoCierre || '—' }}</td>
                    <td class="text-right">
                      <button class="btn btn-xs btn-outline-danger" type="button" (click)="deleteHorario(h)" [disabled]="!h.activo">
                        <i class="fas fa-trash"></i>
                      </button>
                    </td>
                  </tr>
                } @empty {
                  <tr><td colspan="5" class="text-center text-muted py-3">Sin horarios.</td></tr>
                }
              </tbody>
            </table>
          </div>
        </div>
      </div>
    }
  `,
  styles: [`
    .emp-status { display:flex; flex-wrap:wrap; gap:.4rem; margin:-.35rem 0 .85rem; }
    .emp-chip { display:inline-flex; align-items:center; padding:.2rem .55rem; border-radius:999px; border:1px solid #e2e8f0; background:#fff; color:#475569; font-size:.75rem; font-weight:600; }
    .emp-chip.on, .emp-chip.ok { background:#ecfdf5; border-color:#99f6e4; color:#0f766e; }
    .emp-chip.warn { background:#fffbeb; border-color:#fcd34d; color:#92400e; }
    .emp-chip.muted { color:#64748b; }
    .emp-tabs { display:flex; flex-wrap:wrap; gap:.35rem; margin-bottom:.9rem; padding:.35rem; border:1px solid #e2e8f0; border-radius:12px; background:#fff; }
    .emp-tabs button { border:0; background:transparent; border-radius:8px; padding:.45rem .75rem; font-size:.86rem; font-weight:600; color:#475569; cursor:pointer; }
    .emp-tabs button:hover { background:#f1f5f9; }
    .emp-tabs button.on { background:#0f766e; color:#fff; }
    .emp-switches { display:grid; grid-template-columns:repeat(auto-fit,minmax(210px,1fr)); gap:.55rem .85rem; }
    .emp-switch { display:flex; align-items:flex-start; gap:.45rem; margin:0; font-size:.88rem; color:#334155; font-weight:500; }
    .emp-switch input { margin-top:.2rem; }
    .emp-switch small { display:block; color:#94a3b8; font-weight:400; }
    @media (max-width:700px) { .emp-tabs { overflow-x:auto; flex-wrap:nowrap; } .emp-tabs button { white-space:nowrap; } }
  `],
})

export class EmpresaConfigPageComponent implements OnInit, OnDestroy {
  private readonly api = inject(AdminApiService);
  private healthTimer: ReturnType<typeof setInterval> | null = null;

  readonly dias = DIAS;
  readonly cfg = signal<AdminEmpresaConfig | null>(null);
  readonly agencias = signal<AdminAgencia[]>([]);
  readonly depositos = signal<AdminDeposito[]>([]);
  readonly horarios = signal<AdminHorario[]>([]);
  readonly error = signal<string | null>(null);
  readonly ok = signal<string | null>(null);
  readonly editingAgenciaId = signal<number | null>(null);
  readonly testingErp = signal(false);
  readonly erpBusy = signal<'stock' | 'flush' | null>(null);
  readonly erpTest = signal<ErpConnectionTestResult | null>(null);
  readonly erpHealth = signal<AdminErpHealth | null>(null);
  readonly tab = signal<'tienda' | 'operacion' | 'erp' | 'pagos' | 'agencias' | 'horarios'>('tienda');

  form: Partial<AdminEmpresaConfig> = {};
  erpPasswordDraft = '';
  clearErpPassword = false;
  agenciaForm: UpsertAgenciaRequest = {
    descripcionAgencia: '',
    direccion: '',
    idDeposito: null,
    latitud: null,
    longitud: null,
    esSucursalEcommerce: true,
    activo: true,
  };
  horarioForm: UpsertHorarioRequest = {
    idAgencia: 0,
    diaSemana: 1,
    horaPrimeraApertura: '08:00',
    horaPrimerCierre: '12:00',
    horaSegundaApertura: '14:00',
    horaSegundoCierre: '18:00',
    activo: true,
  };

  ngOnInit(): void {
    this.reload();
    this.healthTimer = setInterval(() => this.refreshErpHealth(), 45_000);
  }

  ngOnDestroy(): void {
    if (this.healthTimer) clearInterval(this.healthTimer);
  }

  formatCountdown(seconds: number): string {
    const s = Math.max(0, Math.floor(seconds));
    if (s < 60) return `${s}s`;
    const m = Math.floor(s / 60);
    const r = s % 60;
    return r ? `${m}m ${r}s` : `${m}m`;
  }

  agenciasActivas(): AdminAgencia[] {
    return this.agencias().filter((a) => a.activo);
  }

  diaLabel(d: number): string {
    return DIAS.find((x) => x.value === d)?.label ?? String(d);
  }

  reload(): void {
    this.error.set(null);
    this.erpTest.set(null);
    this.api.getEmpresaConfig().subscribe({
      next: (c) => {
        this.cfg.set(c);
        this.form = { ...c, modoIntegracion: c.modoIntegracion || 'Standalone' };
        this.erpPasswordDraft = '';
        this.clearErpPassword = false;
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
    this.refreshErpHealth();
    this.api.listDepositos().subscribe({
      next: (rows) => this.depositos.set(rows),
      error: () => this.depositos.set([]),
    });
    this.api.listAgencias().subscribe({
      next: (rows) => {
        this.agencias.set(rows);
        const first = rows.find((a) => a.activo);
        if (first && !this.horarioForm.idAgencia) this.horarioForm.idAgencia = first.id;
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
    this.api.listHorarios().subscribe({
      next: (rows) => this.horarios.set(rows),
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  refreshErpHealth(): void {
    this.api.getErpHealth().subscribe({
      next: (h) => this.erpHealth.set(h),
      error: () => this.erpHealth.set(null),
    });
  }

  onModoChange(mode: string): void {
    this.form.modoIntegracion = mode;
    if (mode === 'Integrated') {
      this.form.controlaStockEnCarrito = false;
    }
  }

  activarIntegracionCompleta(): void {
    if (!confirm('¿Activar Integrated? Vas a poder usar el backoffice ERP y este admin a la vez (stock espejo + pedidos al ERP). Se guarda ya.')) return;
    this.ok.set(null);
    this.api.updateEmpresaConfig({ activarIntegracionErpCompleta: true }).subscribe({
      next: (c) => {
        this.cfg.set(c);
        this.form = { ...c };
        this.ok.set('Integración ERP completa activada');
        this.refreshErpHealth();
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  clearErpConnection(): void {
    if (!confirm('¿Limpiar host/db/usuario/password de esta empresa y usar solo el fallback global?')) return;
    this.api.updateEmpresaConfig({ clearErpConnection: true }).subscribe({
      next: (c) => {
        this.cfg.set(c);
        this.form = { ...c };
        this.erpPasswordDraft = '';
        this.clearErpPassword = false;
        this.ok.set('Conexión ERP propia limpiada');
        this.refreshErpHealth();
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  saveConfig(): void {
    this.ok.set(null);
    this.error.set(null);
    const body: Partial<AdminEmpresaConfig> = { ...this.form };
    delete (body as { tieneErpPassword?: boolean }).tieneErpPassword;
    delete (body as { erpConnectionConfigured?: boolean }).erpConnectionConfigured;
    delete (body as { erpConnectionSource?: string }).erpConnectionSource;
    delete (body as { erpWriteBackEffective?: boolean }).erpWriteBackEffective;
    if (this.erpPasswordDraft.trim()) body.erpPassword = this.erpPasswordDraft.trim();
    if (this.clearErpPassword) body.clearErpPassword = true;
    this.api.updateEmpresaConfig(body).subscribe({
      next: (c) => {
        this.cfg.set(c);
        this.form = { ...c };
        this.erpPasswordDraft = '';
        this.clearErpPassword = false;
        this.ok.set('Configuración guardada');
        this.refreshErpHealth();
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  testErp(): void {
    this.testingErp.set(true);
    this.erpTest.set(null);
    const draft =
      this.form.erpHost?.trim() && this.form.erpDatabase?.trim() && this.form.erpUsername?.trim()
        ? {
            erpHost: this.form.erpHost,
            erpPort: this.form.erpPort ?? null,
            erpDatabase: this.form.erpDatabase,
            erpUsername: this.form.erpUsername,
            erpPassword: this.erpPasswordDraft.trim() || null,
            erpSslMode: this.form.erpSslMode ?? null,
            useSavedPasswordIfEmpty: true,
          }
        : {};
    this.api.testErpConnection(draft).subscribe({
      next: (r) => {
        this.testingErp.set(false);
        this.erpTest.set(r);
        this.refreshErpHealth();
      },
      error: (e) => {
        this.testingErp.set(false);
        this.erpTest.set({
          ok: false,
          message: e?.error?.error || e.message || 'Error al probar',
        });
      },
    });
  }

  pullStockNow(): void {
    this.erpBusy.set('stock');
    this.ok.set(null);
    this.error.set(null);
    this.api.syncStatus().subscribe({
      next: (s) => {
        this.erpBusy.set(null);
        const stock = (s?.cursors ?? []).find((c: { entityKey?: string }) => c.entityKey === 'stock');
        const n = stock?.rowsRead ?? stock?.rowsProcessed ?? 0;
        this.ok.set(
          `Stock: cursor ecom-etl (${stock?.lastStatus ?? '—'} · ${n} filas). El pull lo hace ecom-etl, no la API.`,
        );
        this.refreshErpHealth();
      },
      error: (e) => {
        this.erpBusy.set(null);
        this.error.set(e?.error?.error || e.message);
      },
    });
  }

  flushPending(): void {
    if (!confirm('¿Reintentar write-back de pedidos pendientes hacia el ERP?')) return;
    this.erpBusy.set('flush');
    this.ok.set(null);
    this.error.set(null);
    this.api.flushPendingErpOrders().subscribe({
      next: (r) => {
        this.erpBusy.set(null);
        this.ok.set(
          `Write-back: ${r.exitosos} ok · ${r.errores} error · ${r.procesados} procesados`,
        );
        this.refreshErpHealth();
      },
      error: (e) => {
        this.erpBusy.set(null);
        this.error.set(e?.error?.error || e.message);
      },
    });
  }

  editAgencia(a: AdminAgencia): void {
    this.editingAgenciaId.set(a.id);
    this.agenciaForm = {
      codAgencia: a.codAgencia,
      descripcionAgencia: a.descripcionAgencia,
      direccion: a.direccion,
      idDeposito: a.idDeposito,
      latitud: a.latitud,
      longitud: a.longitud,
      esSucursalEcommerce: a.esSucursalEcommerce,
      activo: a.activo,
    };
  }

  cancelEditAgencia(): void {
    this.editingAgenciaId.set(null);
    this.agenciaForm = {
      descripcionAgencia: '',
      direccion: '',
      idDeposito: null,
      latitud: null,
      longitud: null,
      esSucursalEcommerce: true,
      activo: true,
    };
  }

  saveAgencia(): void {
    if (!this.agenciaForm.descripcionAgencia?.trim()) {
      this.error.set('Descripción de agencia requerida');
      return;
    }
    const id = this.editingAgenciaId();
    const req = id
      ? this.api.updateAgencia(id, this.agenciaForm)
      : this.api.createAgencia(this.agenciaForm);
    req.subscribe({
      next: () => {
        this.ok.set(id ? 'Agencia actualizada' : 'Agencia creada');
        this.cancelEditAgencia();
        this.reload();
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  deleteAgencia(a: AdminAgencia): void {
    if (!confirm(`¿Desactivar agencia ${a.descripcionAgencia}?`)) return;
    this.api.deleteAgencia(a.id).subscribe({
      next: () => {
        this.ok.set('Agencia desactivada');
        this.reload();
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  createHorario(): void {
    if (!this.horarioForm.idAgencia) {
      this.error.set('Seleccioná una agencia');
      return;
    }
    this.api.createHorario(this.horarioForm).subscribe({
      next: () => {
        this.ok.set('Horario creado');
        this.reload();
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  deleteHorario(h: AdminHorario): void {
    this.api.deleteHorario(h.id).subscribe({
      next: () => {
        this.ok.set('Horario desactivado');
        this.reload();
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }
}
