#!/bin/bash
set -e

echo "╔══════════════════════════════════════════════════════════════╗"
echo "║  Fixing ContactMessages Table Ownership                     ║"
echo "║                                                             ║"
echo "║  Problem: ContactMessages table owned by postgres, but      ║"
echo "║  scholarrescue_user needs to ALTER columns during EF        ║"
echo "║  migrations at startup.                                     ║"
echo "║                                                             ║"
echo "║  Error: 42501: must be owner of table ContactMessages       ║"
echo "╚══════════════════════════════════════════════════════════════╝"
echo ""

# Stop the service to prevent it from crash-looping
echo "▸ Stopping scholarrescue service..."
systemctl stop scholarrescue 2>/dev/null || true
sleep 2

# Change table ownership to scholarrescue_user
echo "▸ Changing ContactMessages table owner to scholarrescue_user..."
sudo -u postgres psql -d scholarrescue -c "
  ALTER TABLE \"ContactMessages\" OWNER TO scholarrescue_user;
"
echo "  ✓ Table ownership changed successfully."

# Also change any dependent sequences
echo "▸ Checking and fixing sequence ownership..."
sudo -u postgres psql -d scholarrescue -c "
  DO \$\$
  DECLARE
    seq_name text;
  BEGIN
    FOR seq_name IN
      SELECT c.relname
      FROM pg_class c
      JOIN pg_depend d ON d.objid = c.oid
      WHERE c.relkind = 'S'
        AND d.refobjid = '\"ContactMessages\"'::regclass
    LOOP
      EXECUTE format('ALTER SEQUENCE %I OWNER TO scholarrescue_user;', seq_name);
    END LOOP;
  END\$\$;
"
echo "  ✓ Sequence ownership changed."

# Grant all privileges just to be safe
echo "▸ Granting ALL privileges on ContactMessages to scholarrescue_user..."
sudo -u postgres psql -d scholarrescue -c "
  GRANT ALL ON TABLE \"ContactMessages\" TO scholarrescue_user;
"
echo "  ✓ Privileges granted."

# Also grant on the Id sequence if it exists
sudo -u postgres psql -d scholarrescue -c "
  GRANT ALL ON SEQUENCE \"ContactMessages_Id_seq\" TO scholarrescue_user;
" 2>/dev/null || echo "  (no explicit sequence to grant — sequence inherited from table)"

echo ""
echo "▸ Verifying ownership..."
sudo -u postgres psql -d scholarrescue -c "
  SELECT
    tablename,
    tableowner
  FROM pg_tables
  WHERE tablename = 'ContactMessages';
"

echo ""
echo "╔══════════════════════════════════════════════════════════════╗"
echo "║  DONE                                                       ║"
echo "║  Restarting scholarrescue service...                        ║"
echo "╚══════════════════════════════════════════════════════════════╝"
echo ""

# Start the service
systemctl start scholarrescue

# Wait and check
sleep 10
echo "▸ Service status after fix:"
systemctl status scholarrescue --no-pager -l | head -5

echo ""
echo "▸ Recent logs (check for errors):"
journalctl -u scholarrescue --since "-30 seconds" --no-pager | grep -E "fail|error|Error|Exception|PASSED|health" || echo "  ✓ No errors found in recent logs"