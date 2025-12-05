/**
 * Structured Logger Utility
 *
 * Provides controlled logging that:
 * - Never logs sensitive information (tokens, passwords, etc.)
 * - Is disabled in production unless explicitly enabled via feature flags
 * - Provides structured logging for better debugging
 *
 * Security improvements:
 * - Prevents accidental leakage of authentication tokens
 * - Removes console.log in production builds
 * - Provides feature-flag controlled debug logging
 */

export enum LogLevel {
  ERROR = 'error',
  WARN = 'warn',
  INFO = 'info',
  DEBUG = 'debug',
}

interface LogContext {
  [key: string]: unknown;
}

class Logger {
  private isDevelopment: boolean;
  private isDebugEnabled: boolean;

  constructor() {
    this.isDevelopment = process.env.NODE_ENV === 'development';
    // Enable debug logging only if explicitly enabled via feature flag
    this.isDebugEnabled = this.isDevelopment &&
      process.env.NEXT_PUBLIC_ENABLE_DEBUG_LOGGING === 'true';
  }

  /**
   * Sanitizes log context to remove sensitive information
   */
  private sanitizeContext(context: LogContext): LogContext {
    const sensitiveKeys = [
      'token',
      'accessToken',
      'refreshToken',
      'authorization',
      'password',
      'secret',
      'apiKey',
      'api_key',
    ];

    const sanitized: LogContext = {};

    for (const [key, value] of Object.entries(context)) {
      const lowerKey = key.toLowerCase();
      const isSensitive = sensitiveKeys.some(sk => lowerKey.includes(sk));

      if (isSensitive) {
        sanitized[key] = '[REDACTED]';
      } else if (typeof value === 'string' && value.length > 1000) {
        // Truncate very long strings
        sanitized[key] = value.substring(0, 1000) + '... [truncated]';
      } else if (typeof value === 'object' && value !== null) {
        // Recursively sanitize nested objects
        sanitized[key] = this.sanitizeContext(value as LogContext);
      } else {
        sanitized[key] = value;
      }
    }

    return sanitized;
  }

  /**
   * Log an error message
   */
  error(message: string, context?: LogContext): void {
    if (this.isDevelopment) {
      const sanitized = context ? this.sanitizeContext(context) : {};
      console.error(`[ERROR] ${message}`, sanitized);
    }
    // In production, errors should be sent to a logging service
    import('./appInsights').then(({ appInsights }) => {
      if (appInsights) {
        appInsights.trackException({ 
          exception: new Error(message), 
          properties: context ? this.sanitizeContext(context) : undefined 
        });
      }
    });
  }

  /**
   * Log a warning message
   */
  warn(message: string, context?: LogContext): void {
    if (this.isDevelopment) {
      const sanitized = context ? this.sanitizeContext(context) : {};
      console.warn(`[WARN] ${message}`, sanitized);
    }
  }

  /**
   * Log an info message
   */
  info(message: string, context?: LogContext): void {
    if (this.isDevelopment) {
      const sanitized = context ? this.sanitizeContext(context) : {};
      console.info(`[INFO] ${message}`, sanitized);
    }
  }

  /**
   * Log a debug message (only when explicitly enabled)
   */
  debug(message: string, context?: LogContext): void {
    if (this.isDebugEnabled) {
      const sanitized = context ? this.sanitizeContext(context) : {};
      console.debug(`[DEBUG] ${message}`, sanitized);
    }
  }

  /**
   * Log API request (sanitized)
   */
  apiRequest(method: string, url: string, context?: LogContext): void {
    if (this.isDebugEnabled) {
      // Parse URL to hide query parameters that might contain sensitive data
      const sanitizedUrl = this.sanitizeUrl(url);
      this.debug(`API ${method}`, {
        url: sanitizedUrl,
        ...this.sanitizeContext(context || {}),
      });
    }
  }

  /**
   * Log API response (sanitized)
   */
  apiResponse(
    method: string,
    url: string,
    status: number,
    context?: LogContext
  ): void {
    if (this.isDebugEnabled) {
      const sanitizedUrl = this.sanitizeUrl(url);
      this.debug(`API ${method} Response`, {
        url: sanitizedUrl,
        status,
        ...this.sanitizeContext(context || {}),
      });
    }
  }

  /**
   * Sanitize URL to remove potentially sensitive query parameters
   */
  private sanitizeUrl(url: string): string {
    try {
      const urlObj = new URL(url);
      // Show only the pathname, hide query parameters
      return `${urlObj.origin}${urlObj.pathname}${
        urlObj.search ? '?[PARAMS]' : ''
      }`;
    } catch {
      // If URL parsing fails, just return a safe placeholder
      return '[URL]';
    }
  }
}

// Export singleton instance
export const logger = new Logger();
