import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AdminApiService, InventarioMovimientoItem } from '../core/admin-api.service';

@Component({
  standalone: true,
  imports: [DatePipe, RouterLink],
  template: `
    <div class="dx-toolbar">
      <div>
        <h1 class="dx-page-title">Movimientos de stock</h1>
        <p class="dx-page-sub">Historial de ajustes y pedidos (artículo #{{ articuloId }}).</p>
      </div>
      <a class="btn btn-outline-secondary btn-sm" [routerLink]="['/catalogo']">Volver al catálogo</a>
    </div>

    @if (error()) {
      <div class="alert alert-danger">{{ error() }}</div>
    }

    <div class="card">
      <div class="card-body table-responsive p-0">
        <table class="table table-sm table-hover mb-0">
          <thead>
            <tr>
              <th>Fecha</th>
              <th>Tipo</th>
              <th>Cantidad</th>
              <th>Antes</th>
              <th>Después</th>
              <th>Referencia</th>
              <th>Usuario</th>
            </tr>
          </thead>
          <tbody>
            @for (m of rows(); track m.id) {
              <tr>
                <td>{{ m.fecha | date: 'short' }}</td>
                <td><code>{{ m.tipoMovimiento }}</code></td>
                <td>{{ m.cantidad }}</td>
                <td>{{ m.existenciaAntes }}</td>
                <td>{{ m.existenciaDespues }}</td>
                <td>{{ m.referencia || '—' }}</td>
                <td>{{ m.userName || '—' }}</td>
              </tr>
            } @empty {
              <tr>
                <td colspan="7" class="text-center text-muted py-4">
                  {{ loading() ? 'Cargando…' : 'Sin movimientos registrados.' }}
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    </div>
  `,
})
export class InventoryMovimientosPageComponent implements OnInit {
  private readonly api = inject(AdminApiService);
  private readonly route = inject(ActivatedRoute);

  articuloId = 0;
  readonly rows = signal<InventarioMovimientoItem[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.articuloId = Number(this.route.snapshot.paramMap.get('id') || 0);
    if (!this.articuloId) {
      this.error.set('Artículo inválido');
      return;
    }
    this.loading.set(true);
    this.api.listInventarioMovimientos(this.articuloId, 200).subscribe({
      next: (rows) => {
        this.rows.set(rows);
        this.loading.set(false);
      },
      error: (e) => {
        this.loading.set(false);
        this.error.set(e?.error?.error || e.message);
      },
    });
  }
}
