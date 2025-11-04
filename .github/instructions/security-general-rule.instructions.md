# Security General Rule

## Secrets Management
- **NEVER** hardcode secrets. Use `Environment.GetEnvironmentVariable("SECRET_NAME")` or secure services like Azure Key Vault
- **NEVER** store secrets in appsettings.json, .env files, or any plain text files
- Database connection strings are secrets and must never be stored in any file
- It is better the app not work than for a secret to be exposed

## Input Validation & Sanitization
- **ESCAPE ALL INPUTS** contextually before use:
  - **SQL**: Use parameterized queries ONLY. Never string concatenation
  - **HTML/UI**: Encode output to prevent XSS
  - **OS Commands**: Avoid if possible; use APIs that accept arguments as a list
- **SANITIZE BEFORE LOGGING**: Replace newlines/tabs with spaces, use structured logging with placeholders

## Secure Communication & Configuration
- **ENFORCE HTTPS**: Redirect HTTP to HTTPS, set HSTS headers

## Authentication & Authorization
- **PRINCIPLE OF LEAST PRIVILEGE**: Default to no access, explicitly grant permissions
- **AUTHORIZE EVERY ACTION**: Check permissions after authentication for every data access/action

## Dependency & Operational Security
- **FLAG VULNERABLE DEPENDENCIES**: Regularly scan with `npm audit`, `snyk test`, etc.
- **IMPLEMENT RATE LIMITING**: Enforce on public APIs to prevent abuse

## Code Review & Threat Analysis
Ask these questions for each function/endpoint:
1. **Spoofing**: Is the user authenticated?
2. **Tampering**: Is data validated? HTTPS enforced?
3. **Repudiation**: Sufficient audit logs with correlation IDs?
4. **Information Disclosure**: No secrets leaked in logs/errors?
5. **Denial of Service**: Resource limiting on expensive operations?
6. **Elevation of Privilege**: Permissions checked for every resource access?

## Incident Response Readiness
- **LOG FOR INCIDENTS**: Structured logs with correlation IDs (non-negotiable)
- **CLEAR ERROR HANDLING**: No stack traces or internal details exposed to users

## When to Apply
Always at the start of every task. Include `[Security Rule: Active]` in responses if successfully read.