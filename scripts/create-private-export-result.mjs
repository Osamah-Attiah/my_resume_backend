import { createHash } from "node:crypto";
import { readFile, stat, writeFile } from "node:fs/promises";
import { resolve } from "node:path";

const root = process.cwd();
const artifactId = required("GITHUB_ARTIFACT_ID");
const snapshot = JSON.parse(await readFile(resolve(root, "artifacts/publication.json"), "utf8"));
const generated = JSON.parse(await readFile(resolve(root, "artifacts/pdf-qa/manifest.json"), "utf8"));
const artifacts = [];
for (const item of generated) {
  const profile = snapshot.profiles.find(candidate => candidate.slug === item.slug && candidate.locale === item.locale);
  if (!profile?.profileId) throw new Error(`Snapshot profile identity missing for ${item.locale}/${item.slug}`);
  const path = resolve(item.artifactDirectory, "resume.pdf"); const data = await readFile(path);
  const qa = JSON.parse(await readFile(resolve(item.artifactDirectory, "qa-report.json"), "utf8"));
  if (!qa.passed) throw new Error(`PDF QA did not pass for ${item.locale}/${item.slug}`);
  artifacts.push({ profileId: profile.profileId, locale: item.locale, kind: "pdf", templateVersion: profile.pdfDocument?.pdf?.templateKey ?? "ats-classic-v1", pathOrArtifactId: artifactId, sha256: createHash("sha256").update(data).digest("hex"), sizeBytes: (await stat(path)).size, pageCount: qa.pageCount, qaReport: qa, visibility: "private" });
}
await writeFile(resolve(root, "artifacts/private-export-result.json"), JSON.stringify({ state: "succeeded", artifacts }));
function required(name) { const value = process.env[name]; if (!value) throw new Error(`${name} is required`); return value; }
