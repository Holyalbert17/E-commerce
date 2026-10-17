import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiService, ProductListItem, SearchDoc, SearchSuggestResult } from './api.service';

const IDB_NAME = 'casa-imperial-ecom-search-v2';
const IDB_STORE = 'snapshots';
const RAM = new Map<string, ProductListItem[]>();

/**
 * Cadena cliente: RAM Map → IndexedDB → API (/api/search) → guarda en caches.
 * Prefetch silencioso del snapshot al entrar a la tienda.
 */
@Injectable({ providedIn: 'root' })
export class FastSearchService {
  private readonly api = inject(ApiService);
  private worker: Worker | null = null;
  private workerReady = false;
  private pending = new Map<number, (items: ProductListItem[]) => void>();
  private reqId = 0;
  private tenantKey = 'default';
  private prefetching: Promise<void> | null = null;

  setTenant(slug: string | null | undefined): void {
    const next = (slug || 'default').trim() || 'default';
    if (next !== this.tenantKey) {
      this.tenantKey = next;
      RAM.clear();
      this.workerReady = false;
    }
  }

  /** Prefetch top N productos a IndexedDB + worker (no bloquea UI). */
  prefetch(canal = 'B2C'): Promise<void> {
    if (this.prefetching) return this.prefetching;
    this.prefetching = (async () => {
      try {
        const cached = await this.idbGet(this.tenantKey);
        if (cached?.docs?.length) {
          this.ensureWorker(cached.docs);
          return;
        }
        const snap = await firstValueFrom(this.api.searchSnapshot(canal, 2000));
        const docs = snap.docs ?? [];
        await this.idbPut(this.tenantKey, {
          generatedUtc: snap.generatedUtc,
          docs,
        });
        this.ensureWorker(docs);
      } catch {
        /* offline / API down — autocomplete cae a API puntual */
      } finally {
        this.prefetching = null;
      }
    })();
    return this.prefetching;
  }

  /**
   * Suggest local-first. Tipado incremental reutiliza RAM si el prefijo ya se buscó.
   */
  async suggest(raw: string, take = 8, canal = 'B2C'): Promise<{
    items: ProductListItem[];
    source: string;
    tookMs: number;
  }> {
    const q = raw.trim();
    if (q.length < 2) return { items: [], source: 'none', tookMs: 0 };
    const t0 = performance.now();

    // Nivel 1: RAM — filtrar resultado de un query más corto ya cacheado
    const ramHit = this.filterFromRam(q, take);
    if (ramHit) {
      return { items: ramHit, source: 'memory', tookMs: Math.round(performance.now() - t0) };
    }

    // Nivel 2: Web Worker + IndexedDB snapshot
    await this.prefetch(canal);
    if (this.workerReady && this.worker) {
      const items = await this.queryWorker(q, Math.max(take, 24));
      if (items.length) {
        this.remember(q, items);
        return {
          items: items.slice(0, take),
          source: 'indexeddb-worker',
          tookMs: Math.round(performance.now() - t0),
        };
      }
    }

    // Nivel 3: API (Redis/índice local/OpenSearch en servidor)
    try {
      const res = await firstValueFrom(this.api.searchSuggest(q, canal, take));
      const items = (res.items ?? []).map(searchDocToProduct);
      this.remember(q, items);
      return {
        items,
        source: res.source || 'api',
        tookMs: res.tookMs ?? Math.round(performance.now() - t0),
      };
    } catch {
      return { items: [], source: 'error', tookMs: Math.round(performance.now() - t0) };
    }
  }

  applyPatch(patch: { id: number; stockLibre?: number | null; stockOk?: boolean; precioDesde?: number | null }): void {
    this.worker?.postMessage({ type: 'patch', patch });
    for (const [k, list] of [...RAM.entries()]) {
      const i = list.findIndex((p) => p.id === patch.id);
      if (i < 0) continue;
      list[i] = {
        ...list[i],
        stockLibre: patch.stockLibre ?? list[i].stockLibre,
        stockOk: patch.stockOk ?? list[i].stockOk,
        precioDesde: patch.precioDesde ?? list[i].precioDesde,
      };
      RAM.set(k, list);
    }
  }

  private filterFromRam(q: string, take: number): ProductListItem[] | null {
    const lower = q.toLowerCase();
    // Reusar cache de un prefijo más corto (ej. "lap" → "lapt")
    let bestKey: string | null = null;
    for (const key of RAM.keys()) {
      if (lower.startsWith(key) && (!bestKey || key.length > bestKey.length)) {
        bestKey = key;
      }
    }
    if (!bestKey) {
      const exact = RAM.get(lower);
      return exact ? exact.slice(0, take) : null;
    }
    const base = RAM.get(bestKey)!;
    const filtered = base.filter((p) => matches(p, lower));
    if (filtered.length || bestKey === lower) {
      this.remember(q, filtered.length ? filtered : base);
      return (filtered.length ? filtered : base).slice(0, take);
    }
    return null;
  }

  private remember(q: string, items: ProductListItem[]): void {
    const key = q.trim().toLowerCase();
    if (key.length < 2) return;
    RAM.set(key, items);
    // limitar tamaño
    if (RAM.size > 80) {
      const first = RAM.keys().next().value;
      if (first) RAM.delete(first);
    }
  }

  private ensureWorker(docs: SearchDoc[]): void {
    if (typeof Worker === 'undefined') return;
    if (!this.worker) {
      this.worker = new Worker(new URL('./search.worker', import.meta.url), { type: 'module' });
      this.worker.onmessage = (ev: MessageEvent) => {
        const msg = ev.data;
        if (msg?.type === 'ready') {
          this.workerReady = true;
          return;
        }
        if (msg?.type === 'results') {
          const resolve = this.pending.get(msg.id);
          if (resolve) {
            this.pending.delete(msg.id);
            resolve((msg.items ?? []).map(searchDocToProduct));
          }
        }
      };
    }
    this.worker.postMessage({ type: 'setDocs', docs });
    this.workerReady = docs.length > 0;
  }

  private queryWorker(q: string, take: number): Promise<ProductListItem[]> {
    return new Promise((resolve) => {
      if (!this.worker) {
        resolve([]);
        return;
      }
      const id = ++this.reqId;
      this.pending.set(id, resolve);
      this.worker.postMessage({ type: 'query', id, q, take });
      setTimeout(() => {
        if (this.pending.has(id)) {
          this.pending.delete(id);
          resolve([]);
        }
      }, 800);
    });
  }

  private idbGet(key: string): Promise<{ generatedUtc: string; docs: SearchDoc[] } | null> {
    return new Promise((resolve) => {
      if (typeof indexedDB === 'undefined') {
        resolve(null);
        return;
      }
      const req = indexedDB.open(IDB_NAME, 1);
      req.onupgradeneeded = () => {
        const db = req.result;
        if (!db.objectStoreNames.contains(IDB_STORE)) db.createObjectStore(IDB_STORE);
      };
      req.onerror = () => resolve(null);
      req.onsuccess = () => {
        const db = req.result;
        try {
          const tx = db.transaction(IDB_STORE, 'readonly');
          const g = tx.objectStore(IDB_STORE).get(key);
          g.onsuccess = () => resolve(g.result ?? null);
          g.onerror = () => resolve(null);
        } catch {
          resolve(null);
        }
      };
    });
  }

  private idbPut(key: string, value: { generatedUtc: string; docs: SearchDoc[] }): Promise<void> {
    return new Promise((resolve) => {
      if (typeof indexedDB === 'undefined') {
        resolve();
        return;
      }
      const req = indexedDB.open(IDB_NAME, 1);
      req.onupgradeneeded = () => {
        const db = req.result;
        if (!db.objectStoreNames.contains(IDB_STORE)) db.createObjectStore(IDB_STORE);
      };
      req.onerror = () => resolve();
      req.onsuccess = () => {
        const db = req.result;
        try {
          const tx = db.transaction(IDB_STORE, 'readwrite');
          tx.objectStore(IDB_STORE).put(value, key);
          tx.oncomplete = () => resolve();
          tx.onerror = () => resolve();
        } catch {
          resolve();
        }
      };
    });
  }
}

function matches(p: ProductListItem, q: string): boolean {
  const blob = `${p.nombre} ${p.nombreCorto ?? ''} ${p.sku ?? ''} ${p.marca ?? ''}`.toLowerCase();
  return blob.includes(q);
}

export function searchDocToProduct(d: SearchDoc): ProductListItem {
  return {
    id: d.id,
    sku: d.sku ?? null,
    nombre: d.nombre,
    nombreCorto: d.nombreCorto ?? d.nombre,
    marca: d.marca ?? null,
    estadoEcommerce: d.estadoEcommerce ?? 'activo',
    imagenPrincipalUrl: d.imagenPrincipalUrl ?? null,
    precioDesde: d.precioDesde ?? null,
    embalajesCount: 0,
    canal: 'B2C',
    listaPrecio: null,
    stockLibre: d.stockLibre ?? null,
    stockOk: d.stockOk ?? true,
    slugSeo: d.slugSeo ?? null,
  };
}

export type { SearchSuggestResult };
