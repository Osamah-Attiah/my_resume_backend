import { readdir, readFile, rm, writeFile } from "node:fs/promises";
import { dirname, resolve, sep } from "node:path";
import { fileURLToPath } from "node:url";

const out = resolve(dirname(fileURLToPath(import.meta.url)), "../out");
const arabicRoot = resolve(out, "ar");
for (const file of await htmlFiles(arabicRoot)) {
  const html = stripHydration(await readFile(file, "utf8"));
  if (!html.includes('<html lang="en"')) {
    if (isEmptyProjectsNotFound(file)) { await rm(dirname(file), { recursive: true, force: true }); continue; }
    throw new Error(`Expected root html language marker in ${file}`);
  }
  await writeFile(file, html.replace(/<html lang="en"(?: dir="[^"]+")?[^>]*>/, '<html lang="ar" dir="rtl">'));
}

for (const file of await htmlFiles(resolve(out, "en"))) {
  const html = stripHydration(await readFile(file, "utf8"));
  if (!html.includes('<html lang="en"')) {
    if (isEmptyProjectsNotFound(file)) { await rm(dirname(file), { recursive: true, force: true }); continue; }
    throw new Error(`Expected English root language marker in ${file}`);
  }
  await writeFile(file, html.replace(/<html lang="en"(?: dir="[^"]+")?[^>]*>/, '<html lang="en" dir="ltr">'));
}

// Published pages use a tiny framework-independent runtime for progressive
// enhancement. Next's hydration payload is removed; structured data and the
// static runtime are preserved.
for (const file of await htmlFiles(out)) {
  if (file.startsWith(arabicRoot) || file.startsWith(resolve(out, "en"))) continue;
  await writeFile(file, stripHydration(await readFile(file, "utf8")));
}

function stripHydration(html) {
  const result = html
    .replace(/<link\b(?=[^>]*\brel="preload")(?=[^>]*\bas="script")[^>]*>/gi, "")
    .replace(/<script\b(?![^>]*\b(?:type="application\/ld\+json"|data-static-runtime))[^>]*>[\s\S]*?<\/script>/gi, "");
  if (/<script\b(?![^>]*\b(?:type="application\/ld\+json"|data-static-runtime))/i.test(result)) {
    throw new Error("Unexpected executable script remained in static HTML");
  }
  return result;
}

function isEmptyProjectsNotFound(file) {
  return file.split(sep).includes("__no_projects__");
}

async function htmlFiles(directory) {
  const entries = await readdir(directory, { withFileTypes: true });
  const nested = await Promise.all(entries.map(entry => entry.isDirectory() ? htmlFiles(resolve(directory, entry.name)) : entry.name.endsWith(".html") ? [resolve(directory, entry.name)] : []));
  return nested.flat();
}
