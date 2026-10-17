import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminApiService, AdminChofer } from '../core/admin-api.service';

@Component({
  standalone: true,
  imports: [FormsModule],
  template: `
    <div class="dx-toolbar">
      <div>
        <h1 class="dx-page-title">Choferes</h1>
        <p class="dx-page-sub">Maestro de choferes para asignar entregas (paridad ERP · Distribución / IDChofer).</p>
      </div>
      <button type="button" class="btn btn-primary btn-sm" (click)="startCreate()">
        <i class="fas fa-plus mr-1"></i> Nuevo chofer
      </button>
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

    <div class="row">
      <div class="col-lg-4">
        <div class="card card-primary card-outline">
          <div class="card-header">
            <h3 class="card-title">{{ editId ? 'Editar chofer #' + editId : 'Alta de chofer' }}</h3>
          </div>
          <div class="card-body">
            <div class="form-group">
              <label>Nro chofer <span class="text-danger">*</span></label>
              <input class="form-control" type="number" [(ngModel)]="form.nroChofer" name="nro" min="1" />
            </div>
            <div class="form-group">
              <label>Nombre <span class="text-danger">*</span></label>
              <input class="form-control" [(ngModel)]="form.nombre" name="nom" placeholder="Apellido, Nombre" />
            </div>
            <div class="form-group">
              <label>Documento</label>
              <input class="form-control" [(ngModel)]="form.documento" name="doc" placeholder="CI / RUC" />
            </div>
            <div class="form-group">
              <label>Teléfono</label>
              <input class="form-control" [(ngModel)]="form.telefono" name="tel" placeholder="0981…" />
            </div>
            <div class="form-group">
              <label>Observación</label>
              <textarea class="form-control" rows="2" [(ngModel)]="form.observacion" name="obs"></textarea>
            </div>
            <div class="custom-control custom-switch mb-0">
              <input type="checkbox" class="custom-control-input" id="ch-activo" [(ngModel)]="form.activo" name="act" />
              <label class="custom-control-label" for="ch-activo">Activo (disponible para asignar)</label>
            </div>
          </div>
          <div class="card-footer dx-form-actions">
            <button type="button" class="btn btn-primary" (click)="save()" [disabled]="busy()">
              <i class="fas fa-save mr-1"></i> Guardar
            </button>
            @if (editId) {
              <button type="button" class="btn btn-default" (click)="reset()">Cancelar</button>
            }
          </div>
        </div>
      </div>

      <div class="col-lg-8">
        <div class="card">
          <div class="card-header">
            <div class="dx-filters w-100">
              <input
                class="form-control form-control-sm"
                [(ngModel)]="q"
                name="q"
                placeholder="Buscar nombre, doc, teléfono…"
                style="flex:1;min-width:180px"
              />
              <select class="custom-select custom-select-sm" [(ngModel)]="filtroActivo" name="fa" style="max-width:150px">
                <option value="all">Todos</option>
                <option value="1">Solo activos</option>
                <option value="0">Solo inactivos</option>
              </select>
              <span class="text-muted small ml-auto">{{ filtrados().length }} / {{ rows().length }}</span>
            </div>
          </div>
          <div class="card-body table-responsive p-0">
            <table class="table table-hover table-striped mb-0">
              <thead>
                <tr>
                  <th style="width:70px">Nro</th>
                  <th>Nombre</th>
                  <th>Documento</th>
                  <th>Teléfono</th>
                  <th style="width:90px">Estado</th>
                  <th style="width:100px"></th>
                </tr>
              </thead>
              <tbody>
                @for (c of filtrados(); track c.id) {
                  <tr [class.table-secondary]="!c.activo">
                    <td><strong>{{ c.nroChofer }}</strong></td>
                    <td>
                      {{ c.nombre }}
                      @if (c.observacion) {
                        <small>{{ c.observacion }}</small>
                      }
                    </td>
                    <td>{{ c.documento || '—' }}</td>
                    <td>{{ c.telefono || '—' }}</td>
                    <td>
                      @if (c.activo) {
                        <span class="badge badge-soft-success">Activo</span>
                      } @else {
                        <span class="badge badge-soft-danger">Inactivo</span>
                      }
                    </td>
                    <td class="table-actions text-nowrap">
                      <button type="button" class="btn btn-xs btn-info" (click)="edit(c)" title="Editar">
                        <i class="fas fa-edit"></i>
                      </button>
                      @if (c.activo) {
                        <button type="button" class="btn btn-xs btn-outline-danger" (click)="remove(c)" title="Desactivar">
                          <i class="fas fa-ban"></i>
                        </button>
                      }
                    </td>
                  </tr>
                } @empty {
                  <tr>
                    <td colspan="6">
                      <div class="dx-empty">
                        <i class="fas fa-user-tie"></i>
                        <p>No hay choferes con ese filtro.</p>
                        <button type="button" class="btn btn-sm btn-primary" (click)="startCreate()">Crear primero</button>
                      </div>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  `,
})
export class ChoferesAdminPageComponent implements OnInit {
  private readonly api = inject(AdminApiService);
  readonly rows = signal<AdminChofer[]>([]);
  readonly error = signal<string | null>(null);
  readonly ok = signal<string | null>(null);
  readonly busy = signal(false);
  editId: number | null = null;
  form = this.empty();
  q = '';
  filtroActivo: 'all' | '1' | '0' = 'all';

  readonly filtrados = computed(() => {
    const q = this.q.trim().toLowerCase();
    const fa = this.filtroActivo;
    return this.rows().filter((c) => {
      if (fa === '1' && !c.activo) return false;
      if (fa === '0' && c.activo) return false;
      if (!q) return true;
      const blob = `${c.nroChofer} ${c.nombre} ${c.documento || ''} ${c.telefono || ''} ${c.observacion || ''}`.toLowerCase();
      return blob.includes(q);
    });
  });

  ngOnInit(): void {
    this.reload();
  }

  startCreate(): void {
    this.reset();
  }

  edit(c: AdminChofer): void {
    this.editId = c.id;
    this.form = {
      nroChofer: c.nroChofer,
      nombre: c.nombre,
      documento: c.documento || '',
      telefono: c.telefono || '',
      observacion: c.observacion || '',
      activo: c.activo,
    };
  }

  reset(): void {
    this.editId = null;
    this.form = this.empty();
  }

  save(): void {
    if (!this.form.nombre.trim()) {
      this.error.set('Nombre requerido.');
      return;
    }
    if (!this.form.nroChofer || this.form.nroChofer < 1) {
      this.error.set('Nro de chofer debe ser mayor a 0.');
      return;
    }
    this.busy.set(true);
    this.error.set(null);
    const body = {
      nombre: this.form.nombre.trim(),
      nroChofer: Number(this.form.nroChofer) || 0,
      documento: this.form.documento || null,
      telefono: this.form.telefono || null,
      observacion: this.form.observacion || null,
      activo: this.form.activo,
    };
    const req = this.editId ? this.api.updateChofer(this.editId, body) : this.api.createChofer(body);
    req.subscribe({
      next: () => {
        this.busy.set(false);
        this.ok.set(this.editId ? 'Chofer actualizado' : 'Chofer creado');
        this.reset();
        this.reload();
      },
      error: (e) => {
        this.busy.set(false);
        this.error.set(e?.error?.error || e.message);
      },
    });
  }

  remove(c: AdminChofer): void {
    if (!confirm(`¿Desactivar al chofer "${c.nombre}" (#${c.nroChofer})?`)) return;
    this.api.deleteChofer(c.id).subscribe({
      next: () => {
        this.ok.set('Chofer desactivado');
        this.reload();
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  private empty() {
    return { nroChofer: 0, nombre: '', documento: '', telefono: '', observacion: '', activo: true };
  }

  private reload(): void {
    this.api.listChoferes().subscribe({
      next: (rows) => this.rows.set(rows),
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }
}
