$ErrorActionPreference = 'Continue'
$base = 'http://127.0.0.1:5082'
$slug = 'casa-imperial'
$results = [System.Collections.Generic.List[object]]::new()

function Test-Admin([string]$Section, [string]$Method, [string]$Path, [hashtable]$Headers, $Body = $null) {
  $uri = "$base$Path"
  try {
    $params = @{ Uri = $uri; Method = $Method; Headers = $Headers; UseBasicParsing = $true; TimeoutSec = 60 }
    if ($null -ne $Body) {
      $params.ContentType = 'application/json'
      $params.Body = ($Body | ConvertTo-Json -Depth 8 -Compress)
    }
    $resp = Invoke-WebRequest @params
    $preview = ''
    if ($resp.Content) {
      $preview = $resp.Content.Substring(0, [Math]::Min(80, $resp.Content.Length)).Replace("`n", ' ').Replace("`r", '')
    }
    $results.Add([pscustomobject]@{
        Section = $Section; Path = $Path; Status = [int]$resp.StatusCode; Ok = $true; Preview = $preview; Error = ''
      })
  }
  catch {
    $code = 0
    $err = $_.Exception.Message
    if ($_.Exception.Response) {
      try { $code = [int]$_.Exception.Response.StatusCode } catch {}
      try {
        $sr = New-Object IO.StreamReader($_.Exception.Response.GetResponseStream())
        $t = $sr.ReadToEnd()
        if ($t) { $err = $t.Substring(0, [Math]::Min(160, $t.Length)) }
      }
      catch {}
    }
    $results.Add([pscustomobject]@{
        Section = $Section; Path = $Path; Status = $code; Ok = $false; Preview = ''; Error = $err
      })
  }
}

$login = Invoke-RestMethod -Uri "$base/api/admin/auth/login" -Method POST `
  -Body (@{ usuario = 'admin'; password = 'Admin12!' } | ConvertTo-Json) `
  -ContentType 'application/json' -Headers @{ 'X-Tenant-Slug' = $slug }
$token = $login.accessToken
$h = @{ Authorization = "Bearer $token"; 'X-Tenant-Slug' = $slug; Accept = 'application/json' }
Write-Host "LOGIN OK super=$($login.isSuper) perms=$($login.permissions.Count)"

$checks = @(
  @{ S = 'Auth'; P = '/api/admin/auth/me' },
  @{ S = 'Dashboard'; P = '/api/admin/dashboard' },
  @{ S = 'Sync'; P = '/api/admin/sync/status' },
  @{ S = 'Empresa'; P = '/api/admin/empresa/config' },
  @{ S = 'Empresa'; P = '/api/admin/empresa/branding' },
  @{ S = 'Empresa'; P = '/api/admin/empresa/agencias' },
  @{ S = 'Empresa'; P = '/api/admin/empresa/depositos' },
  @{ S = 'Empresa'; P = '/api/admin/empresa/horarios' },
  @{ S = 'Personalizacion'; P = '/api/admin/personalizacion/schemas' },
  @{ S = 'Personalizacion'; P = '/api/admin/personalizacion/home.layout' },
  @{ S = 'Personalizacion'; P = '/api/admin/personalizacion/tema.activo' },
  @{ S = 'Personalizacion'; P = '/api/admin/personalizacion/checkout.textos' },
  @{ S = 'Personalizacion'; P = '/api/admin/personalizacion/widgets.toggles' },
  @{ S = 'CMS'; P = '/api/admin/content/blocks' },
  @{ S = 'CMS'; P = '/api/admin/content/menus' },
  @{ S = 'CMS'; P = '/api/admin/content/audit?take=20' },
  @{ S = 'Catalogo'; P = '/api/admin/products?page=1&pageSize=5' },
  @{ S = 'Parametros'; P = '/api/admin/parametros-producto?tipo=Marca' },
  @{ S = 'Parametros'; P = '/api/admin/parametros-producto?tipo=Linea' },
  @{ S = 'Parametros'; P = '/api/admin/parametros-producto?tipo=Tipo' },
  @{ S = 'Unidades'; P = '/api/admin/unidades-medida' },
  @{ S = 'Promociones'; P = '/api/admin/promotions' },
  @{ S = 'ListasPrecios'; P = '/api/admin/listas-precios' },
  @{ S = 'Tarifas'; P = '/api/admin/shipping/tarifas' },
  @{ S = 'Clientes'; P = '/api/admin/clientes?page=1&pageSize=5' },
  @{ S = 'Clientes'; P = '/api/admin/clientes/solicitudes-mayorista' },
  @{ S = 'Clientes'; P = '/api/admin/clientes/listas-vigentes' },
  @{ S = 'Carritos'; P = '/api/admin/carritos?page=1&pageSize=5' },
  @{ S = 'Pedidos'; P = '/api/admin/orders?page=1&pageSize=5' },
  @{ S = 'Choferes'; P = '/api/admin/choferes' },
  @{ S = 'Reportes'; P = '/api/admin/reports/ventas' },
  @{ S = 'Marketplace'; P = '/api/admin/resenas' },
  @{ S = 'Marketplace'; P = '/api/admin/cupones' },
  @{ S = 'Marketplace'; P = '/api/admin/devoluciones' },
  @{ S = 'CRM'; P = '/api/admin/chat/conversaciones' },
  @{ S = 'Empresas'; P = '/api/admin/tenants' },
  @{ S = 'Seguridad'; P = '/api/admin/manager/roles' },
  @{ S = 'Seguridad'; P = '/api/admin/manager/usuarios' },
  @{ S = 'Seguridad'; P = '/api/admin/manager/functionalidades' },
  @{ S = 'Seguridad'; P = '/api/admin/manager/operaciones-tree' },
  @{ S = 'Planillas'; P = '/api/admin/planillas/clientes/export?format=csv' }
)
foreach ($c in $checks) { Test-Admin $c.S 'GET' $c.P $h }

# Detail drills
try {
  $prods = Invoke-RestMethod -Uri "$base/api/admin/products?page=1&pageSize=1" -Headers $h
  $item = $null
  if ($prods.items) { $item = @($prods.items)[0] }
  elseif ($prods.Items) { $item = @($prods.Items)[0] }
  $productId = $null
  if ($item) { $productId = if ($null -ne $item.id) { [string]$item.id } else { [string]$item.Id } }
  if ($productId) {
    Test-Admin 'Catalogo' 'GET' "/api/admin/products/$productId" $h
    Test-Admin 'Catalogo' 'GET' "/api/admin/products/$productId/detail" $h
    Test-Admin 'Catalogo' 'GET' "/api/admin/products/$productId/images" $h
    Test-Admin 'Catalogo' 'GET' "/api/admin/products/$productId/embalajes" $h
    Test-Admin 'Catalogo' 'GET' "/api/admin/inventory/movimientos?idArticulo=$productId&take=10" $h
  }
}
catch {
  $results.Add([pscustomobject]@{ Section = 'Catalogo'; Path = 'detail-drill'; Status = 0; Ok = $false; Preview = ''; Error = $_.Exception.Message })
}

try {
  $ords = Invoke-RestMethod -Uri "$base/api/admin/orders?page=1&pageSize=1" -Headers $h
  $item = $null
  if ($ords.items) { $item = @($ords.items)[0] }
  elseif ($ords.Items) { $item = @($ords.Items)[0] }
  $orderId = $null
  if ($item) { $orderId = if ($null -ne $item.id) { [string]$item.id } else { [string]$item.Id } }
  if ($orderId) { Test-Admin 'Pedidos' 'GET' "/api/admin/orders/$orderId" $h }
}
catch {
  $results.Add([pscustomobject]@{ Section = 'Pedidos'; Path = 'detail-drill'; Status = 0; Ok = $false; Preview = ''; Error = $_.Exception.Message })
}

try {
  $cars = Invoke-RestMethod -Uri "$base/api/admin/carritos?page=1&pageSize=1" -Headers $h
  $item = $null
  if ($cars.items) { $item = @($cars.items)[0] }
  elseif ($cars.Items) { $item = @($cars.Items)[0] }
  $cartId = $null
  if ($item) { $cartId = if ($null -ne $item.id) { [string]$item.id } else { [string]$item.Id } }
  if ($cartId) { Test-Admin 'Carritos' 'GET' "/api/admin/carritos/$cartId" $h }
}
catch {
  $results.Add([pscustomobject]@{ Section = 'Carritos'; Path = 'detail-drill'; Status = 0; Ok = $false; Preview = ''; Error = $_.Exception.Message })
}

try {
  $listasRaw = Invoke-RestMethod -Uri "$base/api/admin/listas-precios" -Headers $h
  $listas = @($listasRaw)
  if ($listas.Count -gt 0) {
    $firstLista = $listas[0]
    $listaId = if ($null -ne $firstLista.id) { [string]$firstLista.id } else { [string]$firstLista.Id }
    if ($listaId -and $listaId -match '^\d+$') {
      Test-Admin 'ListasPrecios' 'GET' "/api/admin/listas-precios/$listaId" $h
      Test-Admin 'ListasPrecios' 'GET' "/api/admin/listas-precios/$listaId/detalles" $h
    }
  }
}
catch {
  $results.Add([pscustomobject]@{ Section = 'ListasPrecios'; Path = 'detail-drill'; Status = 0; Ok = $false; Preview = ''; Error = $_.Exception.Message })
}

# SPA build markers
$js = (Invoke-WebRequest -Uri 'http://127.0.0.1:4201/main.js' -UseBasicParsing -TimeoutSec 30).Content
$hasToggle = $js.Contains('toggleEmpresa')
$hasDataWidget = $js.Contains('data-widget="treeview"')
$results.Add([pscustomobject]@{
    Section = 'SPA-Build'
    Path    = '/main.js'
    Status  = 200
    Ok      = ($hasToggle -and -not $hasDataWidget)
    Preview = "len=$($js.Length)"
    Error   = $(if ($hasToggle -and -not $hasDataWidget) { '' } else { "toggleEmpresa=$hasToggle hasDataWidget=$hasDataWidget" })
  })

foreach ($r in @('/', '/configuracion', '/catalogo', '/pedidos', '/manager', '/marketplace', '/reportes', '/sync', '/personalizacion', '/contenido', '/clientes', '/listas-precios')) {
  try {
    $resp = Invoke-WebRequest -Uri "http://127.0.0.1:4201$r" -UseBasicParsing
    $ok = $resp.StatusCode -eq 200 -and $resp.Content.Contains('app-root')
    $results.Add([pscustomobject]@{
        Section = 'SPA'; Path = ":4201$r"; Status = [int]$resp.StatusCode; Ok = $ok; Preview = ''; Error = $(if ($ok) { '' } else { 'no app-root' })
      })
  }
  catch {
    $results.Add([pscustomobject]@{ Section = 'SPA'; Path = ":4201$r"; Status = 0; Ok = $false; Preview = ''; Error = $_.Exception.Message })
  }
}

Write-Host ''
$results | Sort-Object Section, Path | Format-Table -AutoSize Section, Status, Ok, Path, Error
$fail = @($results | Where-Object { -not $_.Ok })
$pass = @($results | Where-Object { $_.Ok })
Write-Host "PASS=$($pass.Count) FAIL=$($fail.Count) TOTAL=$($results.Count)"
if ($fail.Count) {
  Write-Host 'FAILURES:'
  $fail | Format-Table -AutoSize Section, Status, Path, Error
}

Write-Host ''
Write-Host '=== SECTION ROLLUP ==='
$results | Group-Object Section | ForEach-Object {
  $f = @($_.Group | Where-Object { -not $_.Ok }).Count
  $p = @($_.Group | Where-Object { $_.Ok }).Count
  [pscustomobject]@{ Section = $_.Name; Pass = $p; Fail = $f; Status = $(if ($f -eq 0) { 'OK' } else { 'FAIL' }) }
} | Sort-Object Section | Format-Table -AutoSize

$out = 'c:\Users\acantero\Desktop\dev-datamex\E-commerce\tools\_admin-smoke-test.json'
$results | ConvertTo-Json -Depth 4 | Set-Content $out -Encoding UTF8
Write-Host "Saved $out"
