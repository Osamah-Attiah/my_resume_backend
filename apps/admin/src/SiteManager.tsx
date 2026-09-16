import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Check, Globe2, LoaderCircle, Pencil, Plus, Save, Trash2, X } from "lucide-react";
import { ApiError, api } from "./api";
import { localizeAdminError, localizeAdminMessage, useAdminI18n } from "./i18n";

type Translation = { locale: "ar" | "en"; title: string; description: string };
type SiteProfile = { profileId: string; pathSlug: string; isDefault: boolean; indexable: boolean; isListed: boolean; sortOrder: number };
type Site = { id: string; version: number; name: string; slug: string; baseUrl?: string; defaultLocale: "ar" | "en"; isPrimaryIdentitySite: boolean; deploymentTargetKey?: string; searchVerificationToken?: string; currentPublicationId?: string; translations: Translation[]; profiles: SiteProfile[]; locales: Array<{ locale: "ar" | "en" }> };
type Profile = { id: string; internalName: string; slug: string };
type Project = { id: string; slug: string; translations: Array<{ locale: "ar" | "en"; name: string }> };
type MediaAsset = { id: string; deliveryUrl?: string; status: string; translations: Array<{ locale: "ar" | "en"; altText: string }> };
type Publication = { id: string; revision: number; state: "queued" | "building" | "validating" | "deploying" | "succeeded" | "failed" | "cancelled"; attemptId: string; attemptNumber: number; requestedAt: string; completedAt?: string; snapshotHash: string; errorSummary?: string };

export function SiteManager() {
  const { l } = useAdminI18n();
  const client = useQueryClient(); const [editing, setEditing] = useState<string | "new" | null>(null);
  const sites = useQuery({ queryKey: ["sites"], queryFn: () => api<Site[]>("/api/v1/admin/sites") });
  const profiles = useQuery({ queryKey: ["profiles"], queryFn: () => api<Profile[]>("/api/v1/admin/profiles") });
  const publish = useMutation({ mutationFn: (id: string) => api<{ publicationId: string; attemptId: string; state: string; dispatchMode: "github" | "manual" | "existing" }>(`/api/v1/admin/sites/${id}/publish`, { method: "POST", body: JSON.stringify({ idempotencyKey: crypto.randomUUID() }) }), onSuccess: (_, id) => { void client.invalidateQueries({ queryKey: ["sites"] }); void client.invalidateQueries({ queryKey: ["site-publications", id] }); } });
  const editingSite = editing && editing !== "new" ? sites.data?.find(site => site.id === editing) : undefined;
  return <><header className="page-header"><div><h1>{l("المواقع والنشر", "Sites & publishing")}</h1><p>{l("كل موقع يملك لغاته ومساراته وSEO ونشره المستقل من snapshot ثابت.", "Each site has its own languages, paths, SEO, and publishing flow from a static snapshot.")}</p></div><button className="button button-primary" onClick={() => setEditing("new")}><Plus size={18} />{l("إضافة موقع", "Add site")}</button></header>
    {editing === "new" || editingSite ? <SiteEditor key={editing === "new" ? "new" : editingSite?.id} site={editing === "new" ? undefined : editingSite} profiles={profiles.data ?? []} close={() => setEditing(null)} saved={() => { setEditing(null); void client.invalidateQueries({ queryKey: ["sites"] }); }} /> : null}
    {publish.isSuccess && <div className="notice success-notice">{publish.data.dispatchMode === "github" ? l("تم إرسال سير العمل تلقائيًا.", "The workflow was dispatched automatically.") : publish.data.dispatchMode === "existing" ? l("طلب النشر موجود مسبقًا.", "The publish request already exists.") : l("لم تُضبط بيانات GitHub في API؛ شغّل سير العمل يدويًا.", "GitHub is not configured in the API; run the workflow manually.")} <code dir="ltr">{publish.data.publicationId}</code> <code dir="ltr">{publish.data.attemptId}</code>.</div>}{publish.isError && <ApiErrorNotice error={publish.error} />}
    {sites.isLoading ? <div className="loading-state">{l("جارٍ التحميل…", "Loading…")}</div> : sites.isError ? <div className="error-state">{l("تعذر تحميل المواقع.", "Unable to load sites.")}</div> : sites.data?.length ? <div className="profile-list">{sites.data.map(site => { const blockers = publishBlockers(site, l); return <article className="site-publication-card" key={site.id}><div className="row"><div><div className="row-title">{site.name}</div><div className="row-meta"><bdi>{site.baseUrl ?? `/${site.slug}`}</bdi> · {blockers.length ? l("غير مهيأ للاستضافة", "Hosting not ready") : l("مهيأ للاستضافة", "Ready for hosting")}</div><ol className="publish-steps"><li className="publish-step done"><Check size={16} />{l("بيانات محفوظة", "Saved data")}</li><li className={`publish-step ${blockers.length ? "blocked" : "current"}`}><LoaderCircle size={16} />{blockers.length ? l("بانتظار اكتمال إعداد الاستضافة", "Waiting for hosting setup") : l("نسخة ثابتة عند طلب النشر", "Static snapshot on publish")}</li><li className="publish-step">{l("آخر نشر ناجح: ", "Last successful publish: ")}{site.currentPublicationId ? l("متاح", "Available") : l("لا يوجد", "None")}</li></ol>{blockers.length > 0 && <div className="publish-readiness" role="status"><strong>{l("أكمل الإعداد قبل طلب النشر", "Complete setup before publishing")}</strong><ul>{blockers.map(blocker => <li key={blocker}>{blocker}</li>)}</ul></div>}</div><div className="row-actions"><button className="button button-secondary" onClick={() => setEditing(site.id)}>{l("الإعداد وSEO", "Settings & SEO")}</button><button className="button button-primary" disabled={publish.isPending || blockers.length > 0} onClick={() => publish.mutate(site.id)}>{blockers.length ? l("أكمل الإعداد", "Complete setup") : l("مراجعة ونشر", "Review & publish")}</button></div></div><SitePublicationHistory site={site} /></article>; })}</div> : <div className="empty-state"><div><Globe2 size={36} /><h2>{l("لا توجد مواقع", "No sites yet")}</h2><p>{l("إنشاء موقع منطقي لا ينشئ حساب استضافة أو نطاقًا تلقائيًا.", "Creating a site record does not automatically create hosting or a domain.")}</p></div></div>}
  </>;
}

const apiErrorMessages: Record<string, [string, string]> = {
  DEPLOYMENT_NOT_CONFIGURED: ["اسم مشروع Cloudflare Pages مطلوب قبل النشر.", "A Cloudflare Pages project name is required before publishing."],
  INVALID_DEPLOYMENT_TARGET: ["اسم مشروع Cloudflare Pages يجب أن يكون بحروف لاتينية صغيرة وأرقام وشرطات فقط.", "The Cloudflare Pages project name may contain lowercase Latin letters, numbers, and hyphens only."],
  BASE_URL_REQUIRED: ["أدخل نطاق الإنتاج الكامل بصيغة HTTPS؛ لا تستخدم localhost هنا.", "Enter the full production HTTPS URL; do not use localhost here."],
  HTTPS_REQUIRED: ["يجب أن يبدأ النطاق أو الرابط بصيغة HTTPS.", "The domain or URL must use HTTPS."],
  SITE_CONFIGURATION_INCOMPLETE: ["اربط بروفايلًا واحدًا على الأقل، واضبط لغة واحدة وبروفايلًا افتراضيًا واحدًا.", "Connect at least one profile and configure one locale and one default profile."],
  TRANSLATION_REQUIRED: ["أكمل بيانات البروفايل بالعربية والإنجليزية قبل النشر.", "Complete the profile in Arabic and English before publishing."],
  VALIDATION_FAILED: ["فشل التحقق من البيانات. راجع الحقول المطلوبة قبل النشر.", "Validation failed. Review the required fields before publishing."],
  VALIDATION_ERROR: ["فشل التحقق من البيانات. راجع الحقول المطلوبة قبل النشر.", "Validation failed. Review the required fields before publishing."],
  PUBLISH_ACTIVE: ["يوجد نشر قيد التنفيذ لهذا الموقع؛ حدّث الحالة أو انتظر انتهائه.", "A publish is already active for this site; refresh its status or wait for it to finish."],
  VERSION_CONFLICT: ["تغيّرت البيانات منذ فتح المحرر. حدّثنا النسخة الأحدث؛ راجعها ثم احفظ مرة أخرى.", "The data changed while the editor was open. The latest version is ready; review it and save again."]
};

function ApiErrorNotice({ error }: { error: unknown }) {
  const { l } = useAdminI18n();
  const apiError = error instanceof ApiError ? error : undefined;
  const message = apiError
    ? apiErrorMessages[apiError.code]
      ? l(...apiErrorMessages[apiError.code])
      : localizeAdminError(apiError, l)
    : l("تعذر إكمال الطلب.", "Unable to complete the request.");
  return <div className="notice" role="alert"><div>{message}</div>{apiError?.errors?.length ? <ul className="notice-details">{apiError.errors.map((item, index) => <li key={`${item.path}-${index}`}><bdi>{item.path}</bdi>: {localizeDetail(item.message, l)}</li>)}</ul> : null}</div>;
}

function localizeDetail(message: string, l: (ar: string, en: string) => string) {
  const known: Record<string, [string, string]> = {
    "Use lowercase Latin letters, numbers and hyphens.": ["استخدم حروفًا لاتينية صغيرة وأرقامًا وشرطات فقط.", "Use lowercase Latin letters, numbers, and hyphens."],
    "Project links must use HTTPS.": ["يجب أن تستخدم روابط المشروع HTTPS.", "Project links must use HTTPS."],
    "Arabic and English highlight text are required.": ["نص النقطة بالعربية والإنجليزية مطلوب.", "Arabic and English highlight text are required."],
    "Organization and job title are required in Arabic and English.": ["جهة العمل والمسمى الوظيفي مطلوبان بالعربية والإنجليزية.", "Organization and job title are required in Arabic and English."],
    "Headline and summary are required.": ["العنوان والملخص مطلوبان.", "Headline and summary are required."],
    "Institution and degree are required in Arabic and English.": ["المؤسسة والدرجة مطلوبتان بالعربية والإنجليزية.", "Institution and degree are required in Arabic and English."],
    "Name and issuer are required in Arabic and English.": ["اسم الشهادة والجهة المانحة مطلوبان بالعربية والإنجليزية.", "Certificate name and issuer are required in Arabic and English."],
    "At least one proficiency label is required.": ["أدخل مستوى الإتقان بالعربية أو الإنجليزية على الأقل.", "At least one proficiency label is required."],
    "Use a BCP 47 language code such as ar or en.": ["استخدم رمز لغة بصيغة BCP 47، مثل ar أو en.", "Use a BCP 47 language code such as ar or en."],
    "Production base URL must use HTTPS.": ["يجب أن يستخدم رابط الإنتاج HTTPS.", "The production base URL must use HTTPS."],
    "A production HTTPS base URL is required before publishing.": ["أدخل رابط إنتاج كاملًا بصيغة HTTPS قبل النشر.", "A production HTTPS base URL is required before publishing."],
    "Configure the site's deployment target before publishing.": ["اضبط اسم مشروع الاستضافة قبل النشر.", "Configure the site's deployment target before publishing."],
    "Use at most 58 lowercase Latin letters, numbers and hyphens.": ["استخدم حتى 58 حرفًا لاتينيًا صغيرًا أو رقمًا أو شرطة فقط.", "Use at most 58 lowercase Latin letters, numbers and hyphens."],
    "At least one locale, one profile, and exactly one default profile are required.": ["يجب تفعيل لغة واحدة وبروفايل واحد وبروفايل افتراضي واحد فقط على الأقل.", "At least one locale, one profile, and exactly one default profile are required."],
    "A publish is already active": ["يوجد نشر قيد التنفيذ حاليًا.", "A publish is already active."],
    "The selected page kind requires a matching profile/project target.": ["نوع الصفحة المختار يحتاج إلى بروفايل أو مشروع مطابق.", "The selected page kind requires a matching profile/project target."],
    "Canonical overrides must use HTTPS.": ["يجب أن تستخدم قيمة Canonical المخصصة HTTPS.", "Canonical overrides must use HTTPS."],
    "Use distinct internal paths and status 301 or 308.": ["استخدم مسارات مختلفة ورمز حالة 301 أو 308.", "Use distinct internal paths and status 301 or 308."],
    "The profile must be attached to this site.": ["يجب ربط البروفايل بهذا الموقع أولًا.", "The profile must be attached to this site."],
    "The project must be selected for this profile's website.": ["يجب اختيار المشروع لموقع هذا البروفايل.", "The project must be selected for this profile's website."],
    "Use Home, Profile, or Project.": ["استخدم الرئيسية أو البروفايل أو المشروع.", "Use Home, Profile, or Project."],
    "Enter only the Google verification token, not an HTML tag.": ["أدخل رمز تحقق Google فقط، وليس وسم HTML.", "Enter only the Google verification token, not an HTML tag."]
  };
  return known[message] ? l(...known[message]) : message;
}

function publishBlockers(site: Site, l: (ar: string, en: string) => string) {
  const blockers: string[] = [];
  if (!site.baseUrl) blockers.push(l("النطاق الكامل HTTPS غير مضبوط، مثل https://اسم-المشروع.pages.dev.", "The full HTTPS URL is not configured, for example https://project-name.pages.dev."));
  else if (!/^https:\/\//i.test(site.baseUrl)) blockers.push(l("النطاق يجب أن يستخدم HTTPS.", "The URL must use HTTPS."));
  if (!site.deploymentTargetKey) blockers.push(l("اسم مشروع Cloudflare Pages غير مضبوط.", "The Cloudflare Pages project name is not configured."));
  if (site.locales.length === 0) blockers.push(l("فعّل لغة واحدة على الأقل للموقع.", "Enable at least one site locale."));
  if (site.profiles.length === 0) blockers.push(l("اربط بروفايلًا واحدًا على الأقل بالموقع.", "Connect at least one profile to the site."));
  if (site.profiles.filter(profile => profile.isDefault).length !== 1) blockers.push(l("يجب تحديد بروفايل افتراضي واحد فقط.", "Exactly one default profile must be selected."));
  const arabic = site.translations.find(translation => translation.locale === "ar");
  const english = site.translations.find(translation => translation.locale === "en");
  if (!arabic?.title.trim() || !arabic.description.trim() || !english?.title.trim() || !english.description.trim()) {
    blockers.push(l("أكمل عنوان ووصف SEO بالعربية والإنجليزية قبل النشر.", "Complete the Arabic and English SEO title and description before publishing."));
  }
  return blockers;
}

function publicationStateLabel(state: Publication["state"], l: (ar: string, en: string) => string) {
  const labels: Record<Publication["state"], [string, string]> = {
    queued: ["في الانتظار", "Queued"], building: ["جارٍ البناء", "Building"], validating: ["جارٍ التحقق", "Validating"], deploying: ["جارٍ النشر", "Deploying"], succeeded: ["ناجح", "Succeeded"], failed: ["فشل", "Failed"], cancelled: ["أُلغي", "Cancelled"]
  };
  return l(...labels[state]);
}

function SitePublicationHistory({ site }: { site: Site }) {
  const { l } = useAdminI18n();
  const client = useQueryClient();
  const publications = useQuery({ queryKey: ["site-publications", site.id], queryFn: () => api<Publication[]>(`/api/v1/admin/sites/${site.id}/publications`) });
  const finish = () => { void client.invalidateQueries({ queryKey: ["site-publications", site.id] }); void client.invalidateQueries({ queryKey: ["sites"] }); };
  const retry = useMutation({ mutationFn: (id: string) => api(`/api/v1/admin/publications/${id}/retry`, { method: "POST" }), onSuccess: finish });
  const reconcile = useMutation({ mutationFn: (id: string) => api(`/api/v1/admin/publications/${id}/reconcile`, { method: "POST" }), onSuccess: finish });
  const rollback = useMutation({ mutationFn: (id: string) => api(`/api/v1/admin/sites/${site.id}/rollback`, { method: "POST", body: JSON.stringify({ sourcePublicationId: id, idempotencyKey: crypto.randomUUID() }) }), onSuccess: finish });
  if (publications.isLoading) return <div className="row-meta">{l("تحميل سجل النشر…", "Loading publish history…")}</div>;
  if (publications.isError) return <div className="error-state">{l("تعذر تحميل سجل النشر.", "Unable to load publish history.")}</div>;
  const items = publications.data?.slice(0, 5) ?? [];
  if (!items.length) return <div className="row-meta">{l("لا توجد محاولات نشر بعد.", "No publish attempts yet.")}</div>;
  return <div className="publication-history"><h3>{l("آخر عمليات النشر", "Recent publishing activity")}</h3>{items.map(item => {
    const active = ["queued", "building", "validating", "deploying"].includes(item.state);
    return <div className="publication-row" key={item.id}><div><b>{l("الإصدار", "Revision")} {item.revision}</b> <span className={`status-chip status-${item.state}`}>{publicationStateLabel(item.state, l)}</span><div className="row-meta"><code dir="ltr">{item.id}</code> · {l("محاولة", "Attempt")} {item.attemptNumber}</div>{item.errorSummary && <div className="row-meta">{localizeAdminMessage(item.errorSummary, l)}</div>}</div><div className="row-actions">{active && <button className="button button-secondary" onClick={() => reconcile.mutate(item.id)}>{l("مزامنة الحالة", "Reconcile")}</button>}{(item.state === "failed" || item.state === "cancelled") && <button className="button button-secondary" onClick={() => retry.mutate(item.id)}>{l("إعادة المحاولة", "Retry")}</button>}{item.state === "succeeded" && item.id !== site.currentPublicationId && <button className="button button-secondary" onClick={() => rollback.mutate(item.id)}>{l("الرجوع لهذه النسخة", "Roll back to this version")}</button>}</div></div>;
  })}{(retry.isError || reconcile.isError || rollback.isError) && <ApiErrorNotice error={retry.error ?? reconcile.error ?? rollback.error} />}</div>;
}

function SiteEditor({ site, profiles, close, saved }: { site?: Site; profiles: Profile[]; close: () => void; saved: () => void }) {
  const { l } = useAdminI18n();
  const client = useQueryClient();
  const ar = site?.translations.find(x => x.locale === "ar"), en = site?.translations.find(x => x.locale === "en");
  const [value, setValue] = useState({ name: site?.name ?? "", slug: site?.slug ?? "", baseUrl: site?.baseUrl ?? "", deploymentTargetKey: site?.deploymentTargetKey ?? "", searchVerificationToken: site?.searchVerificationToken ?? "", titleAr: ar?.title ?? "", descriptionAr: ar?.description ?? "", titleEn: en?.title ?? "", descriptionEn: en?.description ?? "", profileId: profiles[0]?.id ?? "" });
  const set = (key: keyof typeof value, next: string) => setValue(current => ({ ...current, [key]: next }));
  const mutation = useMutation({ mutationFn: async () => {
    if (site) return api<Site>(`/api/v1/admin/sites/${site.id}`, { method: "PUT", body: JSON.stringify(updateBody(site.version, value, site.isPrimaryIdentitySite)) });
    const created = await api<Site>("/api/v1/admin/sites", { method: "POST", body: JSON.stringify({ name: value.name, slug: value.slug, baseUrl: value.baseUrl || null, defaultLocale: "en", isPrimaryIdentitySite: false, locales: ["ar", "en"] }) });
    if (value.profileId) await api<void>(`/api/v1/admin/sites/${created.id}/profiles`, { method: "PUT", body: JSON.stringify({ profiles: [{ profileId: value.profileId, pathSlug: profiles.find(x => x.id === value.profileId)?.slug ?? "portfolio", isDefault: true, indexable: false, isListed: true, sortOrder: 0 }] }) });
    return api<Site>(`/api/v1/admin/sites/${created.id}`, { method: "PUT", body: JSON.stringify(updateBody(created.version, value, created.isPrimaryIdentitySite)) });
  }, onSuccess: saved, onError: error => { if (error instanceof ApiError && error.code === "VERSION_CONFLICT") void client.invalidateQueries({ queryKey: ["sites"] }); } });
  const pagesExample = value.deploymentTargetKey ? `https://${value.deploymentTargetKey}.pages.dev` : "https://project-name.pages.dev";
  return <section className="editor-panel" style={{ marginBottom: 28 }}><div className="toolbar"><h2>{site ? l("إعداد الموقع وSEO", "Site settings & SEO") : l("موقع جديد", "New site")}</h2><button className="icon-button" onClick={close} aria-label={l("إغلاق", "Close")}><X /></button></div>{mutation.isError && <ApiErrorNotice error={mutation.error} />}<div className="form-grid"><Field id="site-name" label={l("الاسم الداخلي", "Internal name")} value={value.name} set={next => set("name", next)} /><Field id="site-slug" label={l("المسار", "Slug")} dir="ltr" value={value.slug} set={next => set("slug", next)} /><div className="field"><label htmlFor="site-url">{l("النطاق الكامل HTTPS", "Full HTTPS URL")}</label><input id="site-url" dir="ltr" value={value.baseUrl} placeholder={pagesExample} onChange={event => set("baseUrl", event.target.value)} /><p className="field-help">{l("هذا هو رابط الإنتاج الذي سيظهر في canonical وملفات SEO؛ لا تستخدم رابط localhost.", "This production URL appears in the canonical URL and SEO files; do not use localhost.")}</p></div><Field id="site-target" label={l("اسم مشروع Cloudflare Pages", "Cloudflare Pages project name")} dir="ltr" value={value.deploymentTargetKey} set={next => set("deploymentTargetKey", next)} />{!site && <div className="field"><label htmlFor="site-profile">{l("البروفايل الافتراضي", "Default profile")}</label><select id="site-profile" value={value.profileId} onChange={event => set("profileId", event.target.value)}>{profiles.map(profile => <option key={profile.id} value={profile.id}>{profile.internalName}</option>)}</select></div>}<Field id="site-verification" label={l("رمز تحقق محرك البحث (اختياري)", "Search verification token (optional)")} dir="ltr" value={value.searchVerificationToken} set={next => set("searchVerificationToken", next)} /><Field id="site-title-ar" label={l("عنوان SEO بالعربية", "Arabic SEO title")} dir="rtl" value={value.titleAr} set={next => set("titleAr", next)} /><Field id="site-title-en" label={l("عنوان SEO بالإنجليزية", "English SEO title")} dir="ltr" value={value.titleEn} set={next => set("titleEn", next)} /><Field id="site-description-ar" label={l("وصف SEO بالعربية", "Arabic SEO description")} dir="rtl" value={value.descriptionAr} set={next => set("descriptionAr", next)} /><Field id="site-description-en" label={l("وصف SEO بالإنجليزية", "English SEO description")} dir="ltr" value={value.descriptionEn} set={next => set("descriptionEn", next)} /></div><button className="button button-primary" disabled={mutation.isPending} onClick={() => mutation.mutate()}><Save size={18} />{l("حفظ الإعداد", "Save settings")}</button>{site && <><SiteProfileManager site={site} profiles={profiles} /><SiteSeoManager site={site} profiles={profiles} /></>}</section>;
}

function SiteProfileManager({ site, profiles }: { site: Site; profiles: Profile[] }) {
  const { l } = useAdminI18n();
  const client = useQueryClient();
  const [items, setItems] = useState<SiteProfile[]>([...site.profiles].sort((a, b) => a.sortOrder - b.sortOrder));
  const available = profiles.filter(profile => !items.some(item => item.profileId === profile.id));
  const update = (index: number, change: Partial<SiteProfile>) => setItems(current => current.map((item, itemIndex) => itemIndex === index ? { ...item, ...change } : item));
  const makeDefault = (index: number) => setItems(current => current.map((item, itemIndex) => ({ ...item, isDefault: itemIndex === index })));
  const add = (profile: Profile) => setItems(current => [...current, { profileId: profile.id, pathSlug: profile.slug, isDefault: current.length === 0, indexable: false, isListed: true, sortOrder: current.length }]);
  const remove = (index: number) => setItems(current => { const next = current.filter((_, itemIndex) => itemIndex !== index); if (current[index]?.isDefault && next.length) next[0] = { ...next[0], isDefault: true }; return next.map((item, itemIndex) => ({ ...item, sortOrder: itemIndex })); });
  const move = (index: number, delta: number) => setItems(current => { const target = index + delta; if (target < 0 || target >= current.length) return current; const next = [...current]; [next[index], next[target]] = [next[target], next[index]]; return next.map((item, itemIndex) => ({ ...item, sortOrder: itemIndex })); });
  const mutation = useMutation({ mutationFn: () => api<void>(`/api/v1/admin/sites/${site.id}/profiles`, { method: "PUT", body: JSON.stringify({ profiles: items.map((item, sortOrder) => ({ ...item, sortOrder })) }) }), onSuccess: () => { void client.invalidateQueries({ queryKey: ["sites"] }); } });
  return <section className="nested-editor"><h3>{l("بروفايلات الموقع", "Site profiles")}</h3><p className="field-help">{l("اختر من البيانات المشتركة، واضبط مسارًا فريدًا لكل بروفايل. يجب أن يوجد بروفايل افتراضي واحد فقط.", "Choose from shared data and set a unique path for each profile. Exactly one default profile is required.")}</p>
    {items.length ? <div className="profile-list">{items.map((item, index) => { const profile = profiles.find(candidate => candidate.id === item.profileId); return <div className="site-profile-editor" key={item.profileId}><div className="site-profile-editor__heading"><strong>{profile?.internalName ?? item.profileId}</strong><div className="row-actions"><button className="icon-button" disabled={index === 0} onClick={() => move(index, -1)} aria-label={l("نقل لأعلى", "Move up")}>↑</button><button className="icon-button" disabled={index === items.length - 1} onClick={() => move(index, 1)} aria-label={l("نقل لأسفل", "Move down")}>↓</button><button className="icon-button danger-icon" disabled={items.length === 1} onClick={() => remove(index)} aria-label={l("إزالة البروفايل", "Remove profile")}><Trash2 /></button></div></div><div className="form-grid"><Field id={`site-profile-path-${item.profileId}`} label={l("مسار البروفايل", "Profile path")} dir="ltr" value={item.pathSlug} set={pathSlug => update(index, { pathSlug })} /><label className="checkbox-row"><input type="radio" name={`default-profile-${site.id}`} checked={item.isDefault} onChange={() => makeDefault(index)} />{l("البروفايل الافتراضي", "Default profile")}</label><label className="checkbox-row"><input type="checkbox" checked={item.indexable} onChange={event => update(index, { indexable: event.target.checked })} />{l("السماح بالفهرسة", "Allow indexing")}</label><label className="checkbox-row"><input type="checkbox" checked={item.isListed} onChange={event => update(index, { isListed: event.target.checked })} />{l("إظهاره في قائمة الموقع", "Show in site navigation")}</label></div></div>; })}</div> : <div className="empty-state">{l("أضف بروفايلًا واحدًا على الأقل.", "Add at least one profile.")}</div>}
    {available.length > 0 && <div className="field site-profile-add"><label htmlFor={`site-profile-add-${site.id}`}>{l("إضافة بروفايل", "Add profile")}</label><select id={`site-profile-add-${site.id}`} value="" onChange={event => { const profile = profiles.find(candidate => candidate.id === event.target.value); if (profile) add(profile); }}><option value="">{l("اختر…", "Choose…")}</option>{available.map(profile => <option key={profile.id} value={profile.id}>{profile.internalName}</option>)}</select></div>}
    {mutation.isError && <div className="notice" role="alert">{localizeAdminError(mutation.error, l)}</div>} {mutation.isSuccess && <div className="notice success-notice">{l("تم حفظ ربط البروفايلات.", "Profile connections saved.")}</div>}
    <button className="button button-secondary" disabled={mutation.isPending || items.length === 0} onClick={() => mutation.mutate()}><Save size={17} />{l("حفظ البروفايلات", "Save profiles")}</button>
  </section>;
}

type SeoPage = { id: string; version: number; locale: "ar" | "en"; pageKind: "Home" | "Profile" | "Project"; profileId?: string; projectId?: string; titleOverride?: string; descriptionOverride?: string; canonicalOverrideUrl?: string; ogAssetId?: string; indexOverride?: boolean };
type Redirect = { id: string; version: number; sourcePath: string; targetPath: string; statusCode: number; isActive: boolean };
type SeoData = { pages: SeoPage[]; redirects: Redirect[] };
type SeoDraft = { id: string; version: number; locale: "ar" | "en"; pageKind: SeoPage["pageKind"]; profileId: string; projectId: string; titleOverride: string; descriptionOverride: string; canonicalOverrideUrl: string; ogAssetId: string; indexMode: string };

function SiteSeoManager({ site, profiles }: { site: Site; profiles: Profile[] }) {
  const { l } = useAdminI18n();
  const client = useQueryClient();
  const query = useQuery({ queryKey: ["site-seo", site.id], queryFn: () => api<SeoData>(`/api/v1/admin/sites/${site.id}/seo`) });
  const projects = useQuery({ queryKey: ["projects"], queryFn: () => api<Project[]>("/api/v1/admin/projects") });
  const media = useQuery({ queryKey: ["media"], queryFn: () => api<MediaAsset[]>("/api/v1/admin/media") });
  const emptyPage: SeoDraft = { id: "", version: 0, locale: "ar", pageKind: "Home", profileId: "", projectId: "", titleOverride: "", descriptionOverride: "", canonicalOverrideUrl: "", ogAssetId: "", indexMode: "inherit" };
  const [page, setPage] = useState<SeoDraft>(emptyPage);
  const [redirect, setRedirect] = useState({ sourcePath: "", targetPath: "", statusCode: 301 });
  const refresh = () => void client.invalidateQueries({ queryKey: ["site-seo", site.id] });
  const savePage = useMutation({ mutationFn: () => api<SeoPage>(`/api/v1/admin/sites/${site.id}/seo/pages`, { method: "PUT", body: JSON.stringify({ id: page.id || null, version: page.version, locale: page.locale, pageKind: page.pageKind, profileId: page.pageKind === "Home" ? null : page.profileId || null, projectId: page.pageKind === "Project" ? page.projectId || null : null, titleOverride: page.titleOverride || null, descriptionOverride: page.descriptionOverride || null, canonicalOverrideUrl: page.canonicalOverrideUrl || null, ogAssetId: page.ogAssetId || null, indexOverride: page.indexMode === "inherit" ? null : page.indexMode === "index" }) }), onSuccess: () => { setPage(emptyPage); refresh(); } });
  const addRedirect = useMutation({ mutationFn: () => api<Redirect>(`/api/v1/admin/sites/${site.id}/redirects`, { method: "POST", body: JSON.stringify(redirect) }), onSuccess: () => { setRedirect({ sourcePath: "", targetPath: "", statusCode: 301 }); refresh(); } });
  const deleteRedirect = useMutation({ mutationFn: (id: string) => api<void>(`/api/v1/admin/sites/${site.id}/redirects/${id}`, { method: "DELETE" }), onSuccess: refresh });
  const editPage = (item: SeoPage) => setPage({ id: item.id, version: item.version, locale: item.locale, pageKind: item.pageKind, profileId: item.profileId ?? "", projectId: item.projectId ?? "", titleOverride: item.titleOverride ?? "", descriptionOverride: item.descriptionOverride ?? "", canonicalOverrideUrl: item.canonicalOverrideUrl ?? "", ogAssetId: item.ogAssetId ?? "", indexMode: item.indexOverride == null ? "inherit" : item.indexOverride ? "index" : "noindex" });
  const readyMedia = media.data?.filter(item => item.status === "Ready" && item.deliveryUrl) ?? [];
  const pageKindLabel = (kind: SeoPage["pageKind"]) => ({ Home: l("الرئيسية", "Home"), Profile: l("البروفايل", "Profile"), Project: l("المشروع", "Project") }[kind]);
  const indexLabel = (mode: string) => ({ inherit: l("موروث", "Inherited"), index: l("فهرسة", "Index"), noindex: l("منع الفهرسة", "Noindex") }[mode] ?? l("حالة فهرسة غير معروفة", "Unknown indexing state"));
  return <section className="nested-editor">
    <h3>{l("تخصيص صفحات SEO", "Customize SEO pages")}</h3><p className="field-help">{l("الأولوية لهذه القيم، ثم حقول البروفايل، ثم القيم المشتقة. منع الفهرسة الموروث لا يمكن تجاوزه.", "These values take priority, followed by profile fields and derived values. Inherited noindex cannot be overridden.")}</p>
    {query.isLoading ? <div className="loading-state">{l("جارٍ تحميل SEO…", "Loading SEO…")}</div> : query.isError ? <div className="notice" role="alert">{l("تعذر تحميل إعدادات SEO.", "Unable to load SEO settings.")}</div> : <>
      <div className="form-grid">
        <div className="field"><label htmlFor="seo-locale">{l("اللغة", "Language")}</label><select id="seo-locale" value={page.locale} onChange={event => setPage({ ...page, locale: event.target.value as "ar" | "en" })}><option value="ar">العربية</option><option value="en">English</option></select></div>
        <div className="field"><label htmlFor="seo-kind">{l("نوع الصفحة", "Page type")}</label><select id="seo-kind" value={page.pageKind} onChange={event => setPage({ ...page, pageKind: event.target.value as SeoPage["pageKind"] })}><option value="Home">{pageKindLabel("Home")}</option><option value="Profile">{pageKindLabel("Profile")}</option><option value="Project">{pageKindLabel("Project")}</option></select></div>
        {page.pageKind !== "Home" && <div className="field"><label htmlFor="seo-profile">{l("البروفايل", "Profile")}</label><select id="seo-profile" value={page.profileId} onChange={event => setPage({ ...page, profileId: event.target.value })}><option value="">{l("اختر…", "Choose…")}</option>{profiles.filter(p => site.profiles.some(sp => sp.profileId === p.id)).map(p => <option key={p.id} value={p.id}>{p.internalName}</option>)}</select></div>}
        {page.pageKind === "Project" && <div className="field"><label htmlFor="seo-project">{l("المشروع", "Project")}</label><select id="seo-project" value={page.projectId} onChange={event => setPage({ ...page, projectId: event.target.value })}><option value="">{l("اختر…", "Choose…")}</option>{projects.data?.map(project => <option key={project.id} value={project.id}>{project.translations.find(x => x.locale === page.locale)?.name ?? project.slug}</option>)}</select></div>}
        <Field id="seo-page-title" label={l("العنوان المخصص", "Custom title")} dir={page.locale === "ar" ? "rtl" : "ltr"} value={page.titleOverride} set={value => setPage({ ...page, titleOverride: value })} />
        <Field id="seo-page-description" label={l("الوصف المخصص", "Custom description")} dir={page.locale === "ar" ? "rtl" : "ltr"} value={page.descriptionOverride} set={value => setPage({ ...page, descriptionOverride: value })} />
        <Field id="seo-page-canonical" label={l("Canonical HTTPS (اختياري)", "Canonical HTTPS (optional)")} dir="ltr" value={page.canonicalOverrideUrl} set={value => setPage({ ...page, canonicalOverrideUrl: value })} />
        <div className="field"><label htmlFor="seo-og-image">{l("صورة المشاركة OG", "OG image")}</label><select id="seo-og-image" value={page.ogAssetId} onChange={event => setPage({ ...page, ogAssetId: event.target.value })}><option value="">{l("توليد تلقائي 1200×630", "Generate automatically 1200×630")}</option>{readyMedia.map(asset => <option key={asset.id} value={asset.id}>{asset.translations.find(value => value.locale === page.locale)?.altText ?? asset.id}</option>)}</select><span className="field-help">{l("يمكن اختيار صورة مرفوعة؛ وإلا يولّد النشر بطاقة ثنائية اللغة آليًا.", "Choose an uploaded image, or publishing will generate a bilingual card automatically.")}</span></div>
        <div className="field"><label htmlFor="seo-index">{l("الفهرسة", "Indexing")}</label><select id="seo-index" value={page.indexMode} onChange={event => setPage({ ...page, indexMode: event.target.value })}><option value="inherit">{indexLabel("inherit")}</option><option value="index">{indexLabel("index")}</option><option value="noindex">{indexLabel("noindex")}</option></select></div>
      </div>
      {savePage.isError && <div className="notice" role="alert">{localizeAdminError(savePage.error, l)}</div>}<button className="button button-secondary" disabled={savePage.isPending} onClick={() => savePage.mutate()}><Save size={17} />{l("حفظ تخصيص الصفحة", "Save page settings")}</button>
      <div className="profile-list seo-list">{query.data?.pages.map(item => <div className="row" key={item.id}><div><div className="row-title">{item.locale.toUpperCase()} · {pageKindLabel(item.pageKind)}</div><div className="row-meta">{item.titleOverride || l("عنوان موروث", "Inherited title")} · {item.ogAssetId ? l("صورة OG مخصصة", "Custom OG image") : l("صورة OG مولّدة", "Generated OG image")} · {indexLabel(item.indexOverride == null ? "inherit" : item.indexOverride ? "index" : "noindex")}</div></div><button className="icon-button" onClick={() => editPage(item)} aria-label={l("تعديل إعداد الصفحة", "Edit page settings")}><Pencil /></button></div>)}</div>
      <h3>{l("إعادة التوجيه", "Redirects")}</h3><div className="form-grid"><Field id="redirect-source" label={l("المسار القديم", "Old path")} dir="ltr" value={redirect.sourcePath} set={value => setRedirect({ ...redirect, sourcePath: value })} /><Field id="redirect-target" label={l("المسار الجديد", "New path")} dir="ltr" value={redirect.targetPath} set={value => setRedirect({ ...redirect, targetPath: value })} /></div>{addRedirect.isError && <div className="notice" role="alert">{localizeAdminError(addRedirect.error, l)}</div>}<button className="button button-secondary" disabled={addRedirect.isPending || !redirect.sourcePath || !redirect.targetPath} onClick={() => addRedirect.mutate()}><Plus size={17} />{l("إضافة تحويل 301", "Add 301 redirect")}</button><div className="profile-list seo-list">{query.data?.redirects.map(item => <div className="row" key={item.id}><div className="row-meta"><bdi>{item.sourcePath}</bdi> ← <bdi>{item.targetPath}</bdi> · {item.statusCode}</div><button className="icon-button danger-icon" onClick={() => deleteRedirect.mutate(item.id)} aria-label={l("حذف التحويل", "Delete redirect")}><Trash2 /></button></div>)}</div>
    </>}
  </section>;
}

function updateBody(version: number, value: { name: string; slug: string; baseUrl: string; deploymentTargetKey: string; searchVerificationToken: string; titleAr: string; descriptionAr: string; titleEn: string; descriptionEn: string }, isPrimaryIdentitySite: boolean) { return { version, name: value.name, slug: value.slug, baseUrl: value.baseUrl || null, defaultLocale: "en", isPrimaryIdentitySite, deploymentTargetKey: value.deploymentTargetKey || null, searchVerificationToken: value.searchVerificationToken || null, locales: ["ar", "en"], en: { title: value.titleEn, description: value.descriptionEn }, ar: { title: value.titleAr, description: value.descriptionAr } }; }
function Field({ id, label, value, set, dir, placeholder }: { id: string; label: string; value: string; set: (value: string) => void; dir?: "ltr" | "rtl"; placeholder?: string }) { return <div className="field"><label htmlFor={id}>{label}</label><input id={id} dir={dir} value={value} placeholder={placeholder} onChange={event => set(event.target.value)} /></div>; }
