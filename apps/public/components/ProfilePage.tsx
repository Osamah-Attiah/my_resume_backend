import { ArrowLeft, ArrowRight, ArrowUpRight, Download, Mail, MapPin, Phone } from "lucide-react";
import Image from "next/image";
import type { PublicProfile, PublicProject } from "@resume/contracts";
import { PageMotion } from "./PageMotion";

const copy = {
  en: {
    skip: "Skip to main content",
    works: "Work",
    about: "About",
    contact: "Contact",
    index: "Index",
    intro: "Profile",
    profile: "Public profile",
    spotlight: "Featured project",
    scroll: "Scroll to explore",
    progress: "Reading progress",
    selected: "Selected work",
    skills: "Technical skills",
    experience: "Experience",
    education: "Education",
    certifications: "Certifications",
    languages: "Languages",
    details: "More context",
    aboutMe: "About me",
    capabilities: "Capabilities",
    personalDetails: "Personal details",
    contactTitle: "Let's build dependable software.",
    view: "View project",
    code: "View code",
    demoLink: "Live demo",
    resume: "Download resume",
    demo: "DEMO CONTENT — Replace every field before publishing.",
    personal: "Personal project",
    openSource: "Open source",
    freelance: "Freelance",
    employment: "Professional work",
    present: "Present"
  },
  ar: {
    skip: "انتقل إلى المحتوى الرئيسي",
    works: "الأعمال",
    about: "نبذة",
    contact: "التواصل",
    index: "الفهرس",
    intro: "الملف التعريفي",
    profile: "الملف الشخصي العام",
    spotlight: "مشروع مميز",
    scroll: "مرّر لاستكشاف المزيد",
    progress: "نسبة التقدم",
    selected: "أعمال مختارة",
    skills: "المهارات التقنية",
    experience: "الخبرة العملية",
    education: "التعليم",
    certifications: "الشهادات",
    languages: "اللغات",
    details: "معلومات إضافية",
    aboutMe: "نبذة عني",
    capabilities: "القدرات التقنية",
    personalDetails: "بيانات شخصية",
    contactTitle: "لنبنِ برمجيات موثوقة.",
    view: "تفاصيل المشروع",
    code: "عرض الكود",
    demoLink: "التجربة المباشرة",
    resume: "تحميل السيرة",
    demo: "محتوى تجريبي — استبدل جميع الحقول قبل النشر.",
    personal: "مشروع شخصي",
    openSource: "مفتوح المصدر",
    freelance: "عمل حر",
    employment: "خبرة مهنية",
    present: "حتى الآن"
  }
} as const;

type Labels = (typeof copy)["ar"] | (typeof copy)["en"];
type CountKind = "projects" | "roles" | "disciplines" | "skillGroups" | "skills" | "entries";

function countLabel(locale: "ar" | "en", value: number, kind: CountKind) {
  const en: Record<CountKind, [string, string]> = {
    projects: ["project", "projects"],
    roles: ["role", "roles"],
    disciplines: ["area", "areas"],
    skillGroups: ["skill group", "skill groups"],
    skills: ["skill", "skills"],
    entries: ["entry", "entries"]
  };
  if (locale === "en") return `${value} ${value === 1 ? en[kind][0] : en[kind][1]}`;
  const ar: Record<CountKind, { one: string; two: string; few: string; many: string; other: string }> = {
    projects: { one: "مشروع", two: "مشروعان", few: "مشاريع", many: "مشروعًا", other: "مشروع" },
    roles: { one: "خبرة", two: "خبرتان", few: "خبرات", many: "خبرة", other: "خبرة" },
    disciplines: { one: "مجال", two: "مجالان", few: "مجالات", many: "مجالًا", other: "مجال" },
    skillGroups: { one: "مجموعة مهارات", two: "مجموعتا مهارات", few: "مجموعات مهارات", many: "مجموعة مهارات", other: "مجموعة مهارات" },
    skills: { one: "مهارة", two: "مهارتان", few: "مهارات", many: "مهارة", other: "مهارة" },
    entries: { one: "عنصر", two: "عنصران", few: "عناصر", many: "عنصرًا", other: "عنصر" }
  };
  const forms = ar[kind];
  const word = value === 1 ? forms.one : value === 2 ? forms.two : value >= 3 && value <= 10 ? forms.few : value >= 11 && value <= 99 ? forms.many : forms.other;
  return `${value} ${word}`;
}

export function ProfilePage({ profile, baseUrl, isDefault = false }: { profile: PublicProfile; baseUrl: string; isDefault?: boolean }) {
  const t = copy[profile.locale];
  const other = profile.locale === "ar" ? "en" : "ar";
  const sections = profile.sections?.length ? profile.sections : ["summary", "skills", "projects", "experience", "education", "certifications", "languages"];
  const enabled = new Set(sections);
  const skills = Map.groupBy(profile.skills, item => item.category);
  const preferredOrder = ["projects", "experience", "skills", "education", "certifications", "languages"];
  const contentSections = [...preferredOrder.filter(key => enabled.has(key)), ...sections.filter(key => key !== "summary" && !preferredOrder.includes(key))];
  const secondaryKeys = ["education", "certifications", "languages"];
  const secondarySections = contentSections.filter(key => secondaryKeys.includes(key));
  const firstSecondary = secondarySections[0];
  const hasAboutSection = enabled.has("summary") && profile.summary.trim().length > 0;
  const contentSectionOffset = hasAboutSection ? 2 : 1;
  const Arrow = profile.direction === "rtl" ? ArrowLeft : ArrowRight;
  const profilePath = isDefault ? "/" + profile.locale + "/" : "/" + profile.locale + "/p/" + profile.slug + "/";
  const otherPath = isDefault ? "/" + other + "/" : "/" + other + "/p/" + profile.slug + "/";
  const canonical = profile.seo.canonical ?? baseUrl + profilePath;
  const contactNumber = contentSections.length + contentSectionOffset + 1;
  const personId = baseUrl + "/#person";
  const websiteId = baseUrl + "/#website";
  const railItems = [
    { target: "intro", href: "#intro", label: t.intro, visible: true },
    ...(hasAboutSection ? [{ target: "about", href: "#about", label: t.aboutMe, visible: true }] : []),
    ...contentSections.filter(key => key === "projects" || key === "experience" || key === "skills").map(key => key === "projects"
      ? { target: "work", href: "#work", label: t.works, visible: profile.projects.length > 0 }
      : key === "experience"
        ? { target: "experience", href: "#experience", label: t.experience, visible: profile.experiences.length > 0 }
        : { target: "skills", href: "#skills", label: t.skills, visible: profile.skills.length > 0 }),
    ...(secondarySections.length > 0 ? [{ target: "details", href: "#details", label: t.details, visible: true }] : []),
    { target: "contact", href: "#contact", label: t.contact, visible: true }
  ].filter(item => item.visible);
  const personJsonLd = {
    "@context": "https://schema.org",
    "@graph": [
      { "@type": "WebSite", "@id": websiteId, url: baseUrl + "/", name: profile.fullName, inLanguage: ["ar", "en"] },
      { "@type": "ProfilePage", "@id": canonical + "#profile-page", url: canonical, inLanguage: profile.locale, isPartOf: { "@id": websiteId }, mainEntity: { "@id": personId } },
      { "@type": "Person", "@id": personId, name: profile.fullName, jobTitle: profile.headline, description: profile.summary, knowsAbout: profile.skills.map(x => x.name), sameAs: profile.links.map(x => x.url) }
    ]
  };

  return <div lang={profile.locale} dir={profile.direction}>
    <a className="skip-link" href="#main">{t.skip}</a>
    {profile.demo && <div className="demo-notice" role="note">{t.demo}</div>}
    <header className="site-header">
      <nav className="container nav" aria-label={profile.locale === "ar" ? "التنقل الرئيسي" : "Primary navigation"}>
        <a className="wordmark" href={profilePath}><span className="wordmark-mark" aria-hidden="true" /><span>{profile.fullName}</span></a>
        <span className="nav-context">{t.profile}</span>
        <div className="nav-links">
          {profile.projects.length > 0 && <a href="#work">{t.works}</a>}
          {hasAboutSection && <a href="#about">{t.about}</a>}
          <a href="#contact">{t.contact}</a>
          <a className="locale-switch" href={otherPath} hrefLang={other} aria-label={other === "ar" ? "العربية" : "English"}>{other === "ar" ? "عر" : "EN"}</a>
        </div>
      </nav>
    </header>
    <div className="scroll-progress" aria-hidden="true"><span /></div>

    <main id="main" className="container public-main">
      <aside className="side-index" aria-label={t.index}>
        <div className="side-index-header"><span>{t.index}</span><span>{t.profile}</span></div>
        <nav className="side-index-links" aria-label={t.index}>
          {railItems.map((item, index) => <a key={item.target} href={item.href} data-index-target={item.target}><span className="side-index-number">{String(index + 1).padStart(2, "0")}</span><span>{item.label}</span></a>)}
        </nav>
        <div className="side-index-footer"><div className="side-index-bar"><span /></div><span>{t.progress}: <bdi data-scroll-value>0%</bdi></span></div>
      </aside>

      <div className="page-column">
        <section id="intro" className="hero hero-stage" aria-labelledby="profile-name" data-section="intro" data-reveal="hero">
          <span className="hero-stage-index">01 / {t.profile}</span>
          <div className="hero-copy">
            <p className="eyebrow">{profile.locale === "ar" ? "ملف شخصي" : "Personal profile"}{profile.demo ? ` · ${profile.locale === "ar" ? "تجريبي" : "Demo"}` : ""}</p>
            <h1 id="profile-name">{profile.fullName}</h1>
            <p className="headline">{profile.headline}</p>
            <div className="hero-actions">
              {profile.projects.length > 0 && <a className="button button-primary" href="#work">{t.works}<Arrow size={18} aria-hidden="true" /></a>}
              <a className="button button-secondary" href={"/resumes/" + profile.slug + "/" + profile.locale + "/resume.pdf"} download>{t.resume}<Download size={18} aria-hidden="true" /></a>
            </div>
            {profile.projects.length > 0 && <a className="hero-scroll" href="#work"><span className="hero-scroll-line" aria-hidden="true" />{t.scroll}</a>}
          </div>
        </section>

        {hasAboutSection && <AboutSection profile={profile} labels={t} number={2} />}
        {contentSections.map((key, index) => {
          const sectionNumber = index + contentSectionOffset + 1;
          if (key === firstSecondary) return <DetailsSection key="details" profile={profile} labels={t} number={sectionNumber} sectionKeys={secondarySections} />;
          if (secondaryKeys.includes(key)) return null;
          if (key === "projects" && profile.projects.length) return <ProjectSection key={key} profile={profile} labels={t} number={sectionNumber} Arrow={Arrow} />;
          if (key === "skills" && profile.skills.length) return <SkillsSection key={key} locale={profile.locale} labels={t} skills={skills} number={sectionNumber} />;
          return <ProfessionalSection key={key} profile={profile} labels={t} sectionKey={key} number={sectionNumber} />;
        })}

        <section id="contact" className="section contact-band" data-section="contact" aria-labelledby="contact-title" data-reveal="contact">
          <div><span className="section-number" aria-hidden="true">{String(contactNumber).padStart(2, "0")}</span><p className="contact-overline">{t.contact}</p><h2 id="contact-title">{t.contactTitle}</h2></div>
          <div className="contact-links">
            {profile.email && <a href={"mailto:" + profile.email}><bdi>{profile.email}</bdi><ArrowUpRight size={15} aria-hidden="true" /></a>}
            {profile.phone && <a href={"tel:" + profile.phone}><bdi>{profile.phone}</bdi><ArrowUpRight size={15} aria-hidden="true" /></a>}
            {profile.links.map(link => <a key={link.url} href={link.url} rel="me noreferrer">{link.label}<ArrowUpRight size={15} aria-hidden="true" /></a>)}
          </div>
        </section>
      </div>
    </main>
    <PageMotion />
    <footer className="footer"><div className="container"><span className="footer-mark" aria-hidden="true" />{profile.headline}{profile.demo ? " · " + t.demo : ""}</div></footer>
    <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: JSON.stringify(personJsonLd).replace(/</g, "\\u003c") }} />
  </div>;
}

function AboutSection({ profile, labels, number }: { profile: PublicProfile; labels: Labels; number: number }) {
  const portraitAlt = profile.locale === "ar" ? `صورة شخصية لـ ${profile.fullName}` : `Portrait of ${profile.fullName}`;
  return <section id="about" className="section about-section" data-section="about" aria-labelledby="about-title" data-reveal="about">
    <div className="section-heading about-section-heading"><div className="section-index-block"><span className="section-number" aria-hidden="true">{String(number).padStart(2, "0")}</span><span className="section-marker" aria-hidden="true">/</span></div><div><p className="section-kicker">{labels.personalDetails}</p><h2 id="about-title">{labels.aboutMe}</h2></div></div>
    <div className="about-panel">
      <figure className="about-portrait">
        <Image src="/images/profile/osama-attiah.webp" alt={portraitAlt} width={1254} height={1254} sizes="(max-width: 720px) calc(100vw - 80px), 420px" />
      </figure>
      <div className="about-copy">
        <div className="about-copy-topline"><span className="about-copy-label">{labels.aboutMe}</span><h3 className="about-name">{profile.fullName}</h3><p className="about-copy-status"><bdi>{profile.headline}</bdi></p></div>
        <p className="about-lead">{profile.summary}</p>
        <ul className="about-details" aria-label={labels.personalDetails}>
          {profile.location && <li><MapPin size={17} aria-hidden="true" /><bdi>{profile.location}</bdi></li>}
          {profile.email && <li><a href={"mailto:" + profile.email}><Mail size={17} aria-hidden="true" /><bdi>{profile.email}</bdi></a></li>}
          {profile.phone && <li><a href={"tel:" + profile.phone}><Phone size={17} aria-hidden="true" /><bdi>{profile.phone}</bdi></a></li>}
        </ul>
        <div className="about-actions">
          {profile.links.length > 0 && <div className="about-links">{profile.links.map(link => <a key={link.url} href={link.url} rel="me noreferrer">{link.label}<ArrowUpRight size={15} aria-hidden="true" /></a>)}</div>}
          <a className="button button-primary" href={"/resumes/" + profile.slug + "/" + profile.locale + "/resume.pdf"} download>{labels.resume}<Download size={18} aria-hidden="true" /></a>
        </div>
      </div>
    </div>
  </section>;
}

function ProjectSection({ profile, labels, number, Arrow }: { profile: PublicProfile; labels: Labels; number: number; Arrow: typeof ArrowLeft }) {
  return <section id="work" className="section work-section" data-section="work" aria-labelledby="work-title" data-reveal="work">
    <div className="section-heading"><div className="section-index-block"><span className="section-number" aria-hidden="true">{String(number).padStart(2, "0")}</span><span className="section-marker" aria-hidden="true">/</span></div><div><p className="section-kicker">{labels.spotlight}</p><h2 id="work-title">{labels.selected}</h2><p className="section-note"><bdi>{countLabel(profile.locale, profile.projects.length, "projects")}</bdi></p></div></div>
    <div className="project-list">
      {profile.projects.map((project, index) => <article className={"project-entry " + (index === 0 ? "project-entry-featured " : "project-entry-row ") + "project-tone-" + (index % 3)} data-reveal="project" data-reveal-index={index} key={project.slug}>
        <ProjectVisual project={project} index={index} labels={labels} />
        <div className="project-copy">
          <div className="project-topline"><span className="project-kicker">{projectKindLabel(project, labels)}</span><span className="project-sequence" aria-hidden="true">{String(index + 1).padStart(2, "0")}</span></div>
          <h3>{project.name}</h3>
          {project.role && project.role !== project.summary && <p className="project-role">{project.role}</p>}
          <p className="project-summary">{project.summary}</p>
          {project.highlights.length > 0 && <ul className="project-highlights">{project.highlights.slice(0, index === 0 ? 3 : 2).map(highlight => <li key={highlight}>{highlight}</li>)}</ul>}
          <ul className="tags" aria-label={labels.skills}>{project.skills.map(skill => <li className="tag" key={skill}><bdi>{skill}</bdi></li>)}</ul>
          <div className="project-actions">
            <a className="button button-primary" href={"/" + profile.locale + "/p/" + profile.slug + "/projects/" + project.slug + "/"}>{labels.view}<Arrow size={18} aria-hidden="true" /></a>
            {project.demoUrl && <a className="button button-secondary" href={project.demoUrl} rel="noreferrer">{labels.demoLink}<ArrowUpRight size={18} aria-hidden="true" /></a>}
            {project.repositoryUrl && <a className="button button-quiet" href={project.repositoryUrl} rel="noreferrer">{labels.code}<ArrowUpRight size={18} aria-hidden="true" /></a>}
          </div>
        </div>
      </article>)}
    </div>
  </section>;
}

function ProjectVisual({ project, index, labels }: { project: PublicProject; index: number; labels: Labels }) {
  const note = project.cover?.caption ?? (project.role !== project.summary ? project.role : undefined) ?? project.description;
  return <div className={"project-visual project-visual-tone-" + (index % 3) + " " + (project.cover ? "has-image" : "no-image")}>
    {project.cover && <Image className="project-image" src={project.cover.src} alt={project.cover.alt} fill priority={index === 0} sizes="(max-width: 920px) 100vw, 58vw" />}
    <span className="visual-overlay" aria-hidden="true" />
    <div className="visual-topline"><span className="visual-index" aria-hidden="true">{String(index + 1).padStart(2, "0")}</span><span className="visual-type">{projectKindLabel(project, labels)}</span></div>
    <div className="visual-content"><span className="visual-name" aria-hidden="true">{project.name}</span>{note && <p>{note}</p>}</div>
    <div className="visual-foot"><bdi>{project.skills.slice(0, 3).join(" · ")}</bdi><ArrowUpRight size={16} aria-hidden="true" /></div>
  </div>;
}

function SkillsSection({ locale, labels, skills, number }: { locale: "ar" | "en"; labels: Labels; skills: Map<string, PublicProfile["skills"]>; number: number }) {
  return <section id="skills" className="section skills-section" data-section="skills" aria-labelledby="skills-title" data-reveal="skills">
    <div className="section-heading"><div className="section-index-block"><span className="section-number" aria-hidden="true">{String(number).padStart(2, "0")}</span><span className="section-marker" aria-hidden="true">/</span></div><div><p className="section-kicker">{labels.capabilities}</p><h2 id="skills-title">{labels.skills}</h2><p className="section-note"><bdi>{countLabel(locale, skills.size, "skillGroups")}</bdi></p></div></div>
    <div className="skill-groups">{Array.from(skills.entries()).map(([category, items], index) => <section className="skill-group" key={category}><div className="skill-group-topline"><span className="skill-group-index" aria-hidden="true">{String(index + 1).padStart(2, "0")}</span><span className="skill-group-count">{countLabel(locale, items.length, "skills")}</span></div><h3>{category}</h3><p>{items.map(x => x.name).join(" · ")}</p></section>)}</div>
  </section>;
}

function DetailsSection({ profile, labels, number, sectionKeys }: { profile: PublicProfile; labels: Labels; number: number; sectionKeys: string[] }) {
  const blocks = [
    { key: "education", title: labels.education, items: profile.educations.map(item => ({ heading: item.degree + " — " + item.institution, meta: [item.startDate, item.endDate].filter(Boolean).join(" — ") })) },
    { key: "certifications", title: labels.certifications, items: profile.certifications.map(item => ({ heading: item.name, meta: item.issuer + (item.issuedOn ? " · " + item.issuedOn : "") })) },
    { key: "languages", title: labels.languages, items: profile.languages.map(item => ({ heading: item.languageCode.toUpperCase(), meta: item.proficiency })) }
  ].filter(block => sectionKeys.includes(block.key) && block.items.length > 0);
  if (!blocks.length) return null;
  return <section id="details" className="section details-section" data-section="details" aria-labelledby="details-title" data-reveal="details">
    <div className="section-heading"><div className="section-index-block"><span className="section-number" aria-hidden="true">{String(number).padStart(2, "0")}</span><span className="section-marker" aria-hidden="true">/</span></div><div><p className="section-kicker">{labels.profile}</p><h2 id="details-title">{labels.details}</h2><p className="section-note">{blocks.map(block => block.title).join(" · ")}</p></div></div>
    <div className="details-grid">{blocks.map((block, blockIndex) => <section className="details-block" id={block.key} aria-labelledby={block.key + "-title"} key={block.key}><div className="details-block-heading"><span className="details-block-index" aria-hidden="true">{String(blockIndex + 1).padStart(2, "0")}</span><h3 id={block.key + "-title"}>{block.title}</h3></div><div className="details-list">{block.items.map((item, itemIndex) => <article className="detail-entry" key={item.heading}><span className="detail-entry-index" aria-hidden="true">{String(itemIndex + 1).padStart(2, "0")}</span><div><h4>{item.heading}</h4><p><bdi>{item.meta}</bdi></p></div></article>)}</div></section>)}</div>
  </section>;
}

function ProfessionalSection({ profile, labels, sectionKey, number }: { profile: PublicProfile; labels: Labels; sectionKey: string; number: number }) {
  const blocks = [
    { key: "experience", title: labels.experience, items: profile.experiences.map(item => ({ heading: item.jobTitle + " — " + item.organization, meta: formatDateRange(item.startDate, item.endDate, labels.present, profile.locale), body: item.summary, bullets: item.highlights })) },
    { key: "education", title: labels.education, items: profile.educations.map(item => ({ heading: item.degree + " — " + item.institution, meta: [item.startDate, item.endDate].filter(Boolean).join(" — "), body: item.notes, bullets: [] })) },
    { key: "certifications", title: labels.certifications, items: profile.certifications.map(item => ({ heading: item.name + " — " + item.issuer, meta: [item.issuedOn, item.expiresOn].filter(Boolean).join(" — "), body: undefined, bullets: [] })) },
    { key: "languages", title: labels.languages, items: profile.languages.map(item => ({ heading: item.languageCode.toUpperCase(), meta: item.proficiency, body: undefined, bullets: [] })) }
  ];
  const block = blocks.find(value => value.key === sectionKey);
  if (!block?.items.length) return null;
  return <section id={sectionKey} className={"section professional-section " + sectionKey + "-section"} data-section={sectionKey} aria-labelledby={sectionKey + "-title"} data-reveal={sectionKey}>
    <div className="section-heading"><div className="section-index-block"><span className="section-number" aria-hidden="true">{String(number).padStart(2, "0")}</span><span className="section-marker" aria-hidden="true">/</span></div><div><p className="section-kicker">{labels.profile}</p><h2 id={sectionKey + "-title"}>{block.title}</h2><p className="section-note"><bdi>{countLabel(profile.locale, block.items.length, "entries")}</bdi></p></div></div>
    <div className="professional-list">{block.items.map((item, index) => <article className={"professional-item " + (item.body || item.bullets.length > 0 ? "has-detail" : "compact")} data-reveal="professional-item" data-reveal-index={index} key={item.heading + "-" + item.meta}><span className="professional-index" aria-hidden="true">{String(index + 1).padStart(2, "0")}</span><div className="professional-heading"><h3>{item.heading}</h3>{item.meta && <p className="professional-meta"><bdi>{item.meta}</bdi></p>}</div>{(item.body || item.bullets.length > 0) && <div className="professional-detail">{item.body && <p>{item.body}</p>}{item.bullets.length > 0 && <ul>{item.bullets.map(value => <li key={value}>{value}</li>)}</ul>}</div>}</article>)}</div>
  </section>;
}

function projectKindLabel(project: PublicProject, labels: Labels) {
  if (project.kind === "OpenSource") return labels.openSource;
  if (project.kind === "Freelance") return labels.freelance;
  if (project.kind === "Employment") return labels.employment;
  return labels.personal;
}

function formatDateRange(startDate: string, endDate: string | undefined, present: string, locale: "ar" | "en") {
  return formatDate(startDate, locale) + " — " + (endDate ? formatDate(endDate, locale) : present);
}

function formatDate(value: string, locale: "ar" | "en") {
  const [year, month] = value.split("-");
  const monthNumber = Number(month);
  if (!year || !month || !Number.isInteger(monthNumber) || monthNumber < 1 || monthNumber > 12) return value;
  return new Intl.DateTimeFormat(locale === "ar" ? "ar-SA" : "en-US", { month: "short", year: "numeric" }).format(new Date(Number(year), monthNumber - 1, 1));
}
