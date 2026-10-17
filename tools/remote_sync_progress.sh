#!/bin/bash
set -eu
export PGPASSWORD="${ECOM_PG_PASSWORD:?Definí ECOM_PG_PASSWORD}"
psql -h 127.0.0.1 -p 5432 -U ecom -d ecommerce -c "
SELECT 'empresa' AS t, count(*) FROM dbo.empresa
UNION ALL SELECT 'config', count(*) FROM dbo.empresaconfiguracionecommerce
UNION ALL SELECT 'articulo', count(*) FROM dbo.articulo WHERE \"IDEmpresa\"=41
UNION ALL SELECT 'articulo_ecom', count(*) FROM dbo.articulo WHERE \"IDEmpresa\"=41 AND \"ActivoEcommerce\"=true
UNION ALL SELECT 'listaprecio', count(*) FROM dbo.listaprecio WHERE \"IDEmpresa\"=41
UNION ALL SELECT 'detalle_cv', count(*) FROM dbo.detallelistapreciocondicionventa WHERE \"IDEmpresa\"=41
UNION ALL SELECT 'stock', count(*) FROM dbo.stock WHERE \"IDEmpresa\"=41
UNION ALL SELECT 'cuenta', count(*) FROM dbo.cuenta WHERE \"IDEmpresa\"=41;
"
psql -h 127.0.0.1 -p 5432 -U ecom -d ecommerce -c '
SELECT "EntityKey","LastStatus","RowsProcessed","RowsRead","RowsInserted","DurationMs","LastError","LastRunUtc"
FROM dbo.synccursor WHERE "IDEmpresa"=41 ORDER BY 1;'
