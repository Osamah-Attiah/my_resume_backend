import type { MetadataRoute } from "next";
import { demoSnapshot } from "../lib/data";

export const dynamic = "force-static";

export default function robots(): MetadataRoute.Robots {
  return { rules: [{ userAgent: "*", allow: "/", disallow: ["/admin/", "/preview/"] }], sitemap: `${demoSnapshot.baseUrl}/sitemap.xml` };
}
