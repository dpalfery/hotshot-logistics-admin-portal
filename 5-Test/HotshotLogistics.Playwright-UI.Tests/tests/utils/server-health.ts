export async function waitForServerReady(url: string, timeoutMs: number): Promise<void> {
  const startTime = Date.now();

  while (Date.now() - startTime < timeoutMs) {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), 5000); // 5-second per-request timeout

    try {
      const response = await fetch(url, {
        signal: controller.signal,
        method: 'HEAD', // Use HEAD to minimize data transfer
      });
      clearTimeout(timeoutId);
      if (response.ok) {
        console.log(`Server is ready at ${url}`);
        return;
      }
    } catch (error) {
      clearTimeout(timeoutId);
      if (error instanceof Error && error.name === 'AbortError') {
        console.warn(`Request to ${url} timed out after 5 seconds`);
      } else {
        const message = error instanceof Error ? error.message : String(error);
        console.warn(`Error checking server at ${url}: ${message}`);
      }
    }

    // Wait 2 seconds before next attempt
    await new Promise(resolve => setTimeout(resolve, 2000));
  }

  throw new Error(`Server at ${url} did not become ready within ${timeoutMs}ms`);
}