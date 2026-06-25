# Deployment Validator & Production Readiness

- [ ] 1. Create DeploymentValidator service with comprehensive checks and report
- [ ] 2. Remove hardcoded Username=postgres rejection (just warn)
- [ ] 3. Add directory creation at startup
- [ ] 4. Add Platform:BaseUrl, SupportEmail, Name validation
- [ ] 5. Add BaseUrl domain validation
- [ ] 6. Add Redis, SMTP, Stripe, Paystack checks
- [ ] 7. Refactor Program.cs to call DeploymentValidator.ValidateAsync()
- [ ] 8. Delete old IConfigurationHealthCheck
- [ ] 9. Build and commit