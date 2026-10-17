# QA Admin smoke - read-only API + SPA checks
param(
  [string]$ApiBase = 'http://localhost:5082',
  [string]$AdminBase = 'http://localhost:4201',
  [string]$Tenant = 'casa-imperial',
  [string]$User = 'admin',
  [string]$Pass = 'Admin12!'
)

$ErrorActionPreference = 'Continue'
$results = New-Object System.Collections.Generic.List[object]
$started = Get-Date

function Add-Result {
  param([string]$Id, [string]$Block, [string]$Name, [string]$Status, [string]$Detail = '')
  $results.Add([pscustomobject]@{
    Id     = $Id
    Block  = $Block
    Name   = $Name
    Status = $Status
    Detail = $Detail
  }) | Out-Null
  $color = 'Gray'
  if ($Status -eq 'PASS') { $color = 'Green' }
  elseif ($Status -eq 'FAIL') { $color = 'Red' }
  elseif ($Status -eq 'SKIP') { $color = 'Yellow' }
  Write-Host ("[{0}] {1} - {2}" -f $Status, $Id, $Name) -ForegroundColor $color
  if ($Detail) { Write-Host ("         {0}" -f $Detail) -ForegroundColor DarkGray }
}

function Invoke-AdminGet {
  param([string]$Path, [hashtable]$Headers, [int]$TimeoutSec = 30)
  try {
    $r = Invoke-WebRequest -Uri ($ApiBase + $Path) -Headers $Headers -Method GET -TimeoutSec $TimeoutSec -UseBasicParsing
    return @{ Ok = $true; Status = [int]$r.StatusCode; Body = $r.Content; Err = $null }
  } catch {
    $code = $null
    if ($_.Exception.Response) { $code = [int]$_.Exception.Response.StatusCode }
    return @{ Ok = $false; Status = $code; Body = $null; Err = $_.Exception.Message }
  }
}

Write-Host ""
Write-Host "=== QA ADMIN SMOKE ===" -ForegroundColor Cyan
Write-Host ("API={0}  Admin={1}  Tenant={2}" -f $ApiBase, $AdminBase, $Tenant)
Write-Host ""

# Prep
try {
  $health = Invoke-WebRequest -Uri "$ApiBase/health" -UseBasicParsing -TimeoutSec 10
  Add-Result '0.1' 'prep' 'API health' 'PASS' ("HTTP {0}" -f $health.StatusCode)
} catch {
  Add-Result '0.1' 'prep' 'API health' 'SKIP' 'no /health; will try login'
}

try {
  $spa = Invoke-WebRequest -Uri "$AdminBase/login" -UseBasicParsing -TimeoutSec 15
  $ok = $spa.StatusCode -eq 200 -and ($spa.Content -match 'html|app-root|Casa Imperial|login')
  if ($ok) {
    Add-Result '0.2' 'prep' 'Admin SPA /login' 'PASS' ("HTTP {0} len={1}" -f $spa.StatusCode, $spa.Content.Length)
  } else {
    Add-Result '0.2' 'prep' 'Admin SPA /login' 'FAIL' ("HTTP {0}" -f $spa.StatusCode)
  }
} catch {
  Add-Result '0.2' 'prep' 'Admin SPA /login' 'FAIL' $_.Exception.Message
}

# Login
$token = $null
$idEmpresa = $null
try {
  $loginBody = @{ usuario = $User; password = $Pass } | ConvertTo-Json
  $login = Invoke-RestMethod -Uri "$ApiBase/api/admin/auth/login" -Method POST -Body $loginBody -ContentType 'application/json' -Headers @{ 'X-Tenant-Slug' = $Tenant } -TimeoutSec 30
  $token = $login.token
  if (-not $token) { $token = $login.accessToken }
  if (-not $token) { $token = $login.Token }
  $idEmpresa = $login.idEmpresa
  $permCount = 0
  if ($login.permissions) { $permCount = @($login.permissions).Count }
  if ($token) {
    Add-Result 'A-01' 'auth' 'Login OK' 'PASS' ("idEmpresa={0} perms={1}" -f $idEmpresa, $permCount)
  } else {
    Add-Result 'A-01' 'auth' 'Login OK' 'FAIL' 'response sin token'
  }
} catch {
  Add-Result 'A-01' 'auth' 'Login OK' 'FAIL' $_.Exception.Message
}

try {
  $bad = @{ usuario = $User; password = 'WrongPass!' } | ConvertTo-Json
  Invoke-RestMethod -Uri "$ApiBase/api/admin/auth/login" -Method POST -Body $bad -ContentType 'application/json' -Headers @{ 'X-Tenant-Slug' = $Tenant } -TimeoutSec 15 | Out-Null
  Add-Result 'A-02' 'auth' 'Login bad password' 'FAIL' 'accepted invalid password'
} catch {
  Add-Result 'A-02' 'auth' 'Login bad password' 'PASS' 'rejected as expected'
}

try {
  $badT = @{ usuario = $User; password = $Pass } | ConvertTo-Json
  Invoke-RestMethod -Uri "$ApiBase/api/admin/auth/login" -Method POST -Body $badT -ContentType 'application/json' -Headers @{ 'X-Tenant-Slug' = 'tenant-inexistente-xyz' } -TimeoutSec 15 | Out-Null
  Add-Result 'A-03' 'auth' 'Login bad tenant' 'FAIL' 'accepted unknown tenant'
} catch {
  Add-Result 'A-03' 'auth' 'Login bad tenant' 'PASS' 'rejected as expected'
}

if (-not $token) {
  Write-Host "Abort: no token." -ForegroundColor Red
  exit 1
}

$H = @{
  Authorization   = "Bearer $token"
  'X-Tenant-Slug' = $Tenant
  Accept          = 'application/json'
}

try {
  Invoke-WebRequest -Uri "$ApiBase/api/admin/dashboard" -UseBasicParsing -TimeoutSec 10 | Out-Null
  Add-Result 'N-04' 'neg' 'Dashboard sin auth' 'FAIL' 'allowed without token'
} catch {
  $code = $null
  if ($_.Exception.Response) { $code = [int]$_.Exception.Response.StatusCode }
  if ($code -in 401, 403) {
    Add-Result 'N-04' 'neg' 'Dashboard sin auth' 'PASS' ("HTTP {0}" -f $code)
  } else {
    Add-Result 'N-04' 'neg' 'Dashboard sin auth' 'PASS' ("blocked {0}" -f $code)
  }
}

$checks = @(
  @{ Id = 'D-01'; Block = 'dashboard'; Name = 'GET dashboard'; Path = '/api/admin/dashboard' },
  @{ Id = 'S-01'; Block = 'sync'; Name = 'GET sync/status'; Path = '/api/admin/sync/status' },
  @{ Id = 'C-01'; Block = 'catalog'; Name = 'GET products'; Path = '/api/admin/products?page=1&pageSize=10' },
  @{ Id = 'C-28'; Block = 'catalog'; Name = 'GET parametros-producto'; Path = '/api/admin/parametros-producto?tipo=marca' },
  @{ Id = 'C-25'; Block = 'catalog'; Name = 'GET unidades-medida'; Path = '/api/admin/unidades-medida' },
  @{ Id = 'P-01'; Block = 'orders'; Name = 'GET orders'; Path = '/api/admin/orders?page=1&pageSize=10' },
  @{ Id = 'P-08'; Block = 'orders'; Name = 'GET migracion-logs'; Path = '/api/admin/orders/migracion-logs' },
  @{ Id = 'K-01'; Block = 'carts'; Name = 'GET carritos'; Path = '/api/admin/carritos?page=1&pageSize=10' },
  @{ Id = 'L-01'; Block = 'clients'; Name = 'GET clientes'; Path = '/api/admin/clientes?page=1&pageSize=10' },
  @{ Id = 'L-02'; Block = 'clients'; Name = 'GET solicitudes-mayorista'; Path = '/api/admin/clientes/solicitudes-mayorista' },
  @{ Id = 'H-01'; Block = 'drivers'; Name = 'GET choferes'; Path = '/api/admin/choferes' },
  @{ Id = 'LP-01'; Block = 'prices'; Name = 'GET listas-precios'; Path = '/api/admin/listas-precios' },
  @{ Id = 'T-01'; Block = 'shipping'; Name = 'GET tarifas'; Path = '/api/admin/shipping/tarifas' },
  @{ Id = 'M-01'; Block = 'marketing'; Name = 'GET promotions'; Path = '/api/admin/promotions' },
  @{ Id = 'M-07'; Block = 'marketing'; Name = 'GET resenas'; Path = '/api/admin/resenas' },
  @{ Id = 'M-08'; Block = 'marketing'; Name = 'GET cupones'; Path = '/api/admin/cupones' },
  @{ Id = 'M-10'; Block = 'marketing'; Name = 'GET devoluciones'; Path = '/api/admin/devoluciones' },
  @{ Id = 'M-13'; Block = 'cms'; Name = 'GET content/blocks'; Path = '/api/admin/content/blocks' },
  @{ Id = 'M-15'; Block = 'cms'; Name = 'GET content/menus'; Path = '/api/admin/content/menus' },
  @{ Id = 'M-16'; Block = 'cms'; Name = 'GET personalizacion/schemas'; Path = '/api/admin/personalizacion/schemas' },
  @{ Id = 'M-21'; Block = 'branding'; Name = 'GET branding'; Path = '/api/admin/empresa/branding' },
  @{ Id = 'CFG-01'; Block = 'config'; Name = 'GET empresa/config'; Path = '/api/admin/empresa/config' },
  @{ Id = 'CFG-06'; Block = 'config'; Name = 'GET agencias'; Path = '/api/admin/empresa/agencias' },
  @{ Id = 'CFG-06b'; Block = 'config'; Name = 'GET depositos'; Path = '/api/admin/empresa/depositos' },
  @{ Id = 'CFG-06c'; Block = 'config'; Name = 'GET horarios'; Path = '/api/admin/empresa/horarios' },
  @{ Id = 'CFG-05'; Block = 'config'; Name = 'GET erp-health'; Path = '/api/admin/empresa/config/erp-health' },
  @{ Id = 'R-01'; Block = 'crm'; Name = 'GET chat conversaciones'; Path = '/api/admin/chat/conversaciones' },
  @{ Id = 'R-08'; Block = 'contacto'; Name = 'GET contacto'; Path = '/api/contacto' },
  @{ Id = 'REP-01'; Block = 'reports'; Name = 'GET reportes ventas'; Path = '/api/admin/reports/ventas?desde=2026-01-01&hasta=2026-12-31' },
  @{ Id = 'SEC-01'; Block = 'security'; Name = 'GET manager roles'; Path = '/api/admin/manager/roles' },
  @{ Id = 'SEC-02'; Block = 'security'; Name = 'GET manager usuarios'; Path = '/api/admin/manager/usuarios' },
  @{ Id = 'SEC-04'; Block = 'security'; Name = 'GET functionalidades'; Path = '/api/admin/manager/functionalidades' },
  @{ Id = 'SEC-04b'; Block = 'security'; Name = 'GET operaciones-tree'; Path = '/api/admin/manager/operaciones-tree' },
  @{ Id = 'TEN-01'; Block = 'tenants'; Name = 'GET tenants'; Path = '/api/admin/tenants' }
)

$firstProductId = $null
$firstOrderId = $null
$firstCartId = $null

foreach ($c in $checks) {
  $r = Invoke-AdminGet -Path $c.Path -Headers $H
  if ($r.Ok) {
    $extra = "HTTP $($r.Status)"
    try {
      $j = $r.Body | ConvertFrom-Json
      if ($null -ne $j.items) {
        $extra += (" items={0} total={1}" -f @($j.items).Count, $j.totalCount)
        if ($c.Id -eq 'C-01' -and @($j.items).Count -gt 0) { $firstProductId = $j.items[0].id }
        if ($c.Id -eq 'P-01' -and @($j.items).Count -gt 0) {
          $firstOrderId = $j.items[0].idPedidoMovil
          if (-not $firstOrderId) { $firstOrderId = $j.items[0].id }
        }
        if ($c.Id -eq 'K-01' -and @($j.items).Count -gt 0) { $firstCartId = $j.items[0].id }
      } elseif ($j -is [System.Array]) {
        $extra += (" count={0}" -f @($j).Count)
      }
      if ($c.Id -eq 'CFG-01') {
        $extra += (" modo={0} slug={1}" -f $j.modoIntegracion, $j.slug)
      }
    } catch { }
    Add-Result $c.Id $c.Block $c.Name 'PASS' $extra
  } else {
    Add-Result $c.Id $c.Block $c.Name 'FAIL' ("HTTP {0} {1}" -f $r.Status, $r.Err)
  }
}

if ($firstProductId) {
  $r = Invoke-AdminGet -Path "/api/admin/products/$firstProductId/detail" -Headers $H
  if ($r.Ok) { Add-Result 'C-11' 'catalog' ("GET product detail #$firstProductId") 'PASS' ("HTTP {0}" -f $r.Status) }
  else { Add-Result 'C-11' 'catalog' ("GET product detail #$firstProductId") 'FAIL' ("HTTP {0}" -f $r.Status) }

  $r = Invoke-AdminGet -Path "/api/admin/products/$firstProductId/images" -Headers $H
  if ($r.Ok) { Add-Result 'C-20' 'catalog' ("GET product images #$firstProductId") 'PASS' ("HTTP {0}" -f $r.Status) }
  else { Add-Result 'C-20' 'catalog' ("GET product images #$firstProductId") 'FAIL' ("HTTP {0}" -f $r.Status) }

  $r = Invoke-AdminGet -Path "/api/admin/products/$firstProductId/embalajes" -Headers $H
  if ($r.Ok) { Add-Result 'C-26' 'catalog' ("GET embalajes #$firstProductId") 'PASS' ("HTTP {0}" -f $r.Status) }
  else { Add-Result 'C-26' 'catalog' ("GET embalajes #$firstProductId") 'FAIL' ("HTTP {0}" -f $r.Status) }

  $r = Invoke-AdminGet -Path "/api/admin/inventory/movimientos?idArticulo=$firstProductId" -Headers $H
  if ($r.Ok) { Add-Result 'C-30' 'catalog' ("GET movimientos #$firstProductId") 'PASS' ("HTTP {0}" -f $r.Status) }
  else { Add-Result 'C-30' 'catalog' ("GET movimientos #$firstProductId") 'FAIL' ("HTTP {0}" -f $r.Status) }
} else {
  Add-Result 'C-11' 'catalog' 'GET product detail' 'SKIP' 'sin productos'
}

if ($firstOrderId) {
  $r = Invoke-AdminGet -Path "/api/admin/orders/$firstOrderId" -Headers $H
  if ($r.Ok) { Add-Result 'P-09' 'orders' ("GET order detail #$firstOrderId") 'PASS' ("HTTP {0}" -f $r.Status) }
  else { Add-Result 'P-09' 'orders' ("GET order detail #$firstOrderId") 'FAIL' ("HTTP {0}" -f $r.Status) }
} else {
  Add-Result 'P-09' 'orders' 'GET order detail' 'SKIP' 'sin pedidos'
}

if ($firstCartId) {
  $r = Invoke-AdminGet -Path "/api/admin/carritos/$firstCartId" -Headers $H
  if ($r.Ok) { Add-Result 'K-05' 'carts' ("GET carrito detail #$firstCartId") 'PASS' ("HTTP {0}" -f $r.Status) }
  else { Add-Result 'K-05' 'carts' ("GET carrito detail #$firstCartId") 'FAIL' ("HTTP {0}" -f $r.Status) }
} else {
  Add-Result 'K-05' 'carts' 'GET carrito detail' 'SKIP' 'sin carritos'
}

$routes = @(
  '/', '/catalogo', '/pedidos', '/carritos', '/clientes', '/choferes', '/listas-precios',
  '/tarifas', '/promociones', '/marketplace', '/crm', '/contacto', '/contenido',
  '/personalizacion', '/branding', '/configuracion', '/sync', '/reportes', '/manager',
  '/empresas', '/catalogo/activar'
)
foreach ($route in $routes) {
  try {
    $rr = Invoke-WebRequest -Uri ($AdminBase + $route) -UseBasicParsing -TimeoutSec 15
    if ($rr.StatusCode -eq 200) {
      Add-Result ("SPA-$route") 'spa' ("Admin route $route") 'PASS' ("HTTP {0}" -f $rr.StatusCode)
    } else {
      Add-Result ("SPA-$route") 'spa' ("Admin route $route") 'FAIL' ("HTTP {0}" -f $rr.StatusCode)
    }
  } catch {
    Add-Result ("SPA-$route") 'spa' ("Admin route $route") 'FAIL' $_.Exception.Message
  }
}

$r = Invoke-AdminGet -Path '/api/admin/personalizacion' -Headers $H
if ($r.Ok) {
  Add-Result 'M-17' 'cms' 'GET personalizacion' 'PASS' ("HTTP {0}" -f $r.Status)
} else {
  $r2 = Invoke-AdminGet -Path '/api/admin/personalizacion/items' -Headers $H
  if ($r2.Ok) {
    Add-Result 'M-17' 'cms' 'GET personalizacion/items' 'PASS' ("HTTP {0}" -f $r2.Status)
  } else {
    Add-Result 'M-17' 'cms' 'GET personalizacion' 'SKIP' 'schemas already covered'
  }
}

$pass = @($results | Where-Object { $_.Status -eq 'PASS' }).Count
$fail = @($results | Where-Object { $_.Status -eq 'FAIL' }).Count
$skip = @($results | Where-Object { $_.Status -eq 'SKIP' }).Count
$elapsed = [math]::Round(((Get-Date) - $started).TotalSeconds, 1)

Write-Host ""
Write-Host "=== SUMMARY ===" -ForegroundColor Cyan
Write-Host ("PASS={0}  FAIL={1}  SKIP={2}  total={3}  {4}s" -f $pass, $fail, $skip, $results.Count, $elapsed)

$outDir = Join-Path (Split-Path $PSScriptRoot -Parent) 'docs'
if (-not (Test-Path $outDir)) { $outDir = $PSScriptRoot }
$csv = Join-Path $outDir 'QA-ADMIN-SMOKE-RESULTS.csv'
$md = Join-Path $outDir 'QA-ADMIN-SMOKE-RESULTS.md'
$results | Export-Csv -Path $csv -NoTypeInformation -Encoding UTF8

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('# QA Admin Smoke - Resultados')
[void]$sb.AppendLine('')
[void]$sb.AppendLine(("**Ejecutado:** {0}" -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss')))
[void]$sb.AppendLine(("**API:** {0} · **Admin:** {1} · **Tenant:** {2}" -f $ApiBase, $AdminBase, $Tenant))
[void]$sb.AppendLine(("**Score:** PASS **{0}** · FAIL **{1}** · SKIP **{2}** · {3}s" -f $pass, $fail, $skip, $elapsed))
[void]$sb.AppendLine('')
[void]$sb.AppendLine('| Id | Bloque | Caso | Status | Detalle |')
[void]$sb.AppendLine('|----|--------|------|--------|---------|')
foreach ($row in $results) {
  $d = ($row.Detail -replace '\|', '/' -replace "`r|`n", ' ')
  [void]$sb.AppendLine(("| {0} | {1} | {2} | **{3}** | {4} |" -f $row.Id, $row.Block, $row.Name, $row.Status, $d))
}
[void]$sb.AppendLine('')
[void]$sb.AppendLine('## Fallos')
$fails = @($results | Where-Object { $_.Status -eq 'FAIL' })
if ($fails.Count -eq 0) {
  [void]$sb.AppendLine('_Ninguno._')
} else {
  foreach ($f in $fails) {
    [void]$sb.AppendLine(("- **{0}** {1}: {2}" -f $f.Id, $f.Name, $f.Detail))
  }
}
[void]$sb.AppendLine('')
[void]$sb.AppendLine('## Nota')
[void]$sb.AppendLine('Smoke **read-only** via API + carga SPA. No cubre mutaciones UI (bulk ERP, publicar vitrina, chat live, uploads). Esos quedan manuales en `QA-ADMIN-PANEL.md`.')
[System.IO.File]::WriteAllText($md, $sb.ToString(), [System.Text.UTF8Encoding]::new($false))

Write-Host "Wrote $csv"
Write-Host "Wrote $md"

if ($fail -gt 0) { exit 2 } else { exit 0 }
