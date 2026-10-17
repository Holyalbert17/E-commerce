# Auto-test: reports/ventas + promo image upload
# Usage: pwsh scripts/test-promo-ventas.ps1
$ErrorActionPreference = 'Stop'
$api = 'http://127.0.0.1:5082'
$h = @{ 'X-Tenant-Slug' = 'demo'; 'Content-Type' = 'application/json' }
$fail = 0

function Ok($msg) { Write-Host "PASS  $msg" -ForegroundColor Green }
function Fail($msg) { Write-Host "FAIL  $msg" -ForegroundColor Red; $script:fail++ }

Write-Host "=== Promo / Ventas auto-test ===" -ForegroundColor Cyan

try {
  Invoke-RestMethod "$api/health" -TimeoutSec 8 | Out-Null
  Ok "API health"
} catch {
  Fail "API no responde en $api"
  exit 1
}

try {
  $adminLogin = Invoke-RestMethod "$api/api/admin/auth/login" -Method Post -Headers $h -Body '{"usuario":"admin","password":"Admin12!","tenantSlug":"demo"}'
  $token = $adminLogin.accessToken
  if (-not $token) { throw 'sin token' }
  Ok "Admin login"
} catch {
  Fail "Admin login: $($_.Exception.Message)"
  exit 1
}

$ha = @{ 'X-Tenant-Slug' = 'demo'; 'Authorization' = "Bearer $token" }

try {
  $ventas = Invoke-RestMethod "$api/api/admin/reports/ventas?from=2026-06-30&to=2026-07-30" -Headers $ha -TimeoutSec 15
  Ok "GET reports/ventas (pedidos=$($ventas.pedidosCount) monto=$($ventas.montoTotal) dias=$($ventas.porDia.Count))"
} catch {
  Fail "GET reports/ventas: $($_.Exception.Message)"
}

$png = [byte[]](0x89,0x50,0x4E,0x47,0x0D,0x0A,0x1A,0x0A,0x00,0x00,0x00,0x0D,0x49,0x48,0x44,0x52,0x00,0x00,0x00,0x01,0x00,0x00,0x00,0x01,0x08,0x02,0x00,0x00,0x00,0x90,0x77,0x53,0xDE,0x00,0x00,0x00,0x0C,0x49,0x44,0x41,0x54,0x08,0xD7,0x63,0xF8,0xCF,0xC0,0x00,0x00,0x00,0x03,0x00,0x01,0x00,0x05,0xFE,0xD4,0xEF,0x00,0x00,0x00,0x00,0x49,0x45,0x4E,0x44,0xAE,0x42,0x60,0x82)
$tmp = Join-Path $env:TEMP "promo-flyer-$(Get-Random).png"
[IO.File]::WriteAllBytes($tmp, $png)

try {
  $outFile = Join-Path $env:TEMP "promo-upload-$(Get-Random).json"
  $code = curl.exe --noproxy '*' -s -o $outFile -w "%{http_code}" -X POST "$api/api/admin/promotions/imagen/upload" `
    -H "Authorization: Bearer $token" -H "X-Tenant-Slug: demo" `
    -F "file=@$tmp;type=image/png"
  $body = Get-Content $outFile -Raw -ErrorAction SilentlyContinue
  Remove-Item $outFile -Force -ErrorAction SilentlyContinue
  if ("$code" -ne '200') { Fail "Upload promo HTTP ${code}: $body" }
  else {
    $json = $body | ConvertFrom-Json
    if (-not $json.url) { Fail "Upload promo sin url" }
    else {
      Ok "Upload promo → $($json.url)"
      $mediaUrl = if ($json.url -match '^http') { $json.url } else { "$api$($json.url)" }
      $m = curl.exe --noproxy '*' -s -o NUL -w "%{http_code}" $mediaUrl
      if ("$m" -eq '200') { Ok "Media flyer accesible" } else { Fail "Media flyer HTTP $m ($mediaUrl)" }
    }
  }
} catch {
  Fail "Upload promo: $($_.Exception.Message)"
} finally {
  Remove-Item $tmp -Force -ErrorAction SilentlyContinue
}

Write-Host ""
if ($fail -eq 0) { Write-Host "ALL PASSED" -ForegroundColor Green; exit 0 }
Write-Host "FAILED: $fail" -ForegroundColor Red
exit 1
