import type { PublicProfile, PublicProject, PublicSiteSnapshot } from "@resume/contracts";

// Apply the same website selection to the saved snapshot and live API data.
// PDF generation uses the original publication snapshot independently.
export function websiteProjects(projects: PublicProject[]) {
  return projects.filter(project => project.kind !== "Freelance");
}

function websiteProfile(profile: PublicProfile): PublicProfile {
  const projects = websiteProjects(profile.projects);
  const slugs = new Set(projects.map(project => project.slug));
  return {
    ...profile,
    projects,
    story: profile.story?.filter(scene => !scene.projectSlug || slugs.has(scene.projectSlug)),
    projectSeo: profile.projectSeo && Object.fromEntries(Object.entries(profile.projectSeo).filter(([slug]) => slugs.has(slug)))
  };
}

export function websiteSnapshot(snapshot: PublicSiteSnapshot): PublicSiteSnapshot {
  return {
    ...snapshot,
    profiles: { ar: websiteProfile(snapshot.profiles.ar), en: websiteProfile(snapshot.profiles.en) },
    allProfiles: snapshot.allProfiles?.map(websiteProfile)
  };
}
