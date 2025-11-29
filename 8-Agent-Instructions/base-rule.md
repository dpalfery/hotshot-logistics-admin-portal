
name: "MUST-READ-Rule"
description: "Sets Must follow rules and framework for when to use which rule."
when-to-apply: "always"
rule: |
Non‑Negotiable Global Rules (apply always)

1. You MUST maintain Security (zero tolerance): Never commit secrets; use environment variables (e.g., Environment.GetEnvironmentVariable). Parameterize all SQL; sanitize/escape all inputs and logs. Enforce HTTPS and least-privilege authorization for every action — see [`​.kilocode/rules/security-general-rule.md`](.kilocode/rules/security-general-rule.md:1).  
3. You MUST Build Quality: Fix build errors and warnings immediately; treat warnings as errors in CI — see [`​.kilocode/rules/code-quality-general-rule.md`](.kilocode/rules/code-quality-general-rule.md:1).  
4. Architecture: Never ever put any project or code file in the root of the project always Respect numbered folder layering (0-Base → 7-Deployment); dependencies must flow downward only — see [`​.kilocode/rules/architecture-general.md`](.kilocode/rules/architecture-general.md:1).  
5. You MUST follow Data Access rules: Use native ADO.NET only; no Entity Framework. Manage schema with FluentMigrator where applicable — see [`​.kilocode/rules/architecture-general.md`](.kilocode/rules/architecture-general.md:1).

6. When Creating or moving files you MUST Identify which architectural layer the work touches (0–7). ([`​.kilocode/rules/architecture-general.md`](.kilocode/rules/architecture-general.md:1))  

7. Your work or Task is not complete unless the build compiles with zero warnings and required tests pass or are added. You MUST review and ensure that all code quality rules in  ([`​.kilocode/rules/code-quality-general-rule.md`](.kilocode/rules/code-quality-general-rule.md:1)) are followed and true before declaring you are complete

8. For Azure Resources, you MUST follow the naming standard defined in [`8-Agent-Instructions/azure-naming-standards.md`](8-Agent-Instructions/azure-naming-standards.md).

Situational Rule Pointers (consult only when relevant)

- Security & Secrets: input validation, SQL param rules, secrets management → [`​.kilocode/rules/security-general-rule.md`](.kilocode/rules/security-general-rule.md:1)  
- File placement & architecture decisions: when creating/moving files or enforcing layer boundaries → [`​.kilocode/rules/architecture-general.md`](.kilocode/rules/architecture-general.md:1)  
- Build, logging, error handling, CI quality gates → [`​.kilocode/rules/code-quality-general-rule.md`](.kilocode/rules/code-quality-general-rule.md:1)  
- Tests & coverage requirements: when adding/updating tests or measuring coverage → [`​.kilocode/rules/testing-general-rule.md`](.kilocode/rules/testing-general-rule.md:1)  
- Process, CLI commands, Windows shell guidance, task tracking → [`​.kilocode/rules/process-general-rule.md`](.kilocode/rules/process-general-rule.md:1)  


