import { demoSnapshot } from "../lib/data";

export default function LanguageEntry() {
  return <main className="language-entry">
    <section className="language-card" aria-labelledby="language-title">
      <p className="eyebrow">{Object.values(demoSnapshot.profiles).some(profile => profile.demo) ? "DEMO / ملف تجريبي" : "Portfolio / الملف الشخصي"}</p>
      <h1 id="language-title">Choose a language<br /><span lang="ar" dir="rtl">اختر اللغة</span></h1>
      <div className="language-options">
        <a className="button button-primary" href="/en/" hrefLang="en">English</a>
        <a className="button button-secondary" href="/ar/" hrefLang="ar"><span lang="ar">العربية</span></a>
      </div>
    </section>
  </main>;
}
