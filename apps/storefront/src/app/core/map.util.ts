/** Helpers de mapas (Leaflet / OSM / Google) para el storefront. */

declare const L: any;

export function isValidLatLng(lat: unknown, lng: unknown): boolean {
  const la = Number(lat);
  const lo = Number(lng);
  if (!Number.isFinite(la) || !Number.isFinite(lo)) return false;
  // Evita Null Island (0,0) y coords basura de config vacía.
  if (Math.abs(la) < 0.01 && Math.abs(lo) < 0.01) return false;
  if (la < -90 || la > 90 || lo < -180 || lo > 180) return false;
  return true;
}

/** Pin en Google Maps. */
export function mapsPinUrl(lat: number, lng: number, label?: string): string {
  const query = label?.trim() ? `${label.trim()} @${lat},${lng}` : `${lat},${lng}`;
  return `https://www.google.com/maps?q=${encodeURIComponent(query)}`;
}

/** Cómo llegar desde la ubicación del usuario. */
export function mapsDirectionsUrl(lat: number, lng: number): string {
  return `https://www.google.com/maps/dir/?api=1&destination=${lat},${lng}`;
}

export function osmEmbedUrl(lat: number, lng: number, delta = 0.012): string {
  return (
    `https://www.openstreetmap.org/export/embed.html?bbox=${lng - delta}%2C${lat - delta}%2C${lng + delta}%2C${lat + delta}` +
    `&layer=mapnik&marker=${lat}%2C${lng}`
  );
}

export function hasLeaflet(): boolean {
  return typeof L !== 'undefined';
}

export function addOsmTiles(map: any): void {
  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
    maxZoom: 19,
  }).addTo(map);
}

/** Pin custom (entrega azul / sucursal rojo). */
export function pinIcon(kind: 'delivery' | 'store' = 'delivery'): any {
  const color = kind === 'store' ? '#c62828' : '#1565c0';
  return L.divIcon({
    className: 'dx-map-pin',
    html: `<span class="dx-map-pin__dot" style="--pin:${color}"></span>`,
    iconSize: [28, 36],
    iconAnchor: [14, 34],
    popupAnchor: [0, -30],
  });
}
