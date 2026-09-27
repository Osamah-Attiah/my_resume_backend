"use client";

import { useEffect, useState } from "react";
import type { PublicProfile, PublicProject } from "@resume/contracts";
import { ProfilePage } from "./ProfilePage";

export function LiveProfilePage({ profile, baseUrl, isDefault = false, siteId, apiBaseUrl }: {
  profile: PublicProfile;
  baseUrl: string;
  isDefault?: boolean;
  siteId?: string;
  apiBaseUrl: string;
}) {
  const [projects, setProjects] = useState(profile.projects);
  const [unavailable, setUnavailable] = useState(false);

  useEffect(() => {
    const script = document.createElement("script");
    script.src = "/site.js";
    script.dataset.staticRuntime = "true";
    document.body.appendChild(script);
    return () => { script.remove(); };
  }, []);

  useEffect(() => {
    if (!siteId || !apiBaseUrl) return;
    let active = true;
    const load = async () => {
      try {
        const response = await fetch(`${apiBaseUrl}/api/v1/public/sites/${encodeURIComponent(siteId)}/profiles/${encodeURIComponent(profile.slug)}/projects?locale=${profile.locale}`, { cache: "no-store" });
        if (!response.ok) throw new Error(`Project API returned ${response.status}`);
        const value: unknown = await response.json();
        if (!isProjectResponse(value)) throw new Error("Invalid project API response");
        if (active) { setProjects(current => JSON.stringify(current) === JSON.stringify(value.projects) ? current : value.projects); setUnavailable(false); }
      } catch {
        if (active) setUnavailable(true);
      }
    };
    void load();
    const timer = window.setInterval(() => { if (!document.hidden) void load(); }, 15_000);
    const onVisible = () => { if (!document.hidden) void load(); };
    document.addEventListener("visibilitychange", onVisible);
    return () => { active = false; window.clearInterval(timer); document.removeEventListener("visibilitychange", onVisible); };
  }, [apiBaseUrl, profile.locale, profile.slug, siteId]);

  useEffect(() => {
    document.querySelectorAll<HTMLElement>("[data-reveal]").forEach(element => element.classList.add("is-visible"));
  }, [projects]);

  return <>
    {unavailable && <div className="live-projects-notice" role="status">{profile.locale === "ar" ? "تعذر تحديث المشاريع الآن؛ تُعرض آخر نسخة منشورة." : "Projects could not be refreshed; showing the latest published version."}</div>}
    <ProfilePage profile={{ ...profile, projects }} baseUrl={baseUrl} isDefault={isDefault} />
  </>;
}

export function isProjectResponse(value: unknown): value is { projects: PublicProject[] } {
  if (!value || typeof value !== "object" || !("projects" in value) || !Array.isArray(value.projects)) return false;
  return value.projects.every(project => project && typeof project.slug === "string" && typeof project.name === "string" && typeof project.summary === "string" && Array.isArray(project.links) && Array.isArray(project.highlights) && Array.isArray(project.skills));
}
