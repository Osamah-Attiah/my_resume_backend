import { copyFile, mkdir, readFile, rm } from "node:fs/promises";
import { existsSync } from "node:fs";
import { spawnSync } from "node:child_process";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const root = resolve(here, "../../..");
const artifactRoot = resolve(root, "artifacts/pdf-qa");
const deliveryRoot = resolve(root, "output/pdf");
const publicResumeRoot = resolve(root, "apps/public/public/resumes");
const python = process.env.PDF_PYTHON_BIN ?? (existsSync(resolve(root, ".venv/bin/python3")) ? resolve(root, ".venv/bin/python3") : "python3");

// All three directories contain generated output only. Removing them prevents a
// profile or locale from an older immutable snapshot leaking into a later build.
await Promise.all([
  rm(artifactRoot, { recursive: true, force: true }),
  rm(deliveryRoot, { recursive: true, force: true }),
  rm(publicResumeRoot, { recursive: true, force: true })
]);

run(process.execPath, [resolve(root, "node_modules/tsx/dist/cli.mjs"), resolve(here, "generate-pdfs.ts")]);
run("dotnet", ["build", resolve(root, "tools/resume.PdfCli/resume.PdfCli.csproj"), "--maxcpucount:1", "--verbosity:quiet", "-p:UseSharedCompilation=false"]);

await mkdir(deliveryRoot, { recursive: true });
const manifest = JSON.parse(await readFile(resolve(artifactRoot, "manifest.json"), "utf8"));
for (const item of manifest) {
  const localeRoot = item.artifactDirectory;
  const raw = resolve(localeRoot, "resume-raw.pdf");
  const final = resolve(localeRoot, "resume.pdf");
  run("dotnet", ["run", "--project", resolve(root, "tools/resume.PdfCli/resume.PdfCli.csproj"), "--no-build", "--", resolve(localeRoot, "resume.json"), raw]);
  run(python, [resolve(here, "normalize_actual_text.py"), raw, final]);
  await copyFile(final, resolve(item.publicDirectory, "resume.pdf"));
  if (item.isDefault) await copyFile(final, resolve(deliveryRoot, item.isDemo ? `resume-${item.locale}-demo.pdf` : `resume-${item.locale}.pdf`));
}

function run(command, args) {
  const result = spawnSync(command, args, { cwd: root, env: process.env, stdio: "inherit" });
  if (result.error) throw result.error;
  if (result.status !== 0) process.exit(result.status ?? 1);
}
