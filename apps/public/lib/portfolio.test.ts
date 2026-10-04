import { afterEach, describe, expect, it, vi } from "vitest";
import { demoSnapshot } from "@resume/contracts";
import { createElement } from "react";
import { renderToStaticMarkup } from "react-dom/server";
import { PortfolioPage } from "../components/portfolio/PortfolioPage";
import { fetchProjects, isProjectResponse, projectsApiUrl } from "./live-projects";
import { portfolioProject, profilePath, resumePath } from "./portfolio";

afterEach(() => vi.unstubAllGlobals());

describe("portfolio backend integration", () => {
  const project = demoSnapshot.profiles.en.projects[0];

  it("uses the publication's site and profile instead of prototype constants", () => {
    expect(projectsApiUrl("https://api.example.com/", "another-site", "another profile", "ar")).toBe("https://api.example.com/api/v1/public/sites/another-site/profiles/another%20profile/projects?locale=ar");
    expect(resumePath("another-profile", "ar")).toBe("/resumes/another-profile/ar/resume.pdf");
    expect(profilePath("another-profile", "en", false)).toBe("/en/p/another-profile/");
    expect(profilePath("another-profile", "ar", true)).toBe("/ar/");
  });

  it("preserves skills, role, and public links and opens details for newly added projects", () => {
    const value = portfolioProject({ ...project, slug: "new-project", skills: ["Flutter"], links: [{ kind: "github", label: "Source", url: "https://github.com/example/project" }] }, "en", "new-profile");
    expect(value).toMatchObject({ tags: ["Flutter"], scope: project.role, href: "/en/project/?profile=new-profile&slug=new-project" });
    expect(value.links.some(link => link.url === "https://github.com/example/project")).toBe(true);
  });

  it("accepts an empty list so removing all projects removes stale cards", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(JSON.stringify({ projects: [] }), { status: 200 })));
    expect(await fetchProjects("https://api.example.com/projects")).toEqual([]);
    expect(isProjectResponse({ projects: [] })).toBe(true);
  });

  it("rejects malformed or failed API responses before replacing saved data", async () => {
    expect(isProjectResponse({ projects: [{ ...project, skills: null }] })).toBe(false);
    expect(isProjectResponse({ projects: [{ ...project, links: [null] }] })).toBe(false);
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response("{}", { status: 503 })));
    await expect(fetchProjects("https://api.example.com/projects")).rejects.toThrow("503");
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response('{"projects":null}', { status: 200 })));
    await expect(fetchProjects("https://api.example.com/projects")).rejects.toThrow("Invalid project API response");
  });

  it("renders published profile data and links before browser JavaScript runs", () => {
    const profile = { ...demoSnapshot.profiles.ar, fullName: "اسم من نسخة النشر", slug: "published-profile" };
    const html = renderToStaticMarkup(createElement(PortfolioPage, { profile, baseUrl: "https://portfolio.example.com", apiBaseUrl: "", isDefault: true }));
    expect(html).toContain("اسم من نسخة النشر");
    expect(html).toContain('lang="ar"');
    expect(html).toContain('dir="rtl"');
    expect(html).toContain("/resumes/published-profile/ar/resume.pdf");
    expect(html).toContain("/ar/project/?profile=published-profile");
    expect(html).toContain('href="/en/"');
    expect(html).toContain('type="application/ld+json"');
    expect(html).not.toContain("resume-api-m3dq.onrender.com");
    expect(html).not.toContain("sofrware-engineer");
  });
});
