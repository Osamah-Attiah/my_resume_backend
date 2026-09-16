import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { ArrowLeft, ArrowRight, ArrowUpRight } from "lucide-react";
import { allProfiles, demoSnapshot, isLocale, profileBySlug, profileFor } from "../../../../../../lib/data";
import { absoluteAsset, projectOgPath } from "../../../../../../lib/og";

export const dynamicParams = false;

export function generateStaticParams() {
  return allProfiles().flatMap(profile => profile.projects.map(project => ({ locale: profile.locale, profileSlug: profile.slug, projectSlug: project.slug })));
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
  const rtl = locale === "ar"; const Back = rtl ? ArrowRight : ArrowLeft;
  const defaultProfile = profileFor(locale).slug === profile.slug; const back = defaultProfile ? "/" + locale + "/#work" : "/" + locale + "/p/" + profile.slug + "/#work";
  return <div className="project-detail-page" lang={locale} dir={rtl ? "rtl" : "ltr"}>{profile.demo && <div className="demo-notice">{rtl ? "محتوى تجريبي" : "DEMO CONTENT"}</div>}<main className="container project-detail-main"><a className="back-link" href={back}><Back size={18} aria-hidden="true" />{rtl ? "العودة إلى الأعمال" : "Back to work"}</a><header className="detail-header"><p className="eyebrow">{rtl ? "مشروع" : "Project"}</p><h1>{project.name}</h1><p className="headline">{project.summary}</p></header>{project.media && project.media.length > 0 && <div className="project-gallery">{project.media.map(image => <figure key={image.src}><img src={image.src} width={image.width} height={image.height} alt={image.alt} />{image.caption && <figcaption>{image.caption}</figcaption>}</figure>)}</div>}<section className="section detail-body"><h2>{rtl ? "حول المشروع" : "About the project"}</h2>{project.description && <p>{project.description}</p>}<ul className="detail-list">{project.highlights.map(item => <li key={item}>{item}</li>)}</ul><ul className="tags">{project.skills.map(skill => <li className="tag" key={skill}><bdi>{skill}</bdi></li>)}</ul>{project.repositoryUrl && <a className="button button-primary" href={project.repositoryUrl} rel="noreferrer">{rtl ? "عرض الكود" : "View code"}<ArrowUpRight size={18} aria-hidden="true" /></a>}</section></main></div>;
}
