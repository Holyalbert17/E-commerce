# QA continue-4: Bancard Stub simulate + favoritos + chat CRM + feeds
param(
  [string]$ApiBase = 'http://localhost:5082',
  [string]$Tenant = 'casa-imperial'
)

$ErrorActionPreference = 'Continue'
$results = New-Object System.Collections.Generic.List[object]
$started = Get-Date
$stamp = Get-Date -Format 'yyyyMMddHHmmss'
$userPass = 'QaE2e12!'

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
    $p = @{ Uri = ($ApiBase + $Path); Method = $Method; Headers = $Headers; UseBasicParsing = $true; TimeoutSec = 90 }
    if ($null -ne $Body) {
      $p.ContentType = 'application/json'
      $p.Body = ConvertTo-Json -InputObject $Body -Depth 8 -Compress
    }
    $r = Invoke-WebRequest @p
    $j = $null
    try { $j = $r.Content | ConvertFrom-Json } catch {}
    return @{ Ok=$true; Status=[int]$r.StatusCode; Json=$j; Raw=$r.Content }
  } catch {
    $code = $null; $raw = $null
    if ($_.Exception.Response) {
      $code = [int]$_.Exception.Response.StatusCode
      try { $raw = (New-Object IO.StreamReader($_.Exception.Response.GetResponseStream())).ReadToEnd() } catch {}
    }
    return @{ Ok=$false; Status=$code; Json=$null; Raw=$raw; Err=$_.Exception.Message }
  }
}

function Get-RootArray($json) {
  if ($null -eq $json) { return @() }
  if ($json -is [System.Array]) { return @($json) }
  if ($json.PSObject.Properties.Name -contains 'items') { return @($json.items) }
  return @($json)
}

Write-Host ""
Write-Host "=== QA CONTINUE-4: BANCARD STUB + FAVORITOS + CHAT + FEEDS ===" -ForegroundColor Cyan

$Hpub = @{ 'X-Tenant-Slug' = $Tenant; Accept = 'application/json' }

# --- Feeds públicos ---
foreach ($pair in @(
  @{ Id='F1'; Name='Feed mas-vendidos'; Path='/api/catalog/mas-vendidos?canal=B2C&page=1&pageSize=12' },
  @{ Id='F2'; Name='Feed ofertas'; Path='/api/catalog/ofertas?canal=B2C&page=1&pageSize=12' },
  @{ Id='F3'; Name='Feed recomendaciones'; Path='/api/engagement/recomendaciones?canal=B2C' }
)) {
  $r = Invoke-Api GET $pair.Path $Hpub
  if ($r.Ok) {
    $n = 0
    if ($r.Json.items) { $n = @($r.Json.items).Count }
    elseif ($r.Json -is [System.Array]) { $n = @($r.Json).Count }
    Add-Result $pair.Id $pair.Name 'PASS' ("n={0}" -f $n)
  } else {
    Add-Result $pair.Id $pair.Name 'FAIL' ("HTTP {0}" -f $r.Status)
  }
}

# --- Auth cliente ---
$session = "qa4-$stamp"
$tipos = Invoke-Api GET '/api/geography/tipos-documento' $Hpub
$tipoId = @(Get-RootArray $tipos.Json)[0].id
$email = "qa4.$stamp@example.com"
$doc = ("7{0}" -f $stamp.Substring($stamp.Length - 7))
$H0 = @{ 'X-Tenant-Slug'=$Tenant; Accept='application/json'; 'X-Cart-Session'=$session }

$reg = Invoke-Api POST '/api/auth/register' $H0 @{
  nombre='QA'; apellido='Four'; idTipoDocumento=$tipoId; numeroDocumento=$doc
  telefono='0215558888'; movil='0981888777'; email=$email
  password=$userPass; passwordConfirmation=$userPass; tenantSlug=$Tenant
}
$token = $null
if ($reg.Ok) {
  Add-Result 'B0' 'Register' 'PASS' $email
  $actUrl = $reg.Json.activationUrlDev
  if (-not $actUrl) { $actUrl = $reg.Json.ActivationUrlDev }
  $c=$null;$k=$null
  if ($actUrl -match '[?&]c=([^&]+)') { $c=$Matches[1] }
  if ($actUrl -match '[?&]k=([^&]+)') { $k=$Matches[1] }
  $act = Invoke-Api POST '/api/auth/activate' $H0 @{ codigo=$c; checksum=$k; tenantSlug=$Tenant }
  if ($act.Ok) {
    $token = $act.Json.accessToken
    if (-not $token) { $token = $act.Json.AccessToken }
    Add-Result 'B1' 'Activate' 'PASS' 'ok'
  } else { Add-Result 'B1' 'Activate' 'FAIL' $act.Raw }
} else {
  Add-Result 'B0' 'Register' 'FAIL' $reg.Raw
  Add-Result 'B1' 'Activate' 'SKIP' 'no register'
}

if (-not $token) {
  Write-Host "No token - abort" -ForegroundColor Red
  exit 2
}

$HU = @{ Authorization="Bearer $token"; 'X-Tenant-Slug'=$Tenant; Accept='application/json'; 'X-Cart-Session'=$session }
$AH = $null
$al = Invoke-Api POST '/api/admin/auth/login' @{ 'X-Tenant-Slug' = $Tenant } @{ usuario = 'admin'; password = 'Admin12!' }
if ($al.Ok) {
  $atok = $al.Json.accessToken; if (-not $atok) { $atok = $al.Json.token }
  $AH = @{ Authorization = "Bearer $atok"; 'X-Tenant-Slug' = $Tenant; Accept = 'application/json' }
  Add-Result 'B2' 'Admin login' 'PASS' 'ok'
} else {
  Add-Result 'B2' 'Admin login' 'FAIL' $al.Raw
}

# --- Favoritos ---
$prods = Invoke-Api GET '/api/catalog/products?canal=B2C&page=1&pageSize=24' $HU
$pick = @(Get-RootArray $prods.Json.items) | Where-Object { $_.stockOk -eq $true } | Select-Object -First 1
if (-not $pick) { $pick = @(Get-RootArray $prods.Json.items)[0] }
$artId = [long]$pick.id

$sec = Invoke-Api POST '/api/favoritos/secciones' $HU @{ nombreLista = "QA4 $stamp"; color = '#336699'; canal = 'B2C' }
if ($sec.Ok) {
  $secId = $sec.Json.id
  Add-Result 'V1' 'Favoritos crear seccion' 'PASS' ("id={0}" -f $secId)
} else {
  Add-Result 'V1' 'Favoritos crear seccion' 'FAIL' ("HTTP {0} {1}" -f $sec.Status, $sec.Raw)
  $secId = $null
}

$addFav = Invoke-Api POST '/api/favoritos/items' $HU @{ idArticulo = $artId; idLista = $secId; nota = 'qa4' }
if ($addFav.Ok) {
  Add-Result 'V2' 'Favoritos agregar item' 'PASS' ("art={0}" -f $artId)
} else {
  Add-Result 'V2' 'Favoritos agregar item' 'FAIL' ("HTTP {0} {1}" -f $addFav.Status, $addFav.Raw)
}

$listFav = Invoke-Api GET '/api/favoritos?canal=B2C' $HU
if ($listFav.Ok) {
  Add-Result 'V3' 'Favoritos listar' 'PASS' ("secciones={0}" -f @(Get-RootArray $listFav.Json).Count)
} else {
  Add-Result 'V3' 'Favoritos listar' 'FAIL' ("HTTP {0}" -f $listFav.Status)
}

# --- Chat cliente + admin CRM ---
$chat = Invoke-Api POST '/api/engagement/chat' $HU @{
  mensaje = "QA4 soporte $stamp"
  asunto = 'consulta'
  forzarNueva = $true
}
$convId = $null
if ($chat.Ok) {
  $convId = $chat.Json.idConversacion
  if (-not $convId) { $convId = $chat.Json.IDConversacion }
  Add-Result 'C1' 'Cliente envia chat' 'PASS' ("conv={0}" -f $convId)
} else {
  Add-Result 'C1' 'Cliente envia chat' 'FAIL' ("HTTP {0} {1}" -f $chat.Status, $chat.Raw)
}

if ($AH -and $convId) {
  $adminList = Invoke-Api GET '/api/admin/chat/conversaciones' $AH
  if ($adminList.Ok) {
    Add-Result 'C2' 'Admin lista chats' 'PASS' 'ok'
  } else {
    Add-Result 'C2' 'Admin lista chats' 'FAIL' ("HTTP {0} {1}" -f $adminList.Status, $adminList.Raw)
  }

  $resp = Invoke-Api POST ("/api/admin/chat/conversaciones/{0}/responder" -f $convId) $AH @{
    mensaje = "QA4 admin reply $stamp"
  }
  if ($resp.Ok -or $resp.Status -eq 200 -or $resp.Status -eq 204) {
    Add-Result 'C3' 'Admin responde chat' 'PASS' ("conv={0}" -f $convId)
  } else {
    Add-Result 'C3' 'Admin responde chat' 'FAIL' ("HTTP {0} {1}" -f $resp.Status, $resp.Raw)
  }
} else {
  Add-Result 'C2' 'Admin lista chats' 'SKIP' 'no admin/conv'
  Add-Result 'C3' 'Admin responde chat' 'SKIP' 'no admin/conv'
}

# --- Bancard Stub: cart + pickup + checkout online + simulate ---
$det = Invoke-Api GET ("/api/catalog/products/{0}?canal=B2C" -f $artId) $HU
$emb = @(Get-RootArray $det.Json.embalajes)[0]
$embId = $emb.id; if (-not $embId) { $embId = $emb.idArticuloEmbalaje }
$add = Invoke-Api POST '/api/cart/items' $HU @{ idArticulo=$artId; cantidad=1; idArticuloEmbalaje=$embId; canal='B2C' }
if ($add.Ok) { Add-Result 'B3' 'Add to cart' 'PASS' ("total={0}" -f $add.Json.montoTotal) }
else { Add-Result 'B3' 'Add to cart' 'FAIL' $add.Raw }

# Widen horario pickup
$dia = [int](Get-Date).DayOfWeek
$backup = $null
if ($AH) {
  $hors = Invoke-Api GET '/api/admin/empresa/horarios?idAgencia=79' $AH
  $todayH = @(Get-RootArray $hors.Json | Where-Object { $_.diaSemana -eq $dia } | Select-Object -First 1)
  if ($todayH) {
    $backup = $todayH
    Invoke-Api PUT ("/api/admin/empresa/horarios/{0}" -f $todayH.id) $AH @{
      idAgencia=[long]$todayH.idAgencia; diaSemana=[byte]$dia
      horaPrimeraApertura='08:00'; horaPrimerCierre='20:00'
      horaSegundaApertura=''; horaSegundoCierre=''; activo=$true
    } | Out-Null
  }
}
Invoke-Api PUT '/api/cart/pickup' $HU @{ idAgencia = 79 } | Out-Null

$co = Invoke-Api POST '/api/checkout' $HU @{
  delivery = $false
  pagarEnLaEntrega = $false
  razonSocial = 'QA Four'
  numeroDocumento = $doc
  nombre = 'QA'
  apellido = 'Four'
  email = $email
  nroTelefono = '0981888777'
  observacion = "QA4 bancard $stamp"
  tipoComprobante = 'factura'
}

if ($backup -and $AH) {
  Invoke-Api PUT ("/api/admin/empresa/horarios/{0}" -f $backup.id) $AH @{
    idAgencia=[long]$backup.idAgencia; diaSemana=[byte]$backup.diaSemana
    horaPrimeraApertura=$backup.horaPrimeraApertura; horaPrimerCierre=$backup.horaPrimerCierre
    horaSegundaApertura=$(if($backup.horaSegundaApertura){$backup.horaSegundaApertura}else{''})
    horaSegundoCierre=$(if($backup.horaSegundoCierre){$backup.horaSegundoCierre}else{''})
    activo=$true
  } | Out-Null
}

if (-not $co.Ok) {
  Add-Result 'B4' 'Checkout online (Stub)' 'FAIL' ("HTTP {0} {1}" -f $co.Status, $co.Raw)
} else {
  $nro = $co.Json.nroPedido; if (-not $nro) { $nro = $co.Json.NroPedido }
  $reqPay = $co.Json.requierePagoOnline
  $mode = $co.Json.bancardMode
  Add-Result 'B4' 'Checkout online (Stub)' 'PASS' ("nro={0} requierePagoOnline={1} mode={2}" -f $nro, $reqPay, $mode)

  $sess = Invoke-Api GET ("/api/payments/bancard/session/{0}" -f $nro) $HU
  if (-not $sess.Ok) {
    Add-Result 'B5' 'Bancard session' 'FAIL' ("HTTP {0} {1}" -f $sess.Status, $sess.Raw)
    Add-Result 'B6' 'Simulate bancard success' 'SKIP' 'no session'
  } else {
    $spid = $sess.Json.shopProcessId
    Add-Result 'B5' 'Bancard session' 'PASS' ("shopProcessId={0} mode={1} completed={2}" -f $spid, $sess.Json.bancardMode, $sess.Json.pagoCompletado)
    if ($spid -and -not $sess.Json.pagoCompletado) {
      $sim = Invoke-Api POST '/api/payments/bancard/simulate-success' $HU @{ shopProcessId = [long]$spid }
      if ($sim.Ok) {
        Add-Result 'B6' 'Simulate bancard success' 'PASS' ("nro={0} msg={1}" -f $sim.Json.nroPedido, $sim.Json.mensaje)
      } else {
        Add-Result 'B6' 'Simulate bancard success' 'FAIL' ("HTTP {0} {1}" -f $sim.Status, $sim.Raw)
      }
    } elseif ($mode -eq 'Off' -or $mode -eq 'off') {
      Add-Result 'B6' 'Simulate bancard success' 'SKIP' 'Bancard Mode=Off - set Stub + restart'
    } else {
      Add-Result 'B6' 'Simulate bancard success' 'SKIP' 'already paid or no shopProcessId'
    }
  }
}

$pCount = @($results | Where-Object Status -eq 'PASS').Count
$fCount = @($results | Where-Object Status -eq 'FAIL').Count
$sCount = @($results | Where-Object Status -eq 'SKIP').Count
$elapsed = [math]::Round(((Get-Date) - $started).TotalSeconds, 1)
Write-Host ""
Write-Host "=== SUMMARY ===" -ForegroundColor Cyan
Write-Host ("PASS={0} FAIL={1} SKIP={2} total={3} {4}s" -f $pCount, $fCount, $sCount, $results.Count, $elapsed)

$outDir = Join-Path (Split-Path $PSScriptRoot -Parent) 'docs'
$md = Join-Path $outDir 'QA-CONTINUE-4-RESULTS.md'
$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('# QA Continue-4 - Bancard Stub + Favoritos + Chat + Feeds')
[void]$sb.AppendLine('')
[void]$sb.AppendLine(("**Ejecutado:** {0}" -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss')))
[void]$sb.AppendLine(("**API:** {0} - Tenant: {1}" -f $ApiBase, $Tenant))
[void]$sb.AppendLine(("**Score:** PASS **{0}** - FAIL **{1}** - SKIP **{2}** - {3}s" -f $pCount, $fCount, $sCount, $elapsed))
[void]$sb.AppendLine('')
[void]$sb.AppendLine(( '| Id | Caso | Status | Detalle |' ))
[void]$sb.AppendLine(( '|----|------|--------|---------|' ))
foreach ($row in $results) {
  $d = [string]$row.Detail
  $d = $d.Replace('|', '/')
  $d = ($d -replace "[\r\n]+", ' ')
  if ($d.Length -gt 200) { $d = $d.Substring(0, 200) + '...' }
  [void]$sb.AppendLine(("| {0} | {1} | **{2}** | {3} |" -f $row.Id, $row.Name, $row.Status, $d))
}
[void]$sb.AppendLine('')
[void]$sb.AppendLine('## Fallos')
$fails = @($results | Where-Object Status -eq 'FAIL')
if ($fails.Count -eq 0) { [void]$sb.AppendLine('_Ninguno._') }
else {
  foreach ($f in $fails) {
    $fd = ([string]$f.Detail -replace "[\r\n]+", ' ')
    [void]$sb.AppendLine(("- **{0}** {1}: {2}" -f $f.Id, $f.Name, $fd))
  }
}
[System.IO.File]::WriteAllText($md, $sb.ToString(), [System.Text.UTF8Encoding]::new($false))
Write-Host "Wrote $md"
if ($fCount -gt 0) { exit 2 } else { exit 0 }
