/**
 * Web Worker: filtra snapshot de catálogo en hilo aparte (no bloquea UI).
 * Mensajes:
 *   { type:'setDocs', docs: SearchDoc[] }
 *   { type:'patch', patch: { id, stockLibre?, stockOk?, precioDesde? } }
 *   { type:'query', id, q, take }
 * Respuesta:
 *   { type:'ready', count }
 *   { type:'results', id, items, tookMs, source:'worker' }
 */
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

let docs: SearchDoc[] = [];

function normalize(s: string | null | undefined): string {
  if (!s) return '';
  return s
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .trim()
    .replace(/\s+/g, ' ');
}

function score(d: SearchDoc, norm: string, tokens: string[]): number {
  const nombre = normalize(d.nombre);
  const corto = normalize(d.nombreCorto);
  const sku = normalize(d.sku);
  const marca = normalize(d.marca);
  let s = 0;
  if (sku && sku === norm) s += 100;
  if (sku && sku.startsWith(norm)) s += 80;
  if (sku && sku.includes(norm)) s += 55;
  if (nombre.startsWith(norm)) s += 70;
  if (nombre.includes(norm)) s += 40;
  if (corto && corto.includes(norm)) s += 35;
  if (marca.includes(norm)) s += 25;
  for (const t of tokens) {
    if (t.length < 2) continue;
    if (nombre.includes(t)) s += 10;
    if (corto.includes(t)) s += 8;
    if (sku.includes(t)) s += 12;
    if (marca.includes(t)) s += 6;
  }
  return s;
}

function query(q: string, take: number): SearchDoc[] {
  const norm = normalize(q);
  if (norm.length < 1) return [];
  const tokens = norm.split(/[\s\-\/\.,_+()]+/).filter((t) => t.length >= 2);
  const scored: { d: SearchDoc; s: number }[] = [];
  for (const d of docs) {
    const sc = score(d, norm, tokens);
    if (sc > 0) scored.push({ d, s: sc });
  }
  scored.sort((a, b) => b.s - a.s || a.d.nombre.localeCompare(b.d.nombre));
  return scored.slice(0, take).map((x) => x.d);
}

addEventListener('message', (ev: MessageEvent) => {
  const msg = ev.data;
  if (!msg || typeof msg !== 'object') return;
  if (msg.type === 'setDocs') {
    docs = Array.isArray(msg.docs) ? msg.docs : [];
    postMessage({ type: 'ready', count: docs.length });
    return;
  }
  if (msg.type === 'patch' && msg.patch?.id != null) {
    const i = docs.findIndex((d) => d.id === msg.patch.id);
    if (i >= 0) {
      docs[i] = { ...docs[i], ...msg.patch };
    }
    return;
  }
  if (msg.type === 'query') {
    const t0 = performance.now();
    const items = query(String(msg.q ?? ''), Math.min(Number(msg.take) || 8, 48));
    postMessage({
      type: 'results',
      id: msg.id,
      items,
      tookMs: Math.round(performance.now() - t0),
      source: 'worker',
    });
  }
});
