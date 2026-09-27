import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { ArrowDown, ArrowLeft, ArrowRight, ArrowUpLeft, ArrowUpRight, Code2, Github, Globe2, Link2, Play, Smartphone } from "lucide-react";
import { projectLinksFor } from "@resume/contracts";
import { allProfiles, demoSnapshot, isLocale, profileBySlug, profileFor } from "../../../../../../lib/data";
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
  const rtl = locale === "ar";
  const displayName = profile.slug === "osamah" && rtl ? "أسامة عطية" : profile.fullName;
  const Back = rtl ? ArrowRight : ArrowLeft;
  const Forward = rtl ? ArrowLeft : ArrowRight;
  const ExternalArrow = rtl ? ArrowUpLeft : ArrowUpRight;
  const defaultProfile = profileFor(locale).slug === profile.slug;
  const profilePath = defaultProfile ? `/${locale}/` : `/${locale}/p/${profile.slug}/`;
  const back = `${profilePath}#work`;
  const projectIndex = profile.projects.findIndex(item => item.slug === project.slug);
  const count = `${String(projectIndex + 1).padStart(2, "0")} / ${String(profile.projects.length).padStart(2, "0")}`;
  const nextProject = profile.projects.length > 1 ? profile.projects[(projectIndex + 1) % profile.projects.length] : undefined;
  const nextUrl = nextProject ? `/${locale}/p/${profile.slug}/projects/${nextProject.slug}/` : undefined;
  const gallery = [...(project.cover ? [project.cover] : []), ...(project.media ?? []).filter(image => image.src !== project.cover?.src)];
  const projectLinks = projectLinksFor(project, locale);
  const kind = rtl
    ? { Personal: "مشروع شخصي", OpenSource: "مصدر مفتوح", Freelance: "عمل حر", Employment: "خبرة وظيفية" }[project.kind]
    : { Personal: "Personal project", OpenSource: "Open source", Freelance: "Freelance", Employment: "Professional work" }[project.kind];
  const copy = rtl
    ? { work: "الأعمال", back: "العودة إلى الأعمال", project: "قراءة مشروع", read: "اقرأ التفاصيل", scope: "نطاق المشروع", type: "نوع العمل", inThis: "في هذه الصفحة", about: "عن المشروع", made: "ما أنجزته", tools: "التقنيات", images: "صور المشروع", links: "روابط المشروع", find: "تصفّح المشروع خارج هذه الصفحة", available: "متاح عبر", store: "التطبيق", source: "الكود", external: "رابط خارجي", newTab: "يفتح في تبويب جديد", next: "المشروع التالي", all: "كل الأعمال" }
    : { work: "Work", back: "Back to work", project: "Project story", read: "Explore the details", scope: "Project scope", type: "Work type", inThis: "On this page", about: "About the project", made: "What I built", tools: "Tools & technologies", images: "Project images", links: "Project links", find: "Explore the project beyond this page", available: "Available on", store: "App", source: "Source", external: "External link", newTab: "opens in a new tab", next: "Next project", all: "All work" };
  const sections = [
    { id: "project-about", label: copy.about },
    ...(project.highlights.length ? [{ id: "project-highlights", label: copy.made }] : []),
    ...(project.skills.length ? [{ id: "project-tools", label: copy.tools }] : []),
    ...(gallery.length ? [{ id: "project-images", label: copy.images }] : [])
  ].map((section, index) => ({ ...section, number: String(index + 1).padStart(2, "0") }));
  const sectionNumber = (id: string) => sections.find(section => section.id === id)?.number;

  return <div className="project-detail-page" lang={locale} dir={rtl ? "rtl" : "ltr"}>
    {profile.demo && <div className="demo-notice">{rtl ? "محتوى تجريبي" : "DEMO CONTENT"}</div>}
    <a className="skip-link" href="#project-story">{copy.read}</a>
    <div className="project-reading-progress" aria-hidden="true"><span /></div>
    <nav className="project-page-nav" aria-label={rtl ? "التنقل في المشروع" : "Project navigation"}>
      <a className="project-page-brand" href={profilePath}><span className="project-page-mark" aria-hidden="true">O<span>.</span></span><span>{displayName}</span></a>
      <a className="project-back" href={back}><Back size={18} aria-hidden="true" /><span>{copy.back}</span></a>
    </nav>
    <main className="project-detail-main">
      <header className="project-hero">
        <div className="project-hero-inner">
          <div className="project-hero-copy">
            <div className="project-overline"><span>{copy.project}</span><span dir="ltr">{count}</span></div>
            <h1><bdi>{project.name}</bdi></h1>
            <p className="project-hero-summary">{project.summary}</p>
            <div className="project-hero-actions">
              <a className="project-read-cue" href="#project-story"><span>{copy.read}</span><ArrowDown size={18} aria-hidden="true" /></a>
              {projectLinks.length > 0 && <a className="project-links-cue" href="#project-links"><span>{copy.links}</span><span className="project-links-count" dir="ltr">{String(projectLinks.length).padStart(2, "0")}</span><ArrowDown size={17} aria-hidden="true" /></a>}
            </div>
          </div>
          <aside className="project-hero-note" aria-label={copy.scope}>
            <span className="project-note-mark" aria-hidden="true">{String(projectIndex + 1).padStart(2, "0")}</span>
            <div className="project-note-fact"><span>{copy.scope}</span><p>{project.role ?? project.summary}</p></div>
            <div className="project-note-fact"><span>{copy.type}</span><p>{kind}</p></div>
            <span className="project-note-line" aria-hidden="true"><i /></span>
          </aside>
        </div>
      </header>

      {projectLinks.length > 0 && <section className="project-links-showcase" id="project-links" aria-labelledby="project-links-title">
        <div className="project-links-inner">
          <div className="project-links-intro" data-reveal="project-links-heading">
            <span className="project-links-kicker">{copy.available} <span dir="ltr">{String(projectLinks.length).padStart(2, "0")}</span></span>
            <h2 id="project-links-title">{copy.links}</h2>
            <p>{copy.find}</p>
          </div>
          <div className="project-link-grid">
            {projectLinks.map((link, index) => {
              const Icon = link.kind === "googlePlay" ? Play : link.kind === "appStore" ? Smartphone : link.kind === "github" ? Github : link.kind === "repository" ? Code2 : link.kind === "website" || link.kind === "demo" ? Globe2 : Link2;
              const category = link.kind === "googlePlay" || link.kind === "appStore" ? copy.store : link.kind === "github" || link.kind === "repository" ? copy.source : copy.external;
              return <a className="project-link-card" href={link.url} key={link.url} target="_blank" rel="noopener noreferrer" aria-label={`${link.label} — ${copy.newTab}`} data-reveal="project-link" data-reveal-index={index}>
                <span className="project-link-icon"><Icon size={24} strokeWidth={1.7} aria-hidden="true" /></span>
                <span className="project-link-text"><small>{category}</small><strong><bdi>{link.label}</bdi></strong><span dir="ltr">{link.host}</span></span>
                <ExternalArrow className="project-link-arrow" size={21} aria-hidden="true" />
              </a>;
            })}
          </div>
        </div>
      </section>}

      <div className="project-reading-layout" id="project-story">
        <aside className="project-reading-rail" aria-label={copy.inThis}>
          <span className="project-rail-label">{copy.inThis}</span>
          <nav aria-label={copy.inThis}>{sections.map(section => <a href={`#${section.id}`} key={section.id}><span>{section.number}</span>{section.label}</a>)}</nav>
          <span className="project-rail-track" aria-hidden="true"><i /></span>
        </aside>
        <div className="project-reading-content">
          <section className="project-reading-section project-about" id="project-about" aria-labelledby="project-about-title" data-reveal="project-about">
            <div className="project-section-head"><span>{sectionNumber("project-about")}</span><h2 id="project-about-title">{copy.about}</h2></div>
            <p>{project.description || project.summary}</p>
          </section>

          {project.highlights.length > 0 && <section className="project-reading-section project-achievements" id="project-highlights" aria-labelledby="project-highlights-title">
            <div className="project-section-head" data-reveal="project-heading"><span>{sectionNumber("project-highlights")}</span><h2 id="project-highlights-title">{copy.made}</h2></div>
            <ol>{project.highlights.map((highlight, index) => <li key={highlight} data-reveal="project-highlight" data-reveal-index={index}><span>{String(index + 1).padStart(2, "0")}</span><p>{highlight}</p></li>)}</ol>
          </section>}

          {project.skills.length > 0 && <section className="project-reading-section project-tools" id="project-tools" aria-labelledby="project-tools-title" data-reveal="project-tools">
            <div className="project-section-head"><span>{sectionNumber("project-tools")}</span><h2 id="project-tools-title">{copy.tools}</h2></div>
            <ul>{project.skills.map(skill => <li key={skill}><bdi>{skill}</bdi></li>)}</ul>
          </section>}

          {gallery.length > 0 && <section className="project-reading-section project-media" id="project-images" aria-labelledby="project-images-title" data-reveal="project-images">
            <div className="project-section-head"><span>{sectionNumber("project-images")}</span><h2 id="project-images-title">{copy.images}</h2></div>
            <div className="project-media-grid">{gallery.map(image => <figure key={image.src}><img src={image.src} width={image.width} height={image.height} alt={image.alt} />{image.caption && <figcaption>{image.caption}</figcaption>}</figure>)}</div>
          </section>}

        </div>
      </div>

      <nav className="project-outro" aria-label={rtl ? "متابعة استكشاف الأعمال" : "Continue exploring work"}>
        <div className="project-outro-inner">
          {nextProject && nextUrl && <a className="project-next" href={nextUrl}><span>{copy.next} <span dir="ltr">{String((projectIndex + 1) % profile.projects.length + 1).padStart(2, "0")} / {String(profile.projects.length).padStart(2, "0")}</span></span><strong><bdi>{nextProject.name}</bdi></strong><Forward size={30} aria-hidden="true" /></a>}
          <a className="project-all" href={back}>{copy.all}<Back size={18} aria-hidden="true" /></a>
        </div>
      </nav>
    </main>
  </div>;
}
