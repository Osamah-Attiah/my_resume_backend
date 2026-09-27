import type { Locale, ResumeProject } from "./index";

export interface DisplayProjectLink { kind: string; label: string; url: string; host: string }

const knownLabels: Record<string, Record<Locale, string>> = {
  googlePlay: { ar: "Google Play", en: "Google Play" },
  appStore: { ar: "App Store", en: "App Store" },
  github: { ar: "GitHub", en: "GitHub" },
  repository: { ar: "المستودع", en: "Repository" },
  website: { ar: "الموقع", en: "Website" },
  demo: { ar: "المعاينة", en: "Live preview" },
  other: { ar: "رابط آخر", en: "Other link" },
};

function legacyKind(url: string, fallback: "repository" | "demo") {
  try {
    const host = new URL(url).hostname.toLowerCase();
    if (host === "play.google.com") return "googlePlay";
    if (host === "apps.apple.com") return "appStore";
    if (host === "github.com" || host.endsWith(".github.com")) return "github";
  } catch { /* Invalid links are omitted below. */ }
  return fallback;
}

export function projectLinksFor(project: ResumeProject, locale: Locale): DisplayProjectLink[] {
  const candidates = [
    ...(project.links ?? []),
    ...(project.repositoryUrl ? [{ kind: legacyKind(project.repositoryUrl, "repository"), label: "", url: project.repositoryUrl }] : []),
    ...(project.demoUrl ? [{ kind: legacyKind(project.demoUrl, "demo"), label: "", url: project.demoUrl }] : []),
  ];
  const seen = new Set<string>();
  const links: DisplayProjectLink[] = [];
  for (const item of candidates) {
    try {
      const parsed = new URL(item.url.trim());
      if (parsed.protocol !== "https:") continue;
      const key = parsed.href.replace(/\/$/, "");
      if (seen.has(key)) continue;
      seen.add(key);
      links.push({ kind: item.kind, label: item.label?.trim() || knownLabels[item.kind]?.[locale] || knownLabels.other[locale], url: parsed.href, host: parsed.hostname.replace(/^www\./, "") });
    } catch { /* Ignore malformed URLs from older snapshots. */ }
  }
  return links;
}
