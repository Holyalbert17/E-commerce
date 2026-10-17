#!/bin/bash
set -eu
export PGPASSWORD="${ECOM_PG_PASSWORD:?Definí ECOM_PG_PASSWORD}"
psql -h 127.0.0.1 -p 5432 -U ecom -d ecommerce <<'SQL'
SELECT 'detallelistaprecio' AS t, count(*) FROM dbo.detallelistaprecio WHERE "IDEmpresa"=41
UNION ALL SELECT 'detalle_cv', count(*) FROM dbo.detallelistapreciocondicionventa WHERE "IDEmpresa"=41
UNION ALL SELECT 'detalle_cv_activo', count(*) FROM dbo.detallelistapreciocondicionventa WHERE "IDEmpresa"=41 AND "Activo"=true;

SELECT l."ID", l."NroListaPrecio", l."Descripcion", l."EsListaPrecioEcommerce",
       (SELECT count(*) FROM dbo.detallelistaprecio d WHERE d."IDListaPrecio"=l."ID") AS det,
       (SELECT count(*) FROM dbo.detallelistapreciocondicionventa d WHERE d."IDListaPrecio"=l."ID") AS cv
FROM dbo.listaprecio l
WHERE l."IDEmpresa"=41
ORDER BY l."EsListaPrecioEcommerce" DESC, l."ID";
SQL
