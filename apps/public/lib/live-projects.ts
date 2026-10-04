"use client";

import { useEffect, useState } from "react";
import type { Locale, PublicProfile, PublicProject } from "@resume/contracts";

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

export async function fetchProjects(url: string, signal?: AbortSignal): Promise<PublicProject[]> {
  const response = await fetch(url, { cache: "no-store", headers: { Accept: "application/json" }, signal });
  if (!response.ok) throw new Error(`Project API returned ${response.status}`);
  const value: unknown = await response.json();
  if (!isProjectResponse(value)) throw new Error("Invalid project API response");
  return value.projects;
}

export function useLiveProjects({ profile, siteId, apiBaseUrl }: { profile: PublicProfile; siteId?: string; apiBaseUrl: string }) {
  const url = siteId && apiBaseUrl ? projectsApiUrl(apiBaseUrl, siteId, profile.slug, profile.locale) : "";
  const [state, setState] = useState<{ url: string; projects: PublicProject[]; status: "syncing" | "live" | "cached"; syncedAt: Date | null }>({ url, projects: profile.projects, status: url ? "syncing" : "cached", syncedAt: null });
  useEffect(() => {
    setState({ url, projects: profile.projects, status: url ? "syncing" : "cached", syncedAt: null });
    if (!url) return;
    let active = true;
    let request: AbortController | null = null;
    const load = async () => {
      if (request || document.hidden) return;
      const controller = new AbortController();
      request = controller;
      const timeout = window.setTimeout(() => controller.abort(), 20_000);
      try {
        const projects = await fetchProjects(url, controller.signal);
        if (active) setState(current => ({ url, projects: JSON.stringify(current.projects) === JSON.stringify(projects) ? current.projects : projects, status: "live", syncedAt: new Date() }));
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
  }, [url, profile.projects]);
  return state.url === url ? state : { url, projects: profile.projects, status: url ? "syncing" as const : "cached" as const, syncedAt: null };
}
