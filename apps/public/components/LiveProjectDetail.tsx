"use client";

import { useEffect, useState } from "react";
import type { PublicProfile } from "@resume/contracts";
import { ProjectDetailPage, ProjectDetailState } from "./ProjectDetailPage";

export function LiveProjectDetail({ locale, profiles, defaultProfileSlug, counterpartSlugs, siteId, apiBaseUrl }: {
  locale: "ar" | "en";
  profiles: PublicProfile[];
  defaultProfileSlug: string;
  counterpartSlugs: string[];
  siteId?: string;
  apiBaseUrl: string;
}) {
  const [route, setRoute] = useState<{ profile: string; slug: string } | null>(null);
  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    setRoute({ profile: params.get("profile") ?? "", slug: params.get("slug") ?? "" });
  }, []);

  const profile = profiles.find(item => item.slug === route?.profile);
  if (!route) return <ProjectDetailState locale={locale} loading />;
  if (!profile || !route.slug) return <ProjectDetailState locale={locale} />;
  return <ProjectDetailPage profile={profile} projectSlug={route.slug} isDefault={profile.slug === defaultProfileSlug}
    hasCounterpart={counterpartSlugs.includes(profile.slug)} siteId={siteId} apiBaseUrl={apiBaseUrl} />;
}
