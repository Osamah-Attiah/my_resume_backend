import { spawnSync } from "node:child_process";
import { resolve } from "node:path";

const root = process.cwd();
run("node_modules/typescript/bin/tsc", ["--noEmit", "-p", "packages/contracts/tsconfig.json"]);
run("node_modules/typescript/bin/tsc", ["-b", "apps/admin"]);
run("node_modules/vite/bin/vite.js", ["build"], "apps/admin");
run("node_modules/typescript/bin/tsc", ["--noEmit", "-p", "packages/resume-template/tsconfig.json"]);
run("node_modules/next/dist/bin/next", ["build"], "apps/public");
run("apps/public/scripts/generate-og-images.mjs", []);
run("apps/public/scripts/postprocess-static.mjs", []);
run("scripts/verify-static.mjs", []);

function run(entry, args, cwd = ".") {
  const result = spawnSync(process.execPath, [resolve(root, entry), ...args], { cwd: resolve(root, cwd), stdio: "inherit", env: process.env });
  if (result.error) throw result.error;
  if (result.status !== 0) process.exit(result.status ?? 1);
}
