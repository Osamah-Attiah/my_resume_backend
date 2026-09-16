import { readFile, readdir, rm, mkdir } from "node:fs/promises";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import sharp from "sharp";

const appRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const publicOutput = resolve(appRoot, "out");
const outputRoot = resolve(publicOutput, "og");
const cards = new Map();

for (const file of await htmlFiles(publicOutput)) {
  const html = await readFile(file, "utf8");
  const imageUrl = meta(html, "property", "og:image");
  if (!imageUrl) continue;
  const target = new URL(imageUrl).pathname.replace(/^\//, "");
  if (!target.startsWith("og/")) continue;
  if (!target.endsWith(".png") || target.includes("..")) throw new Error(`Unsafe Open Graph image path in ${file}`);
  cards.set(target, {
    title: meta(html, "property", "og:title") ?? "Software portfolio",
    subtitle: meta(html, "property", "og:description") ?? "Software Engineer | Flutter & .NET Backend",
    alt: meta(html, "property", "og:image:alt") ?? "Software portfolio",
    rtl: meta(html, "property", "og:locale") === "ar_AR",
    project: file.includes(`${resolve(publicOutput, "ar")}/`) || file.includes(`${resolve(publicOutput, "en")}/`) ? file.includes("/projects/") : false
  });
}

await rm(outputRoot, { recursive: true, force: true });
for (const [target, card] of cards) await render(card, resolve(publicOutput, target));
console.log(`Generated ${cards.size} Open Graph images.`);

async function render(card, path) {
  await mkdir(dirname(path), { recursive: true });
  const [altTitle, ...altContext] = card.alt.split(" — ");
  const title = truncate(altTitle || card.title, 46);
  const subtitleLines = wrap(card.subtitle, card.rtl ? 48 : 62, 2);
  const footer = truncate(altContext.join(" — ") || card.alt, 76);
  const label = card.project ? (card.rtl ? "مشروع مختار" : "SELECTED PROJECT") : (card.rtl ? "الملف المهني" : "SOFTWARE PORTFOLIO");
  const anchor = "start";
  const x = card.rtl ? 1080 : 120;
  const direction = card.rtl ? 'direction="rtl" unicode-bidi="plaintext"' : 'direction="ltr"';
  const titleSize = title.length > 34 ? 48 : title.length > 24 ? 58 : 72;
  const subtitle = subtitleLines.map((line, index) => `<text x="${x}" y="${395 + index * 48}" text-anchor="${anchor}" ${direction} fill="#526057" font-family="${card.rtl ? "Noto Sans Arabic" : "Manrope"}, Arial, sans-serif" font-size="30" font-weight="400">${display(line, card.rtl)}</text>`).join("");
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="1200" height="630" viewBox="0 0 1200 630">
    <rect width="1200" height="630" fill="#f6f5f0"/>
    <rect width="1200" height="18" fill="#245c42"/>
    <circle cx="${card.rtl ? 80 : 1120}" cy="95" r="150" fill="none" stroke="#245c42" stroke-opacity=".18" stroke-width="2"/>
    <text x="${x}" y="165" text-anchor="${anchor}" ${direction} fill="#245c42" font-family="${card.rtl ? "Noto Sans Arabic" : "Manrope"}, Arial, sans-serif" font-size="24" font-weight="700" letter-spacing="${card.rtl ? 0 : 3}">${xml(label)}</text>
    <text x="${x}" y="310" text-anchor="${anchor}" ${direction} fill="#17221d" font-family="${card.rtl ? "Noto Sans Arabic" : "Manrope"}, Arial, sans-serif" font-size="${titleSize}" font-weight="700">${display(title, card.rtl)}</text>
    ${subtitle}
    <line x1="120" x2="1080" y1="500" y2="500" stroke="#d5dbd2" stroke-width="2"/>
    <text x="${x}" y="558" text-anchor="${anchor}" ${direction} fill="#17221d" font-family="${card.rtl ? "Noto Sans Arabic" : "Manrope"}, Arial, sans-serif" font-size="23" font-weight="600">${display(footer, card.rtl)}</text>
  </svg>`;
  await sharp(Buffer.from(svg)).png({ compressionLevel: 9 }).toFile(path);
  const metadata = await sharp(path).metadata();
  if (metadata.width !== 1200 || metadata.height !== 630 || metadata.format !== "png") throw new Error(`Invalid Open Graph image: ${path}`);
}

function meta(html, attribute, value) {
  const match = html.match(new RegExp(`<meta[^>]*${attribute}="${value}"[^>]*content="([^"]*)"[^>]*>`, "i"));
  return match ? entities(match[1]) : undefined;
}

function entities(value) {
  return value.replaceAll("&amp;", "&").replaceAll("&quot;", '"').replaceAll("&#x27;", "'").replaceAll("&#39;", "'").replaceAll("&lt;", "<").replaceAll("&gt;", ">");
}

function truncate(value, maximum) {
  const characters = Array.from(value.trim());
  return characters.length <= maximum ? value.trim() : `${characters.slice(0, maximum - 1).join("")}…`;
}

function wrap(value, maximum, maximumLines) {
  const words = value.trim().split(/\s+/);
  const lines = [];
  for (const word of words) {
    const candidate = lines.length ? `${lines.at(-1)} ${word}` : word;
    if (!lines.length || Array.from(candidate).length > maximum) lines.push(word);
    else lines[lines.length - 1] = candidate;
  }
  if (lines.length <= maximumLines) return lines;
  return [...lines.slice(0, maximumLines - 1), truncate(lines.slice(maximumLines - 1).join(" "), maximum)];
}

function xml(value) {
  return value.replaceAll("&", "&amp;").replaceAll("<", "&lt;").replaceAll(">", "&gt;").replaceAll('"', "&quot;").replaceAll("'", "&apos;");
}

function display(value, rtl) {
  return xml(rtl ? value.replace(/(ASP\.NET|\.NET|Flutter|PostgreSQL)/g, "\u200e$1\u200e") : value);
}

async function htmlFiles(directory) {
  const entries = await readdir(directory, { withFileTypes: true });
  return (await Promise.all(entries.map(entry => entry.isDirectory() ? htmlFiles(resolve(directory, entry.name)) : entry.name.endsWith(".html") ? [resolve(directory, entry.name)] : []))).flat();
}
