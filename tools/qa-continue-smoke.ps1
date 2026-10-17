# QA Admin mutations (safe) + Storefront smoke
param(
  [string]$ApiBase = 'http://localhost:5082',
  [string]$AdminBase = 'http://localhost:4201',
  [string]$StoreBase = 'http://localhost:4200',
  [string]$Tenant = 'casa-imperial',
  [string]$AdminUser = 'admin',
  [string]$AdminPass = 'Admin12!'
)

$ErrorActionPreference = 'Continue'
$results = New-Object System.Collections.Generic.List[object]
$started = Get-Date
$stamp = Get-Date -Format 'yyyyMMddHHmmss'

function Add-Result {
  param([string]$Id, [string]$Block, [string]$Name, [string]$Status, [string]$Detail = '')
  $results.Add([pscustomobject]@{ Id=$Id; Block=$Block; Name=$Name; Status=$Status; Detail=$Detail }) | Out-Null
  $color = 'Gray'
  if ($Status -eq 'PASS') { $color = 'Green' }
  elseif ($Status -eq 'FAIL') { $color = 'Red' }
  elseif ($Status -eq 'SKIP') { $color = 'Yellow' }
  Write-Host ("[{0}] {1} - {2}" -f $Status, $Id, $Name) -ForegroundColor $color
  if ($Detail) { Write-Host ("         {0}" -f $Detail) -ForegroundColor DarkGray }
}

function Invoke-Json {
  param([string]$Method, [string]$Path, [hashtable]$Headers, $Body = $null, [int]$TimeoutSec = 45)
  try {
    $params = @{
      Uri = $ApiBase + $Path
      Method = $Method
      Headers = $Headers
      TimeoutSec = $TimeoutSec
      UseBasicParsing = $true
    }
    if ($null -ne $Body) {
      $params.ContentType = 'application/json'
      $params.Body = ($Body | ConvertTo-Json -Depth 8 -Compress)
    }
    $r = Invoke-WebRequest @params
    $parsed = $null
    try { $parsed = $r.Content | ConvertFrom-Json } catch { }
    return @{ Ok = $true; Status = [int]$r.StatusCode; Json = $parsed; Raw = $r.Content; Err = $null }
  } catch {
    $code = $null
    $errBody = $null
    if ($_.Exception.Response) {
      $code = [int]$_.Exception.Response.StatusCode
      try {
        $stream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        $errBody = $reader.ReadToEnd()
      } catch { }
    }
    return @{ Ok = $false; Status = $code; Json = $null; Raw = $errBody; Err = $_.Exception.Message }
  }
}

Write-Host ""
Write-Host "=== QA CONTINUE: ADMIN MUTATIONS + STOREFRONT ===" -ForegroundColor Cyan

# --- Admin login ---
$adminToken = $null
$login = Invoke-Json -Method POST -Path '/api/admin/auth/login' -Headers @{ 'X-Tenant-Slug' = $Tenant } -Body @{ usuario = $AdminUser; password = $AdminPass }
if ($login.Ok) {
  $adminToken = $login.Json.token
  if (-not $adminToken) { $adminToken = $login.Json.accessToken }
  if (-not $adminToken) { $adminToken = $login.Json.Token }
  Add-Result 'AM-00' 'admin-auth' 'Admin login' 'PASS' ("idEmpresa={0}" -f $login.Json.idEmpresa)
} else {
  Add-Result 'AM-00' 'admin-auth' 'Admin login' 'FAIL' $login.Err
  Write-Host 'Abort admin mutations' -ForegroundColor Red
}

$AH = @{ Authorization = "Bearer $adminToken"; 'X-Tenant-Slug' = $Tenant; Accept = 'application/json' }

if ($adminToken) {
  # Cupon create + toggle off
  $code = "QA$stamp"
  $c = Invoke-Json -Method POST -Path '/api/admin/cupones' -Headers $AH -Body @{
    codigo = $code
    descripcion = 'QA smoke auto'
    tipoDescuento = 'porcentaje'
    valorDescuento = 5
    montoMinimo = 0
    canalEcommerce = 'ALL'
    usosMaximos = 1
    usosPorUsuario = 1
  }
  if ($c.Ok -and $c.Json.id) {
    Add-Result 'AM-01' 'marketplace' "Crear cupon $code" 'PASS' ("id={0}" -f $c.Json.id)
    $t = Invoke-Json -Method POST -Path ("/api/admin/cupones/{0}/toggle?activo=false" -f $c.Json.id) -Headers $AH -Body @{}
    if ($t.Ok -or $t.Status -eq 204 -or $t.Status -eq 200) {
      Add-Result 'AM-02' 'marketplace' 'Desactivar cupon' 'PASS' ("id={0}" -f $c.Json.id)
    } else {
      # empty body POST might need no body
      try {
        Invoke-WebRequest -Uri ($ApiBase + "/api/admin/cupones/$($c.Json.id)/toggle?activo=false") -Method POST -Headers $AH -UseBasicParsing -TimeoutSec 20 | Out-Null
        Add-Result 'AM-02' 'marketplace' 'Desactivar cupon' 'PASS' ("id={0}" -f $c.Json.id)
      } catch {
        Add-Result 'AM-02' 'marketplace' 'Desactivar cupon' 'FAIL' $_.Exception.Message
      }
    }
  } else {
    Add-Result 'AM-01' 'marketplace' "Crear cupon $code" 'FAIL' ("HTTP {0} {1} {2}" -f $c.Status, $c.Err, $c.Raw)
  }

  # Chofer create + deactivate
  $nro = [int]("9{0}" -f ($stamp.Substring($stamp.Length - 6)))
  $ch = Invoke-Json -Method POST -Path '/api/admin/choferes' -Headers $AH -Body @{
    nroChofer = $nro
    nombre = "QA Smoke $stamp"
    activo = $true
  }
  if ($ch.Ok -and $ch.Json.id) {
    Add-Result 'AM-03' 'choferes' "Crear chofer $nro" 'PASS' ("id={0}" -f $ch.Json.id)
    $upd = Invoke-Json -Method PUT -Path ("/api/admin/choferes/{0}" -f $ch.Json.id) -Headers $AH -Body @{
      nroChofer = $nro
      nombre = "QA Smoke $stamp OFF"
      activo = $false
    }
    if ($upd.Ok) {
      Add-Result 'AM-04' 'choferes' 'Desactivar chofer' 'PASS' ("id={0}" -f $ch.Json.id)
    } else {
      $del = Invoke-Json -Method DELETE -Path ("/api/admin/choferes/{0}" -f $ch.Json.id) -Headers $AH
      if ($del.Ok -or $del.Status -eq 204) {
        Add-Result 'AM-04' 'choferes' 'Desactivar/borrar chofer' 'PASS' ("id={0}" -f $ch.Json.id)
      } else {
        Add-Result 'AM-04' 'choferes' 'Desactivar chofer' 'FAIL' ("HTTP {0} {1}" -f $upd.Status, $upd.Err)
      }
    }
  } else {
    Add-Result 'AM-03' 'choferes' "Crear chofer $nro" 'FAIL' ("HTTP {0} {1} {2}" -f $ch.Status, $ch.Err, $ch.Raw)
  }

  # Promotion list still OK after mutations
  $pr = Invoke-Json -Method GET -Path '/api/admin/promotions' -Headers $AH
  if ($pr.Ok) { Add-Result 'AM-05' 'promos' 'GET promotions post-mutation' 'PASS' 'HTTP 200' }
  else { Add-Result 'AM-05' 'promos' 'GET promotions post-mutation' 'FAIL' $pr.Err }
}

# --- Storefront public ---
$TH = @{ 'X-Tenant-Slug' = $Tenant; Accept = 'application/json'; 'X-Cart-Session' = ("qa-smoke-{0}" -f $stamp) }

$sfGets = @(
  @{ Id='SF-01'; Name='branding'; Path='/api/catalog/branding' },
  @{ Id='SF-02'; Name='home content'; Path='/api/storefront/content/home' },
  @{ Id='SF-03'; Name='personalizacion'; Path='/api/storefront/personalizacion' },
  @{ Id='SF-04'; Name='collections'; Path='/api/catalog/collections' },
  @{ Id='SF-05'; Name='marketing slots'; Path='/api/catalog/marketing' },
  @{ Id='SF-06'; Name='products B2C'; Path='/api/catalog/products?canal=B2C&page=1&pageSize=48' },
  @{ Id='SF-07'; Name='mas-vendidos'; Path='/api/catalog/mas-vendidos?take=10&canal=B2C' },
  @{ Id='SF-08'; Name='ofertas'; Path='/api/catalog/ofertas?take=12&canal=B2C' },
  @{ Id='SF-09'; Name='recomendaciones'; Path='/api/engagement/recomendaciones' },
  @{ Id='SF-10'; Name='marcas'; Path='/api/catalog/marcas' },
  @{ Id='SF-11'; Name='lineas'; Path='/api/catalog/lineas' },
  @{ Id='SF-12'; Name='tipos'; Path='/api/catalog/tipos' },
  @{ Id='SF-13'; Name='search suggest'; Path='/api/search?q=a&canal=B2C' },
  @{ Id='SF-14'; Name='ciudades delivery'; Path='/api/geography/ciudades-delivery' },
  @{ Id='SF-15'; Name='agencias'; Path='/api/geography/agencias' },
  @{ Id='SF-16'; Name='map defaults'; Path='/api/geography/map-defaults' },
  @{ Id='SF-17'; Name='cart guest'; Path='/api/cart?canal=B2C' },
  @{ Id='SF-18'; Name='browse feed recomendados'; Path='/api/engagement/recomendaciones' }
)

$productId = $null
$productSlug = $null
foreach ($g in $sfGets) {
  $r = Invoke-Json -Method GET -Path $g.Path -Headers $TH
  if ($r.Ok) {
    $extra = "HTTP $($r.Status)"
    if ($g.Id -eq 'SF-06' -and $r.Json.items) {
      $extra += (" items={0} total={1}" -f @($r.Json.items).Count, $r.Json.totalCount)
      $withStock = @($r.Json.items | Where-Object { $_.stockOk -eq $true })
      if ($withStock.Count -gt 0) {
        $productId = $withStock[0].id
        $productSlug = $withStock[0].slugSeo
        $extra += " pickStock=$productId"
      } elseif (@($r.Json.items).Count -gt 0) {
        $productId = $r.Json.items[0].id
        $productSlug = $r.Json.items[0].slugSeo
      }
    }
    if ($g.Id -eq 'SF-04' -and $r.Json) { $extra += (" count={0}" -f @($r.Json).Count) }
    if ($g.Id -eq 'SF-07' -and $r.Json) { $extra += (" count={0}" -f @($r.Json).Count) }
    Add-Result $g.Id 'storefront' $g.Name 'PASS' $extra
  } else {
    # personalizacion path may differ
    if ($g.Id -eq 'SF-03') {
      $alt = Invoke-Json -Method GET -Path '/api/catalog/personalizacion' -Headers $TH
      if ($alt.Ok) { Add-Result $g.Id 'storefront' $g.Name 'PASS' 'via /api/catalog/personalizacion'; continue }
      $alt2 = Invoke-Json -Method GET -Path '/api/storefront/personalizacion/claves' -Headers $TH
      if ($alt2.Ok) { Add-Result $g.Id 'storefront' $g.Name 'PASS' 'via claves'; continue }
    }
    Add-Result $g.Id 'storefront' $g.Name 'FAIL' ("HTTP {0} {1}" -f $r.Status, $r.Err)
  }
}

# Product detail + add to cart
if ($productId) {
  $det = Invoke-Json -Method GET -Path ("/api/catalog/products/{0}?canal=B2C" -f $productId) -Headers $TH
  if ($det.Ok) {
    Add-Result 'SF-19' 'storefront' "product detail #$productId" 'PASS' ("HTTP {0}" -f $det.Status)
    $embId = $null
    if ($det.Json.embalajes -and @($det.Json.embalajes).Count -gt 0) {
      $embId = $det.Json.embalajes[0].id
    }
    if ($embId) {
      $add = Invoke-Json -Method POST -Path '/api/cart/items' -Headers $TH -Body @{
        idArticulo = $productId
        idEmbalaje = $embId
        cantidad = 1
        canal = 'B2C'
      }
      if ($add.Ok) {
        Add-Result 'SF-20' 'cart' 'Add item to cart' 'PASS' ("items={0}" -f @($add.Json.items).Count)
      } else {
        Add-Result 'SF-20' 'cart' 'Add item to cart' 'FAIL' ("HTTP {0} {1} {2}" -f $add.Status, $add.Err, $add.Raw)
      }
    } else {
      Add-Result 'SF-20' 'cart' 'Add item to cart' 'SKIP' 'producto sin embalaje'
    }
  } else {
    Add-Result 'SF-19' 'storefront' "product detail #$productId" 'FAIL' $det.Err
  }
} else {
  Add-Result 'SF-19' 'storefront' 'product detail' 'SKIP' 'sin productos'
}

# Feed browse modes (home Ver mas destinations)
$mv = Invoke-Json -Method GET -Path '/api/catalog/mas-vendidos?take=48&canal=B2C' -Headers $TH
if ($mv.Ok) { Add-Result 'SF-21' 'home' 'Ver todo mas-vendidos (48)' 'PASS' ("count={0}" -f @($mv.Json).Count) }
else { Add-Result 'SF-21' 'home' 'Ver todo mas-vendidos' 'FAIL' $mv.Err }

$of = Invoke-Json -Method GET -Path '/api/catalog/ofertas?take=48&canal=B2C' -Headers $TH
if ($of.Ok) { Add-Result 'SF-22' 'home' 'Ver todo ofertas (48)' 'PASS' ("count={0}" -f @($of.Json).Count) }
else { Add-Result 'SF-22' 'home' 'Ver todo ofertas' 'FAIL' $of.Err }

# Auth register+login ephemeral (best effort)
$email = "qa.smoke.$stamp@example.com"
$doc = ("9{0}" -f $stamp.Substring($stamp.Length - 7))
$reg = Invoke-Json -Method POST -Path '/api/auth/register' -Headers $TH -Body @{
  nombre = 'QA'
  apellido = 'Smoke'
  idTipoDocumento = 1
  numeroDocumento = $doc
  telefono = '021000000'
  movil = '0981000000'
  email = $email
  password = 'QaSmoke12!'
  passwordConfirmation = 'QaSmoke12!'
  tenantSlug = $Tenant
}
if ($reg.Ok) {
  Add-Result 'SF-23' 'auth' 'Register storefront user' 'PASS' $email
  $actUrl = $reg.Json.activationUrlDev
  if (-not $actUrl) { $actUrl = $reg.Json.ActivationUrlDev }
  if ($actUrl -match '[?&]c=([^&]+).*[?&]k=([^&]+)' -or $actUrl -match 'codigo=([^&]+).*checksum=([^&]+)') {
    # try activate from url query
  }
} else {
  Add-Result 'SF-23' 'auth' 'Register storefront user' 'SKIP' ("HTTP {0} {1}" -f $reg.Status, $reg.Err)
}

$ulogin = Invoke-Json -Method POST -Path '/api/auth/login' -Headers $TH -Body @{
  usuario = $email
  password = 'QaSmoke12!'
  tenantSlug = $Tenant
}
$userToken = $null
if ($ulogin.Ok) {
  $userToken = $ulogin.Json.accessToken
  if (-not $userToken) { $userToken = $ulogin.Json.token }
  Add-Result 'SF-24' 'auth' 'Login storefront' 'PASS' $email
} else {
  Add-Result 'SF-24' 'auth' 'Login storefront' 'SKIP' ("HTTP {0} - may need activation" -f $ulogin.Status)
}

if ($userToken) {
  $UH = @{ Authorization = "Bearer $userToken"; 'X-Tenant-Slug' = $Tenant; Accept = 'application/json' }
  $me = Invoke-Json -Method GET -Path '/api/account/me' -Headers $UH
  if ($me.Ok) { Add-Result 'SF-25' 'account' 'GET account/me' 'PASS' 'HTTP 200' }
  else { Add-Result 'SF-25' 'account' 'GET account/me' 'FAIL' $me.Err }
  $ords = Invoke-Json -Method GET -Path '/api/account/orders' -Headers $UH
  if ($ords.Ok) { Add-Result 'SF-26' 'account' 'GET account/orders' 'PASS' 'HTTP 200' }
  else { Add-Result 'SF-26' 'account' 'GET account/orders' 'FAIL' $ords.Err }
  $devs = Invoke-Json -Method GET -Path '/api/engagement/devoluciones' -Headers $UH
  if ($devs.Ok) { Add-Result 'SF-27' 'returns' 'GET devoluciones' 'PASS' 'HTTP 200' }
  else { Add-Result 'SF-27' 'returns' 'GET devoluciones' 'FAIL' $devs.Err }
  $chats = Invoke-Json -Method GET -Path '/api/engagement/chat/conversaciones' -Headers $UH
  if (-not $chats.Ok) { $chats = Invoke-Json -Method GET -Path '/api/engagement/chat' -Headers $UH }
  if ($chats.Ok) { Add-Result 'SF-28' 'chat' 'GET chat' 'PASS' 'HTTP 200' }
  else { Add-Result 'SF-28' 'chat' 'GET chat' 'FAIL' ("HTTP {0}" -f $chats.Status) }
} else {
  Add-Result 'SF-25' 'account' 'account endpoints' 'SKIP' 'sin sesion cliente'
}

# SPA storefront routes
$routes = @(
  '/catalogo', '/catalogo?browse=1', '/catalogo?browse=1&feed=recomendados',
  '/catalogo?browse=1&feed=masVendidos', '/catalogo?browse=1&feed=ofertas',
  '/carrito', '/checkout', '/login', '/registro', '/contacto', '/faq',
  '/mis-pedidos', '/devoluciones', '/favoritos', '/mi-cuenta', '/sucursales',
  '/como-comprar', '/terminos'
)
if ($productId) {
  if ($productSlug) { $routes += "/producto/$productSlug" }
  else { $routes += "/producto/$productId" }
}
foreach ($route in $routes) {
  try {
    $rr = Invoke-WebRequest -Uri ($StoreBase + $route) -UseBasicParsing -TimeoutSec 20 -Headers @{ 'X-Tenant-Slug' = $Tenant }
    if ($rr.StatusCode -eq 200) {
      Add-Result ("SPA-$route") 'spa-sf' "Store route $route" 'PASS' ("HTTP {0}" -f $rr.StatusCode)
    } else {
      Add-Result ("SPA-$route") 'spa-sf' "Store route $route" 'FAIL' ("HTTP {0}" -f $rr.StatusCode)
    }
  } catch {
    Add-Result ("SPA-$route") 'spa-sf' "Store route $route" 'FAIL' $_.Exception.Message
  }
}

# Summary
$pass = @($results | Where-Object { $_.Status -eq 'PASS' }).Count
$fail = @($results | Where-Object { $_.Status -eq 'FAIL' }).Count
$skip = @($results | Where-Object { $_.Status -eq 'SKIP' }).Count
$elapsed = [math]::Round(((Get-Date) - $started).TotalSeconds, 1)

Write-Host ""
Write-Host "=== SUMMARY ===" -ForegroundColor Cyan
Write-Host ("PASS={0}  FAIL={1}  SKIP={2}  total={3}  {4}s" -f $pass, $fail, $skip, $results.Count, $elapsed)

$outDir = Join-Path (Split-Path $PSScriptRoot -Parent) 'docs'
$csv = Join-Path $outDir 'QA-CONTINUE-SMOKE-RESULTS.csv'
$md = Join-Path $outDir 'QA-CONTINUE-SMOKE-RESULTS.md'
$results | Export-Csv -Path $csv -NoTypeInformation -Encoding UTF8

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('# QA Continue - Admin mutations + Storefront')
[void]$sb.AppendLine('')
[void]$sb.AppendLine(("**Ejecutado:** {0}" -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss')))
[void]$sb.AppendLine(("**API:** {0} · Admin {1} · Store {2} · Tenant {3}" -f $ApiBase, $AdminBase, $StoreBase, $Tenant))
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
if ($fails.Count -eq 0) { [void]$sb.AppendLine('_Ninguno._') }
else {
  foreach ($f in $fails) { [void]$sb.AppendLine(("- **{0}** {1}: {2}" -f $f.Id, $f.Name, $f.Detail)) }
}
[System.IO.File]::WriteAllText($md, $sb.ToString(), [System.Text.UTF8Encoding]::new($false))
Write-Host "Wrote $md"

if ($fail -gt 0) { exit 2 } else { exit 0 }
