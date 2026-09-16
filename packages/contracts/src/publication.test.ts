import { describe, expect, it } from "vitest";
import { demoSnapshot } from "./fixture";
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
});
