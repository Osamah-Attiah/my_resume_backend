import { describe, expect, it } from "vitest";
import { demoSnapshot } from "./fixture";
import { defaultStoryScenes } from "./default-story";
import { normalizePublicationSnapshot } from "./publication";

function envelope(locale: "ar" | "en", purpose?: "privatePdfExport") {
  const profile = demoSnapshot.profiles[locale];
  return { schemaVersion: 1, purpose, baseUrl: "https://private.invalid", profiles: [{ locale, slug: profile.slug, isDefault: true, indexable: false, document: profile, seo: profile.seo }], redirects: [] };
}

describe("publication snapshot normalization", () => {
  it("accepts one explicitly requested language for a private PDF export", () => {
    const snapshot = normalizePublicationSnapshot(envelope("ar", "privatePdfExport"));
    expect(snapshot.allProfiles).toHaveLength(1);
    expect(snapshot.allProfiles?.[0].locale).toBe("ar");
  });

  it("still requires both default languages for a public site publication", () => {
    expect(() => normalizePublicationSnapshot(envelope("ar"))).toThrow(/Missing default en/);
  });

  it("localizes legacy skill categories and language proficiency in Arabic snapshots", () => {
    const profile = demoSnapshot.profiles.ar;
    const rawArabicProfile = {
      ...profile,
      skills: [{ category: "Mobile", name: "Flutter" }, { category: "Backend", name: ".NET" }],
      languages: [{ languageCode: "en", proficiency: "Native" }]
    };
    const snapshot = normalizePublicationSnapshot({
      ...envelope("ar", "privatePdfExport"),
      profiles: [{ ...envelope("ar", "privatePdfExport").profiles[0], document: rawArabicProfile }]
    });
    expect(snapshot.profiles.ar.skills.map(skill => skill.category)).toEqual(["تطبيقات الهاتف المحمول", "الخدمات الخلفية"]);
    expect(snapshot.profiles.ar.languages[0].proficiency).toBe("اللغة الأم");
    expect(snapshot.profiles.ar.direction).toBe("rtl");
  });

  it("keeps only complete story scenes linked to published projects", () => {
    const input = envelope("en", "privatePdfExport");
    const snapshot = normalizePublicationSnapshot({ ...input, profiles: [{ ...input.profiles[0], story: [
      { key: "one", stage: "intro", title: "A real title", body: "An explanation" },
      { key: "two", stage: "offline", title: "Linked", body: "Supported", projectSlug: "sample-resume-platform" },
      { key: "backend-decisions", stage: "backend-focus", title: "Inside a service", body: "Responsibilities" },
      { key: "three", stage: "backend", title: "Missing", body: "Unsupported project", projectSlug: "not-selected" }
    ] }] });
    expect(snapshot.profiles.en.story?.map(scene => scene.key)).toEqual(["one", "two", "backend-decisions"]);
  });

  it("preserves project store and source links from the API snapshot", () => {
    const input = envelope("ar", "privatePdfExport");
    const document = {
      ...input.profiles[0].document,
      projects: [{ ...input.profiles[0].document.projects[0], links: [
        { kind: "googlePlay", label: "", url: "https://play.google.com/store/apps/details?id=sample" },
        { kind: "github", label: "المصدر", url: "https://github.com/example/sample" }
      ] }]
    };
    const snapshot = normalizePublicationSnapshot({ ...input, profiles: [{ ...input.profiles[0], document }] });
    expect(snapshot.profiles.ar.projects[0].links).toEqual(document.projects[0].links);
  });

  it("keeps the editable starting story aligned across languages", () => {
    expect(defaultStoryScenes.ar.map(scene => [scene.key, scene.stage])).toEqual(defaultStoryScenes.en.map(scene => [scene.key, scene.stage]));
    expect(defaultStoryScenes.ar.filter(scene => scene.stage.startsWith("backend"))).toHaveLength(2);
  });
});
