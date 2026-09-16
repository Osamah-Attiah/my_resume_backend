import type { Locale, PublicProfile, PublicProject, PublicSiteSnapshot, ResumeDocument } from "./index";

type PublishedProfile = {
  locale: Locale;
  slug: string;
  isDefault: boolean;
  indexable: boolean;
  isListed?: boolean;
  document: ResumeDocument;
  pdfDocument?: ResumeDocument;
  seo: { title: string; description: string; canonical?: string; ogImage?: { src: string; width: number; height: number; alt: string } };
  projectSeo?: Record<string, { title?: string; description?: string; canonical?: string; ogImage?: { src: string; width: number; height: number; alt: string }; indexable: boolean }>;
  projectKinds?: Record<string, string>;
};
type PublicationEnvelope = { schemaVersion: number; purpose?: "privatePdfExport"; baseUrl?: string; lastModified?: string; site?: { searchVerificationToken?: string }; profiles?: PublishedProfile[]; redirects?: Array<{ source: string; target: string; status: number }> };

export function normalizePublicationSnapshot(input: unknown): PublicSiteSnapshot {
  const value = input as PublicationEnvelope;
  if (value?.schemaVersion !== 1 || !Array.isArray(value.profiles) || !value.profiles.length) throw new Error("Unsupported or empty publication snapshot.");
  if (!value.baseUrl || !value.baseUrl.startsWith("https://")) throw new Error("A production HTTPS baseUrl is required before publishing.");
  const isDemo = new URL(value.baseUrl).hostname.endsWith(".invalid");
  const allProfiles = value.profiles.map(profile => toPublicProfile(profile, isDemo));
  const defaults = Object.fromEntries((["ar", "en"] as const).map(locale => {
    const candidate = value.profiles!.find(x => x.locale === locale && x.isDefault);
    if (!candidate && value.purpose !== "privatePdfExport") throw new Error(`Missing default ${locale} profile in publication snapshot.`);
    if (!candidate) return [locale, allProfiles[0]];
    return [locale, toPublicProfile(candidate, isDemo)];
  })) as Record<Locale, PublicProfile>;
  const redirects = (value.redirects ?? []).filter(x => x.status === 301 || x.status === 308).map(x => ({ source: x.source, target: x.target, status: x.status as 301 | 308 }));
  return { schemaVersion: 1, baseUrl: value.baseUrl.replace(/\/$/, ""), lastModified: value.lastModified ?? new Date().toISOString().slice(0, 10), searchVerificationToken: value.site?.searchVerificationToken, profiles: defaults, allProfiles, redirects };
}

function toPublicProfile(value: PublishedProfile, demo: boolean): PublicProfile {
  const document = {
    ...value.document,
    skills: value.document.skills.map(skill => ({ ...skill, category: localizeCategory(skill.category, value.locale) })),
    languages: value.document.languages.map(language => ({ ...language, proficiency: localizeProficiency(language.proficiency, value.locale) }))
  };
  if (document.locale !== value.locale || !document.fullName || !document.headline) throw new Error(`Incomplete ${value.locale}/${value.slug} profile.`);
  const projects: PublicProject[] = document.projects.map(project => ({ ...project, kind: kind(value.projectKinds?.[project.slug]) }));
  return { ...document, slug: value.slug, indexable: demo ? false : value.indexable, listed: value.isListed, demo, projects, pdfDocument: value.pdfDocument, seo: value.seo, projectSeo: value.projectSeo };
}
function kind(value?: string): PublicProject["kind"] { return value === "OpenSource" || value === "Freelance" || value === "Employment" ? value : "Personal"; }

function localizeCategory(value: string, locale: Locale) {
  if (locale !== "ar") return value;
  const known: Record<string, string> = {
    mobile: "تطبيقات الهاتف المحمول",
    backend: "الخدمات الخلفية",
    data: "البيانات",
    frontend: "الواجهات الأمامية",
    devops: "عمليات التطوير",
    testing: "الاختبارات"
  };
  return known[value.trim().toLowerCase()] ?? value;
}

function localizeProficiency(value: string, locale: Locale) {
  if (locale !== "ar") return value;
  const known: Record<string, string> = {
    native: "اللغة الأم",
    fluent: "بطلاقة",
    professional: "مستوى مهني",
    intermediate: "متوسط",
    basic: "أساسي",
    beginner: "مبتدئ"
  };
  return known[value.trim().toLowerCase()] ?? value;
}
