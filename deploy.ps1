#!/usr/bin/env pwsh
# ScholarRescue Deployment Script

param(
    [Parameter(Mandatory=$false)]
    [string]$VpsHost = "62.171.133.123",
    [Parameter(Mandatory=$false)]
    [string]$VpsUser = "root",
    [Parameter(Mandatory=$false)]
    [string]$AppPath = "/opt/scholarrescue",
    [Parameter(Mandatory=$false)]
    [string]$ServiceName = "scholarrescue"
)

Write-Host "================================" -ForegroundColor Cyan
Write-Host "ScholarRescue Deployment Script" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "VPS Details:" -ForegroundColor Yellow
Write-Host "  Host: $VpsHost"
Write-Host "  User: $VpsUser"
Write-Host "  App Path: $AppPath"
Write-Host "  Service: $ServiceName"
Write-Host ""

Write-Host "Deployment Steps:" -ForegroundColor Yellow
Write-Host "  1. Connect to VPS"
Write-Host "  2. Pull latest changes from GitHub"
Write-Host "  3. Build and publish application"
Write-Host "  4. Restart systemd service"
Write-Host ""

$continue = Read-Host "Continue with deployment? (yes/no)"
if ($continue -ne "yes") {
    Write-Host "Deployment cancelled." -ForegroundColor Red
    exit 0
}

Write-Host ""
Write-Host "Connecting to VPS..." -ForegroundColor Green

$deployCommands = @(
    "cd $AppPath",
    "git pull origin main",
    "dotnet publish -c Release --no-restore",
    "systemctl restart $ServiceName",
    "systemctl status $ServiceName"
) -join " && "

$sshCommand = "ssh $VpsUser@$VpsHost `"$deployCommands`""

Write-Host "Running: ssh $VpsUser@$VpsHost ..." -ForegroundColor Green
Write-Host ""

# Execute SSH command
Invoke-Expression $sshCommand
$exitCode = $LASTEXITCODE

Write-Host ""
if ($exitCode -eq 0) {
    Write-Host "✓ Deployment completed successfully!" -ForegroundColor Green
} else {
    Write-Host "✗ Deployment failed with exit code $exitCode" -ForegroundColor Red
}

exit $exitCode
