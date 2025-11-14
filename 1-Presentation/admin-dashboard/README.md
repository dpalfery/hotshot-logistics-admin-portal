This is a [Next.js](https://nextjs.org) project bootstrapped with [`create-next-app`](https://nextjs.org/docs/app/api-reference/cli/create-next-app).

## Environment Configuration

This project requires Azure AD configuration for authentication. The `scripts/generate-env.js` script automatically creates `.env.local` from system environment variables.

### Required Environment Variables

Set these before running the development server or build:

```bash
export NEXT_PUBLIC_AZURE_CLIENT_ID="your-azure-client-id"
export NEXT_PUBLIC_AZURE_TENANT_ID="your-azure-tenant-id"
```

### CI/Test Environments

In CI pipelines or test environments, the script automatically uses test values if environment variables are not set. This allows E2E tests and builds to run without requiring real Azure AD credentials.

### How It Works

1. **Before dev/build**: The `predev`/`prebuild` script runs `generate-env.js`
2. **Generate .env.local**: Script reads environment variables and creates `.env.local`
3. **CI Detection**: If `CI=true` or `NODE_ENV=test`, uses test defaults automatically
4. **Validation**: In non-CI environments, validates that variables are set and not placeholders

## Getting Started

First, run the development server:

```bash
npm run dev
# or
yarn dev
# or
pnpm dev
# or
bun dev
```

Open [http://localhost:3000](http://localhost:3000) with your browser to see the result.

You can start editing the page by modifying `app/page.tsx`. The page auto-updates as you edit the file.

This project uses [`next/font`](https://nextjs.org/docs/app/building-your-application/optimizing/fonts) to automatically optimize and load [Geist](https://vercel.com/font), a new font family for Vercel.

## Learn More

To learn more about Next.js, take a look at the following resources:

- [Next.js Documentation](https://nextjs.org/docs) - learn about Next.js features and API.
- [Learn Next.js](https://nextjs.org/learn) - an interactive Next.js tutorial.

You can check out [the Next.js GitHub repository](https://github.com/vercel/next.js) - your feedback and contributions are welcome!

## Deploy on Vercel

The easiest way to deploy your Next.js app is to use the [Vercel Platform](https://vercel.com/new?utm_medium=default-template&filter=next.js&utm_source=create-next-app&utm_campaign=create-next-app-readme) from the creators of Next.js.

Check out our [Next.js deployment documentation](https://nextjs.org/docs/app/building-your-application/deploying) for more details.
