import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import {
  AdminApiService,
  AdminArticuloDetail,
  AdminUnidadMedida,
  CatalogoParametro,
  CreateAdminArticuloBody,
} from '../core/admin-api.service';

@Component({
  standalone: true,
  imports: [FormsModule, RouterLink],
  template: `
    <div class="dx-toolbar dx-form-sheet">
      <div>
        <h1 class="dx-page-title">{{ isEdit() ? 'Editar artículo' : (stockLocked() ? 'Alta de artículo' : 'Nuevo artículo') }}</h1>
        <p class="dx-page-sub">
          @if (stockLocked()) {
            El maestro vive en el ERP. Acá: vitrina, SEO e imágenes.
          } @else {
            Paridad ERP · Productos e-commerce (alta con embalaje + código de barra).
          }
        </p>
      </div>
      <a routerLink="/catalogo" class="btn btn-outline-secondary btn-sm">
        <i class="fas fa-arrow-left mr-1"></i> Volver
      </a>
    </div>

    @if (error()) {
      <div class="alert alert-danger dx-form-sheet">{{ error() }}</div>
    }
    @if (ok()) {
      <div class="alert alert-success dx-form-sheet">{{ ok() }}</div>
    }

    @if (!isEdit() && stockLocked()) {
      <div class="card card-outline card-info dx-form-sheet">
        <div class="card-header"><h3 class="card-title">No se copia el artículo desde acá</h3></div>
        <div class="card-body">
          <p class="mb-2">
            En modo Integrated el alta es la misma que en el ERP:
          </p>
          <ol class="mb-3 pl-3">
            <li>Dás de alta el artículo en el <strong>ERP</strong> (Stock → Artículos, o Site → ECommerce → Productos).</li>
            <li><strong>ecom-etl</strong> lo trae con marca, línea, tipo, proveedor, embalaje, stock y precio.</li>
            <li>Acá solo lo <strong>publicás en vitrina</strong> (Activar / inactivar).</li>
          </ol>
          <p class="text-muted small mb-0">
            Reescribir la ficha acá duplica maestros y se pisa en el próximo sync.
          </p>
        </div>
        <div class="card-footer">
          <a routerLink="/catalogo/activar" class="btn btn-primary">
            <i class="fas fa-toggle-on mr-1"></i> Activar vitrina
          </a>
          <a routerLink="/sync" class="btn btn-outline-secondary ml-2">Estado ETL</a>
          <a routerLink="/catalogo" class="btn btn-outline-secondary ml-2">Catálogo</a>
        </div>
      </div>
    } @else {
    <form class="card card-outline card-primary dx-form-sheet" (ngSubmit)="save()">
      <div class="card-header"><h3 class="card-title">{{ masterLocked() ? 'Ficha ERP (solo lectura) + vitrina' : 'Datos del producto' }}</h3></div>
      <div class="card-body">
        @if (masterLocked()) {
          <div class="callout callout-info py-2">
            <small>
              SKU, marca y embalaje vienen del ERP. Editá slug, descripción extensa, imagen y vitrina.
            </small>
          </div>
        }
        <div class="row">
          <div class="col-md-3 form-group">
            <label>Nro. artículo</label>
            <input class="form-control" type="number" [(ngModel)]="form.nroArticulo" name="nro"
                   [disabled]="masterLocked()" />
          </div>
          <div class="col-md-3 form-group">
            <label>SKU *</label>
            <input class="form-control" required [(ngModel)]="form.sku" name="sku" maxlength="100"
                   [disabled]="masterLocked()" />
          </div>
          <div class="col-md-6 form-group">
            <label>Descripción *</label>
            <input class="form-control" required [(ngModel)]="form.descripcion" name="desc" maxlength="100"
                   [disabled]="masterLocked()" />
          </div>
          <div class="col-md-4 form-group">
            <label>Cód. ref. proveedor</label>
            <input class="form-control" [(ngModel)]="form.codReferenciaProveedor" name="cref" maxlength="50"
                   [disabled]="masterLocked()" />
          </div>
          <div class="col-md-4 form-group">
            <label>Marca *</label>
            <select class="custom-select" required [(ngModel)]="form.idMarca" name="marca"
                    [disabled]="masterLocked()">
              <option [ngValue]="0" disabled>Seleccionar…</option>
              @for (p of marcas(); track p.id) {
                <option [ngValue]="p.id">{{ p.descripcion }}</option>
              }
            </select>
          </div>
          <div class="col-md-4 form-group">
            <label>Línea *</label>
            <select class="custom-select" required [(ngModel)]="form.idLinea" name="linea"
                    [disabled]="masterLocked()">
              <option [ngValue]="0" disabled>Seleccionar…</option>
              @for (p of lineas(); track p.id) {
                <option [ngValue]="p.id">{{ p.descripcion }}</option>
              }
            </select>
          </div>
          <div class="col-md-4 form-group">
            <label>Tipo *</label>
            <select class="custom-select" required [(ngModel)]="form.idTipoArticulo" name="tipo"
                    [disabled]="masterLocked()">
              <option [ngValue]="0" disabled>Seleccionar…</option>
              @for (p of tipos(); track p.id) {
                <option [ngValue]="p.id">{{ p.descripcion }}</option>
              }
            </select>
          </div>
          <div class="col-md-4 form-group">
            <label>Proveedor *</label>
            <select class="custom-select" required [(ngModel)]="form.idProveedor" name="prov"
                    [disabled]="masterLocked()">
              <option [ngValue]="0" disabled>Seleccionar…</option>
              @for (p of proveedores(); track p.id) {
                <option [ngValue]="p.id">{{ p.descripcion }}</option>
              }
            </select>
          </div>
          <div class="col-md-4 form-group">
            <label>País</label>
            <select class="custom-select" [(ngModel)]="form.idPais" name="pais"
                    [disabled]="masterLocked()">
              <option [ngValue]="0">—</option>
              @for (p of paises(); track p.id) {
                <option [ngValue]="p.id">{{ p.descripcion }}</option>
              }
            </select>
          </div>
          <div class="col-md-4 form-group">
            <label>Régimen impositivo</label>
            <select class="custom-select" [(ngModel)]="form.idRegimenImpositivo" name="regimen"
                    [disabled]="masterLocked()">
              <option [ngValue]="0">—</option>
              @for (p of regimenes(); track p.id) {
                <option [ngValue]="p.id">{{ p.descripcion }}</option>
              }
            </select>
          </div>
          <div class="col-md-4 form-group">
            <label>Código de barra</label>
            <input class="form-control" [(ngModel)]="form.codigoBarra" name="barra"
                   [disabled]="masterLocked()" />
          </div>
          <div class="col-md-4 form-group">
            <label>Embalaje *</label>
            <select class="custom-select" required [(ngModel)]="form.idEmbalaje" name="emb"
                    [disabled]="masterLocked()">
              <option [ngValue]="0" disabled>Seleccionar…</option>
              @for (p of embalajes(); track p.id) {
                <option [ngValue]="p.id">{{ p.descripcion }}</option>
              }
            </select>
          </div>
          <div class="col-md-4 form-group">
            <label>Unidad de medida *</label>
            <select class="custom-select" required [(ngModel)]="form.codigoUnidadMedida" name="uom"
                    [disabled]="masterLocked()">
              @for (u of unidades(); track u.id) {
                <option [value]="u.codigo">{{ u.nombre }} ({{ u.codigo }})</option>
              }
            </select>
          </div>
          <div class="col-md-4 form-group">
            <label>Cantidad por embalaje *</label>
            <input class="form-control" type="number" min="1" required
                   [(ngModel)]="form.cantidadPorEmbalaje" name="cant"
                   [disabled]="masterLocked()" />
          </div>
          @if (!isEdit() && !stockLocked()) {
            <div class="col-md-4 form-group">
              <label>Stock inicial</label>
              <input class="form-control" type="number" min="0" [(ngModel)]="form.stockInicial" name="stock" />
            </div>
          }
          @if (!isEdit() && stockLocked()) {
            <div class="col-md-12">
              <div class="callout callout-info py-2">
                <small>
                  Modo Integrated: el stock inicial no se carga acá; lo trae ecom-etl al sincronizar el artículo.
                </small>
              </div>
            </div>
          }
          <div class="col-md-8 form-group">
            <label>URL imagen</label>
            <input class="form-control" [(ngModel)]="form.imagenUrl" name="img" placeholder="https://…" />
          </div>
          <div class="col-md-4 form-group">
            <label>Slug SEO</label>
            <input class="form-control" [(ngModel)]="form.slugSeo" name="slug" maxlength="180"
                   placeholder="auto si vacío" />
          </div>
          <div class="col-md-12 form-group">
            <label>Descripción extensa</label>
            <textarea class="form-control" rows="3" [(ngModel)]="form.descripcionExtensa" name="dext"></textarea>
          </div>
          @if (isEdit()) {
            <div class="col-md-3 form-group">
              <label>Estado ecom</label>
              <select class="custom-select" [(ngModel)]="form.estadoEcommerce" name="est">
                <option value="borrador">borrador</option>
                <option value="activo">activo</option>
                <option value="agotado">agotado</option>
                <option value="oculto">oculto</option>
                <option value="descontinuado">descontinuado</option>
              </select>
            </div>
            <div class="col-md-3 form-group pt-4">
              <div class="custom-control custom-switch">
                <input type="checkbox" class="custom-control-input" id="act"
                       [(ngModel)]="form.activo" name="activo" />
                <label class="custom-control-label" for="act">Activo</label>
              </div>
            </div>
            <div class="col-md-3 form-group pt-4">
              <div class="custom-control custom-switch">
                <input type="checkbox" class="custom-control-input" id="ecom"
                       [(ngModel)]="form.activoEcommerce" name="ecom" />
                <label class="custom-control-label" for="ecom">Vitrina (ActivoEcommerce)</label>
              </div>
            </div>
          }
        </div>
        <p class="text-muted small mb-0">
          @if (masterLocked()) {
            Marca, línea, tipo, proveedor y embalaje los mantiene el ERP. Acá: vitrina, slug e imágenes.
          } @else {
            Al crear, el artículo queda <strong>sin publicar</strong> (como en el ERP). Usá Activar / inactivar para la vitrina.
            Parámetros maestros:
            <a routerLink="/parametros-producto">Marcas, líneas, tipos…</a>
          }
        </p>
      </div>
      <div class="card-footer">
        <button type="submit" class="btn btn-primary" [disabled]="saving()">
          <i class="fas fa-save mr-1"></i> {{ isEdit() ? 'Guardar' : 'Crear artículo' }}
        </button>
        @if (isEdit() && !stockLocked()) {
          <button type="button" class="btn btn-outline-danger ml-2" (click)="remove()" [disabled]="saving()">
            <i class="fas fa-trash mr-1"></i> Eliminar
          </button>
        }
        @if (isEdit()) {
          <a class="btn btn-outline-primary ml-2" [routerLink]="['/catalogo', id(), 'imagenes']">Imágenes</a>
          <a class="btn btn-outline-secondary ml-2" [routerLink]="['/catalogo', id(), 'embalajes']">Embalajes</a>
          <a class="btn btn-outline-warning ml-2" routerLink="/catalogo/activar">Vitrina</a>
        }
      </div>
    </form>
    }
  `,
})
export class ProductFormPageComponent implements OnInit {
  private readonly api = inject(AdminApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly isEdit = signal(false);
  readonly id = signal(0);
  readonly error = signal<string | null>(null);
  readonly ok = signal<string | null>(null);
  readonly saving = signal(false);
  readonly stockLocked = signal(false);

  masterLocked(): boolean {
    return this.isEdit() && this.stockLocked();
  }

  readonly marcas = signal<CatalogoParametro[]>([]);
  readonly lineas = signal<CatalogoParametro[]>([]);
  readonly tipos = signal<CatalogoParametro[]>([]);
  readonly proveedores = signal<CatalogoParametro[]>([]);
  readonly embalajes = signal<CatalogoParametro[]>([]);
  readonly paises = signal<CatalogoParametro[]>([]);
  readonly regimenes = signal<CatalogoParametro[]>([]);
  readonly unidades = signal<AdminUnidadMedida[]>([]);

  form: CreateAdminArticuloBody & {
    descripcionAbreviada?: string | null;
    activo?: boolean;
    activoEcommerce?: boolean;
    estadoEcommerce?: string;
  } = {
    sku: '',
    descripcion: '',
    codReferenciaProveedor: '',
    idMarca: 0,
    idLinea: 0,
    idTipoArticulo: 0,
    idProveedor: 0,
    idEmbalaje: 0,
    idPais: 0,
    idRegimenImpositivo: 0,
    codigoUnidadMedida: 'UN',
    cantidadPorEmbalaje: 1,
    nroArticulo: null,
    codigoBarra: '',
    imagenUrl: '',
    descripcionExtensa: '',
    slugSeo: '',
    stockInicial: null,
    activo: true,
    activoEcommerce: false,
    estadoEcommerce: 'borrador',
  };

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    const editId = idParam && idParam !== 'nuevo' ? Number(idParam) : 0;
    this.isEdit.set(editId > 0);
    this.id.set(editId);

    this.api.getEmpresaConfig().subscribe({
      next: (c) => {
        this.stockLocked.set(c?.modoIntegracion === 'Integrated');
        if (this.stockLocked()) this.form.stockInicial = null;
      },
      error: () => undefined,
    });

    forkJoin({
      marcas: this.api.listParametrosProducto('MARCA'),
      lineas: this.api.listParametrosProducto('LINEA'),
      tipos: this.api.listParametrosProducto('TIPO'),
      proveedores: this.api.listParametrosProducto('PROVEEDOR'),
      embalajes: this.api.listParametrosProducto('EMBALAJE'),
      paises: this.api.listParametrosProducto('PAIS'),
      regimenes: this.api.listParametrosProducto('REGIMEN'),
      unidades: this.api.listUnidadesMedida(),
    }).subscribe({
      next: (r) => {
        this.marcas.set(r.marcas);
        this.lineas.set(r.lineas);
        this.tipos.set(r.tipos);
        this.proveedores.set(r.proveedores);
        this.embalajes.set(r.embalajes);
        this.paises.set(r.paises);
        this.regimenes.set(r.regimenes);
        this.unidades.set(r.unidades.filter((u) => u.activo));
        if (!this.isEdit()) {
          this.form.idMarca = r.marcas[0]?.id ?? 0;
          this.form.idLinea = r.lineas[0]?.id ?? 0;
          this.form.idTipoArticulo = r.tipos[0]?.id ?? 0;
          this.form.idProveedor = r.proveedores[0]?.id ?? 0;
          this.form.idEmbalaje = r.embalajes[0]?.id ?? 0;
          this.form.idPais = r.paises[0]?.id ?? 0;
          this.form.idRegimenImpositivo = r.regimenes[0]?.id ?? 0;
          this.form.codigoUnidadMedida = r.unidades.find((u) => u.activo)?.codigo ?? 'UN';
        } else {
          this.loadDetail(editId);
        }
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  private loadDetail(id: number): void {
    this.api.productDetail(id).subscribe({
      next: (d) => this.applyDetail(d),
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  private applyDetail(d: AdminArticuloDetail): void {
    this.form = {
      sku: d.sku ?? '',
      descripcion: d.descripcion,
      codReferenciaProveedor: d.codReferenciaProveedor,
      idMarca: d.idMarca,
      idLinea: d.idLinea ?? 0,
      idTipoArticulo: d.idTipoArticulo ?? 0,
      idProveedor: d.idProveedor ?? 0,
      idEmbalaje: d.idEmbalajeParametro ?? 0,
      idPais: d.idPais ?? 0,
      idRegimenImpositivo: d.idRegimenImpositivo ?? 0,
      codigoUnidadMedida: d.codigoUnidadMedida || 'UN',
      cantidadPorEmbalaje: d.cantidadPorEmbalaje || 1,
      nroArticulo: d.nroArticulo,
      codigoBarra: d.codigoBarra,
      imagenUrl: d.imagenUrl,
      descripcionExtensa: d.descripcionExtensa,
      slugSeo: d.slugSeo ?? '',
      descripcionAbreviada: d.descripcionAbreviada,
      activo: d.activo,
      activoEcommerce: d.activoEcommerce,
      estadoEcommerce: d.estadoEcommerce,
    };
  }

  save(): void {
    this.error.set(null);
    this.ok.set(null);
    if (!this.isEdit() && this.stockLocked()) {
      void this.router.navigate(['/catalogo/activar']);
      return;
    }
    if (!this.form.sku?.trim() || !this.form.descripcion?.trim()) {
      this.error.set('SKU y descripción son obligatorios.');
      return;
    }
    if (!this.form.idMarca || !this.form.idLinea || !this.form.idTipoArticulo
        || !this.form.idProveedor || !this.form.idEmbalaje) {
      this.error.set('Completá marca, línea, tipo, proveedor y embalaje (Parámetros de producto).');
      return;
    }

    this.saving.set(true);
    const body = { ...this.form };
    const req = this.isEdit()
      ? this.api.upsertProductMaster(this.id(), body)
      : this.api.createProduct(body);

    req.subscribe({
      next: (d) => {
        this.saving.set(false);
        this.ok.set(this.isEdit() ? 'Artículo actualizado' : 'Artículo creado (sin publicar en vitrina)');
        if (!this.isEdit()) {
          void this.router.navigate(['/catalogo', d.id, 'editar']);
        } else {
          this.applyDetail(d);
        }
      },
      error: (e) => {
        this.saving.set(false);
        this.error.set(e?.error?.error || e.message);
      },
    });
  }

  remove(): void {
    if (!confirm('¿Eliminar (inactivar) este artículo?')) return;
    this.saving.set(true);
    this.api.deleteProduct(this.id()).subscribe({
      next: () => {
        this.saving.set(false);
        void this.router.navigate(['/catalogo']);
      },
      error: (e) => {
        this.saving.set(false);
        this.error.set(e?.error?.error || e.message);
      },
    });
  }
}
