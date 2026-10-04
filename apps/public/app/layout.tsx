import type { Metadata } from "next";
import "./globals.css";
import "./story.css";
import "../components/portfolio/portfolio.css";
import "@fontsource/manrope/500.css";
import "@fontsource/manrope/800.css";
import "@fontsource-variable/readex-pro";
import "@fontsource-variable/jetbrains-mono";
import { demoSnapshot } from "../lib/data";

export const metadata: Metadata = {
  metadataBase: new URL(demoSnapshot.baseUrl),
  title: demoSnapshot.profiles.en.seo.title,
  description: demoSnapshot.profiles.en.seo.description,
  verification: demoSnapshot.searchVerificationToken ? { google: demoSnapshot.searchVerificationToken } : undefined,
  robots: demoSnapshot.profiles.en.indexable ? { index: true, follow: true } : { index: false, follow: true }
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return <html lang="en" dir="ltr" suppressHydrationWarning><body>{children}</body></html>;
}
