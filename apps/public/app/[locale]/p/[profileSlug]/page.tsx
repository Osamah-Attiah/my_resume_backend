import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { ProfilePage } from "../../../../components/ProfilePage";
import { allProfiles, demoSnapshot, isLocale, profileBySlug } from "../../../../lib/data";
import { absoluteAsset, profileOgPath } from "../../../../lib/og";

export const dynamicParams = false;

export function generateStaticParams() { return allProfiles().map(profile => ({ locale: profile.locale, profileSlug: profile.slug })); }

export async function generateMetadata({ params }: { params: Promise<{ locale: string; profileSlug: string }> }): Promise<Metadata> {
  const { locale, profileSlug } = await params; if (!isLocale(locale)) return {};
  const profile = profileBySlug(locale, profileSlug); if (!profile) return {};
  const canonical = profile.seo.canonical ?? `${demoSnapshot.baseUrl}/${locale}/p/${profile.slug}/`;
  const image = profile.seo.ogImage?.src ?? absoluteAsset(demoSnapshot.baseUrl, profileOgPath(profile));
  const imageAlt = profile.seo.ogImage?.alt ?? `${profile.fullName} — ${profile.headline}`;
  const imageWidth = profile.seo.ogImage?.width ?? 1200;
  const imageHeight = profile.seo.ogImage?.height ?? 630;
  const other = locale === "ar" ? "en" : "ar"; const counterpart = profileBySlug(other, profileSlug);
  const languages = profile.indexable && counterpart?.indexable ? { [locale]: canonical, [other]: counterpart.seo.canonical ?? `${demoSnapshot.baseUrl}/${other}/p/${profile.slug}/` } : undefined;
  return { title: profile.seo.title, description: profile.seo.description, alternates: { canonical, languages }, robots: profile.indexable ? { index: true, follow: true } : { index: false, follow: true }, openGraph: { type: "profile", url: canonical, title: profile.seo.title, description: profile.seo.description, locale: locale === "ar" ? "ar_AR" : "en_US", images: [{ url: image, width: imageWidth, height: imageHeight, alt: imageAlt }] }, twitter: { card: "summary_large_image", title: profile.seo.title, description: profile.seo.description, images: [{ url: image, alt: imageAlt }] } };
}

export default async function TargetedProfilePage({ params }: { params: Promise<{ locale: string; profileSlug: string }> }) {
  const { locale, profileSlug } = await params; if (!isLocale(locale)) notFound();
  const profile = profileBySlug(locale, profileSlug); if (!profile) notFound();
  return <ProfilePage profile={profile} baseUrl={demoSnapshot.baseUrl} />;
}
