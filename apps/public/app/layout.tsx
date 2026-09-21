import type { Metadata } from "next";
import "./globals.css";
import { demoSnapshot } from "../lib/data";

export const metadata: Metadata = {
  metadataBase: new URL(demoSnapshot.baseUrl),
  title: demoSnapshot.profiles.en.seo.title,
  description: demoSnapshot.profiles.en.seo.description,
  verification: demoSnapshot.searchVerificationToken ? { google: demoSnapshot.searchVerificationToken } : undefined,
  robots: demoSnapshot.profiles.en.indexable ? { index: true, follow: true } : { index: false, follow: true }
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return <html lang="en" dir="ltr" suppressHydrationWarning><body>{children}<script src="/site.js" defer data-static-runtime /></body></html>;
}
