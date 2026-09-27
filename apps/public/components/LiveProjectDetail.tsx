"use client";

import { useEffect, useState } from "react";
import { ArrowLeft, ArrowRight, ArrowUpRight } from "lucide-react";
import { projectLinksFor, type PublicProfile, type PublicProject } from "@resume/contracts";
import { isProjectResponse } from "./LiveProfilePage";

export function LiveProjectDetail({ locale, profiles, siteId, apiBaseUrl }: {
  locale: "ar" | "en";
  profiles: PublicProfile[];
  siteId?: string;
  apiBaseUrl: string;
}) {
  const [route, setRoute] = useState({ profile: "", slug: "" });
  const [liveProjects, setLiveProjects] = useState<PublicProject[] | null>(null);
  const [unavailable, setUnavailable] = useState(false);

  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    setRoute({ profile: params.get("profile") ?? "", slug: params.get("slug") ?? "" });
  }, []);

  useEffect(() => {
    if (!route.profile || !siteId || !apiBaseUrl) return;
    let active = true;
    const load = async () => {
      try {
        const response = await fetch(`${apiBaseUrl}/api/v1/public/sites/${encodeURIComponent(siteId)}/profiles/${encodeURIComponent(route.profile)}/projects?locale=${locale}`, { cache: "no-store" });
        if (!response.ok) throw new Error(`Project API returned ${response.status}`);
        const value: unknown = await response.json();
        if (!isProjectResponse(value)) throw new Error("Invalid project API response");
        if (active) { setLiveProjects(current => JSON.stringify(current) === JSON.stringify(value.projects) ? current : value.projects); setUnavailable(false); }
      } catch {
        if (active) setUnavailable(true);
      }
    };
    void load();
    const timer = window.setInterval(() => { if (!document.hidden) void load(); }, 15_000);
    const onVisible = () => { if (!document.hidden) void load(); };
    document.addEventListener("visibilitychange", onVisible);
    return () => { active = false; window.clearInterval(timer); document.removeEventListener("visibilitychange", onVisible); };
  }, [apiBaseUrl, locale, route.profile, siteId]);

  const profile = profiles.find(item => item.slug === route.profile);
  const projects = liveProjects ?? profile?.projects ?? [];
  const project = projects.find(item => item.slug === route.slug);
  const rtl = locale === "ar";
  const Back = rtl ? ArrowRight : ArrowLeft;
  const back = profile ? `/${locale}/p/${profile.slug}/#work` : `/${locale}/#work`;

  useEffect(() => {
    if (project) document.title = `${project.name} — ${profile?.fullName ?? "Portfolio"}`;
  }, [project, profile]);

  if (!route.profile || !route.slug) return <main className="container live-project-detail"><p>{rtl ? "جارٍ تحميل المشروع…" : "Loading project…"}</p></main>;
  if (!project) return <main className="container live-project-detail"><a href={back}><Back size={18} />{rtl ? "العودة إلى المشاريع" : "Back to projects"}</a><h1>{rtl ? "المشروع غير متاح" : "Project unavailable"}</h1>{unavailable && <p>{rtl ? "تعذر الاتصال بالخدمة الآن." : "The project service is unavailable right now."}</p>}</main>;

  const links = projectLinksFor(project, locale);
  const gallery = [...(project.cover ? [project.cover] : []), ...(project.media ?? []).filter(image => image.src !== project.cover?.src)];
  return <div className="project-detail-page" lang={locale} dir={rtl ? "rtl" : "ltr"}>
    <nav className="project-page-nav" aria-label={rtl ? "التنقل في المشروع" : "Project navigation"}>
      <a className="project-page-brand" href={back}>{profile?.fullName ?? "Portfolio"}</a>
      <a className="project-back" href={back}><Back size={18} aria-hidden="true" />{rtl ? "العودة إلى الأعمال" : "Back to work"}</a>
    </nav>
    {unavailable && <div className="live-projects-notice" role="status">{rtl ? "تعذر تحديث المشروع؛ تُعرض آخر بيانات متاحة." : "The project could not be refreshed; showing the latest available data."}</div>}
    <main className="project-detail-main">
      <header className="project-hero"><div className="project-hero-inner"><div className="project-hero-copy">
        <div className="project-overline">{rtl ? "تفاصيل المشروع" : "Project details"}</div>
        <h1><bdi>{project.name}</bdi></h1><p className="project-hero-summary">{project.summary}</p>
        {links.length > 0 && <a className="project-links-cue" href="#project-links">{rtl ? "روابط المشروع" : "Project links"} <ArrowUpRight size={17} aria-hidden="true" /></a>}
      </div><aside className="project-hero-note" aria-label={rtl ? "نطاق المشروع" : "Project scope"}><div className="project-note-fact"><span>{rtl ? "دوري" : "My role"}</span><p>{project.role || project.summary}</p></div></aside></div></header>
      {links.length > 0 && <section className="project-links-showcase" id="project-links" aria-labelledby="project-links-title"><div className="project-links-inner"><div className="project-links-intro"><h2 id="project-links-title">{rtl ? "روابط المشروع" : "Project links"}</h2></div><div className="project-link-grid">{links.map(link => <a className="project-link-card" key={link.url} href={link.url} target="_blank" rel="noopener noreferrer"><span className="project-link-text"><strong><bdi>{link.label}</bdi></strong><span dir="ltr">{link.host}</span></span><ArrowUpRight className="project-link-arrow" size={21} aria-hidden="true" /></a>)}</div></div></section>}
      <div className="project-reading-layout live-project-reading" id="project-story"><div className="project-reading-content">
        <section className="project-reading-section project-about"><div className="project-section-head"><h2>{rtl ? "عن المشروع" : "About the project"}</h2></div><p>{project.description || project.summary}</p></section>
        {project.highlights.length > 0 && <section className="project-reading-section project-achievements"><div className="project-section-head"><h2>{rtl ? "ما أنجزته" : "What I built"}</h2></div><ol>{project.highlights.map((highlight, index) => <li key={`${index}-${highlight}`}><span>{String(index + 1).padStart(2, "0")}</span><p>{highlight}</p></li>)}</ol></section>}
        {project.skills.length > 0 && <section className="project-reading-section project-tools"><div className="project-section-head"><h2>{rtl ? "التقنيات" : "Tools & technologies"}</h2></div><ul>{project.skills.map(skill => <li key={skill}><bdi>{skill}</bdi></li>)}</ul></section>}
        {gallery.length > 0 && <section className="project-reading-section project-media"><div className="project-section-head"><h2>{rtl ? "صور المشروع" : "Project images"}</h2></div><div className="project-media-grid">{gallery.map(image => <figure key={image.src}><img src={image.src} width={image.width} height={image.height} alt={image.alt} />{image.caption && <figcaption>{image.caption}</figcaption>}</figure>)}</div></section>}
      </div></div>
      <nav className="project-outro"><div className="project-outro-inner"><a className="project-all" href={back}>{rtl ? "كل الأعمال" : "All work"}<Back size={18} aria-hidden="true" /></a></div></nav>
    </main>
  </div>;
}
