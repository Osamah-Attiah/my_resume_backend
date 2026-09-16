import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { ProfilePage } from "../../components/ProfilePage";
import { demoSnapshot, isLocale, profileFor } from "../../lib/data";
import { absoluteAsset, profileOgPath } from "../../lib/og";

export function generateStaticParams() { return [{ locale: "en" }, { locale: "ar" }]; }

export async function generateMetadata({ params }: { params: Promise<{ locale: string }> }): Promise<Metadata> {
  const { locale } = await params;
  if (!isLocale(locale)) return {};
  const profile = profileFor(locale);
  const url = profile.seo.canonical ?? `${demoSnapshot.baseUrl}/${locale}/`;
  const image = profile.seo.ogImage?.src ?? absoluteAsset(demoSnapshot.baseUrl, profileOgPath(profile));
  const imageAlt = profile.seo.ogImage?.alt ?? `${profile.fullName} — ${profile.headline}`;
  const imageWidth = profile.seo.ogImage?.width ?? 1200;
  const imageHeight = profile.seo.ogImage?.height ?? 630;
  const other = locale === "ar" ? "en" : "ar";
  const otherProfile = profileFor(other);
  const languages = profile.indexable && otherProfile.indexable ? { [locale]: url, [other]: otherProfile.seo.canonical ?? `${demoSnapshot.baseUrl}/${other}/`, "x-default": demoSnapshot.baseUrl } : undefined;
  return {
    title: profile.seo.title,
    description: profile.seo.description,
    alternates: { canonical: url, languages },
    robots: profile.indexable ? { index: true, follow: true } : { index: false, follow: true },
    openGraph: { type: "profile", url, title: profile.seo.title, description: profile.seo.description, locale: locale === "ar" ? "ar_AR" : "en_US", images: [{ url: image, width: imageWidth, height: imageHeight, alt: imageAlt }] },
    twitter: { card: "summary_large_image", title: profile.seo.title, description: profile.seo.description, images: [{ url: image, alt: imageAlt }] }
  };
}

export default async function Page({ params }: { params: Promise<{ locale: string }> }) {
  const { locale } = await params;
  if (!isLocale(locale)) notFound();
  return <ProfilePage profile={profileFor(locale)} baseUrl={demoSnapshot.baseUrl} isDefault />;
}
