import { createHash } from "node:crypto";
import { readFile, stat, writeFile } from "node:fs/promises";
import { resolve } from "node:path";

const root = process.cwd();
const publicationId = required("PUBLICATION_ID");
const attemptId = required("ATTEMPT_ID");
const deploymentUrl = required("DEPLOYMENT_URL");
const providerDeploymentId = required("PROVIDER_DEPLOYMENT_ID");
const snapshot = JSON.parse(await readFile(resolve(root, "artifacts/publication.json"), "utf8"));
const generated = JSON.parse(await readFile(resolve(root, "artifacts/pdf-qa/manifest.json"), "utf8"));
const pdfArtifacts = [];

for (const item of generated) {
  const profile = snapshot.profiles.find(candidate => candidate.slug === item.slug && candidate.locale === item.locale);
  if (!profile?.profileId) throw new Error(`Snapshot profile identity missing for ${item.locale}/${item.slug}`);
  const path = resolve(item.artifactDirectory, "resume.pdf");
  const data = await readFile(path);
  const qa = JSON.parse(await readFile(resolve(item.artifactDirectory, "qa-report.json"), "utf8"));
  if (!qa.passed) throw new Error(`PDF QA did not pass for ${item.locale}/${item.slug}`);
  pdfArtifacts.push({ profileId: profile.profileId, locale: item.locale, kind: "pdf", templateVersion: profile.pdfDocument?.pdf?.templateKey ?? "ats-classic-v1", pathOrArtifactId: new URL(`/resumes/${item.slug}/${item.locale}/resume.pdf`, snapshot.baseUrl).href, sha256: sha(data), sizeBytes: (await stat(path)).size, pageCount: qa.pageCount, qaReport: qa, visibility: "public" });
}

const evidence = { schemaVersion: 1, publicationId, attemptId, snapshotHash: sha(await readFile(resolve(root, "artifacts/publication.json"))), generatedAt: new Date().toISOString(), pdfs: pdfArtifacts.map(({ qaReport, ...artifact }) => ({ ...artifact, qaPassed: qaReport.passed })) };
const evidenceData = Buffer.from(JSON.stringify(evidence, null, 2));
await writeFile(resolve(root, "artifacts/publication-artifact-manifest.json"), evidenceData);
const artifactName = `public-site-evidence-${publicationId}`;
const result = { state: "succeeded", artifacts: [...pdfArtifacts, { profileId: null, locale: null, kind: "manifest", templateVersion: "site-publication-v1", pathOrArtifactId: artifactName, sha256: sha(evidenceData), sizeBytes: evidenceData.length, pageCount: null, qaReport: { pdfCount: pdfArtifacts.length, allPassed: true }, visibility: "public" }], deployment: { providerDeploymentId, deploymentUrl, state: "succeeded", deployedAt: new Date().toISOString() } };
await writeFile(resolve(root, "artifacts/publication-result.json"), JSON.stringify(result));

function sha(data) { return createHash("sha256").update(data).digest("hex"); }
function required(name) { const value = process.env[name]; if (!value) throw new Error(`${name} is required`); return value; }
