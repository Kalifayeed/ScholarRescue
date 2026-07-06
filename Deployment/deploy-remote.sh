#!/bin/bash
set -e

# ============================================================
# ScholarRescue Production Deploy Script
# ============================================================
# Invoke: ssh deploy@host 'bash -s' < deploy-remote.sh
# Or copy to server and run directly.
#
# Prerequisites (server):
#   - /var/www/ScholarRescue-v2  — git clone of the repository
#   - /var/www/ScholarRescue/publish  — current publish output
#   - dotnet SDK 10, EF Core tools installed
#   - ConnectionStrings__DefaultConnection set as env var or passed below
# ============================================================

DEPLOY_DIR="/var/www/ScholarRescue-v2"
PUBLISH_DIR="/var/www/ScholarRescue/publish"
BACKUP_BASE="/var/backups/scholarrescue"
SERVICE="scholarrescue"
HEALTH_URL="http://localhost:5000/health"

# ═══════════════════════════════════════════════════════════════
# DATABASE CREDENTIALS (LIVE WEBSITE)
# ═══════════════════════════════════════════════════════════════
# The live website database uses:
#   Database=scholarrescue
#   Username=scholarrescue_user
#
# The 'postgres' user is the default PostgreSQL superuser and
# does NOT point to the live website. NEVER use postgres user
# for production operations.
# ═══════════════════════════════════════════════════════════════

# Allow the connection string to be overridden via environment or passed inline
MIGRATION_CONNECTION="${ConnectionStrings__DefaultConnection:-}"

echo "========================================"
echo " ScholarRescue Deploy — $(date --iso-8601=seconds)"
echo "========================================"

# --------------------------------------------------
# Step 0: Validate prerequisites
# --------------------------------------------------
echo ""
echo "[0/6] Validating prerequisites..."

if [ ! -d "$DEPLOY_DIR" ]; then
    echo "FATAL: Source directory $DEPLOY_DIR does not exist."
    echo "       Clone the repository first:"
    echo "       git clone <remote> $DEPLOY_DIR"
    exit 1
fi

if [ -z "$MIGRATION_CONNECTION" ]; then
    echo "FATAL: ConnectionStrings__DefaultConnection is not set."
    echo "       Export it before running this script, e.g.:"
    echo "       export ConnectionStrings__DefaultConnection='Host=localhost;Port=5432;Database=scholarrescue;Username=scholarrescue_user;Password=...'"
    echo ""
    echo "       WARNING: Use scholarrescue_user, NOT postgres!"
    exit 1
fi

# ═══════════════════════════════════════════════════════════════
# Validate database user — reject 'postgres' in production
# ═══════════════════════════════════════════════════════════════
if echo "$MIGRATION_CONNECTION" | grep -qi "Username=postgres" || echo "$MIGRATION_CONNECTION" | grep -qi "User Id=postgres"; then
    echo ""
    echo "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"
    echo "FATAL: Connection string uses Username=postgres!"
    echo ""
    echo "The 'postgres' user is the default PostgreSQL superuser account."
    echo "It does NOT point to the live website database."
    echo ""
    echo "The correct database credentials are:"
    echo "  Database=scholarrescue"
    echo "  Username=scholarrescue_user"
    echo ""
    echo "Set ConnectionStrings__DefaultConnection to the correct value before deploying."
    echo "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"
    exit 1
fi

echo "       Source dir:      $DEPLOY_DIR"
echo "       Publish dir:      $PUBLISH_DIR"
echo "       Backup dir:       $BACKUP_BASE"
echo "       Connection set:   ${MIGRATION_CONNECTION:+yes}"
echo "       OK"

# --------------------------------------------------
# Step 1: Backup current publish output
# --------------------------------------------------
echo ""
echo "[1/6] Backing up current publish output..."

TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_DIR="${BACKUP_BASE}/rollback_${TIMESTAMP}"

mkdir -p "$BACKUP_DIR"

if [ -d "$PUBLISH_DIR" ] && [ -n "$(ls -A "$PUBLISH_DIR" 2>/dev/null)" ]; then
    cp -r "${PUBLISH_DIR}"/* "$BACKUP_DIR/"
    echo "       Backup created at $BACKUP_DIR"
else
    echo "       Publish directory is empty or missing — nothing to back up."
fi

# --------------------------------------------------
# Step 2: Pull latest code from git
# --------------------------------------------------
echo ""
echo "[2/6] Pulling latest code..."

cd "$DEPLOY_DIR"
git fetch origin
git reset --hard origin/main

DEPLOYED_COMMIT=$(git log -1 --oneline)
echo "       Deployed commit: $DEPLOYED_COMMIT"

# --------------------------------------------------
# Step 3: Build and publish
# --------------------------------------------------
echo ""
echo "[3/6] Building and publishing..."

dotnet publish -c Release -r linux-x64 --self-contained false -o "$PUBLISH_DIR"
BUILD_EXIT=$?

if [ $BUILD_EXIT -ne 0 ]; then
    echo ""
    echo "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"
    echo "BUILD FAILED (exit code $BUILD_EXIT)"
    echo "The service has NOT been restarted."
    echo "Previous publish output is still in place."
    echo "Fix the build error and re-run this script."
    echo "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"
    exit $BUILD_EXIT
fi

echo "       Build succeeded. Output: $PUBLISH_DIR"

# --------------------------------------------------
# Step 4: Apply pending database migrations
# --------------------------------------------------
echo ""
echo "[4/6] Applying database migrations..."

cd "$DEPLOY_DIR"
dotnet ef database update --project . --connection "$MIGRATION_CONNECTION"
MIGRATE_EXIT=$?

if [ $MIGRATE_EXIT -ne 0 ]; then
    echo ""
    echo "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"
    echo "DATABASE MIGRATION FAILED (exit code $MIGRATE_EXIT)"
    echo "The service has NOT been restarted."
    echo "The new binaries are published but the database"
    echo "is out of sync. Do NOT restart manually until the"
    echo "migration issue is resolved."
    echo "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"
    exit $MIGRATE_EXIT
fi

echo "       Migrations applied successfully."

# --------------------------------------------------
# Step 5: Restart the service
# --------------------------------------------------
echo ""
echo "[5/6] Restarting $SERVICE service..."

systemctl restart "$SERVICE"
echo "       Waiting for service to come up..."
sleep 5

# --------------------------------------------------
# Step 6: Smoke test via health endpoint
# --------------------------------------------------
echo ""
echo "[6/6] Running smoke test (${HEALTH_URL})..."

HEALTH_STATUS=$(curl -s -o /dev/null -w "%{http_code}" --connect-timeout 10 --max-time 15 "$HEALTH_URL" 2>/dev/null || echo "000")

echo ""
echo "========================================"
echo " DEPLOYMENT SUMMARY"
echo "========================================"

if [ "$HEALTH_STATUS" = "200" ]; then
    echo " Health check:       PASSED (HTTP $HEALTH_STATUS)"
    HEALTH_RESULT="PASSED"
else
    echo " Health check:       FAILED (HTTP ${HEALTH_STATUS})"
    HEALTH_RESULT="FAILED"
    echo ""
    echo " --- Last 50 lines of service logs ---"
    journalctl -u "$SERVICE" -n 50 --no-pager
    echo " --- End of logs ---"
fi

echo " Deployed commit:    $DEPLOYED_COMMIT"
echo " Service status:     $(systemctl is-active "$SERVICE")"
echo ""
echo " For deeper investigation:"
echo "   journalctl -u $SERVICE -n 200 --no-pager"
echo "   less ${BACKUP_DIR}/ (rollback available)"
echo ""
echo " Deployed commit $DEPLOYED_COMMIT — health check $HEALTH_RESULT"
echo "========================================"

if [ "$HEALTH_STATUS" != "200" ]; then
    exit 1
fi