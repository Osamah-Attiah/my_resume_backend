import { projectLinksFor, type Locale, type PublicProject } from "@resume/contracts";

export const profilePath = (slug: string, locale: Locale, isDefault: boolean) => isDefault
  ? `/${locale}/`
  : `/${locale}/p/${encodeURIComponent(slug)}/`;

export const resumePath = (slug: string, locale: Locale) => `/resumes/${encodeURIComponent(slug)}/${locale}/resume.pdf`;

// The query route also supports projects created after the static publication.
export const projectPath = (profileSlug: string, projectSlug: string, locale: Locale) =>
  `/${locale}/project/?profile=${encodeURIComponent(profileSlug)}&slug=${encodeURIComponent(projectSlug)}`;

const kinds = {
  en: { Personal: "Personal project", OpenSource: "Open source", Freelance: "Freelance", Employment: "Professional work" },
  ar: { Personal: "مشروع شخصي", OpenSource: "مفتوح المصدر", Freelance: "عمل حر", Employment: "عمل مهني" }
};

export function portfolioProject(project: PublicProject, locale: Locale, profileSlug: string) {
  return {
    id: project.slug,
    name: project.name,
    kicker: kinds[locale][project.kind] ?? "",
    summary: project.summary,
    tags: project.skills.slice(0, 4),
    scope: project.role,
    href: projectPath(profileSlug, project.slug, locale),
    links: projectLinksFor(project, locale)
  };
}

export type PortfolioProject = ReturnType<typeof portfolioProject>;
