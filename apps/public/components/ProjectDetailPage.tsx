"use client";

import { useEffect } from "react";
import { motion, useScroll } from "framer-motion";
import { ArrowDown, ArrowLeft, ArrowRight, ArrowUpLeft, ArrowUpRight, Code2, Github, Globe2, Link2, Play, Smartphone } from "lucide-react";
import { projectLinksFor, type Locale, type PublicProfile } from "@resume/contracts";
import { useLiveProjects } from "../lib/live-projects";
import { portfolioProject, profilePath, projectPath } from "../lib/portfolio";

const COPY = {
  en: {
    back: "Back to work", project: "Project details", read: "Explore the details", scope: "Project scope",
    inThis: "On this page", about: "About the project", made: "What I built", tools: "Tools & technologies",
    images: "Project images", links: "Explore the project", available: "Project links", store: "Application", source: "Source code",
    external: "Website", newTab: "opens in a new tab", next: "Next project", all: "All work", portfolio: "Portfolio",
    loading: "Loading project…", missing: "Project unavailable", missingBody: "This project may have been removed, or its link is incomplete.",
    unavailable: "The project service is unavailable right now. Please try again.", retry: "Try again",
    cached: "Showing the latest saved project. Live updates are temporarily unavailable.",
    syncing: "Updating…", live: "Live · up to date", saved: "Saved copy", contributions: "Contributions", demo: "Demo content"
  },
  ar: {
    back: "العودة إلى الأعمال", project: "تفاصيل المشروع", read: "اقرأ التفاصيل", scope: "نطاق المشروع",
    inThis: "في هذه الصفحة", about: "عن المشروع", made: "ما أنجزته", tools: "التقنيات",
    images: "صور المشروع", links: "تصفّح المشروع", available: "روابط المشروع", store: "التطبيق", source: "الكود",
    external: "الموقع", newTab: "يفتح في تبويب جديد", next: "المشروع التالي", all: "كل الأعمال", portfolio: "الأعمال",
    loading: "جارٍ تحميل المشروع…", missing: "المشروع غير متاح", missingBody: "قد يكون المشروع أُزيل، أو أن الرابط غير مكتمل.",
    unavailable: "تعذر الاتصال بخدمة المشاريع الآن. حاول مرة أخرى.", retry: "حاول مرة أخرى",
    cached: "نعرض آخر نسخة محفوظة من المشروع. التحديث المباشر غير متاح مؤقتًا.",
    syncing: "جارٍ التحديث…", live: "مباشر · محدّث", saved: "نسخة محفوظة", contributions: "مساهمات", demo: "محتوى تجريبي"
  }
};

export function ProjectDetailState({ locale, loading = false, unavailable = false, back = `/${locale}/#work` }: {
  locale: Locale; loading?: boolean; unavailable?: boolean; back?: string;
}) {
  const t = COPY[locale];
  const Back = locale === "ar" ? ArrowRight : ArrowLeft;
  useEffect(() => {
    document.documentElement.lang = locale;
    document.documentElement.dir = locale === "ar" ? "rtl" : "ltr";
    document.title = loading ? t.loading : t.missing;
  }, [locale, loading, t.loading, t.missing]);
  return <div className="project-detail-page" lang={locale} dir={locale === "ar" ? "rtl" : "ltr"}>
    <main className="pd-state" aria-busy={loading}>
      <a className="pd-back" href={back}><Back size={18} aria-hidden="true" />{t.back}</a>
      <div className="pd-eyebrow"><span aria-hidden="true">—</span>{t.project}</div>
      <h1>{loading ? t.loading : t.missing}</h1>
      {loading ? <div className="pd-loading-lines" role="status" aria-label={t.loading}><span /><span /><span /></div> : <>
        <p>{unavailable ? t.unavailable : t.missingBody}</p>
        {unavailable && <button className="pd-button" onClick={() => window.location.reload()}>{t.retry}</button>}
      </>}
    </main>
  </div>;
}

export function ProjectDetailPage({ profile, projectSlug, isDefault, hasCounterpart, siteId, apiBaseUrl }: {
  profile: PublicProfile; projectSlug: string; isDefault: boolean; hasCounterpart: boolean; siteId?: string; apiBaseUrl: string;
}) {
  const { projects, status } = useLiveProjects({ profile, siteId, apiBaseUrl });
  const { scrollYProgress } = useScroll();
  const locale = profile.locale;
  const rtl = locale === "ar";
  const t = COPY[locale];
  const project = projects.find(item => item.slug === projectSlug);
  const home = profilePath(profile.slug, locale, isDefault);
  const back = `${home}#work`;
  const Back = rtl ? ArrowRight : ArrowLeft;
  const Forward = rtl ? ArrowLeft : ArrowRight;
  const External = rtl ? ArrowUpLeft : ArrowUpRight;

  useEffect(() => {
    document.documentElement.lang = locale;
    document.documentElement.dir = rtl ? "rtl" : "ltr";
    if (project) document.title = profile.projectSeo?.[project.slug]?.title ?? `${project.name} — ${profile.fullName}`;
  }, [locale, rtl, project, profile.fullName, profile.projectSeo]);

  if (!project) return <ProjectDetailState locale={locale} back={back} loading={status === "syncing"} unavailable={status === "cached" && !!siteId && !!apiBaseUrl} />;

  const index = projects.findIndex(item => item.slug === projectSlug);
  const next = projects.length > 1 ? projects[(index + 1) % projects.length] : undefined;
  const number = String(index + 1).padStart(2, "0");
  const count = String(projects.length).padStart(2, "0");
  const kind = portfolioProject(project, locale, profile.slug).kicker;
  const links = projectLinksFor(project, locale);
  const gallery = [...(project.cover ? [project.cover] : []), ...(project.media ?? []).filter(image => image.src !== project.cover?.src)];
  const sections = [
    { id: "project-about", label: t.about },
    ...(project.highlights.length ? [{ id: "project-highlights", label: t.made }] : []),
    ...(project.skills.length ? [{ id: "project-tools", label: t.tools }] : []),
    ...(gallery.length ? [{ id: "project-images", label: t.images }] : [])
  ];
  const sectionNumber = (id: string) => String(sections.findIndex(section => section.id === id) + 1).padStart(2, "0");

  return <div className="project-detail-page" lang={locale} dir={rtl ? "rtl" : "ltr"}>
    <a className="skip-link" href="#project-story">{t.read}</a>
    <motion.div className="pd-progress" aria-hidden="true" style={{ scaleX: scrollYProgress, transformOrigin: rtl ? "right" : "left" }} />
    <nav className="pd-nav" aria-label={rtl ? "التنقل في المشروع" : "Project navigation"}>
      <a className="pd-brand" href={home}><span className="pd-brand-mark" dir="ltr" aria-hidden="true">oa</span><span>{profile.fullName}</span></a>
      <span className="pd-nav-context">{t.portfolio}</span>
      <div className="pd-languages" dir="ltr" role="group" aria-label={rtl ? "اللغة" : "Language"}>
        {(["en", "ar"] as const).map(language => <a key={language}
          href={language === locale || hasCounterpart ? projectPath(profile.slug, project.slug, language) : `/${language}/`}
          hrefLang={language} aria-current={language === locale ? "page" : undefined}>{language === "en" ? "EN" : "عربي"}</a>)}
      </div>
    </nav>
    <main id="project-story" className="pd-main" tabIndex={-1}>
      {profile.demo && <div className="pd-notice">{t.demo}</div>}
      <header className="pd-hero pd-width">
        <div className="pd-hero-top"><a className="pd-back" href={back}><Back size={17} aria-hidden="true" />{t.back}</a>
          <span className="pd-status" role="status" aria-live="polite" aria-atomic="true" data-status={status}><i aria-hidden="true" />{status === "live" ? t.live : status === "syncing" ? t.syncing : t.saved}</span>
        </div>
        {status === "cached" && siteId && apiBaseUrl && <p className="pd-notice" role="status">{t.cached}</p>}
        <div className="pd-hero-grid">
          <div className="pd-hero-copy">
            <div className="pd-eyebrow"><span dir="ltr">{number} / {count}</span>{t.project}</div>
            <h1><bdi>{project.name}</bdi></h1>
            <p className="pd-summary">{project.summary}</p>
            <div className="pd-hero-actions">
              {links[0] && <a className="pd-button" href={links[0].url} target="_blank" rel="noopener noreferrer" aria-label={`${links[0].label} — ${t.newTab}`}><bdi>{links[0].label}</bdi><External size={18} aria-hidden="true" /></a>}
              <a className={`pd-button ${links.length ? "pd-button-outline" : ""}`} href="#project-about">{t.read}<ArrowDown size={18} aria-hidden="true" /></a>
            </div>
          </div>
          <aside className="pd-scope" aria-label={t.scope}>
            <div className="pd-scope-top"><span>{t.scope}</span><span className="pd-mono" dir="ltr">{number} / {count}</span></div>
            <span className="pd-scope-number" dir="ltr" aria-hidden="true">{number}<span>.</span></span>
            <div className="pd-scope-body"><p className="pd-kind">{kind}</p>{project.role && <p className="pd-role">{project.role}</p>}</div>
            {project.highlights.length > 0 && <div className="pd-scope-footer"><span className="pd-mono" dir="ltr">{String(project.highlights.length).padStart(2, "0")}</span><span>{t.contributions}</span><a href="#project-highlights" aria-label={t.made}><ArrowDown size={18} aria-hidden="true" /></a></div>}
          </aside>
        </div>
      </header>

      {links.length > 0 && <section className="pd-links pd-width" id="project-links" aria-labelledby="project-links-title">
        <div><span className="pd-label">{t.available}</span><h2 id="project-links-title">{t.links}</h2></div>
        <div className="pd-link-list">{links.map(link => {
          const Icon = link.kind === "googlePlay" ? Play : link.kind === "appStore" ? Smartphone : link.kind === "github" ? Github : link.kind === "repository" ? Code2 : link.kind === "website" || link.kind === "demo" ? Globe2 : Link2;
          const category = link.kind === "googlePlay" || link.kind === "appStore" ? t.store : link.kind === "github" || link.kind === "repository" ? t.source : t.external;
          return <a className="pd-link" href={link.url} key={link.url} target="_blank" rel="noopener noreferrer" aria-label={`${link.label} — ${t.newTab}`}>
            <span className="pd-link-icon"><Icon size={22} strokeWidth={1.7} aria-hidden="true" /></span>
            <span className="pd-link-copy"><small>{category}</small><strong><bdi>{link.label}</bdi></strong><span className="pd-mono" dir="ltr">{link.host}</span></span><External size={22} aria-hidden="true" />
          </a>;
        })}</div>
      </section>}

      <div className="pd-reading pd-width">
        <aside className="pd-rail"><span className="pd-label">{t.inThis}</span><nav aria-label={t.inThis}>{sections.map((section, i) => <a key={section.id} href={`#${section.id}`}><span className="pd-mono" dir="ltr">{String(i + 1).padStart(2, "0")}</span>{section.label}<ArrowDown size={14} aria-hidden="true" /></a>)}</nav></aside>
        <div className="pd-content">
          <section className="pd-section" id="project-about" aria-labelledby="project-about-title">
            <div className="pd-section-title"><span className="pd-mono">{sectionNumber("project-about")}</span><h2 id="project-about-title">{t.about}</h2></div>
            <p className="pd-description">{project.description || project.summary}</p>
          </section>
          {project.highlights.length > 0 && <section className="pd-section" id="project-highlights" aria-labelledby="project-highlights-title">
            <div className="pd-section-title"><span className="pd-mono">{sectionNumber("project-highlights")}</span><h2 id="project-highlights-title">{t.made}</h2></div>
            <ol className="pd-highlights">{project.highlights.map((highlight, i) => <li key={`${i}-${highlight}`}><span className="pd-mono" dir="ltr">{String(i + 1).padStart(2, "0")}</span><p>{highlight}</p></li>)}</ol>
          </section>}
          {project.skills.length > 0 && <section className="pd-section" id="project-tools" aria-labelledby="project-tools-title">
            <div className="pd-section-title"><span className="pd-mono">{sectionNumber("project-tools")}</span><h2 id="project-tools-title">{t.tools}</h2></div>
            <ul className="pd-tools">{project.skills.map(skill => <li key={skill}><bdi>{skill}</bdi></li>)}</ul>
          </section>}
          {gallery.length > 0 && <section className="pd-section" id="project-images" aria-labelledby="project-images-title">
            <div className="pd-section-title"><span className="pd-mono">{sectionNumber("project-images")}</span><h2 id="project-images-title">{t.images}</h2></div>
            <div className="pd-gallery">{gallery.map(image => <figure key={image.src}><img src={image.src} width={image.width} height={image.height} alt={image.alt} loading="lazy" />{image.caption && <figcaption>{image.caption}</figcaption>}</figure>)}</div>
          </section>}
        </div>
      </div>
      <nav className="pd-outro pd-width" aria-label={rtl ? "متابعة استكشاف الأعمال" : "Continue exploring work"}>
        {next && <a className="pd-next" href={projectPath(profile.slug, next.slug, locale)}><div className="pd-next-label"><span>{t.next}</span><span className="pd-mono" dir="ltr">{String((index + 1) % projects.length + 1).padStart(2, "0")} / {count}</span></div><div className="pd-next-name"><strong><bdi>{next.name}</bdi></strong><Forward size={40} strokeWidth={1.6} aria-hidden="true" /></div></a>}
        <div className="pd-footer"><span>{profile.fullName}</span><a className="pd-back" href={back}>{t.all}<Back size={18} aria-hidden="true" /></a></div>
      </nav>
    </main>
  </div>;
}
