# Export Swagger JSON for Hotshot Logistics API
# This script starts the API, downloads the swagger.json, and saves it for mobile developers

Write-Host "Starting Hotshot Logistics API..." -ForegroundColor Cyan

# Start the API in the background
$apiProcess = Start-Process -FilePath "dotnet" -ArgumentList "run" -PassThru -NoNewWindow -WorkingDirectory $PSScriptRoot

# Wait for API to be ready (max 30 seconds)
$maxAttempts = 30
$attempt = 0
$apiUrl = "https://localhost:7060/swagger/v1/swagger.json"

Write-Host "Waiting for API to start..." -ForegroundColor Yellow

while ($attempt -lt $maxAttempts) {
    try {
        $response = Invoke-WebRequest -Uri $apiUrl -SkipCertificateCheck -TimeoutSec 2 -ErrorAction Stop
        if ($response.StatusCode -eq 200) {
            Write-Host "API is ready!" -ForegroundColor Green
            break
        }
    }
    catch {
        Start-Sleep -Seconds 1
        $attempt++
    }
}

if ($attempt -eq $maxAttempts) {
    Write-Host "ERROR: API did not start in time" -ForegroundColor Red
    Stop-Process -Id $apiProcess.Id -Force
    exit 1
}

# Download the swagger.json
Write-Host "Downloading swagger.json..." -ForegroundColor Cyan

try {
    $swaggerJson = Invoke-WebRequest -Uri $apiUrl -SkipCertificateCheck
    $outputPath = Join-Path $PSScriptRoot "swagger.json"
    $swaggerJson.Content | Out-File -FilePath $outputPath -Encoding UTF8

    Write-Host "SUCCESS: Swagger file exported to: $outputPath" -ForegroundColor Green
    Write-Host ""
    Write-Host "You can now share this file with mobile developers!" -ForegroundColor Cyan
}
catch {
    Write-Host "ERROR: Failed to download swagger.json - $_" -ForegroundColor Red
}
finally {
    # Stop the API
    Write-Host "Stopping API..." -ForegroundColor Yellow
    Stop-Process -Id $apiProcess.Id -Force
    Write-Host "Done!" -ForegroundColor Green
}
