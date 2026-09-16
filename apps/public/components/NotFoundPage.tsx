"use client";

import { usePathname } from "next/navigation";
import { useEffect } from "react";

const copy = {
  ar: {
    title: "الصفحة غير موجودة",
    description: "الصفحة المطلوبة غير موجودة في النسخة المنشورة الحالية.",
    home: "العودة إلى الموقع",
    other: "English"
  },
  en: {
    title: "Page not found",
    description: "The requested public page is not part of this published snapshot.",
    home: "Back to the site",
    other: "العربية"
  }
} as const;

export function NotFoundPage() {
  const pathname = usePathname();
  const locale = pathname?.split("/").filter(Boolean)[0] === "ar" ? "ar" : "en";
  const isArabic = locale === "ar";
  const t = copy[locale];

  useEffect(() => {
    document.documentElement.lang = locale;
    document.documentElement.dir = isArabic ? "rtl" : "ltr";
  }, [isArabic, locale]);

  return <main className="not-found" lang={locale} dir={isArabic ? "rtl" : "ltr"} suppressHydrationWarning><div><p className="eyebrow">404</p><h1>{t.title}</h1><p>{t.description}</p><div className="row-actions"><a className="button button-primary" href={`/${locale}/`}>{t.home}</a><a className="button button-secondary" href={`/${isArabic ? "en" : "ar"}/`} hrefLang={isArabic ? "en" : "ar"}>{t.other}</a></div></div></main>;
}
