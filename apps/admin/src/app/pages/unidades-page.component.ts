import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AdminApiService, AdminCodigoBarra, AdminProductEmbalaje, AdminUnidadMedida } from '../core/admin-api.service';

@Component({
  selector: 'app-unidades-page',
  standalone: true,
  imports: [FormsModule, RouterLink],
  template: `
    <div class="dx-toolbar">
      <div>
        <h1 class="dx-page-title">Unidades y embalajes</h1>
        <p class="dx-page-sub">
          Unidades de medida y embalajes por artículo (paridad ERP ArticuloEmbalaje / CodigoBarra).
        </p>
      </div>
      @if (idArticulo()) {
        <a class="btn btn-outline-secondary btn-sm" routerLink="/catalogo">
          <i class="fas fa-arrow-left mr-1"></i> Catálogo
        </a>
      }
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

        <div class="card card-outline card-primary">
          <div class="card-header"><h3 class="card-title">Unidades de medida (metro, m², kg…)</h3></div>
          <div class="card-body">
            <div class="table-responsive">
              <table class="table table-sm table-hover">
                <thead>
                  <tr><th>Código</th><th>Nombre</th><th>Símbolo</th><th>Decimales</th><th>Incremento</th><th></th></tr>
                </thead>
                <tbody>
                  @for (u of unidades(); track u.id) {
                    <tr>
                      <td><code>{{ u.codigo }}</code></td>
                      <td>{{ u.nombre }}</td>
                      <td>{{ u.simbolo }}</td>
                      <td>
                        @if (u.permiteDecimales) {
                          <span class="badge badge-soft-info">Sí</span>
                        } @else {
                          <span class="badge badge-soft-warning">No</span>
                        }
                      </td>
                      <td>{{ u.incrementoDefault }}</td>
                      <td>
                        <button type="button" class="btn btn-xs btn-outline-primary" (click)="editUnidad(u)">Editar</button>
                      </td>
                    </tr>
                  } @empty {
                    <tr><td colspan="6"><div class="dx-empty py-3"><p>Sin unidades. Cargá el alta abajo.</p></div></td></tr>
                  }
                </tbody>
              </table>
            </div>
            <hr />
            <div class="form-row">
              <div class="form-group col-md-2"><label>Código</label><input class="form-control" [(ngModel)]="uForm.codigo" name="uc" /></div>
              <div class="form-group col-md-3"><label>Nombre</label><input class="form-control" [(ngModel)]="uForm.nombre" name="un" /></div>
              <div class="form-group col-md-2"><label>Símbolo</label><input class="form-control" [(ngModel)]="uForm.simbolo" name="us" /></div>
              <div class="form-group col-md-2"><label>Incremento</label><input type="number" step="0.01" class="form-control" [(ngModel)]="uForm.incrementoDefault" name="ui" /></div>
              <div class="form-group col-md-2"><label>&nbsp;</label><label class="d-block"><input type="checkbox" [(ngModel)]="uForm.permiteDecimales" name="ud" /> Decimales</label></div>
              <div class="form-group col-md-1"><label>&nbsp;</label><button type="button" class="btn btn-primary btn-block" (click)="saveUnidad()">{{ uForm.id ? 'OK' : '+' }}</button></div>
            </div>
          </div>
        </div>

    @if (idArticulo(); as artId) {
      <div class="card card-outline card-secondary">
        <div class="card-header">
          <h3 class="card-title">Embalajes del artículo #{{ artId }}</h3>
          <div class="card-tools">
            <span class="badge badge-soft-info">{{ embalajes().length }}</span>
          </div>
        </div>
        <div class="card-body">
          <div class="table-responsive mb-3">
            <table class="table table-sm table-hover mb-0">
              <thead>
                <tr>
                  <th>Descripción</th>
                  <th>Cant.</th>
                  <th>Unidad</th>
                  <th>Barras</th>
                  <th>Fracción</th>
                  <th>Estado</th>
                  <th style="width:120px"></th>
                </tr>
              </thead>
              <tbody>
                @for (e of embalajes(); track e.id) {
                  <tr [class.table-secondary]="!e.activo">
                    <td>{{ e.descripcion || '—' }}</td>
                    <td>{{ e.cantidadPorEmbalaje }}</td>
                    <td><code>{{ e.codigoUnidadMedida }}</code></td>
                    <td class="small"><code>{{ e.codigoBarras || '—' }}</code></td>
                    <td>
                      @if (e.permiteFraccion) {
                        <span class="badge badge-soft-info">Sí</span>
                      } @else {
                        <span class="badge badge-soft-warning">No</span>
                      }
                    </td>
                    <td>
                      @if (e.activo) {
                        <span class="badge badge-soft-success">Activo</span>
                      } @else {
                        <span class="badge badge-soft-danger">Inactivo</span>
                      }
                    </td>
                    <td class="table-actions text-nowrap">
                      <button type="button" class="btn btn-xs btn-info" (click)="editEmb(e)" title="Editar">
                        <i class="fas fa-edit"></i>
                      </button>
                      <button type="button" class="btn btn-xs btn-outline-secondary" (click)="openBarras(e)" title="Códigos de barra">
                        <i class="fas fa-barcode"></i>
                      </button>
                    </td>
                  </tr>
                } @empty {
                  <tr>
                    <td colspan="7">
                      <div class="dx-empty py-3">
                        <i class="fas fa-box-open"></i>
                        <p>Sin embalajes. Cargá el alta abajo (unidad base, rollo, caja…).</p>
                      </div>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>

          <div class="border rounded p-3 bg-light">
            <h6 class="mb-3">{{ eForm.id ? 'Editar embalaje #' + eForm.id : 'Alta de embalaje' }}</h6>
            <div class="form-row">
              <div class="form-group col-md-3">
                <label>Descripción</label>
                <input class="form-control" [(ngModel)]="eForm.descripcion" name="ed" placeholder="Por metro / Rollo" />
              </div>
              <div class="form-group col-md-2">
                <label>Cant. base</label>
                <input type="number" step="0.01" class="form-control" [(ngModel)]="eForm.cantidadPorEmbalaje" name="ec" />
              </div>
              <div class="form-group col-md-2">
                <label>Unidad</label>
                <select class="form-control" [(ngModel)]="eForm.codigoUnidadMedida" name="eu">
                  @for (u of unidades(); track u.codigo) {
                    <option [ngValue]="u.codigo">{{ u.codigo }} — {{ u.nombre }}</option>
                  }
                </select>
              </div>
              <div class="form-group col-md-2">
                <label>Cód. barras principal</label>
                <input class="form-control" [(ngModel)]="eForm.codigoBarras" name="eb" />
              </div>
              <div class="form-group col-md-1">
                <label>Paso</label>
                <input type="number" step="0.01" class="form-control" [(ngModel)]="eForm.incrementoVenta" name="ei" />
              </div>
              <div class="form-group col-md-1">
                <label>Mín.</label>
                <input type="number" step="0.01" class="form-control" [(ngModel)]="eForm.cantidadMinimaVenta" name="em" />
              </div>
              <div class="form-group col-md-1">
                <label>&nbsp;</label>
                <div class="custom-control custom-switch mt-2">
                  <input type="checkbox" class="custom-control-input" id="efrac" [(ngModel)]="eForm.permiteFraccion" name="ef" />
                  <label class="custom-control-label" for="efrac">Fracción</label>
                </div>
              </div>
            </div>
            <div class="dx-form-actions">
              <button type="button" class="btn btn-primary btn-sm" (click)="saveEmb()">
                <i class="fas fa-save mr-1"></i> {{ eForm.id ? 'Actualizar' : 'Agregar' }}
              </button>
              @if (eForm.id) {
                <button type="button" class="btn btn-default btn-sm" (click)="resetEmb()">Cancelar</button>
              }
            </div>
          </div>

          @if (barcodeEmbId(); as embId) {
            <hr />
            <h5 class="mb-2"><i class="fas fa-barcode mr-1"></i> Códigos de barras · embalaje #{{ embId }}</h5>
            <ul class="list-unstyled mb-3">
              @for (c of codigos(); track c.id) {
                <li class="mb-1 d-flex align-items-center">
                  <code class="mr-2">{{ c.codigoBarra }}</code>
                  @if (!c.activo) {
                    <span class="badge badge-soft-danger mr-2">inactivo</span>
                  }
                  <button
                    type="button"
                    class="btn btn-xs btn-outline-danger"
                    (click)="removeCodigo(c)"
                    [disabled]="!c.activo"
                  >
                    Quitar
                  </button>
                </li>
              } @empty {
                <li class="text-muted">Sin códigos adicionales</li>
              }
            </ul>
            <div class="form-inline">
              <input class="form-control form-control-sm mr-2" [(ngModel)]="nuevoCodigo" name="nbc" placeholder="Nuevo código" />
              <button type="button" class="btn btn-sm btn-primary" (click)="addCodigo()">Agregar</button>
              <button type="button" class="btn btn-sm btn-default ml-2" (click)="barcodeEmbId.set(null)">Cerrar</button>
            </div>
          }
        </div>
      </div>
    } @else {
      <div class="alert alert-info mb-0">
        <i class="fas fa-info-circle mr-1"></i>
        Para editar embalajes de un producto, abrilo desde el
        <a routerLink="/catalogo">catálogo</a>
        (acción Embalajes) o la ruta
        <code>/catalogo/&#123;id&#125;/embalajes</code>.
      </div>
    }
  `,
})
export class UnidadesPageComponent implements OnInit {
  private readonly api = inject(AdminApiService);
  private readonly route = inject(ActivatedRoute);

  readonly unidades = signal<AdminUnidadMedida[]>([]);
  readonly embalajes = signal<AdminProductEmbalaje[]>([]);
  readonly codigos = signal<AdminCodigoBarra[]>([]);
  readonly barcodeEmbId = signal<number | null>(null);
  readonly idArticulo = signal<number | null>(null);
  readonly error = signal<string | null>(null);
  readonly ok = signal<string | null>(null);
  nuevoCodigo = '';

  uForm: Partial<AdminUnidadMedida> & { id?: number } = {
    codigo: '',
    nombre: '',
    simbolo: '',
    permiteDecimales: false,
    incrementoDefault: 1,
    orden: 0,
    activo: true,
  };

  eForm: Partial<AdminProductEmbalaje> & { id?: number } = {
    descripcion: '',
    cantidadPorEmbalaje: 1,
    codigoBarras: '',
    codigoUnidadMedida: 'UN',
    permiteFraccion: false,
    incrementoVenta: 1,
    cantidadMinimaVenta: 1,
    paraFacturacion: true,
    paraStock: true,
    activo: true,
  };

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.idArticulo.set(Number.isFinite(id) && id > 0 ? id : null);
    this.reloadUnidades();
    if (this.idArticulo()) this.reloadEmb();
  }

  reloadUnidades(): void {
    this.api.listUnidadesMedida().subscribe({
      next: (rows) => this.unidades.set(rows),
      error: (e) => this.error.set(e?.error?.error || 'Error unidades'),
    });
  }

  reloadEmb(): void {
    const id = this.idArticulo();
    if (!id) return;
    this.api.listProductEmbalajes(id).subscribe({
      next: (rows) => this.embalajes.set(rows),
      error: (e) => this.error.set(e?.error?.error || 'Error embalajes'),
    });
  }

  editUnidad(u: AdminUnidadMedida): void {
    this.uForm = { ...u };
  }

  saveUnidad(): void {
    const body = {
      codigo: this.uForm.codigo || '',
      nombre: this.uForm.nombre || '',
      simbolo: this.uForm.simbolo,
      permiteDecimales: !!this.uForm.permiteDecimales,
      incrementoDefault: Number(this.uForm.incrementoDefault) || 1,
      orden: Number(this.uForm.orden) || 0,
      activo: this.uForm.activo !== false,
    };
    const req = this.uForm.id
      ? this.api.updateUnidadMedida(this.uForm.id, body)
      : this.api.createUnidadMedida(body);
    req.subscribe({
      next: () => {
        this.ok.set('Unidad guardada');
        this.uForm = { codigo: '', nombre: '', simbolo: '', permiteDecimales: false, incrementoDefault: 1, orden: 0, activo: true };
        this.reloadUnidades();
      },
      error: (e) => this.error.set(e?.error?.error || 'No se pudo guardar'),
    });
  }

  editEmb(e: AdminProductEmbalaje): void {
    this.eForm = { ...e };
  }

  resetEmb(): void {
    this.eForm = {
      descripcion: '',
      cantidadPorEmbalaje: 1,
      codigoBarras: '',
      codigoUnidadMedida: 'UN',
      permiteFraccion: false,
      incrementoVenta: 1,
      cantidadMinimaVenta: 1,
      paraFacturacion: true,
      paraStock: true,
      activo: true,
    };
  }

  saveEmb(): void {
    const artId = this.idArticulo();
    if (!artId) return;
    const body = {
      descripcion: this.eForm.descripcion,
      cantidadPorEmbalaje: Number(this.eForm.cantidadPorEmbalaje) || 1,
      codigoBarras: this.eForm.codigoBarras || null,
      codigoUnidadMedida: this.eForm.codigoUnidadMedida || 'UN',
      permiteFraccion: !!this.eForm.permiteFraccion,
      incrementoVenta: Number(this.eForm.incrementoVenta) || 1,
      cantidadMinimaVenta: Number(this.eForm.cantidadMinimaVenta) || 1,
      paraFacturacion: this.eForm.paraFacturacion !== false,
      paraStock: this.eForm.paraStock !== false,
      activo: this.eForm.activo !== false,
      precioDemo: this.eForm.precioDemo ?? null,
    };
    const req = this.eForm.id
      ? this.api.updateProductEmbalaje(artId, this.eForm.id, body)
      : this.api.createProductEmbalaje(artId, body);
    req.subscribe({
      next: () => {
        this.ok.set('Embalaje guardado');
        this.resetEmb();
        this.reloadEmb();
      },
      error: (e) => this.error.set(e?.error?.error || 'No se pudo guardar embalaje'),
    });
  }

  openBarras(e: AdminProductEmbalaje): void {
    this.barcodeEmbId.set(e.id);
    this.nuevoCodigo = '';
    this.api.listCodigosBarra(e.id).subscribe({
      next: (rows) => this.codigos.set(rows),
      error: (err) => this.error.set(err?.error?.error || 'Error códigos'),
    });
  }

  addCodigo(): void {
    const embId = this.barcodeEmbId();
    const code = this.nuevoCodigo.trim();
    if (!embId || !code) return;
    this.api.createCodigoBarra(embId, { codigoBarra: code, activo: true }).subscribe({
      next: () => {
        this.nuevoCodigo = '';
        this.openBarras({ id: embId } as AdminProductEmbalaje);
        this.reloadEmb();
        this.ok.set('Código agregado');
      },
      error: (e) => this.error.set(e?.error?.error || 'No se pudo agregar'),
    });
  }

  removeCodigo(c: AdminCodigoBarra): void {
    this.api.deleteCodigoBarra(c.idArticuloEmbalaje, c.id).subscribe({
      next: () => {
        this.openBarras({ id: c.idArticuloEmbalaje } as AdminProductEmbalaje);
        this.reloadEmb();
      },
      error: (e) => this.error.set(e?.error?.error || 'No se pudo quitar'),
    });
  }
}
