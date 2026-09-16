import type { PublicSiteSnapshot } from "./index";

export const demoSnapshot: PublicSiteSnapshot = {
  schemaVersion: 1,
  baseUrl: "https://example.invalid",
  lastModified: "2026-09-11",
  redirects: [{ source: "/en/p/portfolio/", target: "/en/", status: 301 }],
  profiles: {
    en: {
      locale: "en",
      direction: "ltr",
      slug: "portfolio",
      indexable: false,
      demo: true,
      fullName: "DEMO PROFILE",
      headline: "Software Engineer | Flutter & .NET Backend",
      summary: "Clearly labeled test content for validating the portfolio, bilingual layouts, static publishing, and ATS resume generation. Replace every field before publishing.",
      email: "demo@example.invalid",
      phone: "+966 50 000 0000",
      links: [
        { kind: "GitHub", label: "Sample GitHub", url: "https://github.com/example" },
        { kind: "LinkedIn", label: "Sample LinkedIn", url: "https://www.linkedin.com" }
      ],
      skills: [
        { category: "Mobile", name: "Flutter" },
        { category: "Backend", name: ".NET" },
        { category: "Backend", name: "ASP.NET Core" },
        { category: "Data", name: "PostgreSQL" }
      ],
      projects: [{
        slug: "sample-resume-platform",
        name: "Sample Resume Platform",
        role: "Demo project",
        kind: "Personal",
        summary: "A fictional project used to verify bilingual content selection and static publishing.",
        description: "This is test data, not professional experience or a client engagement.",
        highlights: [
          "Verifies Arabic and English layouts without claiming a business outcome.",
          "Keeps public pages and resume files available without a live backend."
        ],
        skills: ["React", "ASP.NET Core", "PostgreSQL"],
        repositoryUrl: "https://github.com/example/sample"
      }],
      experiences: [],
      educations: [],
      certifications: [],
      languages: [{ languageCode: "ar", proficiency: "Sample only" }, { languageCode: "en", proficiency: "Sample only" }],
      pdf: { paperSize: "Letter", fontSize: 9.5, marginMm: 16.5, targetPages: 2 },
      seo: {
        title: "DEMO PROFILE — Software Engineer, Flutter & .NET",
        description: "Test-only portfolio for a Software Engineer focused on Flutter and .NET backend development."
      }
    },
    ar: {
      locale: "ar",
      direction: "rtl",
      slug: "portfolio",
      indexable: false,
      demo: true,
      fullName: "ملف تجريبي",
      headline: "مهندس برمجيات | Flutter وخدمات خلفية باستخدام .NET",
      summary: "محتوى اختبار معلّم بوضوح للتحقق من معرض الأعمال والعرض ثنائي اللغة والنشر الثابت وتوليد سيرة مناسبة للاستخراج النصي. استبدل جميع الحقول قبل النشر.",
      email: "demo@example.invalid",
      phone: "+966 50 000 0000",
      links: [
        { kind: "GitHub", label: "GitHub تجريبي", url: "https://github.com/example" },
        { kind: "LinkedIn", label: "LinkedIn تجريبي", url: "https://www.linkedin.com" }
      ],
      skills: [
        { category: "تطبيقات الهاتف المحمول", name: "Flutter" },
        { category: "الخدمات الخلفية", name: ".NET" },
        { category: "الخدمات الخلفية", name: "ASP.NET Core" },
        { category: "البيانات", name: "PostgreSQL" }
      ],
      projects: [{
        slug: "sample-resume-platform",
        name: "منصة سيرة تجريبية",
        role: "مشروع تجريبي",
        kind: "Personal",
        summary: "مشروع افتراضي للتحقق من انتقاء المحتوى ثنائي اللغة والنشر الثابت.",
        description: "هذه بيانات اختبار، وليست خبرة وظيفية أو عملاً لعميل حقيقي.",
        highlights: [
          "يتحقق من تنسيق العربية والإنجليزية دون ادعاء نتيجة تجارية.",
          "يبقي الصفحات العامة وملفات السيرة متاحة دون الحاجة إلى تشغيل خدمة خلفية حيّة."
        ],
        skills: ["React", "ASP.NET Core", "PostgreSQL"],
        repositoryUrl: "https://github.com/example/sample"
      }],
      experiences: [],
      educations: [],
      certifications: [],
      languages: [{ languageCode: "ar", proficiency: "للاختبار فقط" }, { languageCode: "en", proficiency: "For testing only" }],
      pdf: { paperSize: "Letter", fontSize: 9.5, marginMm: 16.5, targetPages: 2 },
      seo: {
        title: "ملف تجريبي — مهندس برمجيات، Flutter و.NET",
        description: "معرض أعمال تجريبي لمهندس برمجيات متخصص في Flutter والخدمات الخلفية باستخدام .NET."
      }
    }
  }
};
