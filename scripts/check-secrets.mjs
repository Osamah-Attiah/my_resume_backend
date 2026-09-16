import { readdir, readFile } from "node:fs/promises";
import { extname, join, relative } from "node:path";

const root = process.cwd();
const ignored = new Set([".git", "node_modules", "bin", "obj", ".next", "dist", "out", "artifacts", "output", ".pdf-deps", ".venv"]);
const textExtensions = new Set([".cs", ".csproj", ".json", ".ts", ".tsx", ".js", ".mjs", ".md", ".yml", ".yaml", ".toml", ".example", ""]);
const findings = [];
await walk(root);
if (findings.length) { console.error(`Potential secret material found:\n${findings.join("\n")}`); process.exit(1); }
console.log("Secret and private-key source scan passed.");

async function walk(dir) {
  for (const entry of await readdir(dir, { withFileTypes: true })) {
    if (ignored.has(entry.name)) continue;
    const path = join(dir, entry.name); const rel = relative(root, path);
    if (rel === "scripts/check-secrets.mjs") continue;
    if (entry.isDirectory()) { await walk(path); continue; }
    if (!textExtensions.has(extname(entry.name)) && entry.name !== ".env.example" && entry.name !== ".gitignore") continue;
    const value = await readFile(path, "utf8").catch(() => "");
    if (/-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----|\bgh[opusr]_[A-Za-z0-9]{30,}\b|\bAKIA[0-9A-Z]{16}\b|\bsk_live_[A-Za-z0-9]{20,}\b/.test(value)) findings.push(rel);
  }
}
