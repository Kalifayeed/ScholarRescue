# ScholarRescue Production Deployment Package

## What's Included

| File | Purpose |
|------|---------|
| `production-setup.sh` | **One-script server setup** — run on a fresh Ubuntu VPS |
| `nginx-scholarrescue.conf` | Production Nginx config with SSL, HSTS, caching, WebSocket support |
| `../appsettings.Production.json` | Production configuration with environment variable placeholders |

## Quick Deploy (Fresh Ubuntu VPS)

```bash
# 1. Copy files to server
scp -r Deployment/* root@your-server:/tmp/
scp -r Deployment/nginx-scholarrescue.conf root@your-server:/tmp/

# 2. SSH in and run
ssh root@your-server
bash /tmp/production-setup.sh

# 3. Deploy application binaries (from local build)
dotnet publish -c Release -o ./publish
rsync -avz ./publish/ root@your-server:/var/www/scholarrescue/
scp appsettings.Production.json root@your-server:/var/www/scholarrescue/

# 4. Set environment secrets
ssh root@your-server
nano /etc/systemd/system/scholarrescue.service
# Fill in: Paystack__SecretKey, Email__SmtpHost, etc.

# 5. Get SSL certificate
certbot --nginx -d scholarrescue.com -d www.scholarrescue.com

# 6. Apply migrations & start
dotnet ef database update
systemctl start scholarrescue

# 7. Verify
curl -I https://scholarrescue.com
```

## Environment Variables

Set these in `/etc/systemd/system/scholarrescue.service`:

| Variable | Description | Source |
|----------|-------------|--------|
| `Paystack__SecretKey` | Paystack live secret key | Paystack Dashboard |
| `Paystack__PublicKey` | Paystack live public key | Paystack Dashboard |
| `Email__SmtpHost` | SMTP server (e.g., smtp.gmail.com) | Email provider |
| `Email__SmtpPort` | SMTP port (587 for TLS) | Email provider |
| `Email__Username` | SMTP login email | Email provider |
| `Email__Password` | SMTP password or app password | Email provider |

## Backup

Backups run daily at 2 AM via cron. Stored in `/var/backups/scholarrescue/`.

Manual backup:
```bash
sudo bash /usr/local/bin/scholarrescue-backup.sh
```

Restore:
```bash
gunzip -c /var/backups/scholarrescue/db_20260610_020000.sql.gz | psql -h localhost -U scholarrescue_user scholarrescue
```

## Monitoring

- **Systemd**: `systemctl status scholarrescue`
- **Nginx**: `nginx -t && systemctl status nginx`
- **PostgreSQL**: `systemctl status postgresql`
- **Redis**: `systemctl status redis-server`
- **Logs**: `journalctl -u scholarrescue -f`
- **Nginx logs**: `/var/log/nginx/scholarrescue.access.log`

## Server Requirements

| Resource | Minimum | Recommended |
|----------|---------|-------------|
| CPU | 2 cores | 4 cores |
| RAM | 4 GB | 8 GB |
| Disk | 20 GB SSD | 40 GB SSD |
| OS | Ubuntu 22.04 LTS | Ubuntu 24.04 LTS |
| Domain | scholarrescue.com | + www subdomain |

## Estimated Deployment Time

| Step | Time |
|------|------|
| Run `production-setup.sh` | ~5 minutes |
| Build & upload application | ~2 minutes |
| Set environment secrets | ~2 minutes |
| SSL certificate | ~1 minute |
| Apply migrations | ~1 minute |
| **Total** | **~11 minutes** |