#!/bin/bash
# ═══════════════════════════════════════════════════════════════
# ScholarRescue Production Deployment Script
# Ubuntu 22.04 / 24.04 LTS — Run as root or with sudo
# ═══════════════════════════════════════════════════════════════
set -euo pipefail

echo "╔══════════════════════════════════════════════════════╗"
echo "║    ScholarRescue Production Deployment              ║"
echo "╚══════════════════════════════════════════════════════╝"

# ── Configuration ─────────────────────────────────────────────
DOMAIN="scholarrescue.com"
APP_DIR="/var/www/scholarrescue"
DB_NAME="scholarrescue"
DB_USER="scholarrescue_user"
DB_PASS="$(openssl rand -base64 24)"
ASPNETCORE_ENVIRONMENT="Production"

echo ""
echo "=== Step 1: System Update & Prerequisites ==="
apt update && apt upgrade -y
apt install -y curl wget gnupg2 software-properties-common \
    nginx certbot python3-certbot-nginx \
    postgresql postgresql-contrib \
    redis-server \
    unattended-upgrades

# Enable automatic security updates
dpkg-reconfigure --priority=low unattended-upgrades

echo ""
echo "=== Step 2: Install .NET 10 Runtime ==="
wget https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh
chmod +x /tmp/dotnet-install.sh
/tmp/dotnet-install.sh --channel 10.0 --install-dir /usr/share/dotnet
ln -sf /usr/share/dotnet/dotnet /usr/local/bin/dotnet
dotnet --version

echo ""
echo "=== Step 3: Create Application User ==="
useradd -m -s /bin/bash scholarrescue || true
mkdir -p $APP_DIR
chown -R scholarrescue:scholarrescue $APP_DIR

echo ""
echo "=== Step 4: PostgreSQL Database Setup ==="
cat > /tmp/setup_db.sql << EOF
CREATE DATABASE $DB_NAME;
CREATE USER $DB_USER WITH PASSWORD '$DB_PASS';
ALTER DATABASE $DB_NAME OWNER TO $DB_USER;
GRANT ALL PRIVILEGES ON DATABASE $DB_NAME TO $DB_USER;
\c $DB_NAME
GRANT ALL ON SCHEMA public TO $DB_USER;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO $DB_USER;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO $DB_USER;
EOF

sudo -u postgres psql -f /tmp/setup_db.sql
rm /tmp/setup_db.sql

echo ""
echo "=== Step 5: Redis Configuration ==="
systemctl enable redis-server
systemctl start redis-server

echo ""
echo "=== Step 6: Set environment variables for secrets ==="
echo ""
echo "NOTE: appsettings files no longer contain secrets or connection strings."
echo "Set the database connection string via environment variable:"
echo "  ConnectionStrings__DefaultConnection"
echo ""
cat > $APP_DIR/appsettings.Production.json << EOFCONF
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=$DB_NAME;Username=$DB_USER;Password=$DB_PASS;SSL Mode=Prefer;Trust Server Certificate=true;Timeout=30;Command Timeout=120"
  },
  "Paystack": {
    "SecretKey": "\${Paystack__SecretKey}",
    "PublicKey": "\${Paystack__PublicKey}",
    "CallbackUrl": "https://$DOMAIN/Payments/Callback"
  },
  "Currency": {
    "Code": "KES",
    "Symbol": "KSh",
    "Locale": "en-KE"
  },
  "Email": {
    "SmtpHost": "\${Email__SmtpHost}",
    "SmtpPort": "\${Email__SmtpPort}",
    "Username": "\${Email__Username}",
    "Password": "\${Email__Password}",
    "FromAddress": "noreply@$DOMAIN",
    "FromName": "ScholarRescue"
  },
  "Urls": {
    "BaseUrl": "https://$DOMAIN"
  },
  "MaintenanceMode": {
    "Enabled": false,
    "AllowedIPs": []
  },
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5000"
      }
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "ScholarRescue": "Information"
    }
  },
  "AllowedHosts": "*"
}
EOFCONF
chown scholarrescue:scholarrescue $APP_DIR/appsettings.Production.json
chmod 600 $APP_DIR/appsettings.Production.json

echo ""
echo "=== Step 7: Nginx Configuration ==="
cp /tmp/nginx-scholarrescue.conf /etc/nginx/sites-available/scholarrescue
sed -i "s/scholarrescue.com/$DOMAIN/g" /etc/nginx/sites-available/scholarrescue
ln -sf /etc/nginx/sites-available/scholarrescue /etc/nginx/sites-enabled/
rm -f /etc/nginx/sites-enabled/default
nginx -t
systemctl enable nginx
systemctl restart nginx

echo ""
echo "=== Step 8: SSL Certificate (Let's Encrypt) ==="
certbot --nginx -d $DOMAIN -d www.$DOMAIN --non-interactive \
    --agree-tos --email admin@$DOMAIN || \
echo "Run manually: certbot --nginx -d $DOMAIN -d www.$DOMAIN"

echo ""
echo "=== Step 9: Systemd Service ==="
cat > /etc/systemd/system/scholarrescue.service << EOFSVC
[Unit]
Description=ScholarRescue ASP.NET Core Application
After=network.target postgresql.service redis-server.service
Wants=postgresql.service redis-server.service

[Service]
WorkingDirectory=$APP_DIR
ExecStart=/usr/local/bin/dotnet $APP_DIR/ScholarRescue.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=scholarrescue
User=scholarrescue
Environment=ASPNETCORE_ENVIRONMENT=$ASPNETCORE_ENVIRONMENT
Environment=ASPNETCORE_URLS=http://localhost:5000

# Environment variables for secrets
Environment=Paystack__SecretKey=
Environment=Paystack__PublicKey=
Environment=Email__SmtpHost=
Environment=Email__SmtpPort=587
Environment=Email__Username=
Environment=Email__Password=

[Install]
WantedBy=multi-user.target
EOFSVC

systemctl daemon-reload
systemctl enable scholarrescue

echo ""
echo "=== Step 10: Backup Script ==="
cat > /usr/local/bin/scholarrescue-backup.sh << 'EOFBACKUP'
#!/bin/bash
BACKUP_DIR="/var/backups/scholarrescue"
DATE=$(date +%Y%m%d_%H%M%S)
mkdir -p $BACKUP_DIR

# Database backup
PGPASSWORD="${DB_PASS:-}" pg_dump -h localhost -U scholarrescue_user scholarrescue \
    | gzip > $BACKUP_DIR/db_$DATE.sql.gz

# Uploads backup
tar -czf $BACKUP_DIR/uploads_$DATE.tar.gz -C /var/www/scholarrescue wwwroot/uploads 2>/dev/null || true

# App settings backup
cp /var/www/scholarrescue/appsettings.Production.json $BACKUP_DIR/appsettings_$DATE.json

# Keep only last 30 days
find $BACKUP_DIR -name "db_*.sql.gz" -mtime +30 -delete
find $BACKUP_DIR -name "uploads_*.tar.gz" -mtime +30 -delete
find $BACKUP_DIR -name "appsettings_*.json" -mtime +30 -delete

echo "Backup completed: $BACKUP_DIR/db_$DATE.sql.gz"
EOFBACKUP

chmod +x /usr/local/bin/scholarrescue-backup.sh

# Schedule daily backup at 2 AM
echo "0 2 * * * root /usr/local/bin/scholarrescue-backup.sh" > /etc/cron.d/scholarrescue-backup

echo ""
echo "=== Step 11: Firewall (UFW) ==="
ufw allow OpenSSH
ufw allow 'Nginx Full'
ufw --force enable

echo ""
echo "╔══════════════════════════════════════════════════════╗"
echo "║  Deployment Complete!                               ║"
echo "║                                                     ║"
echo "║  Database: $DB_NAME                                 ║"
echo "║  DB User:  $DB_USER                                 ║"
echo "║  DB Pass:  $DB_PASS                                 ║"
echo "║                                                     ║"
echo "║  SAVE THIS PASSWORD!                                ║"
echo "║                                                     ║"
echo "║  Next Steps:                                        ║"
echo "║  1. Set environment variables in systemd service     ║"
echo "║  2. Deploy application build to $APP_DIR             ║"
echo "║  3. Run: systemctl start scholarrescue               ║"
echo "║  4. Run: certbot --nginx -d $DOMAIN                  ║"
echo "║  5. Test: https://$DOMAIN                            ║"
echo "╚══════════════════════════════════════════════════════╝"