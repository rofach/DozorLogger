$envData = @{}
if (Test-Path ".env") {
    Get-Content ".env" | ForEach-Object {
        $line = $_.Trim()
        if ($line -and -not $line.StartsWith("#") -and $line.Contains("=")) {
            $parts = $line.Split("=", 2)
            $envData[$parts[0].Trim()] = $parts[1].Trim()
        }
    }
}

$webhookUrl = $envData["GOOGLE_SHEETS_WEBHOOK_URL"]
$routeId = if ($envData["ROUTE_ID"]) { $envData["ROUTE_ID"] } else { "2381" }
$targetLat = if ($envData["TARGET_LAT"]) { $envData["TARGET_LAT"] } else { "49.4307129" }
$targetLng = if ($envData["TARGET_LNG"]) { $envData["TARGET_LNG"] } else { "32.0694703" }
$azimuth = if ($envData["EXPECTED_AZIMUTH"]) { $envData["EXPECTED_AZIMUTH"] } else { "130" }
$radius = if ($envData["STOP_RADIUS_METERS"]) { $envData["STOP_RADIUS_METERS"] } else { "50" }
$directRadius = if ($envData["DIRECT_POINT_RADIUS_METERS"]) { $envData["DIRECT_POINT_RADIUS_METERS"] } else { "30" }
$enableDirect = if ($envData["ENABLE_DIRECT_POINT_CHECK"]) { $envData["ENABLE_DIRECT_POINT_CHECK"] } else { "true" }

Write-Host "Route ID: $routeId | Stop: $targetLat, $targetLng | Radius: ${radius}m | Azimuth: $azimuth" -ForegroundColor Yellow
Write-Host "Vector Algorithm: Active | Direct Point Check: $enableDirect (${directRadius}m)" -ForegroundColor Yellow
Write-Host "Webhook: $webhookUrl" -ForegroundColor Yellow
Write-Host "Deploying to Azure Container Apps (polandcentral)..." -ForegroundColor Green

az containerapp up `
  --name dozor-tracker `
  --resource-group rg-dozor-tracker `
  --location polandcentral `
  --source . `
  --env-vars `
    ROUTE_ID="$routeId" `
    TARGET_LAT="$targetLat" `
    TARGET_LNG="$targetLng" `
    STOP_RADIUS_METERS="$radius" `
    DIRECT_POINT_RADIUS_METERS="$directRadius" `
    ENABLE_DIRECT_POINT_CHECK="$enableDirect" `
    EXPECTED_AZIMUTH="$azimuth" `
    POLL_INTERVAL_SECONDS="5" `
    GOOGLE_SHEETS_WEBHOOK_URL="$webhookUrl"

if ($LASTEXITCODE -eq 0) {
    az containerapp update `
      --name dozor-tracker `
      --resource-group rg-dozor-tracker `
      --min-replicas 1 `
      --max-replicas 1

    Write-Host "Deployment completed successfully!" -ForegroundColor Green
} else {
    Write-Host "Deployment failed." -ForegroundColor Red
}
