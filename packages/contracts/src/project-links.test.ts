import { describe, expect, it } from "vitest";
import { projectLinksFor } from "./project-links";
import type { ResumeProject } from "./index";

const project: ResumeProject = { slug: "sample", name: "Sample", summary: "Sample project", highlights: [], skills: [] };

describe("project links", () => {
  it("shows typed store links and keeps distinct legacy links", () => {
    const links = projectLinksFor({ ...project, links: [
      { kind: "googlePlay", label: "", url: "https://play.google.com/store/apps/details?id=sample" },
      { kind: "other", label: "Read the case study", url: "https://example.com/story" },
    ], demoUrl: "https://play.google.com/store/apps/details?id=sample", repositoryUrl: "https://github.com/example/sample" }, "ar");
    expect(links.map(link => [link.label, link.host])).toEqual([
      ["Google Play", "play.google.com"],
      ["Read the case study", "example.com"],
      ["GitHub", "github.com"],
    ]);
  });

  it("omits unsafe and malformed external URLs", () => {
    const links = projectLinksFor({ ...project, links: [
      { kind: "other", label: "Unsafe", url: "javascript:alert(1)" },
      { kind: "other", label: "Malformed", url: "not-a-link" },
      { kind: "website", label: "", url: "https://example.com" },
    ] }, "ar");
    expect(links.map(link => link.label)).toEqual(["الموقع"]);
  });
});
