# Core Base Rules (Non-Negotiable)

## Global Rules (Always Apply)
1. **Security (Zero Tolerance)**: Never commit secrets; use environment variables. Parameterize all SQL; sanitize/escape all inputs and logs. Enforce HTTPS and least-privilege authorization.
2. **Build Quality**: Fix build errors and warnings immediately; treat warnings as errors in CI.
3. **Architecture**: Never put projects/files in root. Respect numbered folder layering (0-Base → 7-Deployment); dependencies flow downward only.
4. **Data Access**: Use native ADO.NET only; no Entity Framework. Manage schema with FluentMigrator.

## Task-Start Requirements
- Identify architectural layer touched (0–7)
- Check security constraints and secrets handling
- Ensure build compiles with zero warnings and required tests pass

## Situational Rule Pointers
- **Memory Bank**: For task tracking, status files, specialized modes
- **Security**: Input validation, SQL parameters, secrets management
- **Architecture**: File placement, layer boundaries
- **Code Quality**: Build, logging, error handling, CI gates
- **Testing**: Coverage requirements, test organization
- **Process**: CLI commands, Windows shell, task tracking

## When to Apply
Always - these are the foundation rules that govern all development activities.