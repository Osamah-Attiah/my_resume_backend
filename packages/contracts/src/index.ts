export type Locale = "ar" | "en";

export interface ResumeDocument {
  locale: Locale;
  direction: "rtl" | "ltr";
  fullName: string;
  location?: string;
  headline: string;
  summary: string;
  email?: string;
  phone?: string;
  links: ResumeLink[];
  skills: ResumeSkill[];
  projects: ResumeProject[];
  experiences: ResumeExperience[];
  educations: ResumeEducation[];
  certifications: ResumeCertification[];
  languages: ResumeLanguage[];
  sections?: string[];
  pdf: { paperSize: "A4" | "Letter"; fontSize: number; marginMm: number; targetPages: number };
}

export interface ResumeLink { kind: string; label: string; url: string }
export interface ResumeSkill { category: string; name: string }
export interface ResumeMedia { src: string; width: number; height: number; alt: string; caption?: string }
export interface ResumeProject { slug: string; name: string; role?: string; summary: string; description?: string; repositoryUrl?: string; demoUrl?: string; highlights: string[]; skills: string[]; cover?: ResumeMedia; media?: ResumeMedia[] }
export interface ResumeExperience { organization: string; jobTitle: string; location?: string; startDate: string; endDate?: string; summary?: string; highlights: string[] }
export interface ResumeEducation { institution: string; degree: string; fieldOfStudy?: string; location?: string; startDate?: string; endDate?: string; notes?: string }
export interface ResumeCertification { name: string; issuer: string; issuedOn?: string; expiresOn?: string; credentialUrl?: string }
export interface ResumeLanguage { languageCode: string; proficiency: string }
export interface PublicProject extends ResumeProject { kind: "Personal" | "OpenSource" | "Freelance" | "Employment" }
export interface PublicProfile extends Omit<ResumeDocument, "projects"> { slug: string; indexable: boolean; listed?: boolean; demo?: boolean; projects: PublicProject[]; pdfDocument?: ResumeDocument; seo: { title: string; description: string; canonical?: string; ogImage?: ResumeMedia }; projectSeo?: Record<string, { title?: string; description?: string; canonical?: string; ogImage?: ResumeMedia; indexable: boolean }> }
export interface PublicSiteSnapshot { schemaVersion: 1; baseUrl: string; lastModified: string; searchVerificationToken?: string; profiles: Record<Locale, PublicProfile>; allProfiles?: PublicProfile[]; redirects: Array<{ source: string; target: string; status: 301 | 308 }> }

export { demoSnapshot } from "./fixture";
export { normalizePublicationSnapshot } from "./publication";
