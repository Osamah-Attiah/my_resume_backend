import { mkdir, readFile, writeFile } from "node:fs/promises";
import { createRequire } from "node:module";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "playwright";
import { renderToStaticMarkup } from "react-dom/server";
import { createElement } from "react";
import { demoSnapshot as fixture, normalizePublicationSnapshot, type PublicSiteSnapshot } from "@resume/contracts";
import { printCss, ResumeTemplate } from "../src/ResumeTemplate";

const require = createRequire(import.meta.url);
const here = dirname(fileURLToPath(import.meta.url));
const root = resolve(here, "../../..");
const outputRoot = resolve(root, "artifacts/pdf-qa");
const snapshot = await loadSnapshot();

async function font(path: string) { return (await readFile(require.resolve(path))).toString("base64"); }
const fonts = {
  arabic400: await font("@fontsource/cairo/files/cairo-arabic-400-normal.woff2"),
  arabic700: await font("@fontsource/cairo/files/cairo-arabic-700-normal.woff2"),
  latin400: await font("@fontsource/manrope/files/manrope-latin-400-normal.woff2"),
  latin700: await font("@fontsource/manrope/files/manrope-latin-700-normal.woff2")
};

let browser;
try { browser = await chromium.launch({ channel: "chrome", headless: true }); }
catch { browser = await chromium.launch({ headless: true }); }

const manifest: Array<{ locale: "ar" | "en"; slug: string; artifactDirectory: string; publicDirectory: string; isDefault: boolean; isDemo: boolean }> = [];
const profiles = snapshot.allProfiles ?? Object.values(snapshot.profiles);
const uniqueProfiles = profiles.filter((profile, index) => profiles.findIndex(x => x.locale === profile.locale && x.slug === profile.slug) === index);
for (const document of uniqueProfiles) {
  const locale = document.locale;
  const resumeDocument = { ...(document.pdfDocument ?? document), slug: document.slug, demo: document.demo };
  const isDefault = snapshot.profiles[locale].slug === document.slug;
  const artifactDirectory = isDefault ? resolve(outputRoot, locale) : resolve(outputRoot, "profiles", document.slug, locale);
  const publicDirectory = resolve(root, "apps/public/public/resumes", document.slug, locale);
  const content = renderToStaticMarkup(createElement(ResumeTemplate, { document: resumeDocument }));
  const html = `<!doctype html><html lang="${locale}" dir="${resumeDocument.direction}"><head><meta charset="utf-8"><title>${escapeHtml(resumeDocument.fullName)} — ${escapeHtml(resumeDocument.headline)}</title><style>${printCss(resumeDocument, fonts)}</style></head><body>${content}</body></html>`;
  await mkdir(artifactDirectory, { recursive: true });
  await mkdir(publicDirectory, { recursive: true });
  await writeFile(resolve(artifactDirectory, "resume.json"), JSON.stringify(resumeDocument, null, 2));
  await writeFile(resolve(artifactDirectory, "resume.html"), html);
  const page = await browser.newPage();
  await page.setContent(html, { waitUntil: "load" });
  await page.evaluate(() => globalThis.document.fonts.ready);
  const path = resolve(artifactDirectory, "resume.pdf");
  await page.pdf({ path, format: resumeDocument.pdf.paperSize, printBackground: true, preferCSSPageSize: true, displayHeaderFooter: false, tagged: true });
  await writeFile(resolve(publicDirectory, "resume.pdf"), await readFile(path));
  await page.close();
  manifest.push({ locale, slug: document.slug, artifactDirectory, publicDirectory, isDefault, isDemo: !!document.demo });
}
await browser.close();
await writeFile(resolve(outputRoot, "manifest.json"), JSON.stringify(manifest, null, 2));

function escapeHtml(value: string) { return value.replaceAll("&", "&amp;").replaceAll("<", "&lt;").replaceAll(">", "&gt;").replaceAll('"', "&quot;"); }
async function loadSnapshot(): Promise<PublicSiteSnapshot> {
  const path = process.env.PUBLIC_SNAPSHOT_PATH;
  return path ? normalizePublicationSnapshot(JSON.parse(await readFile(resolve(path), "utf8"))) : fixture;
}
