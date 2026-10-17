import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import {
  AdminAgencia,
  AdminApiService,
  AdminEmpresaConfig,
  AdminErpHealth,
  AdminHorario,
} from '../core/admin-api.service';
import { EmpresaConfigPageComponent } from './empresa-config-page.component';

function cfg(partial: Partial<AdminEmpresaConfig> = {}): AdminEmpresaConfig {
  return {
    idEmpresa: 41,
    razonSocial: 'Casa Imperial',
    nombreFantasia: 'CI',
    numeroDocumento: '80000000-0',
    slugTienda: 'casa-imperial',
    dominioCustom: null,
    realizaDelivery: true,
    realizaPickup: true,
    cobraEnLaEntrega: false,
    cobraAlHacerPedido: true,
    montoPedidoMinimo: 0,
    procesaPedidosPorPlanilla: false,
    procesaPedidosERP: true,
    controlaStockEnCarrito: false,
    modoIntegracion: 'Integrated',
    syncStockContinuo: true,
    migrarPedidosAlGenerar: true,
    syncStockIntervaloMinutos: 1,
    idDeposito: 1,
    idArticuloDelivery: null,
    direccionURLFacebook: null,
    direccionURLTwitter: null,
    direccionURLInstagram: null,
    direccionURLYoutube: null,
    direccionURLWhatsapp: null,
    ...partial,
  } as AdminEmpresaConfig;
}

function health(partial: Partial<AdminErpHealth> = {}): AdminErpHealth {
  return {
    hasConnection: true,
    connectionSource: 'Tenant',
    writeBackEffective: true,
    syncStockContinuo: true,
    syncStockIntervaloMinutos: 1,
    migrarPedidosAlGenerar: true,
    procesaPedidosERP: true,
    modoIntegracion: 'Integrated',
    idDeposito: 1,
    lastStockSyncUtc: '2026-08-17T12:00:00Z',
    lastStockStatus: 'ok',
    lastStockRows: 10,
    lastStockError: null,
    pendingOrdersWriteBack: 2,
    hint: 'ok',
    ...partial,
  };
}

function agencia(partial: Partial<AdminAgencia> = {}): AdminAgencia {
  return {
    id: 7,
    codAgencia: 1,
    descripcionAgencia: 'Central',
    direccion: 'Calle 1',
    idCiudad: null,
    idDeposito: 1,
    latitud: null,
    longitud: null,
    esSucursalEcommerce: true,
    activo: true,
    ...partial,
  };
}

function horario(partial: Partial<AdminHorario> = {}): AdminHorario {
  return {
    id: 9,
    idAgencia: 7,
    nombreAgencia: 'Central',
    diaSemana: 1,
    horaPrimeraApertura: '08:00',
    horaPrimerCierre: '12:00',
    horaSegundaApertura: '14:00',
    horaSegundoCierre: '18:00',
    activo: true,
    ...partial,
  };
}

describe('EmpresaConfigPageComponent', () => {
  let api: jasmine.SpyObj<AdminApiService>;
  let confirmSpy: jasmine.Spy;

  beforeEach(async () => {
    api = jasmine.createSpyObj<AdminApiService>('AdminApiService', [
      'getEmpresaConfig',
      'getErpHealth',
      'listDepositos',
      'listAgencias',
      'listHorarios',
      'updateEmpresaConfig',
      'testErpConnection',
      'syncStatus',
      'flushPendingErpOrders',
      'createAgencia',
      'updateAgencia',
      'deleteAgencia',
      'createHorario',
      'deleteHorario',
    ]);

    api.getEmpresaConfig.and.returnValue(of(cfg()));
    api.getErpHealth.and.returnValue(of(health()));
    api.listDepositos.and.returnValue(of([{ id: 1, codigo: 1, descripcion: 'Depósito 1', esDepositoEcommerce: true }]));
    api.listAgencias.and.returnValue(of([agencia()]));
    api.listHorarios.and.returnValue(of([horario()]));
    api.updateEmpresaConfig.and.callFake((body) => of(cfg(body as Partial<AdminEmpresaConfig>)));
    api.testErpConnection.and.returnValue(of({ ok: true, message: 'OK', source: 'Tenant', canReadEmpresa: true, canReadStock: true }));
    api.syncStatus.and.returnValue(of({ cursors: [{ entityKey: 'stock', lastStatus: 'ok', rowsRead: 12 }] }));
    api.flushPendingErpOrders.and.returnValue(of({ procesados: 2, exitosos: 2, errores: 0, logs: [] }));
    api.createAgencia.and.returnValue(of(agencia({ id: 99, descripcionAgencia: 'Nueva' })));
    api.updateAgencia.and.returnValue(of(agencia({ descripcionAgencia: 'Editada' })));
    api.deleteAgencia.and.returnValue(of(void 0));
    api.createHorario.and.returnValue(of(horario({ id: 55 })));
    api.deleteHorario.and.returnValue(of(void 0));

    confirmSpy = spyOn(window, 'confirm').and.returnValue(true);

    await TestBed.configureTestingModule({
      imports: [EmpresaConfigPageComponent],
      providers: [provideRouter([]), { provide: AdminApiService, useValue: api }],
    }).compileComponents();
  });

  function create(): EmpresaConfigPageComponent {
    const fixture = TestBed.createComponent(EmpresaConfigPageComponent);
    fixture.detectChanges();
    return fixture.componentInstance;
  }

  it('carga config, salud ERP, depósitos, agencias y horarios al iniciar', () => {
    const c = create();
    expect(api.getEmpresaConfig).toHaveBeenCalled();
    expect(api.getErpHealth).toHaveBeenCalled();
    expect(api.listDepositos).toHaveBeenCalled();
    expect(api.listAgencias).toHaveBeenCalled();
    expect(api.listHorarios).toHaveBeenCalled();
    expect(c.cfg()?.razonSocial).toBe('Casa Imperial');
    expect(c.erpHealth()?.hasConnection).toBeTrue();
    expect(c.agencias().length).toBe(1);
    expect(c.horarios().length).toBe(1);
    expect(c.horarioForm.idAgencia).toBe(7);
  });

  it('cambia de pestaña sin perder el formulario', () => {
    const c = create();
    c.form.razonSocial = 'Editado';
    c.tab.set('erp');
    expect(c.tab()).toBe('erp');
    expect(c.form.razonSocial).toBe('Editado');
  });

  it('al pasar a Integrated desactiva controlaStockEnCarrito', () => {
    const c = create();
    c.form.controlaStockEnCarrito = true;
    c.onModoChange('Integrated');
    expect(c.form.modoIntegracion).toBe('Integrated');
    expect(c.form.controlaStockEnCarrito).toBeFalse();
  });

  it('guarda configuración y limpia flags de password', () => {
    const c = create();
    c.form.razonSocial = 'Nueva RS';
    c.erpPasswordDraft = 'secret';
    c.clearErpPassword = false;
    c.saveConfig();
    expect(api.updateEmpresaConfig).toHaveBeenCalled();
    const body = api.updateEmpresaConfig.calls.mostRecent().args[0] as Record<string, unknown>;
    expect(body['razonSocial']).toBe('Nueva RS');
    expect(body['erpPassword']).toBe('secret');
    expect(body['tieneErpPassword']).toBeUndefined();
    expect(c.ok()).toBe('Configuración guardada');
    expect(c.erpPasswordDraft).toBe('');
  });

  it('guarda con clearErpPassword cuando el checkbox está activo', () => {
    const c = create();
    c.clearErpPassword = true;
    c.saveConfig();
    const body = api.updateEmpresaConfig.calls.mostRecent().args[0] as Record<string, unknown>;
    expect(body['clearErpPassword']).toBeTrue();
    expect(c.clearErpPassword).toBeFalse();
  });

  it('muestra error si falla el guardado', () => {
    api.updateEmpresaConfig.and.returnValue(throwError(() => ({ error: { error: 'No se pudo guardar' } })));
    const c = create();
    c.saveConfig();
    expect(c.error()).toBe('No se pudo guardar');
  });

  it('activa integración completa solo si el usuario confirma', () => {
    const c = create();
    c.activarIntegracionCompleta();
    expect(confirmSpy).toHaveBeenCalled();
    expect(api.updateEmpresaConfig).toHaveBeenCalledWith({ activarIntegracionErpCompleta: true });
    expect(c.ok()).toBe('Integración ERP completa activada');
  });

  it('no activa integración si el usuario cancela el confirm', () => {
    confirmSpy.and.returnValue(false);
    const c = create();
    api.updateEmpresaConfig.calls.reset();
    c.activarIntegracionCompleta();
    expect(api.updateEmpresaConfig).not.toHaveBeenCalled();
  });

  it('limpia la conexión ERP propia tras confirmar', () => {
    const c = create();
    c.clearErpConnection();
    expect(api.updateEmpresaConfig).toHaveBeenCalledWith({ clearErpConnection: true });
    expect(c.ok()).toBe('Conexión ERP propia limpiada');
  });

  it('prueba conexión ERP con draft cuando hay host/db/user', () => {
    const c = create();
    c.form.erpHost = '10.99.99.74';
    c.form.erpDatabase = 'erp_db';
    c.form.erpUsername = 'user';
    c.erpPasswordDraft = 'pwd';
    c.testErp();
    expect(api.testErpConnection).toHaveBeenCalledWith(
      jasmine.objectContaining({
        erpHost: '10.99.99.74',
        erpDatabase: 'erp_db',
        erpUsername: 'user',
        erpPassword: 'pwd',
        useSavedPasswordIfEmpty: true,
      }),
    );
    expect(c.testingErp()).toBeFalse();
    expect(c.erpTest()?.ok).toBeTrue();
  });

  it('prueba conexión ERP con body vacío si no hay draft completo', () => {
    const c = create();
    c.form.erpHost = '';
    c.testErp();
    expect(api.testErpConnection).toHaveBeenCalledWith({});
  });

  it('consulta cursor de stock (ecom-etl) sin mutar ERP', () => {
    const c = create();
    c.pullStockNow();
    expect(api.syncStatus).toHaveBeenCalled();
    expect(c.erpBusy()).toBeNull();
    expect(c.ok()).toContain('cursor ecom-etl');
  });

  it('reintenta write-back de pedidos pendientes', () => {
    const c = create();
    c.flushPending();
    expect(confirmSpy).toHaveBeenCalled();
    expect(api.flushPendingErpOrders).toHaveBeenCalled();
    expect(c.ok()).toContain('Write-back: 2 ok');
    expect(c.erpBusy()).toBeNull();
  });

  it('no hace flush si se cancela el confirm', () => {
    confirmSpy.and.returnValue(false);
    const c = create();
    c.flushPending();
    expect(api.flushPendingErpOrders).not.toHaveBeenCalled();
  });

  it('crea agencia nueva y recarga', () => {
    const c = create();
    api.listAgencias.calls.reset();
    c.agenciaForm.descripcionAgencia = 'Sucursal Sur';
    c.saveAgencia();
    expect(api.createAgencia).toHaveBeenCalled();
    expect(c.ok()).toBe('Agencia creada');
    expect(c.editingAgenciaId()).toBeNull();
    expect(api.listAgencias).toHaveBeenCalled();
  });

  it('actualiza agencia existente', () => {
    const c = create();
    c.editAgencia(agencia());
    c.agenciaForm.descripcionAgencia = 'Central editada';
    c.saveAgencia();
    expect(api.updateAgencia).toHaveBeenCalledWith(7, jasmine.objectContaining({ descripcionAgencia: 'Central editada' }));
    expect(c.ok()).toBe('Agencia actualizada');
  });

  it('exige descripción al guardar agencia', () => {
    const c = create();
    c.agenciaForm.descripcionAgencia = '  ';
    c.saveAgencia();
    expect(api.createAgencia).not.toHaveBeenCalled();
    expect(c.error()).toBe('Descripción de agencia requerida');
  });

  it('desactiva agencia tras confirmar', () => {
    const c = create();
    c.deleteAgencia(agencia());
    expect(confirmSpy).toHaveBeenCalled();
    expect(api.deleteAgencia).toHaveBeenCalledWith(7);
    expect(c.ok()).toBe('Agencia desactivada');
  });

  it('crea horario cuando hay agencia seleccionada', () => {
    const c = create();
    c.horarioForm.idAgencia = 7;
    c.createHorario();
    expect(api.createHorario).toHaveBeenCalledWith(jasmine.objectContaining({ idAgencia: 7 }));
    expect(c.ok()).toBe('Horario creado');
  });

  it('exige agencia al crear horario', () => {
    const c = create();
    c.horarioForm.idAgencia = 0;
    c.createHorario();
    expect(api.createHorario).not.toHaveBeenCalled();
    expect(c.error()).toBe('Seleccioná una agencia');
  });

  it('desactiva horario', () => {
    const c = create();
    c.deleteHorario(horario());
    expect(api.deleteHorario).toHaveBeenCalledWith(9);
    expect(c.ok()).toBe('Horario desactivado');
  });

  it('formatea countdown de próximo sync', () => {
    const c = create();
    expect(c.formatCountdown(45)).toBe('45s');
    expect(c.formatCountdown(90)).toBe('1m 30s');
    expect(c.formatCountdown(120)).toBe('2m');
  });
});
