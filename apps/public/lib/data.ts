import { existsSync, readFileSync } from "node:fs";
import { resolve } from "node:path";
import { demoSnapshot as fixture, normalizePublicationSnapshot, type Locale, type PublicProfile, type PublicSiteSnapshot } from "@resume/contracts";
import { websiteSnapshot } from "./website-content";

export function isLocale(value: string): value is Locale { return value === "ar" || value === "en"; }
export const demoSnapshot: PublicSiteSnapshot = websiteSnapshot(loadSnapshot());
export function profileFor(locale: Locale) { return demoSnapshot.profiles[locale]; }
export function allProfiles() { return demoSnapshot.allProfiles ?? Object.values(demoSnapshot.profiles); }
export function profileBySlug(locale: Locale, slug: string): PublicProfile | undefined { return allProfiles().find(x => x.locale === locale && x.slug === slug); }
export function browserProfile(profile: PublicProfile): PublicProfile {
  const { pdfDocument: _pdfDocument, ...publicFields } = profile;
  return publicFields;
}

function loadSnapshot() {
  const path = process.env.PUBLIC_SNAPSHOT_PATH ?? resolve(process.cwd(), "../../artifacts/publication.json");
  if (!existsSync(path)) return fixture;
  return normalizePublicationSnapshot(JSON.parse(readFileSync(resolve(path), "utf8")));
}
