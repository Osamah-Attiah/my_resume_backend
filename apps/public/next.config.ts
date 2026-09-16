import type { NextConfig } from "next";
import { resolve } from "node:path";

const config: NextConfig = {
  // Static export is required for the deployed public site. In development,
  // keep Next's server fallback so an old/unknown profile URL renders a normal
  // 404 instead of throwing the export-only missing-static-param error.
  output: process.env.NODE_ENV === "development" ? undefined : "export",
  trailingSlash: true,
  images: { unoptimized: true },
  turbopack: { root: resolve(process.cwd(), "../..") }
};

export default config;
