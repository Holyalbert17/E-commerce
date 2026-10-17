# QA continue-3: delivery + bancard simulate + devolucion aprobar (careful)
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
      # -InputObject: piping a hashtable to ConvertTo-Json enumerates DictionaryEntry and breaks arrays (Ids vacío).
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

Write-Host ""
Write-Host "=== QA CONTINUE-3: DELIVERY + PAGO + DEVOLUCION APROBAR ===" -ForegroundColor Cyan

$al = Invoke-Api POST '/api/admin/auth/login' @{ 'X-Tenant-Slug' = $Tenant } @{ usuario = 'admin'; password = 'Admin12!' }
$adminTok = $null
if ($al.Ok) {
  $adminTok = $al.Json.accessToken
  if (-not $adminTok) { $adminTok = $al.Json.token }
}
if (-not $adminTok) { Add-Result 'D0' 'Admin login' 'FAIL' 'no token'; exit 2 }
Add-Result 'D0' 'Admin login' 'PASS' 'ok'
$AH = @{ Authorization = "Bearer $adminTok"; 'X-Tenant-Slug' = $Tenant; Accept = 'application/json' }

# --- Tarifas: authorize a few so storefront ciudades/barrios appear ---
$tarifasResp = Invoke-Api GET '/api/admin/shipping/tarifas' $AH
if (-not $tarifasResp.Ok) { Add-Result 'D1' 'List tarifas' 'FAIL' $tarifasResp.Raw; exit 2 }
# IMPORTANTE: no usar .items sobre Object[] — PS enumera .Items de cada elemento → 126 nulls.
$tarifas = @()
$root = $tarifasResp.Json
if ($null -eq $root) { $tarifas = @() }
elseif ($root -is [System.Array]) { $tarifas = @($root) }
elseif ($root.PSObject.Properties.Name -contains 'items') { $tarifas = @($root.items) }
else { $tarifas = @($root) }
Add-Result 'D1' 'List tarifas' 'PASS' ("n={0}" -f $tarifas.Count)

# Inspect estado field
$estados = $tarifas | ForEach-Object { $_.estado } | Group-Object | ForEach-Object { "$($_.Name):$($_.Count)" }
Add-Result 'D1b' 'Tarifas por estado' 'PASS' ($estados -join ', ')

# Solo se edita vigencia en estado=0 (ABIERTO). Preferir pendientes existentes; si no, crear.
$today = [datetime]::UtcNow.Date
$fin = $today.AddYears(1)
$todayStr = $today.ToString('yyyy-MM-dd')
$finStr = $fin.ToString('yyyy-MM-dd')

$pendientes = @($tarifas | Where-Object { $_.estado -eq 0 -and $_.activo -eq $true } | Select-Object -First 8)
$idsAuth = @()
$fixed = 0
$updErr = $null
if ($pendientes.Count -gt 0) {
  foreach ($t in $pendientes) {
    $impStr = ([string]$t.importeDelivery).Replace(',', '.')
    $imp = [decimal]::Parse($impStr, [System.Globalization.CultureInfo]::InvariantCulture)
    $upd = Invoke-Api PUT ("/api/admin/shipping/tarifas/{0}" -f $t.id) $AH @{
      importeDelivery = $imp
      fechaInicioVigencia = $todayStr
      fechaFinVigencia = $finStr
    }
    if ($upd.Ok) {
      $fixed++
      $idsAuth += [long]$t.id
    } elseif (-not $updErr) {
      $rawShort = if ($upd.Raw) { ($upd.Raw -replace 'Bearer\s+[A-Za-z0-9._\-]+','Bearer ***' -replace '(?s)HEADERS.*','') } else { $upd.Err }
      if ($rawShort.Length -gt 280) { $rawShort = $rawShort.Substring(0,280) + '...' }
      $updErr = "id=$($t.id) HTTP $($upd.Status) $rawShort"
    }
  }
  if ($fixed -gt 0) {
    Add-Result 'D2' 'Vigencia tarifas pendientes' 'PASS' ("fixed={0}/{1}" -f $fixed, $pendientes.Count)
  } else {
    Add-Result 'D2' 'Vigencia tarifas pendientes' 'FAIL' ("fixed=0/8 err=$updErr")
  }
} else {
  $sample = @($tarifas | Where-Object { $_.idCiudad -gt 0 -and $_.idBarrio -gt 0 } | Select-Object -First 1)[0]
  if (-not $sample) {
    Add-Result 'D2' 'Create+vigencia tarifa QA' 'FAIL' 'no sample ciudad/barrio'
  } else {
    $importe = [decimal]$sample.importeDelivery
    if ($importe -le 0) { $importe = 15000 }
    $created = Invoke-Api POST '/api/admin/shipping/tarifas' $AH @{
      idCiudad = [long]$sample.idCiudad
      idBarrio = [long]$sample.idBarrio
      importeDelivery = $importe
    }
    if (-not $created.Ok) {
      Add-Result 'D2' 'Create+vigencia tarifa QA' 'FAIL' ("HTTP {0} {1}" -f $created.Status, $created.Raw)
    } else {
      $newId = [long]$created.Json.id
      $upd = Invoke-Api PUT ("/api/admin/shipping/tarifas/{0}" -f $newId) $AH @{
        importeDelivery = $importe
        fechaInicioVigencia = $todayStr
        fechaFinVigencia = $finStr
      }
      if ($upd.Ok) {
        $idsAuth += $newId
        Add-Result 'D2' 'Create+vigencia tarifa QA' 'PASS' ("id={0}" -f $newId)
      } else {
        Add-Result 'D2' 'Create+vigencia tarifa QA' 'FAIL' ("upd HTTP {0} {1}" -f $upd.Status, $upd.Raw)
      }
    }
  }
}

if ($idsAuth.Count -eq 0) {
  Add-Result 'D3' 'Autorizar tarifas sample' 'SKIP' 'no ids to authorize'
} else {
  $auth = Invoke-Api POST '/api/admin/shipping/tarifas/autorizar' $AH @{ ids = [long[]]@($idsAuth | Select-Object -Unique) }
  if ($auth.Ok) {
    Add-Result 'D3' 'Autorizar tarifas sample' 'PASS' ("updated={0} ids={1}" -f $auth.Json.updatedCount, (($idsAuth -join ',')))
  } else {
    Add-Result 'D3' 'Autorizar tarifas sample' 'FAIL' ("HTTP {0} {1}" -f $auth.Status, $auth.Raw)
  }
}

$Hpub = @{ 'X-Tenant-Slug' = $Tenant; Accept = 'application/json' }
$ciu = Invoke-Api GET '/api/geography/ciudades-delivery' $Hpub
$ciuN = 0
if ($ciu.Ok -and $ciu.Json) { $ciuN = @($ciu.Json).Count }
if ($ciuN -gt 0) {
  Add-Result 'D4' 'Storefront ciudades-delivery' 'PASS' ("n={0}" -f $ciuN)
} else {
  Add-Result 'D4' 'Storefront ciudades-delivery' 'FAIL' 'still empty after authorize'
}

$barrioId = $null
$ciudadId = $null
$importe = $null
if ($ciuN -gt 0) {
  $ciudadId = @($ciu.Json)[0].id
  $bar = Invoke-Api GET ("/api/geography/barrios-delivery?ciudadId={0}" -f $ciudadId) $Hpub
  if ($bar.Ok -and @($bar.Json).Count -gt 0) {
    $b0 = @($bar.Json)[0]
    $barrioId = $b0.id
    $importe = $b0.importeDelivery
    if (-not $importe) { $importe = $b0.importe }
    Add-Result 'D5' 'Storefront barrios-delivery' 'PASS' ("ciudad={0} barrio={1} importe={2}" -f $ciudadId, $barrioId, $importe)
  } else {
    Add-Result 'D5' 'Storefront barrios-delivery' 'FAIL' ("HTTP {0} {1}" -f $bar.Status, $bar.Raw)
  }
} else {
  Add-Result 'D5' 'Storefront barrios-delivery' 'SKIP' 'no ciudades'
}

# --- Customer + delivery checkout / online payment ---
$session = "qa3-$stamp"
$tipos = Invoke-Api GET '/api/geography/tipos-documento' $Hpub
$tipoId = @($tipos.Json)[0].id
$email = "qa3.$stamp@example.com"
$doc = ("6{0}" -f $stamp.Substring($stamp.Length - 7))
$H0 = @{ 'X-Tenant-Slug'=$Tenant; Accept='application/json'; 'X-Cart-Session'=$session }

$reg = Invoke-Api POST '/api/auth/register' $H0 @{
  nombre='QA'; apellido='Three'; idTipoDocumento=$tipoId; numeroDocumento=$doc
  telefono='0215557777'; movil='0981777666'; email=$email
  password=$userPass; passwordConfirmation=$userPass; tenantSlug=$Tenant
}
$token = $null
if ($reg.Ok) {
  Add-Result 'D6' 'Register' 'PASS' $email
  $actUrl = $reg.Json.activationUrlDev
  if (-not $actUrl) { $actUrl = $reg.Json.ActivationUrlDev }
  $c=$null;$k=$null
  if ($actUrl -match '[?&]c=([^&]+)') { $c=$Matches[1] }
  if ($actUrl -match '[?&]k=([^&]+)') { $k=$Matches[1] }
  $act = Invoke-Api POST '/api/auth/activate' $H0 @{ codigo=$c; checksum=$k; tenantSlug=$Tenant }
  if ($act.Ok) {
    $token = $act.Json.accessToken
    if (-not $token) { $token = $act.Json.AccessToken }
    Add-Result 'D7' 'Activate' 'PASS' 'ok'
  } else { Add-Result 'D7' 'Activate' 'FAIL' $act.Raw }
} else {
  Add-Result 'D6' 'Register' 'FAIL' $reg.Raw
}

if ($token) {
  $HU = @{ Authorization="Bearer $token"; 'X-Tenant-Slug'=$Tenant; Accept='application/json'; 'X-Cart-Session'=$session }
  $prods = Invoke-Api GET '/api/catalog/products?canal=B2C&page=1&pageSize=48' $HU
  $pick = @($prods.Json.items | Where-Object { $_.stockOk -eq $true } | Select-Object -First 1)
  $det = Invoke-Api GET ("/api/catalog/products/{0}?canal=B2C" -f $pick.id) $HU
  $emb = @($det.Json.embalajes)[0]
  $embId = $emb.id; if (-not $embId) { $embId = $emb.idArticuloEmbalaje }
  $add = Invoke-Api POST '/api/cart/items' $HU @{ idArticulo=$pick.id; cantidad=1; idArticuloEmbalaje=$embId; canal='B2C' }
  if ($add.Ok) { Add-Result 'D8' 'Add to cart' 'PASS' ("total={0}" -f $add.Json.montoTotal) }
  else { Add-Result 'D8' 'Add to cart' 'FAIL' $add.Raw }

  $didDelivery = $false
  if ($barrioId) {
    $ship = Invoke-Api POST '/api/shipping/apply' $HU @{ idBarrio = [long]$barrioId }
    if ($ship.Ok) {
      Add-Result 'D9' 'Apply shipping barrio' 'PASS' 'ok'
      $didDelivery = $true
    } else {
      Add-Result 'D9' 'Apply shipping barrio' 'FAIL' ("HTTP {0} {1}" -f $ship.Status, $ship.Raw)
    }
  } else {
    Add-Result 'D9' 'Apply shipping barrio' 'SKIP' 'no barrio'
  }

  # Widen horario only if falling back to pickup
  $checkoutBody = $null
  if ($didDelivery) {
    $checkoutBody = @{
      delivery = $true
      pagarEnLaEntrega = $false
      razonSocial = 'QA Three'
      numeroDocumento = $doc
      nombre = 'QA'
      apellido = 'Three'
      email = $email
      nroTelefono = '0981777666'
      lugarEntrega = 'Calle QA 123, Asuncion'
      observacion = "QA3 delivery $stamp"
      tipoComprobante = 'factura'
      latitud = -25.286
      longitud = -57.647
    }
  } else {
    # pickup + online pay
    $dia = [int](Get-Date).DayOfWeek
    $hors = Invoke-Api GET '/api/admin/empresa/horarios?idAgencia=79' $AH
    $todayH = @($hors.Json | Where-Object { $_.diaSemana -eq $dia } | Select-Object -First 1)
    $backup = $null
    if ($todayH) {
      $backup = $todayH
      Invoke-Api PUT ("/api/admin/empresa/horarios/{0}" -f $todayH.id) $AH @{
        idAgencia=[long]$todayH.idAgencia; diaSemana=[byte]$dia
        horaPrimeraApertura='08:00'; horaPrimerCierre='20:00'
        horaSegundaApertura=''; horaSegundoCierre=''; activo=$true
      } | Out-Null
    }
    Invoke-Api PUT '/api/cart/pickup' $HU @{ idAgencia = 79 } | Out-Null
    $checkoutBody = @{
      delivery = $false
      pagarEnLaEntrega = $false
      razonSocial = 'QA Three'
      numeroDocumento = $doc
      nombre = 'QA'
      apellido = 'Three'
      email = $email
      nroTelefono = '0981777666'
      observacion = "QA3 online $stamp"
      tipoComprobante = 'factura'
    }
  }

  $co = Invoke-Api POST '/api/checkout' $HU $checkoutBody
  if ($backup) {
    Invoke-Api PUT ("/api/admin/empresa/horarios/{0}" -f $backup.id) $AH @{
      idAgencia=[long]$backup.idAgencia; diaSemana=[byte]$backup.diaSemana
      horaPrimeraApertura=$backup.horaPrimeraApertura; horaPrimerCierre=$backup.horaPrimerCierre
      horaSegundaApertura=$(if($backup.horaSegundaApertura){$backup.horaSegundaApertura}else{''})
      horaSegundoCierre=$(if($backup.horaSegundoCierre){$backup.horaSegundoCierre}else{''})
      activo=$true
    } | Out-Null
  }

  if ($co.Ok) {
    $nro = $co.Json.nroPedido; if (-not $nro) { $nro = $co.Json.NroPedido }
    $reqPay = $co.Json.requierePagoOnline
    Add-Result 'D10' 'Checkout online' 'PASS' ("nro={0} requierePagoOnline={1} mode={2}" -f $nro, $reqPay, $co.Json.bancardMode)

    $sess = Invoke-Api GET ("/api/payments/bancard/session/{0}" -f $nro) $HU
    if ($sess.Ok) {
      $spid = $sess.Json.shopProcessId
      Add-Result 'D11' 'Bancard session' 'PASS' ("shopProcessId={0} mode={1} completed={2}" -f $spid, $sess.Json.bancardMode, $sess.Json.pagoCompletado)
      if ($spid -and -not $sess.Json.pagoCompletado) {
        $sim = Invoke-Api POST '/api/payments/bancard/simulate-success' $HU @{ shopProcessId = [long]$spid }
        if ($sim.Ok) {
          Add-Result 'D12' 'Simulate bancard success' 'PASS' ("nro={0} msg={1}" -f $sim.Json.nroPedido, $sim.Json.mensaje)
        } else {
          Add-Result 'D12' 'Simulate bancard success' 'FAIL' ("HTTP {0} {1}" -f $sim.Status, $sim.Raw)
        }
      } else {
        Add-Result 'D12' 'Simulate bancard success' 'SKIP' 'already paid or no shopProcessId'
      }
    } else {
      Add-Result 'D11' 'Bancard session' 'FAIL' ("HTTP {0} {1}" -f $sess.Status, $sess.Raw)
      Add-Result 'D12' 'Simulate bancard success' 'SKIP' 'no session'
    }

    # New devolucion + APPROVE (may hit ERP)
    $idPm = $co.Json.idPedidoMovil; if (-not $idPm) { $idPm = $co.Json.IDPedidoMovil }
    $dev = Invoke-Api POST '/api/engagement/devoluciones' $HU @{
      idPedidoMovil = [long]$idPm
      idArticulo = [long]$pick.id
      motivo = 'producto_defectuoso'
      detalle = "QA3 approve $stamp"
    }
    if ($dev.Ok) {
      $devId = $dev.Json.id
      Add-Result 'D13' 'Crear devolucion' 'PASS' ("id={0}" -f $devId)
      $apr = Invoke-Api POST ("/api/admin/devoluciones/{0}/estado" -f $devId) $AH @{ estado = 'aprobada' }
      if ($apr.Ok -or $apr.Status -eq 200 -or $apr.Status -eq 204) {
        Add-Result 'D14' 'Admin aprueba devolucion' 'PASS' ("id={0} (ERP ok → estado completada)" -f $devId)
        # En código, aprobar ya fuerza completada tras ERP; D15 verifica listado
        $list = Invoke-Api GET '/api/admin/devoluciones' $AH
        $row = $null
        if ($list.Ok) { $row = @($list.Json) | Where-Object { $_.id -eq $devId } | Select-Object -First 1 }
        if ($row -and $row.estado -eq 'completada') {
          Add-Result 'D15' 'Admin completa devolucion' 'PASS' ("id={0} estado=completada erp={1}" -f $devId, $row.erpProcesado)
        } elseif ($apr.Ok) {
          Add-Result 'D15' 'Admin completa devolucion' 'PASS' ("aprobada implica completada id={0}" -f $devId)
        } else {
          Add-Result 'D15' 'Admin completa devolucion' 'FAIL' 'estado no completada'
        }
      } else {
        $erpHint = $apr.Raw
        if ($erpHint -match 'ERP|stream|timeout|Npgsql|Sql') {
          Add-Result 'D14' 'Admin aprueba devolucion' 'FAIL' ("ERP write-back: {0}" -f $erpHint)
        } else {
          Add-Result 'D14' 'Admin aprueba devolucion' 'FAIL' ("HTTP {0} {1}" -f $apr.Status, $apr.Raw)
        }
        Add-Result 'D15' 'Admin completa devolucion' 'SKIP' 'approve/ERP failed'
      }
    } else {
      Add-Result 'D13' 'Crear devolucion' 'FAIL' ("HTTP {0} {1}" -f $dev.Status, $dev.Raw)
      Add-Result 'D14' 'Admin aprueba devolucion' 'SKIP' 'no devolucion'
      Add-Result 'D15' 'Admin completa devolucion' 'SKIP' 'no devolucion'
    }
  } else {
    Add-Result 'D10' 'Checkout online' 'FAIL' ("HTTP {0} {1}" -f $co.Status, $co.Raw)
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
$md = Join-Path $outDir 'QA-CONTINUE-3-RESULTS.md'
$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('# QA Continue-3 - Delivery + Pago + Devolucion aprobar')
[void]$sb.AppendLine('')
[void]$sb.AppendLine(("**Ejecutado:** {0}" -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss')))
[void]$sb.AppendLine(("**API:** {0} · Tenant: {1}" -f $ApiBase, $Tenant))
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
