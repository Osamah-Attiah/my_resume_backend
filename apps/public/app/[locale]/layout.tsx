import { notFound } from "next/navigation";
import { isLocale } from "../../lib/data";
import { LocaleDocument } from "../../components/LocaleDocument";

export function generateStaticParams() { return [{ locale: "en" }, { locale: "ar" }]; }

export default async function LocaleLayout({ children, params }: { children: React.ReactNode; params: Promise<{ locale: string }> }) {
  const { locale } = await params;
  if (!isLocale(locale)) notFound();
  return <><LocaleDocument locale={locale} />{children}</>;
}
