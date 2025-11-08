# Integration Tests Setup Checklist

Use this checklist to set up GitHub Actions integration tests for your repository.

## Prerequisites

- [ ] Repository is on GitHub
- [ ] You have admin access to the repository
- [ ] GitHub Actions is enabled for your repository

## Step 1: Set Up GitHub Secret

### Option A: Web Interface (Recommended)

1. - [ ] Go to your GitHub repository
2. - [ ] Click **Settings** (top navigation)
3. - [ ] Click **Secrets and variables** → **Actions** (left sidebar)
4. - [ ] Click **New repository secret** (green button)
5. - [ ] Enter the following:
   - Name: `SQL_SA_PASSWORD`
   - Value: A strong password (min 8 chars, uppercase, lowercase, numbers, special chars)
   - Example: `MySecure!Pass123`
6. - [ ] Click **Add secret**

### Option B: GitHub CLI (Alternative)

```bash
# Install GitHub CLI if needed
# https://cli.github.com/

# Authenticate
gh auth login

# Set the secret
gh secret set SQL_SA_PASSWORD --body "YourStrong!Passw0rd"

# Verify it was set
gh secret list
```

### Password Requirements

Your `SQL_SA_PASSWORD` must contain:
- ✅ At least 8 characters
- ✅ Uppercase letters (A-Z)
- ✅ Lowercase letters (a-z)
- ✅ Numbers (0-9)
- ✅ Special characters (!@#$%^&*)

❌ **DO NOT** use weak passwords like:
- `password`
- `Password123`
- `12345678`

## Step 2: Verify Workflow Files

- [ ] Confirm `.github/workflows/integration-tests.yml` exists
- [ ] Confirm `.github/workflows/INTEGRATION_TESTS_SETUP.md` exists (this file)

```bash
# Check if files exist
ls -la .github/workflows/integration-tests.yml
ls -la .github/workflows/INTEGRATION_TESTS_SETUP.md
```

## Step 3: Test the Workflow

### Option A: Manual Trigger (Safest)

1. - [ ] Go to your GitHub repository
2. - [ ] Click **Actions** tab
3. - [ ] Select "Integration Tests with SQL Server" workflow
4. - [ ] Click **Run workflow** dropdown
5. - [ ] Select your branch
6. - [ ] Click **Run workflow**
7. - [ ] Wait for the workflow to complete (~5-8 minutes)
8. - [ ] Verify all steps passed (green checkmarks)

### Option B: Push to Trigger

```bash
# Make a small change to trigger the workflow
echo "# Test" >> README.md
git add README.md
git commit -m "test: trigger integration tests workflow"
git push
```

## Step 4: Verify Results

- [ ] Workflow completed successfully (all green checkmarks)
- [ ] SQL Server container started
- [ ] DbSetup CLI ran successfully
- [ ] Integration tests passed
- [ ] Test results uploaded as artifacts

### If Workflow Fails

1. **Check the workflow logs**:
   - Go to **Actions** → Click on the failed run
   - Expand each step to see detailed logs
   - Look for red ❌ marks

2. **Common issues**:
   - ❌ Secret not set → Go back to Step 1
   - ❌ SQL Server not ready → Check health check logs
   - ❌ DbSetup CLI failed → Review DbSetup step output
   - ❌ Tests failed → Check test logs for specific errors

3. **Get help**:
   - Review `INTEGRATION_TESTS_SETUP.md` troubleshooting section
   - Check GitHub Actions status page
   - Review SQL Server container logs in workflow output

## Step 5: Configure Branch Protection (Optional)

Require integration tests to pass before merging PRs:

1. - [ ] Go to **Settings** → **Branches**
2. - [ ] Click **Add rule** or edit existing rule for `main`
3. - [ ] Check "Require status checks to pass before merging"
4. - [ ] Search for and select: `integration-tests`
5. - [ ] Click **Create** or **Save changes**

## Step 6: Monitor and Maintain

### Regular Maintenance

- [ ] Rotate `SQL_SA_PASSWORD` every 90 days
- [ ] Review failed workflow runs weekly
- [ ] Keep SQL Server image updated (`mcr.microsoft.com/mssql/server:2022-latest`)
- [ ] Monitor GitHub Actions minutes usage

### GitHub Actions Minutes

Check your usage:
1. - [ ] Go to **Settings** → **Billing and plans**
2. - [ ] Review **Actions minutes** usage
3. - [ ] Typical cost: ~5-8 minutes per run

**Free tier**: 2,000 minutes/month (private repos), unlimited (public repos)

## Troubleshooting Quick Reference

| Issue | Solution |
|-------|----------|
| "Secret not found" | Set `SQL_SA_PASSWORD` in repository secrets |
| "SQL Server not ready" | Check password meets complexity requirements |
| "DbSetup CLI failed" | Review DbSetup step logs, verify migrations |
| "Tests failed" | Check test logs, verify seed data |
| "Workflow not triggering" | Verify file paths match trigger conditions |

## Success Criteria

Your setup is complete when:

- ✅ GitHub secret `SQL_SA_PASSWORD` is set
- ✅ Workflow runs successfully on manual trigger
- ✅ All steps show green checkmarks
- ✅ Test results are uploaded as artifacts
- ✅ Workflow triggers automatically on PR/push

## Next Steps

After successful setup:

1. **Add more tests**: Expand integration test coverage
2. **Optimize workflow**: Add caching, parallel jobs
3. **Monitor results**: Set up notifications for failures
4. **Document**: Update team wiki with this setup

## Resources

- **Main Documentation**: `INTEGRATION_TESTS_SETUP.md`
- **DbSetup CLI**: `7-Deployment/DbSetup/README.md`
- **GitHub Actions Docs**: https://docs.github.com/en/actions
- **SQL Server Docker**: https://hub.docker.com/_/microsoft-mssql-server

---

## Support Commands

```bash
# Check if secret is set
gh secret list

# View workflow runs
gh run list --workflow=integration-tests.yml

# View latest run details
gh run view --log

# Re-run failed workflow
gh run rerun <run-id>

# Check repository settings
gh repo view --json name,visibility,defaultBranchRef
```

---

**Last Updated**: 2025-11-08
**Version**: 1.0
**Maintainer**: DevOps Team
