import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { LiveProjectDetail } from "../../../components/LiveProjectDetail";
import { allProfiles, browserProfile, demoSnapshot, isLocale } from "../../../lib/data";
import "../../project-detail.css";

export function generateStaticParams() { return [{ locale: "ar" }, { locale: "en" }]; }
export const metadata: Metadata = { robots: { index: false, follow: true } };

export default async function ProjectPage({ params }: { params: Promise<{ locale: string }> }) {
  const { locale } = await params;
  if (!isLocale(locale)) notFound();
  return <LiveProjectDetail locale={locale} profiles={allProfiles().filter(profile => profile.locale === locale).map(browserProfile)} siteId={demoSnapshot.siteId} apiBaseUrl={process.env.PUBLIC_API_BASE_URL?.replace(/\/$/, "") ?? ""} />;
}
