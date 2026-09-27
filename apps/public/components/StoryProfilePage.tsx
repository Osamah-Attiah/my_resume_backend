import Image from "next/image";
import { ArrowLeft, ArrowRight, ArrowUpRight, Download } from "lucide-react";
import { isOsamahProfileSlug, projectLinksFor, type Locale, type PublicProfile, type StoryScene } from "@resume/contracts";
import { storyFrames, storyFrameStyle } from "../lib/story-frames";

const words = {
  ar: { skip: "تجاوز التجربة إلى الأعمال", work: "الأعمال", experience: "الخبرة", contact: "التواصل", resume: "تحميل السيرة PDF", explore: "مرّر لتتبع الرحلة", detail: "كيف يعمل؟", visualTop: "من التطبيق إلى الخدمة الخلفية", mobileWork: "تطبيقات الهاتف", backendWork: "الخدمات الخلفية", focus: "تطبيقات / خدمات خلفية", backendIllustrative: "هندسة الخدمات الخلفية", ui: "الواجهة", state: "الحالة", localData: "بيانات محلية", app: "التطبيق", request: "طلب جديد", recipient: "المستلم", amount: "القيمة", send: "أرسل الطلب", sending: "جارٍ الإرسال", sent: "تم إرسال الطلب", local: "محفوظ محليًا", sync: "مزامنة عند عودة الاتصال", toService: "طلب إلى الخدمة", service: "الخدمة الخلفية", receive: "استقبال", validate: "تحقق", rules: "قواعد العمل", data: "البيانات", result: "النتيجة", toApp: "استجابة للتطبيق", serviceLogic: "منطق الخدمة", response: "استجابة واضحة", basTitle: "Bas / مسارا تكامل", basWeb: "واجهة Flutter Web", basClients: "تطبيقات Flutter", basBridge: "الجسر", basKmp: "مكتبة KMP", idrisTitle: "Idris / من التشغيل إلى التطبيق", idrisSteps: ["طلب", "زيارة قياس", "فحص جودة", "تسليم"], archive: "كل الأعمال", role: "دوري في هذا العمل", illustrative: "شرح تفاعلي", separate: "مشروع مستقل", about: "عن أسامة", aboutLead: "أعمل بين واجهة الهاتف ومنطق الخدمة الخلفية، وأهتم بما يحتاجه كل طرف ليفهم الآخر.", more: "اقرأ المشروع", next: "اختَر ما تريد معرفته بعد ذلك", projects: "استكشف المشاريع", email: "راسلني" },
  en: { skip: "Skip the story and see work", work: "Work", experience: "Experience", contact: "Contact", resume: "Download resume PDF", explore: "Scroll to follow the journey", detail: "How it works", visualTop: "FROM APP TO BACKEND", mobileWork: "Mobile apps", backendWork: "Backend services", focus: "APPS / BACKEND SERVICES", backendIllustrative: "BACKEND ENGINEERING", ui: "UI", state: "STATE", localData: "LOCAL DATA", app: "The app", request: "New request", recipient: "Recipient", amount: "Amount", send: "Send request", sending: "Sending…", sent: "Request sent", local: "Saved locally", sync: "Sync when connected", toService: "Request to service", service: "Backend service", receive: "Receive", validate: "Validate", rules: "Business rules", data: "Data", result: "Result", toApp: "Response to app", serviceLogic: "Service logic", response: "Clear response", basTitle: "Bas / two integration paths", basWeb: "Flutter Web interface", basClients: "Flutter clients", basBridge: "Bridge", basKmp: "KMP library", idrisTitle: "Idris / operations to app", idrisSteps: ["Order", "Measure", "Quality check", "Delivery"], archive: "All work", role: "My role in this work", illustrative: "Interactive explanation", separate: "Separate project", about: "About Osamah", aboutLead: "I work between the mobile interface and backend logic, paying attention to what each side needs from the other.", more: "Read the project", next: "Where would you like to go next?", projects: "Explore projects", email: "Email me" }
} as const;

const visualStages: Record<Locale, Record<StoryScene["stage"], string>> = {
  ar: { intro: "البداية", action: "الفعل", layers: "داخل التطبيق", offline: "دون اتصال", backend: "إلى الخدمة الخلفية", "backend-focus": "داخل الخدمة", integration: "تكاملات Bas", operations: "عمليات Idris", return: "النتيجة" },
  en: { intro: "THE BEGINNING", action: "THE ACTION", layers: "INSIDE THE APP", offline: "OFFLINE", backend: "TO THE BACKEND", "backend-focus": "INSIDE THE SERVICE", integration: "BAS INTEGRATIONS", operations: "IDRIS OPERATIONS", return: "THE RESULT" }
};

function StoryTitle({ title, intro }: { title: string; intro: boolean }) {
  const parts = intro ? title.split(/\.\s+/, 2) : [];
  if (parts.length === 2) return <><span className="story-title-first">{parts[0]}.</span><span className="story-title-accent">{parts[1]}</span></>;
  const splitAt = title.lastIndexOf(" ");
  if (splitAt < 0) return <>{title}</>;
  return <>{title.slice(0, splitAt)} <span className="story-title-accent">{title.slice(splitAt + 1)}</span></>;
}

function StorySceneVisual({ total, locale, t }: { total: number; locale: Locale; t: (typeof words)[Locale] }) {
  return <div className="story-visual story-visual-traveler" data-story-visual data-story-labels={JSON.stringify(visualStages[locale])} style={storyFrameStyle("intro")} aria-hidden="true">
    <div className="story-visual-inner">
      <div className="story-visual-top"><span data-story-stage-label>{visualStages[locale].intro}</span><span data-story-counter>01 / {String(total).padStart(2, "0")}</span></div>
      <div className="story-art" role="presentation">
        <div className="story-phone">
          <div className="story-phone-top"><span className="story-shell-dots"><i /><i /><i /></span><span className="story-shell-mode story-shell-mode-app">APP / UI</span><span className="story-shell-mode story-shell-mode-service">SERVICE / LOGIC</span></div>
          <span className="story-shell-spine"><i /></span>
          <div className="story-phone-screen" data-phone-screen>
            <div className="story-phone-view story-phone-view-form">
            <span className="story-app-label">{t.app}</span>
            <span className="story-app-title">{t.request}</span>
            <span className="story-form-line"><span>{t.recipient}</span></span><span className="story-form-line story-form-line-short"><span>{t.amount}</span></span>
            <span className="story-app-button"><span className="story-app-button-idle">{t.send}</span><span className="story-app-button-sending">{t.sending}</span><span className="story-app-button-sent">{t.sent}</span></span>
            <span className="story-app-result">✓ {t.sent}</span>
            <span className="story-local-note">✓ {t.local}</span>
            </div>
            <div className="story-phone-view story-phone-view-layers">
              <span className="story-app-label">{t.app}</span><span className="story-app-title">{t.state}</span>
              <ol className="story-screen-steps"><li data-screen-step="1"><i>01</i><span>{t.ui}</span><b>✓</b></li><li data-screen-step="2"><i>02</i><span>{t.state}</span><b>✓</b></li><li data-screen-step="3"><i>03</i><span>{t.localData}</span><b>✓</b></li></ol>
              <span className="story-screen-footnote">{t.local}</span>
            </div>
            <div className="story-phone-view story-phone-view-offline">
              <span className="story-app-label">{t.app}</span><span className="story-app-title">{t.sync}</span>
              <span className="story-screen-status"><i>✓</i>{t.local}</span>
              <span className="story-screen-sync-line"><i /></span>
              <span className="story-screen-footnote">{t.sync}</span>
            </div>
            <div className="story-phone-view story-phone-view-backend">
              <span className="story-app-label">{t.service} / 01</span>
              <div className="story-console-request" dir="ltr"><b>POST</b><code>/api/requests</code><span>→</span></div>
              <span className="story-console-track"><i /></span>
              <ol className="story-screen-steps"><li data-screen-step="1"><i>01</i><span>{t.receive}</span><b>✓</b></li><li data-screen-step="2"><i>02</i><span>{t.rules}</span><b>✓</b></li><li data-screen-step="3"><i>03</i><span>{t.data}</span><b>✓</b></li><li data-screen-step="4"><i>04</i><span>{t.result}</span><b>✓</b></li></ol>
              <span className="story-screen-footnote">{t.toApp} <span aria-hidden="true">↗</span></span>
            </div>
            <div className="story-phone-view story-phone-view-backend-focus">
              <span className="story-app-label">{t.service} / 02</span>
              <span className="story-app-title">{t.serviceLogic}<span className="story-console-cursor" /></span>
              <ol className="story-decision-steps"><li data-screen-step="1"><i>01</i><span>{t.validate}</span></li><li data-screen-step="2"><i>02</i><span>{t.rules}</span></li><li data-screen-step="3"><i>03</i><span>{t.data}</span></li></ol>
              <span className="story-decision-result"><i>04 / {t.result}</i><strong>{t.response}</strong><span>{t.toApp} ↗</span></span>
            </div>
            <div className="story-phone-view story-phone-view-integration">
              <span className="story-app-label">Bas</span><span className="story-app-title">{t.basTitle}</span>
              <ol className="story-screen-steps"><li data-screen-step="1"><i>01</i><span>{t.basClients}</span><b>→</b></li><li data-screen-step="2"><i>02</i><span>{t.basBridge}</span><b>→</b></li><li data-screen-step="3"><i>03</i><span>{t.basKmp}</span><b>✓</b></li></ol>
              <span className="story-screen-footnote">Kotlin · Java · Swift</span>
            </div>
            <div className="story-phone-view story-phone-view-operations">
              <span className="story-app-label">Idris</span><span className="story-app-title">{t.idrisTitle}</span>
              <ol className="story-screen-steps"><li data-screen-step="1"><i>01</i><span>{t.idrisSteps[0]}</span><b>✓</b></li><li data-screen-step="2"><i>02</i><span>{t.idrisSteps[1]}</span><b>✓</b></li><li data-screen-step="3"><i>03</i><span>{t.idrisSteps[2]}</span><b>✓</b></li><li data-screen-step="4"><i>04</i><span>{t.idrisSteps[3]}</span><b>✓</b></li></ol>
            </div>
            <div className="story-phone-view story-phone-view-return">
              <span className="story-app-label">{t.app}</span><span className="story-app-title">{t.result}</span>
              <span className="story-screen-success">✓</span><span className="story-screen-footnote">{t.sent}</span>
            </div>
          </div>
        </div>
      </div>
      <div className="story-visual-bottom"><span data-story-caption>{visualStages[locale].intro}</span><span className="story-visual-progress"><i data-story-progress style={{ width: `${100 / total}%` }} /></span></div>
    </div>
  </div>;
}

export function StoryProfilePage({ profile, story, baseUrl, isDefault }: { profile: PublicProfile; story: StoryScene[]; baseUrl: string; isDefault: boolean }) {
  const t = words[profile.locale];
  const displayName = profile.slug === "osamah" && profile.locale === "ar" ? "أسامة عطية" : profile.fullName;
  const other = profile.locale === "ar" ? "en" : "ar";
  const Arrow = profile.locale === "ar" ? ArrowLeft : ArrowRight;
  const profilePath = isDefault ? `/${profile.locale}/` : `/${profile.locale}/p/${profile.slug}/`;
  const otherPath = isDefault ? `/${other}/` : `/${other}/p/${profile.slug}/`;
  const canonical = profile.seo.canonical ?? `${baseUrl}${profilePath}`;
  const projectUrl = (slug: string) => `/${profile.locale}/project/?profile=${encodeURIComponent(profile.slug)}&slug=${encodeURIComponent(slug)}`;
  const resumeUrl = `/resumes/${profile.slug}/${profile.locale}/resume.pdf`;
  const linked = new Set(story.map(scene => scene.projectSlug).filter(Boolean));
  const featured = profile.projects.filter(project => linked.has(project.slug));
  const archive = profile.projects.filter(project => !linked.has(project.slug));
  const mobileScene = story.find(scene => scene.stage === "layers" || scene.stage === "action");
  const backendScene = story.find(scene => scene.stage === "backend" || scene.stage === "backend-focus");
  const jsonLd = { "@context": "https://schema.org", "@graph": [
    { "@type": "WebSite", "@id": `${baseUrl}/#website`, url: `${baseUrl}/`, name: displayName, inLanguage: ["ar", "en"] },
    { "@type": "ProfilePage", "@id": `${canonical}#profile-page`, url: canonical, inLanguage: profile.locale, isPartOf: { "@id": `${baseUrl}/#website` }, mainEntity: { "@id": `${baseUrl}/#person` } },
    { "@type": "Person", "@id": `${baseUrl}/#person`, name: displayName, jobTitle: profile.headline, description: profile.summary, knowsAbout: profile.skills.map(skill => skill.name), sameAs: profile.links.map(link => link.url) }
  ] };
  return <div className="story-page" lang={profile.locale} dir={profile.direction}>
    <a className="skip-link" href="#work">{t.skip}</a>
    <header className="story-header">
      <a className="story-brand" href={profilePath}><span className="story-brand-mark" aria-hidden="true">O<span>.</span></span><span>{displayName}</span></a>
      <nav aria-label={profile.locale === "ar" ? "التنقل الرئيسي" : "Main navigation"}>
        <a href="#work">{t.work}</a><a href="#contact">{t.contact}</a>
        <a className="story-locale" href={otherPath} hrefLang={other}>{other === "ar" ? "العربية" : "English"}</a>
      </nav>
    </header>
    <main id="main">
      <div className="story-sequence" data-story-sequence>
        <div className="story-script">
          {story.map((scene, index) => {
            const project = scene.projectSlug ? profile.projects.find(item => item.slug === scene.projectSlug) : undefined;
            return <section className="story-beat story-flow-beat" data-story-beat={index} data-story-number={String(index + 1).padStart(2, "0")} data-story-stage={scene.stage} id={index === 0 ? "intro" : `scene-${scene.key}`} key={scene.key} aria-labelledby={`story-title-${scene.key}`}>
              <div className="story-beat-heading">
                <div className="story-beat-meta"><span>{String(index + 1).padStart(2, "0")} / {String(story.length).padStart(2, "0")}</span><span>{index === 0 ? t.focus : scene.stage === "backend" || scene.stage === "backend-focus" ? t.backendIllustrative : project ? t.separate : visualStages[profile.locale][scene.stage]}</span></div>
                {index === 0 && <div className="story-person">{isOsamahProfileSlug(profile.slug) && <Image src="/images/profile/osama-attiah.webp" alt={profile.locale === "ar" ? `صورة ${displayName}` : `Portrait of ${displayName}`} width={80} height={80} priority />}<span>{displayName}<small>{profile.locale === "ar" ? "تطبيقات هاتف وخدمات خلفية" : "Mobile apps & backend services"}</small></span></div>}
                {index === 0 ? <h1 id={`story-title-${scene.key}`} aria-label={scene.title}><StoryTitle title={scene.title} intro /></h1> : <h2 id={`story-title-${scene.key}`} aria-label={scene.title}><StoryTitle title={scene.title} intro={false} /></h2>}
              </div>
              <div className="story-visual-anchor" data-story-anchor data-story-index={index} data-story-stage={scene.stage} data-story-frame={JSON.stringify(storyFrames[scene.stage])} data-story-label={scene.visualLabel ?? ""} aria-hidden="true">
                {index === 0 && <StorySceneVisual total={story.length} locale={profile.locale} t={t} />}
              </div>
              <div className="story-beat-explanation">
                <p className="story-beat-body">{scene.body}</p>
                {index === 0 && mobileScene && backendScene && <nav className="story-practice-links" aria-label={profile.locale === "ar" ? "مجالات العمل في القصة" : "Areas of work in the story"}><a href={`#scene-${mobileScene.key}`}><span>01</span>{t.mobileWork}<Arrow size={16} aria-hidden="true" /></a><a href={`#scene-${backendScene.key}`}><span>02</span>{t.backendWork}<Arrow size={16} aria-hidden="true" /></a></nav>}
                {scene.detail && <details className="story-detail"><summary>{t.detail}<span aria-hidden="true">+</span></summary><p>{scene.detail}</p></details>}
                {project && <a className="story-project-link" href={projectUrl(project.slug)}><span>{t.role}: <bdi>{project.name}</bdi></span><Arrow size={18} aria-hidden="true" /></a>}
                {index === 0 && <div className="story-hero-actions"><a href="#work">{t.projects}<Arrow size={18} /></a><a href={resumeUrl} download>{t.resume}<Download size={17} /></a>{profile.experiences.length > 0 && <a href="#experience">{t.experience}<Arrow size={18} /></a>}</div>}
                {index === 0 && <a className="story-scroll-cue" href={`#scene-${story[1]?.key ?? scene.key}`}><span aria-hidden="true">↓</span>{t.explore}</a>}
              </div>
            </section>;
          })}
        </div>
      </div>
      <section id="work" className="story-work" aria-labelledby="story-work-title">
        <div className="story-section-heading"><span>01 / {t.work}</span><h2 id="story-work-title">{t.archive}</h2><p>{profile.locale === "ar" ? "قصص مستقلة؛ لكل مشروع نطاق ودور موضحان في صفحته." : "Separate stories, each with its own scope and role."}</p></div>
        <div className="story-projects">
          {[...featured, ...archive].map((project, index) => <article className="story-project" key={project.slug}>
            <span className="story-project-number">{String(index + 1).padStart(2, "0")}</span>
            <div><span className="story-project-type">{project.kind === "Employment" ? (profile.locale === "ar" ? "خبرة وظيفية" : "Professional work") : project.kind === "Freelance" ? (profile.locale === "ar" ? "عمل حر" : "Freelance") : profile.locale === "ar" ? "مشروع" : "Project"}</span><h3>{project.name}</h3><p>{project.summary}</p><div className="story-project-external-links">{projectLinksFor(project, profile.locale).map(link => <a href={link.url} key={link.url} target="_blank" rel="noopener noreferrer" aria-label={`${link.label} — ${profile.locale === "ar" ? "يفتح في تبويب جديد" : "opens in a new tab"}`}><bdi>{link.label}</bdi><ArrowUpRight size={15} aria-hidden="true" /></a>)}</div></div>
            <a href={projectUrl(project.slug)} aria-label={`${t.more}: ${project.name}`}><ArrowUpRight size={24} /><span>{t.more}</span></a>
          </article>)}
        </div>
      </section>
      {profile.experiences.length > 0 && <section id="experience" className="story-experience" aria-labelledby="story-experience-title"><div className="story-section-heading"><span>02 / {t.experience}</span><h2 id="story-experience-title">{t.experience}</h2><p>{profile.locale === "ar" ? "الأدوار التي شكّلت عملي عبر تطبيقات الهاتف والتكامل مع الأنظمة." : "The roles behind my mobile work and system integrations."}</p></div><div className="story-experience-list">{profile.experiences.map((item, index) => <article key={`${item.organization}-${item.startDate}-${index}`}><span>{String(index + 1).padStart(2, "0")}</span><div><h3>{item.jobTitle}</h3><p className="story-experience-org">{item.organization}</p>{item.summary && <p>{item.summary}</p>}{item.highlights[0] && <p>{item.highlights[0]}</p>}</div></article>)}</div></section>}
      <section className="story-about" aria-labelledby="story-about-title"><div><span>{profile.experiences.length > 0 ? "03" : "02"} / {t.about}</span><h2 id="story-about-title">{displayName}</h2>{profile.slug === "osamah" && <p className="story-about-lead">{t.aboutLead}</p>}<p>{profile.summary.split("\n")[0]}</p></div><a href={resumeUrl} download>{t.resume}<Download size={18} /></a></section>
      <section id="contact" className="story-contact" aria-labelledby="story-contact-title"><span>{profile.experiences.length > 0 ? "04" : "03"} / {t.contact}</span><h2 id="story-contact-title">{t.next}</h2><div>{profile.email && <a href={`mailto:${profile.email}`}>{t.email} <ArrowUpRight size={19} /></a>}{profile.phone && <a href={`tel:${profile.phone}`}><bdi>{profile.phone}</bdi><ArrowUpRight size={18} /></a>}<a href={resumeUrl} download>{t.resume}<Download size={18} /></a>{profile.links.map(link => <a key={link.url} href={link.url} rel="me noreferrer">{link.label}<ArrowUpRight size={18} /></a>)}</div></section>
    </main>
    <footer className="story-footer"><span>{displayName}</span><a href="#intro">↑ {profile.locale === "ar" ? "العودة للبداية" : "Back to top"}</a></footer>
    <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd).replace(/</g, "\\u003c") }} />
  </div>;
}
