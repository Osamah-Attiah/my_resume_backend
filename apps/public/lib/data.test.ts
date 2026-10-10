import { describe, expect, it } from "vitest";
import { demoSnapshot } from "@resume/contracts";
import { browserProfile } from "./data";
import { websiteSnapshot } from "./website-content";

describe("browser profile", () => {
  it("does not serialize PDF-only content into the hydrated public page", () => {
    const profile = { ...demoSnapshot.profiles.en, pdfDocument: { ...demoSnapshot.profiles.en, summary: "PDF_ONLY_PRIVATE_COPY" } };
    const browser = browserProfile(profile);
    expect(browser).not.toHaveProperty("pdfDocument");
    expect(JSON.stringify(browser)).not.toContain("PDF_ONLY_PRIVATE_COPY");
  });

  it("excludes freelance projects from every website profile and linked content without changing PDF data", () => {
    const profile = demoSnapshot.profiles.en;
    const freelance = { ...profile.projects[0], slug: "freelance-only", kind: "Freelance" as const, name: "Hidden freelance project" };
    const saved = { ...profile, projects: [...profile.projects, freelance],
      pdfDocument: { ...profile, projects: [freelance] },
      story: [{ key: "hidden", stage: "intro" as const, title: freelance.name, body: "Hidden story", projectSlug: freelance.slug }],
      projectSeo: { [freelance.slug]: { title: freelance.name, indexable: true } }
    };
    const snapshot = websiteSnapshot({ ...demoSnapshot, profiles: { en: saved, ar: { ...saved, locale: "ar" } }, allProfiles: [saved] });
    for (const website of [...Object.values(snapshot.profiles), ...snapshot.allProfiles!]) {
      expect(website.projects).toEqual(profile.projects);
      expect(website.story).toEqual([]);
      expect(website.projectSeo).toEqual({});
      expect(website.pdfDocument).toBe(saved.pdfDocument);
      expect(JSON.stringify(browserProfile(website))).not.toContain(freelance.name);
    }
    expect(saved.projects).toContain(freelance);
    expect(saved.pdfDocument.projects).toEqual([freelance]);
  });
});
