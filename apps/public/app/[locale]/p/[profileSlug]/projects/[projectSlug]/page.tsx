import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { ProjectDetailPage } from "../../../../../../components/ProjectDetailPage";
import { allProfiles, browserProfile, demoSnapshot, isLocale, profileBySlug, profileFor } from "../../../../../../lib/data";
import { absoluteAsset, projectOgPath } from "../../../../../../lib/og";
import "../../../../../project-detail.css";

export const dynamicParams = false;

export function generateStaticParams() {
  const params = allProfiles().flatMap(profile => profile.projects.map(project => ({ locale: profile.locale, profileSlug: profile.slug, projectSlug: project.slug })));
  if (params.length > 0) return params;

  // Static export requires at least one path for this dynamic route. Generate
  // one private placeholder that the page resolves to notFound(), so profiles
  // with no projects export cleanly without adding demo content.
  const profile = allProfiles()[0];
  return profile ? [{ locale: profile.locale, profileSlug: profile.slug, projectSlug: "__no_projects__" }] : [];
}

export async function generateMetadata({ params }: { params: Promise<{ locale: string; profileSlug: string; projectSlug: string }> }): Promise<Metadata> {
  const { locale, profileSlug, projectSlug } = await params;
  if (!isLocale(locale)) return {};
  const profile = profileBySlug(locale, profileSlug); const project = profile?.projects.find(x => x.slug === projectSlug);
  if (!project || !profile) return {};
  const setting = profile.projectSeo?.[project.slug];
  const canonical = setting?.canonical ?? `${demoSnapshot.baseUrl}/${locale}/p/${profile.slug}/projects/${project.slug}/`;
  const image = setting?.ogImage?.src ?? absoluteAsset(demoSnapshot.baseUrl, projectOgPath(profile, project.slug));
  const imageAlt = setting?.ogImage?.alt ?? `${project.name} — ${profile.fullName}`;
  const imageWidth = setting?.ogImage?.width ?? 1200;
  const imageHeight = setting?.ogImage?.height ?? 630;
  const indexable = profile.indexable && setting?.indexable !== false;
  const title = setting?.title ?? `${project.name} — ${profile.fullName}`;
  const description = setting?.description ?? project.summary;
  const other = locale === "ar" ? "en" : "ar";
  const counterpart = profileBySlug(other, profileSlug);
  const counterpartProject = counterpart?.projects.find(value => value.slug === projectSlug);
  const counterpartSetting = counterpart?.projectSeo?.[projectSlug];
  const counterpartIndexable = counterpart?.indexable === true && counterpartSetting?.indexable !== false;
  const languages = indexable && counterpartProject && counterpartIndexable ? { [locale]: canonical, [other]: counterpartSetting?.canonical ?? `${demoSnapshot.baseUrl}/${other}/p/${counterpart.slug}/projects/${counterpartProject.slug}/` } : undefined;
  return { title, description, alternates: { canonical, languages }, robots: indexable ? { index: true, follow: true } : { index: false, follow: true }, openGraph: { type: "article", url: canonical, title, description, locale: locale === "ar" ? "ar_AR" : "en_US", images: [{ url: image, width: imageWidth, height: imageHeight, alt: imageAlt }] }, twitter: { card: "summary_large_image", title, description, images: [{ url: image, alt: imageAlt }] } };
}

export default async function ProjectDetail({ params }: { params: Promise<{ locale: string; profileSlug: string; projectSlug: string }> }) {
  const { locale, profileSlug, projectSlug } = await params;
  if (!isLocale(locale)) notFound();
  const profile = profileBySlug(locale, profileSlug); const project = profile?.projects.find(x => x.slug === projectSlug);
  if (!project || !profile) notFound();
  return <ProjectDetailPage profile={browserProfile(profile)} projectSlug={project.slug}
    isDefault={profileFor(locale).slug === profile.slug}
    hasCounterpart={!!profileBySlug(locale === "ar" ? "en" : "ar", profile.slug)}
    siteId={demoSnapshot.siteId} apiBaseUrl={process.env.PUBLIC_API_BASE_URL?.replace(/\/$/, "") ?? ""} />;
}
