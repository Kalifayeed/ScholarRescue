# ScholarRescue Deployment Script
# Usage: powershell -File deploy.ps1
$ErrorActionPreference = "Stop"
$Server = "root@62.171.133.123"
$StagingDir = "/var/www/scholarrescue_staging"
$AppDir = "/var/www/scholarrescue"
$PublishDir = "h:\ScholarRescue\publish-linux"
$ServiceName = "scholarrescue"

Write-Host "=== Step 1: Uploading publish files to staging..." -ForegroundColor Cyan
scp -r -o StrictHostKeyChecking=no "$PublishDir\*" "${Server}:${StagingDir}/"

Write-Host "=== Step 2: Uploading remote deploy script to server..." -ForegroundColor Cyan
scp -o StrictHostKeyChecking=no "Deployment/deploy-remote.sh" "${Server}:/var/www/scholarrescue_staging/deploy-remote.sh"

Write-Host "=== Step 3: Deploying to production & restarting service..." -ForegroundColor Cyan
ssh -o StrictHostKeyChecking=no $Server "bash /var/www/scholarrescue_staging/deploy-remote.sh"

Write-Host "=== Done ===" -ForegroundColor Green