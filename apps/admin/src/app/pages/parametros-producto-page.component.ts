import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AdminApiService, CatalogoParametro } from '../core/admin-api.service';

const TIPOS = [
  { code: 'MARCA', label: 'Marcas' },
  { code: 'LINEA', label: 'Líneas' },
  { code: 'TIPO', label: 'Tipos' },
  { code: 'EMBALAJE', label: 'Embalajes' },
  { code: 'PROVEEDOR', label: 'Proveedores' },
  { code: 'PAIS', label: 'Países' },
  { code: 'REGIMEN', label: 'Régimen impositivo' },
] as const;

@Component({
  standalone: true,
  imports: [FormsModule, RouterLink],
  template: `
    <div class="dx-toolbar">
      <div>
        <h1 class="dx-page-title">Parámetros de producto</h1>
        <p class="dx-page-sub">Paridad ERP · Marca, línea, tipo, embalaje y proveedor.</p>
      </div>
      <a routerLink="/catalogo" class="btn btn-outline-secondary btn-sm">Catálogo</a>
    </div>

    @if (error()) {
      <div class="alert alert-danger">{{ error() }}</div>
    }
    @if (ok()) {
      <div class="alert alert-success">{{ ok() }}</div>
    }

    <ul class="nav nav-tabs mb-3">
      @for (t of tipos; track t.code) {
        <li class="nav-item">
          <a class="nav-link" href="#"
             [class.active]="tipo() === t.code"
             (click)="selectTipo($event, t.code)">{{ t.label }}</a>
        </li>
      }
    </ul>

    <div class="card card-outline card-info mb-3">
      <div class="card-header"><h3 class="card-title">{{ editingId() ? 'Editar' : 'Nuevo' }}</h3></div>
      <div class="card-body">
        <div class="form-row align-items-end">
          <div class="col-md-2 form-group mb-0">
            <label>Código</label>
            <input class="form-control" type="number" [(ngModel)]="codigo" name="cod" />
          </div>
          <div class="col-md-6 form-group mb-0">
            <label>Descripción *</label>
            <input class="form-control" [(ngModel)]="descripcion" name="desc" maxlength="100" />
          </div>
          <div class="col-md-4">
            <button type="button" class="btn btn-primary" (click)="save()">
              <i class="fas fa-save mr-1"></i> {{ editingId() ? 'Guardar' : 'Agregar' }}
            </button>
            @if (editingId()) {
              <button type="button" class="btn btn-default ml-2" (click)="cancelEdit()">Cancelar</button>
            }
          </div>
        </div>
      </div>
    </div>

    <div class="card card-outline card-primary">
      <div class="card-header param-list-head">
        <span class="param-list-title">Listado</span>
        <div class="custom-control custom-switch mb-0">
          <input type="checkbox" class="custom-control-input" id="ina"
                 [ngModel]="incluirInactivos()" (ngModelChange)="toggleInactivos($event)" />
          <label class="custom-control-label" for="ina">Incluir inactivos</label>
        </div>
      </div>
      <div class="card-body p-0 table-responsive">
        <table class="table table-hover mb-0">
          <thead>
            <tr>
              <th style="width:90px">Código</th>
              <th>Descripción</th>
              <th style="width:90px">Activo</th>
              <th style="width:120px"></th>
            </tr>
          </thead>
          <tbody>
            @for (p of items(); track p.id) {
              <tr [class.text-muted]="!p.activo">
                <td>{{ p.codigo }}</td>
                <td>{{ p.descripcion }}</td>
                <td>{{ p.activo ? 'Sí' : 'No' }}</td>
                <td>
                  <button type="button" class="btn btn-xs btn-outline-primary" (click)="edit(p)">
                    <i class="fas fa-edit"></i>
                  </button>
                  @if (p.activo) {
                    <button type="button" class="btn btn-xs btn-outline-danger ml-1" (click)="remove(p)">
                      <i class="fas fa-trash"></i>
                    </button>
                  }
                </td>
              </tr>
            } @empty {
              <tr><td colspan="4" class="text-center text-muted py-4">Sin parámetros. Agregá el primero.</td></tr>
            }
          </tbody>
        </table>
      </div>
    </div>
  `,
  styles: [
    `
      .param-list-head {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 1rem;
        flex-wrap: wrap;
      }
      .param-list-title {
        font-size: 1.1rem;
        font-weight: 700;
        margin: 0;
      }
      .param-list-head .custom-control {
        padding-left: 2.25rem;
        min-width: 9.5rem;
      }
    `,
  ],
})
export class ParametrosProductoPageComponent implements OnInit {
  private readonly api = inject(AdminApiService);

  readonly tipos = TIPOS;
  readonly tipo = signal<string>('MARCA');
  readonly items = signal<CatalogoParametro[]>([]);
  readonly error = signal<string | null>(null);
  readonly ok = signal<string | null>(null);
  readonly editingId = signal<number | null>(null);
  readonly incluirInactivos = signal(false);

  codigo: number | null = null;
  descripcion = '';

  ngOnInit(): void {
    this.load();
  }

  selectTipo(ev: Event, code: string): void {
    ev.preventDefault();
    this.tipo.set(code);
    this.cancelEdit();
    this.load();
  }

  toggleInactivos(v: boolean): void {
    this.incluirInactivos.set(v);
    this.load();
  }

  load(): void {
    this.error.set(null);
    this.api.listParametrosProducto(this.tipo(), this.incluirInactivos()).subscribe({
      next: (r) => this.items.set(r ?? []),
      error: (e) => this.error.set(this.errMsg(e)),
    });
  }

  private errMsg(e: any): string {
    const body = e?.error;
    if (typeof body === 'string' && body.trim()) return body;
    if (body?.error) return String(body.error);
    if (body?.detail) return String(body.detail);
    if (body?.title) return String(body.title);
    if (e?.status === 503) return 'Base de datos no disponible temporalmente. Reintentá.';
    if (e?.status === 0) return 'Sin conexión con la API. Verificá que esté en :5082.';
    return e?.message || 'Error al cargar parámetros.';
  }

  edit(p: CatalogoParametro): void {
    this.editingId.set(p.id);
    this.codigo = p.codigo;
    this.descripcion = p.descripcion;
  }

  cancelEdit(): void {
    this.editingId.set(null);
    this.codigo = null;
    this.descripcion = '';
  }

  save(): void {
    this.error.set(null);
    this.ok.set(null);
    if (!this.descripcion.trim()) {
      this.error.set('Descripción requerida.');
      return;
    }
    const body = {
      tipo: this.tipo(),
      descripcion: this.descripcion.trim(),
      codigo: this.codigo ?? undefined,
      activo: true,
    };
    const id = this.editingId();
    const req = id
      ? this.api.updateParametroProducto(id, body)
      : this.api.createParametroProducto(body);

    req.subscribe({
      next: () => {
        this.ok.set('Guardado');
        this.cancelEdit();
        this.load();
      },
      error: (e) => this.error.set(this.errMsg(e)),
    });
  }

  remove(p: CatalogoParametro): void {
    if (!confirm(`¿Inactivar "${p.descripcion}"?`)) return;
    this.api.deleteParametroProducto(p.id).subscribe({
      next: () => {
        this.ok.set('Inactivado');
        this.load();
      },
      error: (e) => this.error.set(this.errMsg(e)),
    });
  }
}
