import { NextResponse } from 'next/server';
import type { NextRequest } from 'next/server';
import { generateCspHeader, generateNonce } from './src/lib/csp';

// Production HTTPS enforcement middleware
export function middleware(request: NextRequest) {
  const response = NextResponse.next();

  // HTTPS enforcement in production
  if (process.env.NODE_ENV === 'production') {
    const forwardedProto = request.headers.get('x-forwarded-proto');
    const host = request.headers.get('host') || '';

    // Redirect HTTP to HTTPS
    if (forwardedProto === 'http' || (!forwardedProto && request.url.startsWith('http://'))) {
      const httpsUrl = `https://${host}${request.nextUrl.pathname}${request.nextUrl.search}`;
      return NextResponse.redirect(httpsUrl, 301);
    }

    // Additional security headers for production
    response.headers.set('Strict-Transport-Security', 'max-age=31536000; includeSubDomains; preload');
    response.headers.set('X-Frame-Options', 'DENY');
    response.headers.set('X-Content-Type-Options', 'nosniff');
    response.headers.set('X-XSS-Protection', '1; mode=block');
    response.headers.set('Referrer-Policy', 'strict-origin-when-cross-origin');

    // Get API URL from environment (runtime)
    // This ensures the middleware CSP includes the actual API origin
    const apiUrl = process.env.NEXT_PUBLIC_API_URL || '';

    // Generate a nonce for this request (for inline scripts/styles)
    const nonce = generateNonce();

    // Generate CSP header with API origin and nonce
    const cspHeader = generateCspHeader({
      apiUrl,
      isDevelopment: false,
      nonce,
    });

    response.headers.set('Content-Security-Policy', cspHeader);

    // Make nonce available to the response for use in inline scripts
    // This can be accessed via headers in API routes or server components
    response.headers.set('X-Nonce', nonce);
  }

  // Authentication bypass for static files and API routes that don't require auth
  const { pathname } = request.nextUrl;

  // Skip authentication for:
  // - Static files (favicon, images, etc.)
  // - API routes (handled by backend)
  // - Login page
  // - Public assets
  if (
    pathname.startsWith('/_next/') ||
    pathname.startsWith('/favicon.ico') ||
    pathname.startsWith('/api/') ||
    pathname === '/login' ||
    pathname.startsWith('/public/') ||
    pathname.includes('.')
  ) {
    return response;
  }

  // For protected routes, let the AuthProvider handle authentication
  // This middleware focuses on HTTPS and security headers only
  return response;
}

// Configure which paths the middleware should run on
export const config = {
  matcher: [
    /*
     * Match all request paths except for the ones starting with:
     * - _next/static (static files)
     * - _next/image (image optimization files)
     * - favicon.ico (favicon file)
     * - public folder
     */
    '/((?!_next/static|_next/image|favicon.ico|public/).*)',
  ],
};