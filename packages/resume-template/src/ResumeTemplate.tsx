import React from "react";
import type { ResumeDocument, ResumeEducation, ResumeExperience, ResumeLanguage, ResumeProject, ResumeCertification } from "@resume/contracts";

const headings = {
  en: {
    summary: "PROFESSIONAL SUMMARY",
    skills: "TECHNICAL SKILLS",
    core: "CORE COMPETENCIES",
    projects: "SELECTED PUBLISHED APPLICATIONS",
    experience: "PROFESSIONAL EXPERIENCE",
    education: "EDUCATION",
    certifications: "CERTIFICATIONS",
    languages: "LANGUAGES",
    credentials: "CERTIFICATIONS & LANGUAGES",
    ongoing: "Present",
    graduated: "Graduated",
    demo: "Live Demo",
    repository: "Repository"
  },
  ar: {
    summary: "الملخص المهني",
    skills: "المهارات التقنية",
    core: "الكفاءات الأساسية",
    projects: "التطبيقات المنشورة المختارة",
    experience: "الخبرة العملية",
    education: "التعليم",
    certifications: "الشهادات",
    languages: "اللغات",
    credentials: "الشهادات واللغات",
    ongoing: "حتى الآن",
    graduated: "التخرج",
    demo: "التجربة",
    repository: "المستودع"
  }
} as const;

type Labels = (typeof headings)["en"] | (typeof headings)["ar"];

export function ResumeTemplate({ document }: { document: ResumeDocument }) {
  const t = headings[document.locale];
  const sections = document.sections?.length
    ? document.sections
    : ["summary", "skills", "experience", "projects", "education", "certifications", "languages"];
  const hasCertifications = document.certifications.length > 0;
  const hasLanguages = document.languages.length > 0;
  const certificationsEnabled = sections.includes("certifications");
  const languagesEnabled = sections.includes("languages");
  const groupedSkills = document.skills.reduce((groups, item) => {
    groups.set(item.category, [...(groups.get(item.category) ?? []), item]);
    return groups;
  }, new Map<string, ResumeDocument["skills"]>());
  let credentialsRendered = false;
  const hasCoreSkills = document.skills.some(skill => isCoreCategory(skill.category, document.locale));
  const hasTechnicalSkills = document.skills.some(skill => !isCoreCategory(skill.category, document.locale));
  let technicalSkillsRendered = false;

  return <main className="resume" lang={document.locale} dir={document.direction}>
    <header className="identity">
      <h1>{document.locale === "en" ? document.fullName.toUpperCase() : document.fullName}</h1>
      <p className="headline">{document.headline}</p>
      <p className="contact" dir="ltr">{[document.location, document.phone, document.email].filter(Boolean).join(" | ")}</p>
      {document.links.length > 0 && <p className="contact links" dir="ltr">{document.links.map((link, index) => <React.Fragment key={link.url}>{index > 0 && <span aria-hidden="true"> | </span>}<a href={link.url}><bdi>{link.label}</bdi></a></React.Fragment>)}</p>}
    </header>
    {sections.map(key => {
      if (key === "certifications" && !credentialsRendered && hasCertifications) {
        credentialsRendered = true;
        return certificationsEnabled && languagesEnabled && hasLanguages
          ? <ResumeSection key={key} title={t.credentials}><Credentials certifications={document.certifications} languages={document.languages} locale={document.locale} labels={t} /></ResumeSection>
          : <ResumeSection key={key} title={t.certifications}><Certifications items={document.certifications} locale={document.locale} /></ResumeSection>;
      }
      if (key === "languages" && !credentialsRendered && hasLanguages) {
        credentialsRendered = true;
        return certificationsEnabled && hasCertifications
          ? <ResumeSection key={key} title={t.credentials}><Credentials certifications={document.certifications} languages={document.languages} locale={document.locale} labels={t} /></ResumeSection>
          : <ResumeSection key={key} title={t.languages}><Languages items={document.languages} locale={document.locale} /></ResumeSection>;
      }
      const section = renderSection(key, document, t, groupedSkills, !hasCoreSkills);
      if (key === "skills") technicalSkillsRendered = !hasCoreSkills;
      if (key === "projects" && hasCoreSkills && hasTechnicalSkills && !technicalSkillsRendered) {
        technicalSkillsRendered = true;
        return <React.Fragment key={key}>{section}<TechnicalSkills document={document} labels={t} /></React.Fragment>;
      }
      return section;
    })}
    {hasCoreSkills && hasTechnicalSkills && !technicalSkillsRendered && <TechnicalSkills key="technical-skills-fallback" document={document} labels={t} />}
  </main>;
}

function renderSection(key: string, document: ResumeDocument, t: Labels, groupedSkills: Map<string, ResumeDocument["skills"]>, includeTechnical = true) {
  if (key === "summary" && document.summary) return <ResumeSection key={key} title={t.summary}><p>{document.summary}</p></ResumeSection>;
  if (key === "skills" && document.skills.length) {
    const core = Array.from(groupedSkills.entries()).filter(([category]) => isCoreCategory(category, document.locale));
    const technical = Array.from(groupedSkills.entries()).filter(([category]) => !isCoreCategory(category, document.locale));
    return <React.Fragment key={key}>
      {core.length > 0 && <ResumeSection title={t.core}><p>{core.flatMap(([, skills]) => skills.map(skill => skill.name)).join(" | ")}</p></ResumeSection>}
      {includeTechnical && technical.length > 0 && <ResumeSection title={t.skills}>{technical.map(([category, skills]) => <p key={category}><strong>{category}:</strong> <bdi>{skills.map(skill => skill.name).join(" | ")}</bdi></p>)}</ResumeSection>}
    </React.Fragment>;
  }
  if (key === "experience" && document.experiences.length) return <ResumeSection key={key} title={t.experience}>{document.experiences.map(item => <ExperienceEntry key={`${item.organization}-${item.startDate}`} item={item} locale={document.locale} labels={t} />)}</ResumeSection>;
  if (key === "projects" && document.projects.length) return <ResumeSection key={key} title={t.projects}>{document.projects.map(project => <ProjectEntry key={project.slug} project={project} locale={document.locale} labels={t} />)}</ResumeSection>;
  if (key === "education" && document.educations.length) return <ResumeSection key={key} title={t.education}>{document.educations.map(item => <EducationEntry key={`${item.institution}-${item.degree}`} item={item} locale={document.locale} labels={t} />)}</ResumeSection>;
  return null;
}

function ExperienceEntry({ item, locale, labels }: { item: ResumeExperience; locale: "ar" | "en"; labels: Labels }) {
  return <article className="entry">
    <div className="entry-heading">
      <h3>{item.jobTitle} | {item.organization}{item.location && <span className="meta-inline"> - {item.location}</span>}</h3>
      <span className="date" dir="ltr">{formatDateRange(item.startDate, item.endDate, locale, labels.ongoing)}</span>
    </div>
    {item.summary && <p>{item.summary}</p>}
    {item.highlights.length > 0 && <ul>{item.highlights.map(point => <li key={point}>{point}</li>)}</ul>}
  </article>;
}

function ProjectEntry({ project, locale, labels }: { project: ResumeProject; locale: "ar" | "en"; labels: Labels }) {
  const links = [project.demoUrl ? { label: project.demoUrl.includes("play.google.com") ? "Google Play" : project.demoUrl.includes("apps.apple.com") ? "App Store" : labels.demo, url: project.demoUrl } : null, project.repositoryUrl ? { label: project.repositoryUrl.includes("github.com") ? "GitHub" : labels.repository, url: project.repositoryUrl } : null].filter(Boolean) as Array<{ label: string; url: string }>;
  return <article className="entry project-entry">
    <p className="project-line"><strong>{project.name}</strong>{project.summary && <> - {project.summary}</>}{links.map((link, index) => <React.Fragment key={link.url}> | <a href={link.url}>{link.label}</a></React.Fragment>)}</p>
    {project.role && <p className="meta">{project.role}</p>}
    {project.highlights.length > 0 && <ul>{project.highlights.map(point => <li key={point}>{point}</li>)}</ul>}
  </article>;
}

function TechnicalSkills({ document, labels }: { document: ResumeDocument; labels: Labels }) {
  const groups = Array.from(document.skills.reduce((map, skill) => {
    if (!isCoreCategory(skill.category, document.locale)) map.set(skill.category, [...(map.get(skill.category) ?? []), skill]);
    return map;
  }, new Map<string, ResumeDocument["skills"]>()).entries());
  return groups.length > 0 ? <ResumeSection title={labels.skills}>{groups.map(([category, skills]) => <p key={category}><strong>{category}:</strong> <bdi>{skills.map(skill => skill.name).join(" | ")}</bdi></p>)}</ResumeSection> : null;
}

function EducationEntry({ item, locale, labels }: { item: ResumeEducation; locale: "ar" | "en"; labels: Labels }) {
  const title = item.fieldOfStudy ? (locale === "ar" ? `${item.degree} في ${item.fieldOfStudy}` : `${item.degree} in ${item.fieldOfStudy}`) : item.degree;
  const date = item.endDate ? `${labels.graduated} ${year(item.endDate, locale)}` : item.startDate ? `${formatDate(item.startDate, locale)} - ${labels.ongoing}` : "";
  return <article className="entry"><p><strong>{title}</strong>{item.institution && <> | {item.institution}{item.location ? `, ${item.location}` : ""}</>}{date && <> | {date}</>}</p>{item.notes && <p className="meta">{item.notes}</p>}</article>;
}

function Certifications({ items, locale }: { items: ResumeCertification[]; locale: "ar" | "en" }) {
  return <>{items.map(item => <p className="credential" key={`${item.name}-${item.issuer}`}><strong>{item.name}</strong>{item.issuer && <> | {item.issuer}</>}{item.issuedOn && <> | {formatDate(item.issuedOn, locale)}</>}</p>)}</>;
}

function Languages({ items, locale }: { items: ResumeLanguage[]; locale: "ar" | "en" }) {
  return <p className="credential">{items.map((item, index) => <React.Fragment key={item.languageCode}>{index > 0 && <span aria-hidden="true"> | </span>}<strong>{languageName(item.languageCode, locale)}:</strong> {item.proficiency}</React.Fragment>)}</p>;
}

function Credentials({ certifications, languages, locale, labels }: { certifications: ResumeCertification[]; languages: ResumeLanguage[]; locale: "ar" | "en"; labels: Labels }) {
  return <><Certifications items={certifications} locale={locale} /><Languages items={languages} locale={locale} /></>;
}

function ResumeSection({ title, children }: { title: string; children: React.ReactNode }) { return <section><h2>{title}</h2>{children}</section>; }

function formatDateRange(start: string, end: string | undefined, locale: "ar" | "en", ongoing: string) { return `${formatDate(start, locale)} - ${end ? formatDate(end, locale) : ongoing}`; }
function formatDate(value: string, locale: "ar" | "en") {
  const date = new Date(`${value.slice(0, 7)}-01T00:00:00Z`);
  if (Number.isNaN(date.valueOf())) return value;
  return new Intl.DateTimeFormat(locale === "ar" ? "ar-u-ca-gregory" : "en-US", { month: "short", year: "numeric", timeZone: "UTC" }).format(date);
}
function year(value: string, locale: "ar" | "en") { return formatDate(value, locale).replace(/[^0-9٠-٩]/g, "").trim() || value.slice(0, 4); }
function languageName(code: string, locale: "ar" | "en") { return code.toLowerCase() === "ar" ? (locale === "ar" ? "العربية" : "Arabic") : code.toLowerCase() === "en" ? "English" : code; }
function isCoreCategory(category: string, locale: "ar" | "en") { return /core|competenc/i.test(category) || /أساسي|جوهر/.test(category) || (locale === "en" && category.toLowerCase().includes("core")); }

export function printCss(document: ResumeDocument, fonts: { arabic400: string; arabic700: string; latin400: string; latin700: string }) {
  const family = document.locale === "ar" ? "Cairo" : "Manrope";
  return `
    @font-face { font-family: "Cairo"; src: url(data:font/woff2;base64,${fonts.arabic400}) format("woff2"); font-weight: 400; font-style: normal; }
    @font-face { font-family: "Cairo"; src: url(data:font/woff2;base64,${fonts.arabic700}) format("woff2"); font-weight: 700; font-style: normal; }
    @font-face { font-family: "Manrope"; src: url(data:font/woff2;base64,${fonts.latin400}) format("woff2"); font-weight: 400; font-style: normal; }
    @font-face { font-family: "Manrope"; src: url(data:font/woff2;base64,${fonts.latin700}) format("woff2"); font-weight: 700; font-style: normal; }
    @page { size: ${document.pdf.paperSize}; margin: ${document.pdf.marginMm}mm; }
    * { box-sizing: border-box; }
    html { font-family: "${family}", sans-serif; color: #2F3B4A; background: #fff; font-size: ${document.pdf.fontSize}pt; }
    body { margin: 0; }
    .resume { width: 100%; line-height: ${document.locale === "ar" ? 1.58 : 1.25}; }
    .identity { margin-bottom: 10pt; }
    h1 { margin: 0; color: #1F4F70; font-size: 21pt; line-height: 1.2; }
    .headline { margin: 2pt 0 0; color: #1F4F70; font-size: 11.5pt; font-weight: 700; }
    .contact { margin: 2pt 0 0; color: #68717B; font-size: 9pt; text-align: ${document.locale === "ar" ? "right" : "left"}; overflow-wrap: anywhere; }
    .links { margin-top: 0; }
    h2 { margin: 8pt 0 3pt; padding-bottom: 2pt; border-bottom: .75pt solid #727272; color: #1F4F70; font-size: 11.5pt; line-height: 1.25; break-after: avoid; page-break-after: avoid; }
    h3 { margin: 0; font-size: 10.8pt; line-height: 1.3; }
    p { margin: 2pt 0; orphans: 2; widows: 2; }
    ul { margin: 2pt 0 3pt; padding-inline-start: 17pt; }
    li { margin: 1pt 0; }
    .entry { margin-top: 3pt; break-inside: avoid; page-break-inside: avoid; }
    .entry-heading { display: flex; align-items: baseline; justify-content: space-between; gap: 10pt; }
    .entry-heading h3 { flex: 1 1 auto; }
    .date { flex: 0 0 auto; color: #68717B; font-size: 9.5pt; font-style: italic; white-space: nowrap; }
    .meta, .meta-inline { color: #68717B; font-size: 9.5pt; font-style: italic; }
    .project-line, .credential { margin-top: 3pt; }
    .project-links a, a { color: #1A73C8; text-decoration: underline; text-decoration-thickness: .5pt; text-underline-offset: 1pt; }
    .project-links { font-size: 9.6pt; }
    .resume[dir="rtl"] .entry-heading { flex-direction: row-reverse; }
    .resume[dir="rtl"] ul { padding-inline-start: 0; padding-inline-end: 17pt; }
    bdi { unicode-bidi: isolate; }
  `;
}
