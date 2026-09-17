import { mkdir, readFile, stat, writeFile } from "node:fs/promises";
import { execFile as execFileCallback } from "node:child_process";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { promisify } from "node:util";
import type { PublicProfile } from "@resume/contracts";

const execFile = promisify(execFileCallback);
const root = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const artifactRoot = resolve(root, "artifacts/pdf-qa");
const fontCache = resolve(root, ".fontconfig-cache");

async function binary(name: string) {
  const envName = `${name.replaceAll("-", "_").toUpperCase()}_BIN`;
  if (process.env[envName]) return process.env[envName]!;
  return name;
}

const tools = { pdftotext: await binary("pdftotext"), pdffonts: await binary("pdffonts"), pdfinfo: await binary("pdfinfo"), pdftoppm: await binary("pdftoppm") };
const failures: string[] = [];
await mkdir(artifactRoot, { recursive: true });
await mkdir(fontCache, { recursive: true });

const manifest = JSON.parse(await readFile(resolve(artifactRoot, "manifest.json"), "utf8")) as Array<{ locale: "ar" | "en"; slug: string; artifactDirectory: string }>;
for (const item of manifest) {
  const locale = item.locale;
  const pdf = resolve(item.artifactDirectory, "resume.pdf");
  const textPath = resolve(item.artifactDirectory, "text.txt");
  const layoutPath = resolve(item.artifactDirectory, "text-layout.txt");
  const document = JSON.parse(await readFile(resolve(item.artifactDirectory, "resume.json"), "utf8")) as PublicProfile;
  const processOptions = { env: { ...process.env, XDG_CACHE_HOME: fontCache } };
  const { stdout: plain } = await execFile(tools.pdftotext, [pdf, "-"], processOptions);
  const { stdout: layout } = await execFile(tools.pdftotext, ["-layout", pdf, "-"], processOptions);
  const { stdout: fonts } = await execFile(tools.pdffonts, [pdf], processOptions);
  const { stdout: info } = await execFile(tools.pdfinfo, [pdf], processOptions);
  await writeFile(textPath, plain); await writeFile(layoutPath, layout);
  await execFile(tools.pdftoppm, ["-png", "-r", "110", pdf, resolve(artifactRoot, locale, "page")], processOptions);
  const normalized = normalizeForSearch(plain);
  const expected = [document.fullName, document.headline.split("|")[0]?.trim(), document.skills[0]?.name, document.projects[0]?.name, document.email].filter((value): value is string => !!value);
  const key = `${locale}/${item.slug}`;
  for (const token of expected) if (!containsExtractedToken(normalized, token)) failures.push(`${key}: missing extracted token: ${token}`);
  if (/[\uFB50-\uFDFF\uFE70-\uFEFF�]/u.test(plain)) failures.push(`${key}: extraction contains presentation forms or replacement characters`);
  if (!/yes\s+yes/i.test(fonts)) failures.push(`${key}: fonts are not embedded with Unicode mapping`);
  const pages = Number(info.match(/^Pages:\s+(\d+)/m)?.[1] ?? 0);
  if (pages < 1 || pages > document.pdf.targetPages) failures.push(`${key}: unexpected page count ${pages}`);
  if ((await stat(pdf)).size > 2_000_000) failures.push(`${key}: PDF exceeds 2 MB`);
  const report = { locale, slug: item.slug, passed: !failures.some(failure => failure.startsWith(key)), checkedAt: new Date().toISOString(), expectedTokens: expected, pageCount: pages, sizeBytes: (await stat(pdf)).size, fonts, pdfInfo: info };
  await writeFile(resolve(item.artifactDirectory, "qa-report.json"), JSON.stringify(report, null, 2));
}

if (failures.length) { console.error(failures.join("\n")); process.exitCode = 1; }
else console.log("Arabic and English PDF text, fonts, page count, and size checks passed.");

function normalizeForSearch(value: string) {
  return value.normalize("NFKC")
    .replace(/[\u061c\u200e\u200f\u202a-\u202e\u2066-\u2069]/g, "")
    .replace(/\s+/g, " ")
    .trim()
    .toLocaleLowerCase();
}

function containsExtractedToken(text: string, token: string) {
  const normalized = normalizeForSearch(token);
  if (text.includes(normalized)) return true;
  if (!/[\u0600-\u06ff]/u.test(token)) return false;
  const words = normalized.split(" ").filter(Boolean);
  return words.length > 1 && text.includes([...words].reverse().join(" "));
}
