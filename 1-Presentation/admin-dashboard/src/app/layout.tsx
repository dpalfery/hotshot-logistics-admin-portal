import type { Metadata } from "next";
import { Inter } from "next/font/google";
import "./globals.css";
import { Providers } from "@/lib/providers";
import { AppInsightsProvider } from "@/components/AppInsightsProvider";

const inter = Inter({
  subsets: ["latin"],
});

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
      <body className={`${inter.className} antialiased`}>
        <Providers>
          <AppInsightsProvider>
            {children}
          </AppInsightsProvider>
        </Providers>
      </body>
    </html>
  );
}
