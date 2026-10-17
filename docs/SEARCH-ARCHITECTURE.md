# Arquitectura de búsqueda rápida — Datamex E-Commerce

**Fecha:** 5 ago 2026  
**Principio:** funciona **Standalone** sin Redis/OpenSearch; engancha Redis/OpenSearch cuando hay config. El ERP solo alimenta el espejo EcomDb (sync); **nunca** se busca contra Postgres ERP en hot-path.

```
ERP ──sync/CDC──► EcomDb (Postgres/SQLite)
                      │
                      ▼
              Índice local (RAM API)  ← default
                      │
         ┌────────────┼────────────┐
         ▼            ▼            ▼
      MemoryCache   Redis*     OpenSearch*
         │
         ▼
   GET /api/search*
         │
         ▼
   Storefront
   RAM Map → IndexedDB → Web Worker → API
```

\* Opcional (`Search:RedisConnection`, `Search:OpenSearchUrl`).

## Capas

| Nivel | Dónde | Qué hace |
|---|---|---|
| 1 | Browser `Map` | Reusa/filtra resultados de tipado previo (`lap`→`lapt`) sin red |
| 2 | IndexedDB + Web Worker | Snapshot prefetch (`/api/search/snapshot`); filtra sin bloquear UI |
| 3 | API MemoryCache / Redis | Cache de respuestas `q+canal+page` |
| 4 | Índice local / OpenSearch | Scoring + fuzzy leve; OS si `Engine=OpenSearch` |
| 5 | EF Catalog | Fallback si índice vacío |
| 6 | EcomDb | Escrituras (stock/precio); patch incremental `POST /api/search/patch` |

## Endpoints

- `GET /api/search?q=&take=` — suggest
- `GET /api/search/results?q=&page=` — resultados
- `GET /api/search/snapshot?max=` — prefetch IndexedDB
- `POST /api/search/patch` — `{ id, stockLibre?, precioDesde? }`
- `POST /api/search/rebuild` — rebuild índice tenant

## Config (`appsettings`)

```json
"Search": {
  "Enabled": true,
  "Engine": "Local",
  "RedisConnection": "",
  "OpenSearchUrl": "",
  "SnapshotMaxDocs": 2000,
  "IndexRefreshMinutes": 5
}
```

Respuestas comprimidas con **Brotli/gzip** (`UseResponseCompression`).

## Independencia ERP

- Standalone: índice desde EcomDb local; sin CS ERP.
- Integrated: sync ERP → rebuild periódico (`SearchIndexHostedService`); write-back de pedidos no afecta búsqueda.
- OpenSearch/Redis son **aceleradores**, no requisito.
