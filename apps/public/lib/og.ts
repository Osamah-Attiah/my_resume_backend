import type { PublicProfile } from "@resume/contracts";

export function profileOgPath(profile: PublicProfile) {
  return `/og/${profile.locale}/${profile.slug}.png`;
}

export function projectOgPath(profile: PublicProfile, projectSlug: string) {
  return `/og/${profile.locale}/${profile.slug}/${projectSlug}.png`;
}

export function absoluteAsset(baseUrl: string, path: string) {
  return `${baseUrl.replace(/\/$/, "")}${path}`;
}
