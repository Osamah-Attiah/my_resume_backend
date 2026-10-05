"use client";

import { useEffect, useState } from "react";
import type { Locale, PublicProfile, PublicProject, ResumeExperience } from "@resume/contracts";

export function projectsApiUrl(apiBaseUrl: string, siteId: string, profileSlug: string, locale: Locale) {
  return `${apiBaseUrl.replace(/\/$/, "")}/api/v1/public/sites/${encodeURIComponent(siteId)}/profiles/${encodeURIComponent(profileSlug)}/projects?locale=${locale}`;
}

const strings = (value: unknown): value is string[] => Array.isArray(value) && value.every(item => typeof item === "string");
export function isProjectResponse(value: unknown): value is { projects: PublicProject[] } {
  if (!value || typeof value !== "object" || !("projects" in value) || !Array.isArray(value.projects)) return false;
  return value.projects.every(project => project && typeof project.slug === "string" && typeof project.name === "string" && typeof project.summary === "string"
    && strings(project.highlights) && strings(project.skills) && Array.isArray(project.links)
    && project.links.every((link: unknown) => !!link && typeof link === "object" && "url" in link && typeof link.url === "string" && "kind" in link && typeof link.kind === "string")
    && ["Personal", "OpenSource", "Freelance", "Employment"].includes(project.kind));
}

type PortfolioContent = { projects: PublicProject[]; experiences?: ResumeExperience[] };
const optionalString = (value: unknown) => value == null || typeof value === "string";
export function isExperienceResponse(value: unknown): value is ResumeExperience[] {
  return Array.isArray(value) && value.every(experience => experience && typeof experience.organization === "string"
    && typeof experience.jobTitle === "string" && typeof experience.startDate === "string"
    && optionalString(experience.endDate) && optionalString(experience.summary) && optionalString(experience.location)
    && strings(experience.highlights));
}

// One public request refreshes projects and the selected website employment history together.
export async function fetchPortfolioContent(url: string, signal?: AbortSignal): Promise<PortfolioContent> {
  const response = await fetch(url, { cache: "no-store", headers: { Accept: "application/json" }, signal });
  if (!response.ok) throw new Error(`Project API returned ${response.status}`);
  const value: unknown = await response.json();
  if (!isProjectResponse(value)) throw new Error("Invalid project API response");
  if ("experiences" in value) {
    if (!isExperienceResponse(value.experiences)) throw new Error("Invalid experience API response");
    return { projects: value.projects, experiences: value.experiences };
  }
  return { projects: value.projects };
}

export async function fetchProjects(url: string, signal?: AbortSignal): Promise<PublicProject[]> {
  return (await fetchPortfolioContent(url, signal)).projects;
}

export function useLiveProjects({ profile, siteId, apiBaseUrl }: { profile: PublicProfile; siteId?: string; apiBaseUrl: string }) {
  const url = siteId && apiBaseUrl ? projectsApiUrl(apiBaseUrl, siteId, profile.slug, profile.locale) : "";
  const [state, setState] = useState<{ url: string; projects: PublicProject[]; experiences: ResumeExperience[]; status: "syncing" | "live" | "cached"; syncedAt: Date | null }>({ url, projects: profile.projects, experiences: profile.experiences, status: url ? "syncing" : "cached", syncedAt: null });
  useEffect(() => {
    setState({ url, projects: profile.projects, experiences: profile.experiences, status: url ? "syncing" : "cached", syncedAt: null });
    if (!url) return;
    let active = true;
    let request: AbortController | null = null;
    const load = async () => {
      if (request || document.hidden) return;
      const controller = new AbortController();
      request = controller;
      const timeout = window.setTimeout(() => controller.abort(), 20_000);
      try {
        const { projects, experiences } = await fetchPortfolioContent(url, controller.signal);
        if (active) setState(current => ({ url,
          projects: JSON.stringify(current.projects) === JSON.stringify(projects) ? current.projects : projects,
          // An empty response removes all old jobs; an older API without this field keeps its saved fallback.
          experiences: experiences === undefined || JSON.stringify(current.experiences) === JSON.stringify(experiences) ? current.experiences : experiences,
          status: "live", syncedAt: new Date()
        }));
      } catch {
        if (active) setState(current => ({ ...current, status: "cached" }));
      } finally {
        window.clearTimeout(timeout);
        if (request === controller) request = null;
      }
    };
    void load();
    const timer = window.setInterval(() => { void load(); }, 15_000);
    const onVisible = () => { if (!document.hidden) void load(); };
    document.addEventListener("visibilitychange", onVisible);
    return () => { active = false; request?.abort(); window.clearInterval(timer); document.removeEventListener("visibilitychange", onVisible); };
  }, [url, profile.projects, profile.experiences]);
  return state.url === url ? state : { url, projects: profile.projects, experiences: profile.experiences, status: url ? "syncing" as const : "cached" as const, syncedAt: null };
}
