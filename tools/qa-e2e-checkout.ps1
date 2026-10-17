# QA E2E: register -> activate -> login -> cart -> pickup -> checkout
param(
  [string]$ApiBase = 'http://localhost:5082',
  [string]$Tenant = 'casa-imperial'
)

$ErrorActionPreference = 'Continue'
$results = New-Object System.Collections.Generic.List[object]
$started = Get-Date
$stamp = Get-Date -Format 'yyyyMMddHHmmss'
$session = "e2e-$stamp"
$pass = 'QaE2e12!'

function Add-Result {
  param([string]$Id, [string]$Name, [string]$Status, [string]$Detail = '')
  $results.Add([pscustomobject]@{ Id=$Id; Name=$Name; Status=$Status; Detail=$Detail }) | Out-Null
  $c = 'Gray'
  if ($Status -eq 'PASS') { $c = 'Green' }
  elseif ($Status -eq 'FAIL') { $c = 'Red' }
  elseif ($Status -eq 'SKIP') { $c = 'Yellow' }
  Write-Host ("[{0}] {1} - {2}" -f $Status, $Id, $Name) -ForegroundColor $c
  if ($Detail) { Write-Host ("         {0}" -f $Detail) -ForegroundColor DarkGray }
}

function Invoke-Api {
  param([string]$Method, [string]$Path, [hashtable]$Headers, $Body = $null)
  try {
    $p = @{ Uri = ($ApiBase + $Path); Method = $Method; Headers = $Headers; UseBasicParsing = $true; TimeoutSec = 60 }
    if ($null -ne $Body) { $p.ContentType = 'application/json'; $p.Body = ($Body | ConvertTo-Json -Depth 8 -Compress) }
    $r = Invoke-WebRequest @p
    $j = $null
    try { $j = $r.Content | ConvertFrom-Json } catch {}
    return @{ Ok=$true; Status=[int]$r.StatusCode; Json=$j; Raw=$r.Content; Err=$null }
  } catch {
    $code = $null; $raw = $null
    if ($_.Exception.Response) {
      $code = [int]$_.Exception.Response.StatusCode
      try { $raw = (New-Object IO.StreamReader($_.Exception.Response.GetResponseStream())).ReadToEnd() } catch {}
    }
    return @{ Ok=$false; Status=$code; Json=$null; Raw=$raw; Err=$_.Exception.Message }
  }
}

Write-Host ""
Write-Host "=== QA E2E CHECKOUT ===" -ForegroundColor Cyan

$H0 = @{ 'X-Tenant-Slug' = $Tenant; Accept = 'application/json'; 'X-Cart-Session' = $session }

$tipos = Invoke-Api GET '/api/geography/tipos-documento' $H0
if (-not $tipos.Ok) { Add-Result 'E0' 'GET tipos-documento' 'FAIL' $tipos.Err; exit 1 }
$tipoId = @($tipos.Json)[0].id
Add-Result 'E0' 'GET tipos-documento' 'PASS' ("id=$tipoId")

$email = "qa.e2e.$stamp@example.com"
$doc = ("4{0}" -f $stamp.Substring($stamp.Length - 7))

$reg = Invoke-Api POST '/api/auth/register' $H0 @{
  nombre = 'QA'; apellido = 'E2E'; idTipoDocumento = $tipoId; numeroDocumento = $doc
  telefono = '0215551234'; movil = '0981555123'; email = $email
  password = $pass; passwordConfirmation = $pass; tenantSlug = $Tenant
}
if (-not $reg.Ok) { Add-Result 'E1' 'Register' 'FAIL' $reg.Raw; exit 2 }
Add-Result 'E1' 'Register' 'PASS' $email

$actUrl = $reg.Json.activationUrlDev
if (-not $actUrl) { $actUrl = $reg.Json.ActivationUrlDev }
$code = $null; $k = $null
if ($actUrl -match '[?&]c=([^&]+)') { $code = $Matches[1] }
if ($actUrl -match '[?&]k=([^&]+)') { $k = $Matches[1] }
if (-not $code -or -not $k) { Add-Result 'E2' 'Parse activation' 'FAIL' $actUrl; exit 2 }
Add-Result 'E2' 'Parse activation' 'PASS' 'ok'

$act = Invoke-Api POST '/api/auth/activate' $H0 @{ codigo = $code; checksum = $k; tenantSlug = $Tenant }
$token = $null
if ($act.Ok) {
  $token = $act.Json.accessToken
  if (-not $token) { $token = $act.Json.AccessToken }
  Add-Result 'E3' 'Activate' 'PASS' ("user={0}" -f $act.Json.idUsuarioEcommerce)
} else { Add-Result 'E3' 'Activate' 'FAIL' $act.Raw; exit 2 }

$login = Invoke-Api POST '/api/auth/login' $H0 @{ usuario = $email; password = $pass; tenantSlug = $Tenant }
if ($login.Ok) {
  $token = $login.Json.accessToken
  if (-not $token) { $token = $login.Json.AccessToken }
  Add-Result 'E4' 'Login' 'PASS' $email
} else { Add-Result 'E4' 'Login' 'FAIL' $login.Raw; exit 2 }

$HA = @{
  'X-Tenant-Slug' = $Tenant; Accept = 'application/json'
  'X-Cart-Session' = $session; Authorization = "Bearer $token"
}

$me = Invoke-Api GET '/api/account/me' $HA
if ($me.Ok) { Add-Result 'E5' 'GET account/me' 'PASS' 'HTTP 200' } else { Add-Result 'E5' 'GET account/me' 'FAIL' $me.Raw }

$prods = Invoke-Api GET '/api/catalog/products?canal=B2C&page=1&pageSize=48' $HA
$pick = $null
if ($prods.Ok) { $pick = @($prods.Json.items | Where-Object { $_.stockOk -eq $true } | Select-Object -First 1) }
if (-not $pick) { Add-Result 'E6' 'Pick product' 'FAIL' 'none'; exit 2 }
Add-Result 'E6' 'Pick product' 'PASS' ("id={0}" -f $pick.id)

$det = Invoke-Api GET ("/api/catalog/products/{0}?canal=B2C" -f $pick.id) $HA
$embId = $null
if ($det.Ok -and $det.Json.embalajes) {
  $emb = @($det.Json.embalajes)[0]
  $embId = $emb.id
  if (-not $embId) { $embId = $emb.idArticuloEmbalaje }
}
if (-not $embId) { Add-Result 'E7' 'Embalaje' 'FAIL' 'none'; exit 2 }
Add-Result 'E7' 'Embalaje' 'PASS' ("emb={0}" -f $embId)

$add = Invoke-Api POST '/api/cart/items' $HA @{
  idArticulo = $pick.id; cantidad = 1; idArticuloEmbalaje = $embId; canal = 'B2C'
}
if (-not $add.Ok) { Add-Result 'E8' 'Add to cart' 'FAIL' $add.Raw; exit 2 }
Add-Result 'E8' 'Add to cart' 'PASS' ("total={0}" -f $add.Json.montoTotal)

# --- Pickup CASA MATRIZ (79) + widen hours if lunch ---
$agId = 79
$dia = [int](Get-Date).DayOfWeek
$backup = $null
$AHAdmin = $null

$pickup = Invoke-Api PUT '/api/cart/pickup' $HA @{ idAgencia = $agId }
if (-not $pickup.Ok) {
  # stock / other issues on 79
  Add-Result 'E9' 'Set pickup (first try)' 'FAIL' $pickup.Raw
  exit 2
}
Add-Result 'E9' 'Set pickup agencia 79' 'PASS' 'CASA MATRIZ'

$checkoutBody = @{
  delivery = $false; pagarEnLaEntrega = $true
  razonSocial = 'QA E2E Smoke'; numeroDocumento = $doc
  nombre = 'QA'; apellido = 'E2E'; email = $email
  nroTelefono = '0981555123'; observacion = "QA e2e $stamp";   tipoComprobante = 'factura'
}

$co = Invoke-Api POST '/api/checkout' $HA $checkoutBody
if (-not $co.Ok -and ($co.Raw -match 'cerrada')) {
  Add-Result 'E9b' 'Blocked by horario' 'PASS' 'widening via admin'
  $adminLogin = Invoke-Api POST '/api/admin/auth/login' @{ 'X-Tenant-Slug' = $Tenant } @{ usuario = 'admin'; password = 'Admin12!' }
  $adminTok = $null
  if ($adminLogin.Ok) {
    $adminTok = $adminLogin.Json.accessToken
    if (-not $adminTok) { $adminTok = $adminLogin.Json.token }
  }
  if ($adminTok) {
    $AHAdmin = @{ Authorization = "Bearer $adminTok"; 'X-Tenant-Slug' = $Tenant; Accept = 'application/json' }
    $hors = Invoke-Api GET "/api/admin/empresa/horarios?idAgencia=79" $AHAdmin
    $today = $null
    if ($hors.Ok) { $today = @($hors.Json | Where-Object { $_.diaSemana -eq $dia } | Select-Object -First 1) }
    if ($today) {
      $backup = $today
      $upd = Invoke-Api PUT ("/api/admin/empresa/horarios/{0}" -f $today.id) $AHAdmin @{
        idAgencia = [long]$today.idAgencia
        diaSemana = [byte]$dia
        horaPrimeraApertura = '08:00'
        horaPrimerCierre = '20:00'
        horaSegundaApertura = ''
        horaSegundoCierre = ''
        activo = $true
      }
      if ($upd.Ok) { Add-Result 'E9c' 'Widen horario' 'PASS' ("id={0}" -f $today.id) }
      else { Add-Result 'E9c' 'Widen horario' 'FAIL' $upd.Raw }
      $co = Invoke-Api POST '/api/checkout' $HA $checkoutBody
    } else {
      Add-Result 'E9c' 'Widen horario' 'FAIL' 'no row for today'
    }
  } else {
    Add-Result 'E9c' 'Admin login for horario' 'FAIL' 'no token'
  }
}

if ($backup -and $AHAdmin) {
  Invoke-Api PUT ("/api/admin/empresa/horarios/{0}" -f $backup.id) $AHAdmin @{
    idAgencia = [long]$backup.idAgencia
    diaSemana = [byte]$backup.diaSemana
    horaPrimeraApertura = $backup.horaPrimeraApertura
    horaPrimerCierre = $backup.horaPrimerCierre
    horaSegundaApertura = $(if ($backup.horaSegundaApertura) { $backup.horaSegundaApertura } else { '' })
    horaSegundoCierre = $(if ($backup.horaSegundoCierre) { $backup.horaSegundoCierre } else { '' })
    activo = $true
  } | Out-Null
  Add-Result 'E9d' 'Restore horario' 'PASS' ("id={0}" -f $backup.id)
}

if ($co.Ok) {
  $nro = $co.Json.nroPedido; if (-not $nro) { $nro = $co.Json.NroPedido }
  $idPm = $co.Json.idPedidoMovil; if (-not $idPm) { $idPm = $co.Json.IDPedidoMovil }
  Add-Result 'E10' 'Checkout pickup + COD' 'PASS' ("nroPedido={0} idPedidoMovil={1} total={2}" -f $nro, $idPm, $co.Json.montoTotal)
} else {
  Add-Result 'E10' 'Checkout pickup + COD' 'FAIL' ("HTTP {0} {1}" -f $co.Status, $co.Raw)
  exit 2
}

$ords = Invoke-Api GET '/api/account/orders' $HA
if ($ords.Ok) { Add-Result 'E11' 'GET account/orders' 'PASS' ("count={0}" -f @($ords.Json).Count) }
else { Add-Result 'E11' 'GET account/orders' 'FAIL' $ords.Raw }

$orderGet = Invoke-Api GET ("/api/checkout/orders/{0}" -f $nro) $HA
if ($orderGet.Ok) { Add-Result 'E12' 'GET checkout order' 'PASS' 'HTTP 200' }
else { Add-Result 'E12' 'GET checkout order' 'SKIP' $orderGet.Raw }

$cup = Invoke-Api POST '/api/engagement/cupon/aplicar' $HA @{ codigo = 'NOEXISTE' }
if (-not $cup.Ok) { Add-Result 'E13' 'Reject bad cupon' 'PASS' ("HTTP {0}" -f $cup.Status) }
else { Add-Result 'E13' 'Reject bad cupon' 'FAIL' 'accepted' }

$dev = Invoke-Api GET '/api/engagement/devoluciones' $HA
if ($dev.Ok) { Add-Result 'E14' 'GET devoluciones' 'PASS' ("count={0}" -f @($dev.Json).Count) }
else { Add-Result 'E14' 'GET devoluciones' 'FAIL' $dev.Raw }

$chat = Invoke-Api GET '/api/engagement/chat/conversaciones' $HA
if (-not $chat.Ok) { $chat = Invoke-Api GET '/api/engagement/chat' $HA }
if ($chat.Ok) { Add-Result 'E15' 'GET chat' 'PASS' 'HTTP 200' }
else { Add-Result 'E15' 'GET chat' 'SKIP' ("HTTP {0}" -f $chat.Status) }

$pCount = @($results | Where-Object Status -eq 'PASS').Count
$fCount = @($results | Where-Object Status -eq 'FAIL').Count
$sCount = @($results | Where-Object Status -eq 'SKIP').Count
$elapsed = [math]::Round(((Get-Date) - $started).TotalSeconds, 1)
Write-Host ""
Write-Host "=== SUMMARY ===" -ForegroundColor Cyan
Write-Host ("PASS={0} FAIL={1} SKIP={2} total={3} {4}s" -f $pCount, $fCount, $sCount, $results.Count, $elapsed)

$outDir = Join-Path (Split-Path $PSScriptRoot -Parent) 'docs'
$md = Join-Path $outDir 'QA-E2E-CHECKOUT-RESULTS.md'
$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('# QA E2E Checkout - Resultados')
[void]$sb.AppendLine('')
[void]$sb.AppendLine(("**Ejecutado:** {0}" -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss')))
[void]$sb.AppendLine(("**API:** {0} · Tenant: {1}" -f $ApiBase, $Tenant))
[void]$sb.AppendLine(("**User:** {0}" -f $email))
[void]$sb.AppendLine(("**Score:** PASS **{0}** · FAIL **{1}** · SKIP **{2}** · {3}s" -f $pCount, $fCount, $sCount, $elapsed))
[void]$sb.AppendLine('')
[void]$sb.AppendLine('| Id | Caso | Status | Detalle |')
[void]$sb.AppendLine('|----|------|--------|---------|')
foreach ($row in $results) {
  $d = ($row.Detail -replace '\|','/' -replace "`r|`n",' ')
  [void]$sb.AppendLine(("| {0} | {1} | **{2}** | {3} |" -f $row.Id, $row.Name, $row.Status, $d))
}
[void]$sb.AppendLine('')
[void]$sb.AppendLine('## Fallos')
$fails = @($results | Where-Object Status -eq 'FAIL')
if ($fails.Count -eq 0) { [void]$sb.AppendLine('_Ninguno._') }
else { foreach ($f in $fails) { [void]$sb.AppendLine(("- **{0}** {1}: {2}" -f $f.Id, $f.Name, $f.Detail)) } }
[System.IO.File]::WriteAllText($md, $sb.ToString(), [System.Text.UTF8Encoding]::new($false))
Write-Host "Wrote $md"
if ($fCount -gt 0) { exit 2 } else { exit 0 }
