import { Injectable } from '@angular/core';
import { getTenantSlug } from './tenant.interceptor';
import { SeguidosItem } from './api.service';

export type SeguidosAlertKind = 'back_in_stock' | 'price_down' | 'price_up';

export interface SeguidosAlert {
  kind: SeguidosAlertKind;
  label: string;
}

interface Snapshot {
  precioDesde: number | null;
  stockOk: boolean;
  stockLibre: number | null;
}

type SnapshotMap = Record<string, Snapshot>;

const KEY = 'ecom_seguidos_base_v1';

@Injectable({ providedIn: 'root' })
export class SeguidosAlertsService {
  private storageKey(): string {
    const tenant = getTenantSlug() || 'default';
    const uid = localStorage.getItem('ecom_user_id') || 'anon';
    return `${KEY}:${tenant}:${uid}`;
  }

  private read(): SnapshotMap {
    try {
      const raw = JSON.parse(localStorage.getItem(this.storageKey()) || '{}');
      return raw && typeof raw === 'object' ? (raw as SnapshotMap) : {};
    } catch {
      return {};
    }
  }

  private write(map: SnapshotMap): void {
    try {
      localStorage.setItem(this.storageKey(), JSON.stringify(map));
    } catch {
      /* ignore */
    }
  }

  private snapOf(it: SeguidosItem): Snapshot {
    return {
      precioDesde: it.precioDesde ?? null,
      stockOk: it.stockOk !== false,
      stockLibre: it.stockLibre ?? null,
    };
  }

  private diff(base: Snapshot, cur: Snapshot): SeguidosAlert[] {
    const alerts: SeguidosAlert[] = [];
    if (base.stockOk === false && cur.stockOk) {
      alerts.push({ kind: 'back_in_stock', label: 'Volvió a stock' });
    }
    if (base.precioDesde != null && cur.precioDesde != null && cur.precioDesde < base.precioDesde) {
      alerts.push({ kind: 'price_down', label: 'Bajó de precio' });
    } else if (base.precioDesde != null && cur.precioDesde != null && cur.precioDesde > base.precioDesde) {
      alerts.push({ kind: 'price_up', label: 'Subió de precio' });
    }
    return alerts;
  }

  /**
   * Calcula alertas vs baseline. Productos nuevos (sin baseline) se registran
   * en silencio para la próxima comparación.
   */
  evaluate(items: SeguidosItem[]): Map<number, SeguidosAlert[]> {
    const base = this.read();
    let dirty = false;
    const out = new Map<number, SeguidosAlert[]>();
    const keep = new Set(items.map((i) => String(i.idArticulo)));

    for (const it of items) {
      const key = String(it.idArticulo);
      const cur = this.snapOf(it);
      const prev = base[key];
      if (!prev) {
        base[key] = cur;
        dirty = true;
        continue;
      }
      const alerts = this.diff(prev, cur);
      if (alerts.length) out.set(it.idArticulo, alerts);
    }

    for (const k of Object.keys(base)) {
      if (!keep.has(k)) {
        delete base[k];
        dirty = true;
      }
    }
    if (dirty) this.write(base);
    return out;
  }

  /** Actualiza baseline a valores actuales (alerta “vista”). */
  dismiss(items: SeguidosItem[], onlyIds?: number[]): void {
    const base = this.read();
    const filter = onlyIds ? new Set(onlyIds) : null;
    for (const it of items) {
      if (filter && !filter.has(it.idArticulo)) continue;
      base[String(it.idArticulo)] = this.snapOf(it);
    }
    this.write(base);
  }
}
