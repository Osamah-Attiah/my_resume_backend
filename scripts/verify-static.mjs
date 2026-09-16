import { access, readFile, readdir } from "node:fs/promises";
import { relative, resolve } from "node:path";

const out = resolve(process.cwd(), "apps/public/out");
const errors = [];
const ar = await text("ar/index.html"), en = await text("en/index.html"), missing = await text("404.html"), robots = await text("robots.txt"), headers = await text("_headers");
check(ar.startsWith("<!DOCTYPE html>"), "Arabic page must be a complete HTML document");
check(ar.includes('<html lang="ar" dir="rtl"'), "Arabic root lang/dir missing");
check(en.includes('<html lang="en" dir="ltr"'), "English root lang/dir missing");
for (const [name, html] of [["ar", ar], ["en", en]]) {
  check(html.includes('rel="canonical"'), `${name}: canonical missing`);
  const indexable = !html.includes('name="robots" content="noindex, follow"');
  const alternateLinks = html.match(/<link rel="alternate" hrefLang="[^"]+" href="[^"]+"\/>/g) ?? [];
  if (indexable) check(alternateLinks.some(value => value.includes('hrefLang="ar"')) && alternateLinks.some(value => value.includes('hrefLang="en"')) && alternateLinks.some(value => value.includes('hrefLang="x-default"')), `${name}: hreflang set incomplete`);
  else check(alternateLinks.length === 0, `${name}: noindex page must not advertise hreflang alternates`);
  check(html.includes('type="application/ld+json"'), `${name}: JSON-LD missing`);
  const jsonLdText = html.match(/<script type="application\/ld\+json">([\s\S]*?)<\/script>/)?.[1];
  try {
    const jsonLd = JSON.parse(jsonLdText ?? "");
    const types = (jsonLd["@graph"] ?? []).map(value => value["@type"]);
    check(types.includes("WebSite") && types.includes("ProfilePage") && types.includes("Person"), `${name}: JSON-LD graph is incomplete`);
  } catch { errors.push(`${name}: JSON-LD is not valid JSON`); }
  check(html.includes('property="og:image"') && html.includes('name="twitter:image"'), `${name}: social image metadata missing`);
  check(html.includes('name="twitter:card" content="summary_large_image"'), `${name}: large Twitter card missing`);
  const ogImageUrl = html.match(/property="og:image" content="(https:\/\/[^\"]+)"/)?.[1];
  if (!ogImageUrl) errors.push(`${name}: Open Graph image URL is invalid`);
  else { const ogImage = new URL(ogImageUrl); if (ogImage.pathname.startsWith("/og/")) await exists(ogImage.pathname.slice(1)); }
  check(!/<script\b(?![^>]*\btype="application\/ld\+json")/i.test(html), `${name}: unexpected executable script in static output`);
  check(!/<link\b(?=[^>]*\brel="preload")(?=[^>]*\bas="script")/i.test(html), `${name}: unexpected script preload in static output`);
  check(html.includes('name="robots"'), `${name}: robots metadata missing`);
  if (html.includes("example.invalid")) check(html.includes('name="robots" content="noindex, follow"'), `${name}: demo content must stay noindex`);
  check(!html.includes("localhost") && !html.includes("127.0.0.1"), `${name}: local URL leaked into static output`);
}
check(missing.includes("404") && missing.includes('name="robots" content="noindex"'), "404 page or noindex missing");
check(robots.includes("Sitemap:"), "robots sitemap missing");
check(!/^Disallow: \/\s*$/m.test(robots), "robots must not block crawlers from reading page-level noindex metadata");
check(headers.includes("/resumes/*") && headers.includes("X-Robots-Tag: noindex") && headers.includes("Content-Type: application/pdf"), "PDF noindex/content-type headers missing");
await exists("icon.svg");
for (const html of [ar, en]) {
  const pdf = html.match(/href="\/(resumes\/[^\"]+\.pdf)"/)?.[1];
  if (!pdf) errors.push("Public resume download link missing"); else await exists(pdf);
}
const manifest = JSON.parse(await readFile(resolve(process.cwd(), "artifacts/pdf-qa/manifest.json"), "utf8"));
const expectedPdfPaths = manifest.map(item => `resumes/${item.slug}/${item.locale}/resume.pdf`).sort();
for (const root of [resolve(process.cwd(), "apps/public/public/resumes"), resolve(out, "resumes")]) {
  const actualPdfPaths = (await files(root)).filter(path => path.endsWith(".pdf")).map(path => `resumes/${relative(root, path)}`).sort();
  check(JSON.stringify(actualPdfPaths) === JSON.stringify(expectedPdfPaths), `Stale or missing public PDF files in ${relative(process.cwd(), root)}: expected ${expectedPdfPaths.join(", ")}; found ${actualPdfPaths.join(", ")}`);
}
if (errors.length) { console.error(errors.join("\n")); process.exit(1); }
console.log("Static HTML, script-free delivery, locale direction, SEO/social metadata, robots, icon, 404, and public PDF checks passed.");

async function text(path) { try { return await readFile(resolve(out, path), "utf8"); } catch { errors.push(`Missing static file: ${path}`); return ""; } }
async function exists(path) { try { await access(resolve(out, path)); } catch { errors.push(`Missing static file: ${path}`); } }
async function files(root) { const entries = await readdir(root, { withFileTypes: true }); return (await Promise.all(entries.map(entry => entry.isDirectory() ? files(resolve(root, entry.name)) : [resolve(root, entry.name)]))).flat(); }
function check(value, message) { if (!value) errors.push(message); }
