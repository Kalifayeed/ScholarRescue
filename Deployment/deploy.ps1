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

Write-Host "=== Step 2: Deploying to production & restarting service..." -ForegroundColor Cyan
$remoteCmds = @'
set -e
echo "Stopping service..."
systemctl stop scholarrescue || true

echo "Backing up current deployment..."
if [ -d /var/www/scholarrescue ]; then
    BACKUP_DIR="/var/backups/scholarrescue/rollback_$(date +%Y%m%d_%H%M%S)"
    mkdir -p "$BACKUP_DIR"
    cp -r /var/www/scholarrescue/* "$BACKUP_DIR/" 2>/dev/null || true
    echo "Backup created at $BACKUP_DIR"
fi

echo "Deploying new files..."
mkdir -p /var/www/scholarrescue
rsync -av --delete /var/www/scholarrescue_staging/ /var/www/scholarrescue/

echo "Setting permissions..."
chown -R scholarrescue:scholarrescue /var/www/scholarrescue
chmod +x /var/www/scholarrescue/ScholarRescue 2>/dev/null || true

echo "Applying EF Core migrations..."
cd /var/www/scholarrescue
/usr/local/bin/dotnet /var/www/scholarrescue/ScholarRescue.dll --apply-migrations 2>&1 || true

echo "Starting service..."
systemctl start scholarrescue

echo "Waiting for service to start..."
sleep 5

echo "Checking service status..."
systemctl status scholarrescue --no-pager -l

echo ""
echo "=== Deployment Complete ==="
echo "Service: $(systemctl is-active scholarrescue)"
echo "Check logs: journalctl -u scholarrescue -n 50 --no-pager"
'@

ssh -o StrictHostKeyChecking=no $Server @remoteCmds

Write-Host "=== Done ===" -ForegroundColor Green