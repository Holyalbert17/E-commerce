# Auto-test branding storefront
# Usage: pwsh scripts/test-branding.ps1
$ErrorActionPreference = 'Stop'
$api = 'http://127.0.0.1:5082'
$h = @{ 'X-Tenant-Slug' = 'demo'; 'Content-Type' = 'application/json' }
$fail = 0

function Ok($msg) { Write-Host "PASS  $msg" -ForegroundColor Green }
function Fail($msg) { Write-Host "FAIL  $msg" -ForegroundColor Red; $script:fail++ }

Write-Host "=== Branding auto-test ===" -ForegroundColor Cyan

try {
  $health = Invoke-RestMethod "$api/health" -TimeoutSec 8
  Ok "API health"
} catch {
  Fail "API no responde en $api"
  exit 1
}

try {
  $pub = Invoke-RestMethod "$api/api/catalog/branding" -Headers @{ 'X-Tenant-Slug' = 'demo' } -TimeoutSec 8
  if (-not $pub.nombreTienda) { Fail "GET público sin nombreTienda" } else { Ok "GET /api/catalog/branding ($($pub.nombreTienda))" }
} catch {
  Fail "GET público branding: $($_.Exception.Message)"
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

$ha = @{ 'X-Tenant-Slug' = 'demo'; 'Authorization' = "Bearer $token"; 'Content-Type' = 'application/json' }

$newName = "Demo Brand $(Get-Random -Maximum 9999)"
try {
  $upd = Invoke-RestMethod "$api/api/admin/empresa/branding" -Method Put -Headers $ha -Body (@{
    nombreTiendaDisplay = $newName
    taglineTienda = 'Auto-test tagline'
    logoMark = 'DB'
    colorPrimario = '#0D47A1'
    colorSecundario = '#002171'
    colorAcento = '#FF6F00'
    direccionURLInstagram = 'https://instagram.com/demo'
    direccionURLWhatsapp = 'https://wa.me/595981000000'
  } | ConvertTo-Json)
  if ($upd.nombreTienda -ne $newName) { Fail "PUT branding nombre mismatch" } else { Ok "PUT branding colores/nombre" }
  if ($upd.colorPrimario -ne '#0D47A1') { Fail "color primario no persistió" } else { Ok "Color primario #0D47A1" }
} catch {
  Fail "PUT branding: $($_.Exception.Message)"
}

$png = [byte[]](0x89,0x50,0x4E,0x47,0x0D,0x0A,0x1A,0x0A,0x00,0x00,0x00,0x0D,0x49,0x48,0x44,0x52,0x00,0x00,0x00,0x01,0x00,0x00,0x00,0x01,0x08,0x02,0x00,0x00,0x00,0x90,0x77,0x53,0xDE,0x00,0x00,0x00,0x0C,0x49,0x44,0x41,0x54,0x08,0xD7,0x63,0xF8,0xCF,0xC0,0x00,0x00,0x00,0x03,0x00,0x01,0x00,0x05,0xFE,0xD4,0xEF,0x00,0x00,0x00,0x00,0x49,0x45,0x4E,0x44,0xAE,0x42,0x60,0x82)
$tmp = Join-Path $env:TEMP "brand-logo-$(Get-Random).png"
[IO.File]::WriteAllBytes($tmp, $png)

try {
  $outFile = Join-Path $env:TEMP "brand-upload-$(Get-Random).json"
  $code = curl.exe --noproxy '*' -s -o $outFile -w "%{http_code}" -X POST "$api/api/admin/empresa/branding/upload/logo" `
    -H "Authorization: Bearer $token" -H "X-Tenant-Slug: demo" `
    -F "file=@$tmp;type=image/png"
  $body = Get-Content $outFile -Raw -ErrorAction SilentlyContinue
  Remove-Item $outFile -Force -ErrorAction SilentlyContinue
  if ("$code" -ne '200') { Fail "Upload logo HTTP ${code}: $body" }
  else {
    $json = $body | ConvertFrom-Json
    if (-not $json.logoUrl) { Fail "Upload logo sin logoUrl" }
    else {
      Ok "Upload logo → $($json.logoUrl)"
      $mediaUrl = if ($json.logoUrl -match '^http') { $json.logoUrl } else { "$api$($json.logoUrl)" }
      $m = curl.exe --noproxy '*' -s -o NUL -w "%{http_code}" $mediaUrl
      if ("$m" -eq '200') { Ok "Media logo accesible" } else { Fail "Media logo HTTP $m" }
    }
  }
} catch {
  Fail "Upload logo: $($_.Exception.Message)"
} finally {
  Remove-Item $tmp -Force -ErrorAction SilentlyContinue
}

try {
  $pub2 = Invoke-RestMethod "$api/api/catalog/branding" -Headers @{ 'X-Tenant-Slug' = 'demo' }
  if ($pub2.nombreTienda -ne $newName) { Fail "Público no refleja nombre nuevo" } else { Ok "Público refleja branding" }
  if (-not $pub2.logoUrl) { Fail "Público sin logoUrl" } else { Ok "Público tiene logo" }
  if ($pub2.colorPrimario -ne '#0D47A1') { Fail "Público sin color" } else { Ok "Público tiene color primario" }
} catch {
  Fail "Re-check público: $($_.Exception.Message)"
}

try {
  Invoke-RestMethod "$api/api/admin/empresa/branding" -Method Put -Headers $ha -Body '{"colorPrimario":"rojo"}' | Out-Null
  Fail "Debió rechazar color inválido"
} catch {
  Ok "Valida color inválido"
}

Write-Host ""
if ($fail -eq 0) {
  Write-Host "ALL PASSED" -ForegroundColor Green
  exit 0
} else {
  Write-Host "$fail FAILED" -ForegroundColor Red
  exit 1
}
