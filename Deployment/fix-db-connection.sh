#!/bin/bash
set -e
echo "╔══════════════════════════════════════════════════════════════╗"
echo "║  Fixing ScholarRescue Database Connection                  ║"
echo "║                                                             ║"
echo "║  CORRECT DB:  Database=scholarrescue                       ║"
echo "║               Username=scholarrescue_user                  ║"
echo "║                                                             ║"
echo "║  WRONG DB:    Username=postgres (does NOT point to live)   ║"
echo "╚══════════════════════════════════════════════════════════════╝"

# ═══════════════════════════════════════════════════════════════
# IMPORTANT: The live website database uses scholarrescue_user.
# The 'postgres' user is the default PostgreSQL superuser and
# does NOT point to the live website. All implementations must
# use the scholarrescue_user account.
# ═══════════════════════════════════════════════════════════════

# Create the override directory if it doesn't exist
mkdir -p /etc/systemd/system/scholarrescue.service.d

# Write the override with correct connection string
# ═══════════════════════════════════════════════════════════════
# CREDENTIALS: Database=scholarrescue; Username=scholarrescue_user
# ═══════════════════════════════════════════════════════════════
cat > /etc/systemd/system/scholarrescue.service.d/override.conf << 'EOF'
[Service]
Environment="ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=scholarrescue;Username=scholarrescue_user;Password=Consuelo4994"
EOF

echo ""
echo "Override file written:"
cat /etc/systemd/system/scholarrescue.service.d/override.conf

# Reload systemd and restart
systemctl daemon-reload
systemctl stop scholarrescue 2>/dev/null || true
systemctl start scholarrescue

sleep 5

echo ""
echo "=== Service Status ==="
systemctl status scholarrescue --no-pager -l

echo ""
echo "=== Startup Logs (Database Info) ==="
journalctl -u scholarrescue --no-pager -n 40 | grep -E "Database|Startup|FATAL|ERROR|active|PASSED"

echo ""
echo "╔══════════════════════════════════════════════════════════════╗"
echo "║  VERIFICATION                                               ║"
echo "║  Check the logs above for:                                  ║"
echo "║  - 'Database User: scholarrescue_user'  ✓ correct           ║"
echo "║  - 'Database User: postgres'             ✗ WRONG            ║"
echo "║  - 'Database user validation PASSED'     ✓ guard passed     ║"
echo "╚══════════════════════════════════════════════════════════════╝"
