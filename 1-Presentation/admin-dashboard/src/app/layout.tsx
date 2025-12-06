import type { Metadata } from "next";
import "./globals.css";
import { Providers } from "@/lib/providers";
import { AppInsightsProvider } from "@/components/AppInsightsProvider";

export const metadata: Metadata = {
  title: "Hotshot Logistics Admin Dashboard",
  description: "Admin dashboard for Hotshot Logistics platform",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body className="antialiased">
        <Providers>
          <AppInsightsProvider>
            {children}
          </AppInsightsProvider>
        </Providers>
      </body>
    </html>
  );
}
