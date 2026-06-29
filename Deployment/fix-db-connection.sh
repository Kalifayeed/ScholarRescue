#!/bin/bash
set -e
echo "=== Fixing ScholarRescue Database Connection ==="

# Create the override directory if it doesn't exist
mkdir -p /etc/systemd/system/scholarrescue.service.d

# Write the override with correct connection string
cat > /etc/systemd/system/scholarrescue.service.d/override.conf << 'EOF'
[Service]
Environment="ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=scholarrescue;Username=scholarrescue_user;Password=Consuelo4994"
EOF

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
journalctl -u scholarrescue --no-pager -n 40 | grep -E "Database|Startup|FATAL|ERROR|active"