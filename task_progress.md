# Task: Fix Database Connection to Point to Correct Live Database

## ❌ Problem
The three major recent implementations have been applied to the wrong database.
The correct database uses: `Database=scholarrescue; Username=scholarrescue_user`
The wrong database uses: `Username=postgres` and does NOT point to the live website.

## Root Cause
The connection string is loaded from the environment variable `ConnectionStrings__DefaultConnection`. 
This variable was set with the wrong `postgres` user instead of `scholarrescue_user`.

## ✅ Fix Plan
- [x] Add startup validation in `Program.cs` to reject incorrect database user
- [x] Update `Deployment/fix-db-connection.sh` with clearer guidance
- [x] Update `Deployment/deploy-remote.sh` to validate connection on deploy
- [x] Update `AI_MEMORY/KNOWN_ISSUES.md` with this entry
- [x] Update `AI_MEMORY/ARCHITECTURE_DECISIONS.md` with DB credentials documentation