import type { MetadataRoute } from "next";
import { allProfiles, demoSnapshot } from "../lib/data";

export const dynamic = "force-static";

export default function sitemap(): MetadataRoute.Sitemap {
  return allProfiles().flatMap(profile => {
    if (!profile.indexable) return [];
    const isDefault = demoSnapshot.profiles[profile.locale].slug === profile.slug;
    const pageUrl = isDefault ? `${demoSnapshot.baseUrl}/${profile.locale}/` : `${demoSnapshot.baseUrl}/${profile.locale}/p/${profile.slug}/`;
    return [{ url: pageUrl, lastModified: demoSnapshot.lastModified }, ...profile.projects.map(project => ({ url: `${demoSnapshot.baseUrl}/${profile.locale}/p/${profile.slug}/projects/${project.slug}/`, lastModified: demoSnapshot.lastModified }))];
  });
}
