import { spawnSync } from "node:child_process";
import { resolve } from "node:path";

const root = process.cwd();
for (const config of ["packages/contracts", "apps/admin", "apps/public", "packages/resume-template"]) {
  const result = spawnSync(process.execPath, [resolve(root, "node_modules/vitest/vitest.mjs"), "run", "--passWithNoTests", "--root", resolve(root, config)], { cwd: root, stdio: "inherit", env: process.env });
  if (result.error) throw result.error;
  if (result.status !== 0) process.exit(result.status ?? 1);
}
run("scripts/check-secrets.mjs");
run("scripts/verify-static.mjs");

function run(entry) {
  const result = spawnSync(process.execPath, [resolve(root, entry)], { cwd: root, stdio: "inherit", env: process.env });
  if (result.error) throw result.error;
  if (result.status !== 0) process.exit(result.status ?? 1);
}
