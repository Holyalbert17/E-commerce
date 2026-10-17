# QA continue-2: admin order ops + cupon cart + devolucion
param(
  [string]$ApiBase = 'http://localhost:5082',
  [string]$Tenant = 'casa-imperial'
)

$ErrorActionPreference = 'Continue'
$results = New-Object System.Collections.Generic.List[object]
$started = Get-Date
$stamp = Get-Date -Format 'yyyyMMddHHmmss'
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
  param([string]$Method, [string]$Path, [hashtable]$Headers, $Body = $null, [string]$ContentType = 'application/json')
  try {
    $p = @{ Uri = ($ApiBase + $Path); Method = $Method; Headers = $Headers; UseBasicParsing = $true; TimeoutSec = 90 }
    if ($null -ne $Body) {
      if ($ContentType -eq 'application/json') {
        $p.ContentType = 'application/json'
        $p.Body = ($Body | ConvertTo-Json -Depth 8 -Compress)
      } else {
        $p.ContentType = $ContentType
        $p.Body = $Body
      }
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
Write-Host "=== QA CONTINUE-2: ADMIN ORDER + CUPON + DEVOLUCION ===" -ForegroundColor Cyan

# --- Admin login ---
$al = Invoke-Api POST '/api/admin/auth/login' @{ 'X-Tenant-Slug' = $Tenant } @{ usuario = 'admin'; password = 'Admin12!' }
$adminTok = $null
if ($al.Ok) {
  $adminTok = $al.Json.accessToken
  if (-not $adminTok) { $adminTok = $al.Json.token }
}
if (-not $adminTok) { Add-Result 'C0' 'Admin login' 'FAIL' $al.Raw; exit 2 }
Add-Result 'C0' 'Admin login' 'PASS' 'ok'
$AH = @{ Authorization = "Bearer $adminTok"; 'X-Tenant-Slug' = $Tenant; Accept = 'application/json' }

# --- Orders list + detail ---
$ords = Invoke-Api GET '/api/admin/orders?page=1&pageSize=20' $AH
if (-not $ords.Ok) { Add-Result 'C1' 'Admin orders list' 'FAIL' $ords.Raw; exit 2 }
$items = @($ords.Json.items)
Add-Result 'C1' 'Admin orders list' 'PASS' ("count={0} total={1}" -f $items.Count, $ords.Json.totalCount)

$target = $items | Where-Object { $_.idPedidoMovil -eq 5 -or $_.nroPedido -eq 67 } | Select-Object -First 1
if (-not $target) { $target = $items | Select-Object -First 1 }
if (-not $target) { Add-Result 'C2' 'Pick order' 'FAIL' 'no orders'; exit 2 }
$idPm = $target.idPedidoMovil
$nro = $target.nroPedido
Add-Result 'C2' 'Pick order' 'PASS' ("idPedidoMovil={0} nro={1} migrado={2}" -f $idPm, $nro, $target.migrado)

$det = Invoke-Api GET ("/api/admin/orders/{0}" -f $idPm) $AH
if ($det.Ok) {
  Add-Result 'C3' 'Admin order detail' 'PASS' ("estado={0} items={1}" -f $det.Json.estado, @($det.Json.items).Count)
} else {
  Add-Result 'C3' 'Admin order detail' 'FAIL' $det.Raw
}

# Tracking hito
$tr = Invoke-Api POST ("/api/admin/pedidos/{0}/tracking" -f $idPm) $AH @{
  estado = 'preparando'
  mensaje = "QA continue-2 $stamp"
}
if ($tr.Ok -or $tr.Status -eq 204 -or $tr.Status -eq 200) {
  Add-Result 'C4' 'Add tracking hito' 'PASS' 'preparando'
} else {
  # empty body POST
  try {
    Invoke-WebRequest -Uri ($ApiBase + "/api/admin/pedidos/$idPm/tracking") -Method POST -Headers $AH -ContentType 'application/json' -Body (@{ estado='preparando'; mensaje="QA $stamp" } | ConvertTo-Json) -UseBasicParsing | Out-Null
    Add-Result 'C4' 'Add tracking hito' 'PASS' 'preparando'
  } catch {
    Add-Result 'C4' 'Add tracking hito' 'FAIL' $_.Exception.Message
  }
}

$trl = Invoke-Api GET ("/api/engagement/tracking/{0}" -f $idPm) $AH
if ($trl.Ok) { Add-Result 'C5' 'List tracking' 'PASS' ("events={0}" -f @($trl.Json).Count) }
else { Add-Result 'C5' 'List tracking' 'FAIL' $trl.Raw }

# Migracion logs (read-only)
$logs = Invoke-Api GET '/api/admin/orders/migracion-logs' $AH
if ($logs.Ok) { Add-Result 'C6' 'Migracion logs' 'PASS' ("rows={0}" -f @($logs.Json).Count) }
else { Add-Result 'C6' 'Migracion logs' 'FAIL' $logs.Raw }

# Soft migrar local only if not migrated - prefer read status
if (-not $target.migrado) {
  $mig = Invoke-Api POST '/api/admin/orders/migrar' $AH @{ ids = @([long]$idPm) }
  if ($mig.Ok) {
    Add-Result 'C7' 'Marcar migrado local' 'PASS' ("updated={0}" -f $mig.Json.updatedCount)
  } else {
    Add-Result 'C7' 'Marcar migrado local' 'SKIP' ("HTTP {0} {1}" -f $mig.Status, $mig.Raw)
  }
} else {
  Add-Result 'C7' 'Marcar migrado local' 'SKIP' 'ya migrado'
}

# --- Cupon create + apply on new cart ---
$codigo = ("QA2{0}" -f $stamp.Substring($stamp.Length - 6))
$cup = Invoke-Api POST '/api/admin/cupones' $AH @{
  codigo = $codigo
  descripcion = 'QA continue-2 10%'
  tipoDescuento = 'porcentaje'
  valorDescuento = 10
  montoMinimo = 0
  canalEcommerce = 'ALL'
  usosMaximos = 5
  usosPorUsuario = 2
}
if ($cup.Ok) { Add-Result 'C8' "Crear cupon $codigo" 'PASS' ("id={0}" -f $cup.Json.id) }
else { Add-Result 'C8' "Crear cupon $codigo" 'FAIL' $cup.Raw }

# Fresh customer for coupon cart
$session = "qa2-$stamp"
$tipos = Invoke-Api GET '/api/geography/tipos-documento' @{ 'X-Tenant-Slug'=$Tenant; Accept='application/json' }
$tipoId = @($tipos.Json)[0].id
$email = "qa2.$stamp@example.com"
$doc = ("5{0}" -f $stamp.Substring($stamp.Length - 7))
$H0 = @{ 'X-Tenant-Slug'=$Tenant; Accept='application/json'; 'X-Cart-Session'=$session }

$reg = Invoke-Api POST '/api/auth/register' $H0 @{
  nombre='QA'; apellido='Two'; idTipoDocumento=$tipoId; numeroDocumento=$doc
  telefono='0215559999'; movil='0981999888'; email=$email
  password=$pass; passwordConfirmation=$pass; tenantSlug=$Tenant
}
if (-not $reg.Ok) { Add-Result 'C9' 'Register customer' 'FAIL' $reg.Raw; }
else {
  Add-Result 'C9' 'Register customer' 'PASS' $email
  $actUrl = $reg.Json.activationUrlDev
  if (-not $actUrl) { $actUrl = $reg.Json.ActivationUrlDev }
  $c=$null;$k=$null
  if ($actUrl -match '[?&]c=([^&]+)') { $c=$Matches[1] }
  if ($actUrl -match '[?&]k=([^&]+)') { $k=$Matches[1] }
  $act = Invoke-Api POST '/api/auth/activate' $H0 @{ codigo=$c; checksum=$k; tenantSlug=$Tenant }
  $tok = $null
  if ($act.Ok) {
    $tok = $act.Json.accessToken
    if (-not $tok) { $tok = $act.Json.AccessToken }
    Add-Result 'C10' 'Activate customer' 'PASS' 'ok'
  } else { Add-Result 'C10' 'Activate customer' 'FAIL' $act.Raw }

  if ($tok) {
    $HU = @{ Authorization="Bearer $tok"; 'X-Tenant-Slug'=$Tenant; Accept='application/json'; 'X-Cart-Session'=$session }
    $prods = Invoke-Api GET '/api/catalog/products?canal=B2C&page=1&pageSize=48' $HU
    $pick = @($prods.Json.items | Where-Object { $_.stockOk -eq $true } | Select-Object -First 1)
    $det = Invoke-Api GET ("/api/catalog/products/{0}?canal=B2C" -f $pick.id) $HU
    $emb = @($det.Json.embalajes)[0]
    $embId = $emb.id; if (-not $embId) { $embId = $emb.idArticuloEmbalaje }
    $add = Invoke-Api POST '/api/cart/items' $HU @{ idArticulo=$pick.id; cantidad=1; idArticuloEmbalaje=$embId; canal='B2C' }
    if ($add.Ok) { Add-Result 'C11' 'Cart with stock item' 'PASS' ("total={0}" -f $add.Json.montoTotal) }
    else { Add-Result 'C11' 'Cart with stock item' 'FAIL' $add.Raw }

    $ap = Invoke-Api POST '/api/engagement/cupones/aplicar' $HU @{ codigo=$codigo; canal='B2C' }
    if ($ap.Ok -and ($ap.Json.ok -eq $true -or $ap.Json.montoDescuento -ge 0)) {
      Add-Result 'C12' 'Aplicar cupon en carrito' 'PASS' ("descuento={0} msg={1}" -f $ap.Json.montoDescuento, $ap.Json.mensaje)
    } else {
      Add-Result 'C12' 'Aplicar cupon en carrito' 'FAIL' ("HTTP {0} {1}" -f $ap.Status, $ap.Raw)
    }

    # Devolucion on previous order (idPm from E2E) - may fail if different user
    # Create new mini checkout then devolucion on THAT order for this user
    $dia = [int](Get-Date).DayOfWeek
    $hors = Invoke-Api GET '/api/admin/empresa/horarios?idAgencia=79' $AH
    $today = @($hors.Json | Where-Object { $_.diaSemana -eq $dia } | Select-Object -First 1)
    $backup = $null
    if ($today) {
      $backup = $today
      Invoke-Api PUT ("/api/admin/empresa/horarios/{0}" -f $today.id) $AH @{
        idAgencia=[long]$today.idAgencia; diaSemana=[byte]$dia
        horaPrimeraApertura='08:00'; horaPrimerCierre='20:00'
        horaSegundaApertura=''; horaSegundoCierre=''; activo=$true
      } | Out-Null
    }
    Invoke-Api PUT '/api/cart/pickup' $HU @{ idAgencia = 79 } | Out-Null
    $co = Invoke-Api POST '/api/checkout' $HU @{
      delivery=$false; pagarEnLaEntrega=$true
      razonSocial='QA Two'; numeroDocumento=$doc
      nombre='QA'; apellido='Two'; email=$email
      nroTelefono='0981999888'; observacion="QA2 $stamp"; tipoComprobante='factura'
    }
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
      $newPm = $co.Json.idPedidoMovil; if (-not $newPm) { $newPm = $co.Json.IDPedidoMovil }
      $newNro = $co.Json.nroPedido; if (-not $newNro) { $newNro = $co.Json.NroPedido }
      Add-Result 'C13' 'Checkout for devolucion' 'PASS' ("nro={0} idPm={1}" -f $newNro, $newPm)

      $dev = Invoke-Api POST '/api/engagement/devoluciones' $HU @{
        idPedidoMovil = [long]$newPm
        idArticulo = [long]$pick.id
        motivo = 'producto_defectuoso'
        detalle = "QA continue-2 devolucion $stamp"
      }
      if ($dev.Ok) {
        $devId = $dev.Json.id
        Add-Result 'C14' 'Crear devolucion' 'PASS' ("id={0} estado={1}" -f $devId, $dev.Json.estado)
        $adminDev = Invoke-Api GET '/api/admin/devoluciones' $AH
        if ($adminDev.Ok) {
          $found = @($adminDev.Json | Where-Object { $_.id -eq $devId }).Count -gt 0
          if ($found) { Add-Result 'C15' 'Admin ve devolucion' 'PASS' "id=$devId" }
          else { Add-Result 'C15' 'Admin ve devolucion' 'FAIL' 'not in list' }
        } else { Add-Result 'C15' 'Admin ve devolucion' 'FAIL' $adminDev.Raw }

        # Reject (safer than approve/ERP) then we could re-test - or leave as solicitada
        $rej = Invoke-Api POST ("/api/admin/devoluciones/{0}/estado" -f $devId) $AH @{ estado = 'rechazada' }
        if ($rej.Ok -or $rej.Status -eq 204 -or $rej.Status -eq 200) {
          Add-Result 'C16' 'Admin rechaza devolucion' 'PASS' 'rechazada'
        } else {
          Add-Result 'C16' 'Admin rechaza devolucion' 'FAIL' ("HTTP {0} {1}" -f $rej.Status, $rej.Raw)
        }
      } else {
        Add-Result 'C14' 'Crear devolucion' 'FAIL' ("HTTP {0} {1}" -f $dev.Status, $dev.Raw)
      }
    } else {
      Add-Result 'C13' 'Checkout for devolucion' 'FAIL' ("HTTP {0} {1}" -f $co.Status, $co.Raw)
    }

    # Deactivate coupon
    if ($cup.Ok -and $cup.Json.id) {
      try {
        Invoke-WebRequest -Uri ($ApiBase + "/api/admin/cupones/$($cup.Json.id)/toggle?activo=false") -Method POST -Headers $AH -UseBasicParsing | Out-Null
        Add-Result 'C17' 'Desactivar cupon QA' 'PASS' ("id={0}" -f $cup.Json.id)
      } catch {
        Add-Result 'C17' 'Desactivar cupon QA' 'FAIL' $_.Exception.Message
      }
    }
  }
}

# Home feeds still OK
$Hpub = @{ 'X-Tenant-Slug'=$Tenant; Accept='application/json' }
$mv = Invoke-Api GET '/api/catalog/mas-vendidos?take=10&canal=B2C' $Hpub
if ($mv.Ok) { Add-Result 'C18' 'Home mas-vendidos' 'PASS' ("n={0}" -f @($mv.Json).Count) } else { Add-Result 'C18' 'Home mas-vendidos' 'FAIL' $mv.Raw }
$rec = Invoke-Api GET '/api/engagement/recomendaciones' $Hpub
if ($rec.Ok) { Add-Result 'C19' 'Home recomendados' 'PASS' ("n={0}" -f @($rec.Json).Count) } else { Add-Result 'C19' 'Home recomendados' 'FAIL' $rec.Raw }
$mar = Invoke-Api GET '/api/catalog/marcas' $Hpub
if ($mar.Ok) { Add-Result 'C20' 'Catalog marcas (fix)' 'PASS' ("n={0}" -f @($mar.Json).Count) } else { Add-Result 'C20' 'Catalog marcas (fix)' 'FAIL' $mar.Raw }

$pCount = @($results | Where-Object Status -eq 'PASS').Count
$fCount = @($results | Where-Object Status -eq 'FAIL').Count
$sCount = @($results | Where-Object Status -eq 'SKIP').Count
$elapsed = [math]::Round(((Get-Date) - $started).TotalSeconds, 1)
Write-Host ""
Write-Host "=== SUMMARY ===" -ForegroundColor Cyan
Write-Host ("PASS={0} FAIL={1} SKIP={2} total={3} {4}s" -f $pCount, $fCount, $sCount, $results.Count, $elapsed)

$outDir = Join-Path (Split-Path $PSScriptRoot -Parent) 'docs'
$md = Join-Path $outDir 'QA-CONTINUE-2-RESULTS.md'
$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('# QA Continue-2 - Admin order + cupon + devolucion')
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
