# Phase 12C – Writer Screen Names, Account Uniqueness & Multi-Account Fraud Detection

## Implementation Plan

### Core Files Created/Modified:
- [ ] `Models/AccountFraudAlert.cs` ✅ Created
- [ ] `Services/IAccountFraudService.cs` ✅ Renamed
- [ ] `Services/AccountFraudService.cs` - Rename from FraudDetectionService.cs
- [ ] `Models/ApplicationUser.cs` - Add ScreenName, WriterId, LastKnownIPAddress, RegistrationIPAddress, LastUsernameChangeDate, UsernameChangeCount
- [ ] `Data/ScholarRescueDbContext.cs` - Add AccountFraudAlerts DbSet
- [ ] `Program.cs` - Register IAccountFraudService
- [ ] Migration for new columns/tables