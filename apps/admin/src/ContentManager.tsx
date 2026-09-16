import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Save, X } from "lucide-react";
import { api } from "./api";
import { HighlightEditor, type Highlight } from "./HighlightEditor";
import { localizeAdminError, useAdminI18n } from "./i18n";

type Kind = "skills" | "experiences" | "educations" | "certifications" | "languages";
type Item = {
  id: string;
  version: number;
  canonicalName?: string;
  category?: string;
  categoryAr?: string;
  categoryEn?: string;
  languageCode?: string;
  proficiency?: string;
  proficiencyAr?: string;
  proficiencyEn?: string;
  employmentType?: string;
  startDate?: string;
  endDate?: string;
  issuedOn?: string;
  expiresOn?: string;
  isCurrent?: boolean;
  organizationUrl?: string;
  credentialId?: string;
  credentialUrl?: string;
  translations?: Array<Record<string, string>>;
  highlights?: Highlight[];
};

const sections: Array<{ key: Kind; ar: string; en: string; helpAr: string; helpEn: string }> = [
  { key: "skills", ar: "المهارات", en: "Skills", helpAr: "اسم تقني، وتصنيف، وترجمة عرض عربية وإنجليزية.", helpEn: "A technical name, localized category, and Arabic and English display names." },
  { key: "experiences", ar: "الخبرة الوظيفية (اختيارية)", en: "Employment history (optional)", helpAr: "أضف وظيفة حقيقية فقط؛ لا تحوّل مشروعًا شخصيًا إلى خبرة.", helpEn: "Add real employment only; never present a personal project as employment." },
  { key: "educations", ar: "التعليم", en: "Education", helpAr: "المؤسسة والدرجة بالعربية والإنجليزية.", helpEn: "Institution and degree in Arabic and English." },
  { key: "certifications", ar: "الشهادات", en: "Certifications", helpAr: "شهادة حقيقية ورابط اعتماد HTTPS اختياري.", helpEn: "A real certification and an optional HTTPS credential URL." },
  { key: "languages", ar: "اللغات", en: "Languages", helpAr: "رمز اللغة ومستوى الإتقان بالعربية والإنجليزية.", helpEn: "Language code and proficiency in Arabic and English." }
];

const itemCount = (value: number, isArabic: boolean) => isArabic
  ? `${value} ${value === 1 ? "عنصر" : value === 2 ? "عنصران" : value >= 3 && value <= 10 ? "عناصر" : "عنصرًا"}`
  : `${value} ${value === 1 ? "item" : "items"}`;

export function ContentManager() {
  const { l, isArabic } = useAdminI18n();
  const client = useQueryClient();
  const [active, setActive] = useState<Kind | null>(null);
  const query = useQuery({ queryKey: ["content"], queryFn: async () => Object.fromEntries(await Promise.all(sections.map(async section => [section.key, await api<Item[]>(`/api/v1/admin/${section.key}`)]))) as Record<Kind, Item[]> });
  const archive = useMutation({ mutationFn: ({ kind, id }: { kind: Kind; id: string }) => api<void>(`/api/v1/admin/${kind}/${id}`, { method: "DELETE" }), onSuccess: () => void client.invalidateQueries({ queryKey: ["content"] }) });
  return <>
    <header className="page-header"><div><h1>{l("المحتوى المهني", "Professional content")}</h1><p>{l("حقائق مشتركة تُنتقى داخل البروفايلات، مع ترجمة مستقلة لكل لغة.", "Shared facts selected by profiles, with an independent translation for each language.")}</p></div>{active && <button className="button button-primary" onClick={() => setActive(null)}><X size={18} />{l("إغلاق الإدارة", "Close editor")}</button>}</header>
    {query.isLoading ? <div className="loading-state" role="status">{l("جارٍ التحميل…", "Loading…")}</div> : query.isError ? <div className="error-state" role="alert">{l("تعذر تحميل المحتوى.", "Unable to load content.")}</div> : <div className="profile-list">{sections.map(section => <section className="row" key={section.key}><div><div className="row-title">{l(section.ar, section.en)}</div><div className="row-meta">{itemCount(query.data?.[section.key].length ?? 0, isArabic)} · {l(section.helpAr, section.helpEn)}</div></div><div className="row-actions"><button className="button button-secondary" aria-expanded={active === section.key} onClick={() => setActive(active === section.key ? null : section.key)}>{l("إدارة", "Manage")}</button></div></section>)}</div>}
    {active && <Editor kind={active} items={query.data?.[active] ?? []} created={() => void client.invalidateQueries({ queryKey: ["content"] })} archive={id => archive.mutate({ kind: active, id })} />}
    <div className="notice" style={{ marginTop: 28 }}>{l("غياب الخبرة الوظيفية حالة صحيحة؛ سيُخفى القسم بالكامل من الموقع والسيرة بدل عرض بيانات وهمية.", "No employment history is valid; the section stays hidden from the site and resume instead of showing invented data.")}</div>
  </>;
}

function Editor({ kind, items, created, archive }: { kind: Kind; items: Item[]; created: () => void; archive: (id: string) => void }) {
  const { l } = useAdminI18n();
  const [value, setValue] = useState<Record<string, string | boolean>>({ current: true });
  const [editing, setEditing] = useState<Item | null>(null);
  const section = sections.find(item => item.key === kind)!;
  const save = useMutation({ mutationFn: (body: unknown) => api<Item>(`/api/v1/admin/${kind}${editing ? `/${editing.id}` : ""}`, { method: editing ? "PUT" : "POST", body: JSON.stringify(body) }), onSuccess: () => { setValue({ current: true }); setEditing(null); created(); } });
  const set = (key: string, next: string | boolean) => setValue(current => ({ ...current, [key]: next }));
  const commonTranslations = {
    en: { organization: value.organizationEn, jobTitle: value.jobTitleEn, location: value.locationEn || null, summary: value.summaryEn || null, institution: value.institutionEn, degree: value.degreeEn, fieldOfStudy: value.fieldOfStudyEn || null, notes: value.notesEn || null, name: value.nameEn, issuer: value.issuerEn },
    ar: { organization: value.organizationAr, jobTitle: value.jobTitleAr, location: value.locationAr || null, summary: value.summaryAr || null, institution: value.institutionAr, degree: value.degreeAr, fieldOfStudy: value.fieldOfStudyAr || null, notes: value.notesAr || null, name: value.nameAr, issuer: value.issuerAr }
  };
  const submit = () => {
    const version = editing?.version ?? 0;
    if (kind === "skills") save.mutate({ version, canonicalName: value.canonicalName, category: value.categoryEn || value.categoryAr || "", categoryEn: value.categoryEn, categoryAr: value.categoryAr, displayNameEn: value.displayNameEn, displayNameAr: value.displayNameAr });
    if (kind === "languages") save.mutate({ version, languageCode: value.languageCode, proficiency: value.proficiencyEn || value.proficiencyAr || "", proficiencyEn: value.proficiencyEn, proficiencyAr: value.proficiencyAr });
    if (kind === "experiences") save.mutate({ version, employmentType: value.employmentType || "full-time", startDate: value.startDate, endDate: value.current ? null : value.endDate || null, isCurrent: !!value.current, organizationUrl: value.url || null, en: commonTranslations.en, ar: commonTranslations.ar });
    if (kind === "educations") save.mutate({ version, startDate: value.startDate || null, endDate: value.current ? null : value.endDate || null, isCurrent: !!value.current, en: commonTranslations.en, ar: commonTranslations.ar });
    if (kind === "certifications") save.mutate({ version, issuedOn: value.startDate || null, expiresOn: value.endDate || null, credentialId: value.credentialId || null, credentialUrl: value.url || null, en: commonTranslations.en, ar: commonTranslations.ar });
  };
  const edit = (item: Item) => { setEditing(item); setValue(toValue(kind, item)); };
  return <section className="editor-panel" style={{ marginTop: 28 }} aria-label={l("إدارة المحتوى", "Content editor")}><div className="toolbar"><h2>{editing ? l("تعديل", "Edit") : l("إضافة", "Add")} {l(section.ar, section.en)}</h2>{editing && <button className="button button-secondary" onClick={() => { setEditing(null); setValue({ current: true }); }}><X size={17} />{l("إلغاء التعديل", "Cancel edit")}</button>}</div>{save.isError && <div className="notice" role="alert">{localizeAdminError(save.error, l)}</div>}<div className="form-grid">
    {kind === "skills" && <><Field id="canonicalName" label={l("الاسم التقني", "Technical name")} dir="ltr" value={value.canonicalName} set={set} /><Field id="categoryEn" label={l("التصنيف بالإنجليزية", "English category")} dir="ltr" value={value.categoryEn} set={set} /><Field id="categoryAr" label={l("التصنيف بالعربية", "Arabic category")} dir="rtl" value={value.categoryAr} set={set} /><Field id="displayNameAr" label={l("اسم العرض بالعربية", "Arabic display name")} dir="rtl" value={value.displayNameAr} set={set} /><Field id="displayNameEn" label={l("اسم العرض بالإنجليزية", "English display name")} dir="ltr" value={value.displayNameEn} set={set} /></>}
    {kind === "languages" && <><Field id="languageCode" label={l("رمز اللغة", "Language code")} dir="ltr" value={value.languageCode} set={set} /><Field id="proficiencyAr" label={l("مستوى الإتقان بالعربية", "Arabic proficiency")} dir="rtl" value={value.proficiencyAr} set={set} /><Field id="proficiencyEn" label={l("مستوى الإتقان بالإنجليزية", "English proficiency")} dir="ltr" value={value.proficiencyEn} set={set} /></>}
    {kind === "experiences" && <><Field id="organizationAr" label={l("جهة العمل بالعربية", "Arabic organization")} dir="rtl" value={value.organizationAr} set={set} /><Field id="organizationEn" label={l("جهة العمل بالإنجليزية", "English organization")} dir="ltr" value={value.organizationEn} set={set} /><Field id="jobTitleAr" label={l("المسمى بالعربية", "Arabic job title")} dir="rtl" value={value.jobTitleAr} set={set} /><Field id="jobTitleEn" label={l("المسمى بالإنجليزية", "English job title")} dir="ltr" value={value.jobTitleEn} set={set} /><Field id="locationAr" label={l("الموقع بالعربية", "Arabic location")} dir="rtl" value={value.locationAr} set={set} /><Field id="locationEn" label={l("الموقع بالإنجليزية", "English location")} dir="ltr" value={value.locationEn} set={set} /><Field id="summaryAr" label={l("الملخص بالعربية", "Arabic summary")} dir="rtl" value={value.summaryAr} set={set} /><Field id="summaryEn" label={l("الملخص بالإنجليزية", "English summary")} dir="ltr" value={value.summaryEn} set={set} /><Field id="employmentType" label={l("نوع التوظيف", "Employment type")} dir="ltr" value={value.employmentType} set={set} /><Field id="url" label={l("رابط الجهة (HTTPS)", "Organization URL (HTTPS)")} dir="ltr" value={value.url} set={set} /></>}
    {kind === "educations" && <><Field id="institutionAr" label={l("المؤسسة بالعربية", "Arabic institution")} dir="rtl" value={value.institutionAr} set={set} /><Field id="institutionEn" label={l("المؤسسة بالإنجليزية", "English institution")} dir="ltr" value={value.institutionEn} set={set} /><Field id="degreeAr" label={l("الدرجة بالعربية", "Arabic degree")} dir="rtl" value={value.degreeAr} set={set} /><Field id="degreeEn" label={l("الدرجة بالإنجليزية", "English degree")} dir="ltr" value={value.degreeEn} set={set} /><Field id="fieldOfStudyAr" label={l("التخصص بالعربية", "Arabic field of study")} dir="rtl" value={value.fieldOfStudyAr} set={set} /><Field id="fieldOfStudyEn" label={l("التخصص بالإنجليزية", "English field of study")} dir="ltr" value={value.fieldOfStudyEn} set={set} /><Field id="locationAr" label={l("الموقع بالعربية", "Arabic location")} dir="rtl" value={value.locationAr} set={set} /><Field id="locationEn" label={l("الموقع بالإنجليزية", "English location")} dir="ltr" value={value.locationEn} set={set} /><Field id="notesAr" label={l("ملاحظات بالعربية", "Arabic notes")} dir="rtl" value={value.notesAr} set={set} /><Field id="notesEn" label={l("ملاحظات بالإنجليزية", "English notes")} dir="ltr" value={value.notesEn} set={set} /></>}
    {kind === "certifications" && <><Field id="nameAr" label={l("اسم الشهادة بالعربية", "Arabic certificate name")} dir="rtl" value={value.nameAr} set={set} /><Field id="nameEn" label={l("اسم الشهادة بالإنجليزية", "English certificate name")} dir="ltr" value={value.nameEn} set={set} /><Field id="issuerAr" label={l("الجهة بالعربية", "Arabic issuer")} dir="rtl" value={value.issuerAr} set={set} /><Field id="issuerEn" label={l("الجهة بالإنجليزية", "English issuer")} dir="ltr" value={value.issuerEn} set={set} /><Field id="credentialId" label={l("رقم الاعتماد", "Credential ID")} dir="ltr" value={value.credentialId} set={set} /><Field id="url" label={l("رابط الاعتماد (HTTPS)", "Credential URL (HTTPS)")} dir="ltr" value={value.url} set={set} /></>}
    {(kind === "experiences" || kind === "educations" || kind === "certifications") && <><Field id="startDate" label={kind === "certifications" ? l("تاريخ الإصدار", "Issue date") : l("تاريخ البدء", "Start date")} type="date" value={value.startDate} set={set} /><Field id="endDate" label={kind === "certifications" ? l("تاريخ الانتهاء", "Expiry date") : l("تاريخ النهاية", "End date")} type="date" value={value.endDate} set={set} />{kind !== "certifications" && <label className="checkbox-row"><input type="checkbox" checked={!!value.current} onChange={event => set("current", event.target.checked)} />{l("مستمر حاليًا", "Currently ongoing")}</label>}</>}
  </div><div className="toolbar-actions"><button className="button button-primary" disabled={save.isPending} onClick={submit}><Save size={18} />{editing ? l("حفظ التعديل", "Save changes") : l("حفظ", "Save")}</button></div>
  {kind === "experiences" && editing && <HighlightEditor basePath={`/api/v1/admin/experiences/${editing.id}/highlights`} highlights={editing.highlights ?? []} queryKey={["content"]} label={l("نقاط الخبرة", "Experience highlights")} />}
  <h3>{l("العناصر الحالية", "Current items")}</h3>{items.length ? <div className="profile-list">{items.map(item => <div className="row" key={item.id}><div><div className="row-title">{title(item, l)}</div><div className="row-meta">{l("نسخة", "Version")} {item.version}</div></div><div className="row-actions"><button className="button button-secondary" onClick={() => edit(item)}>{l("تعديل", "Edit")}</button><button className="button button-danger" onClick={() => archive(item.id)}>{l("أرشفة/حذف", "Archive/delete")}</button></div></div>)}</div> : <div className="empty-state">{l("لا توجد عناصر في هذا القسم.", "No items in this section.")}</div>}</section>;
}

function Field({ id, label, value, set, dir, type = "text" }: { id: string; label: string; value?: string | boolean; set: (key: string, value: string) => void; dir?: "ltr" | "rtl"; type?: string }) { return <div className="field"><label htmlFor={`content-${id}`}>{label}</label><input id={`content-${id}`} type={type} dir={dir} value={String(value ?? "")} onChange={event => set(id, event.target.value)} /></div>; }
function title(item: Item, l: (ar: string, en: string) => string) { const ar = item.translations?.find(x => x.locale === "ar"); const en = item.translations?.find(x => x.locale === "en"); return item.canonicalName || item.languageCode || ar?.organization || ar?.institution || ar?.name || en?.organization || en?.institution || en?.name || l("عنصر", "Item"); }
function toValue(kind: Kind, item: Item): Record<string, string | boolean> {
  const ar = item.translations?.find(x => x.locale === "ar") ?? {}, en = item.translations?.find(x => x.locale === "en") ?? {};
  if (kind === "skills") return { canonicalName: item.canonicalName ?? "", categoryAr: item.categoryAr ?? (item.category && item.category !== item.categoryEn ? item.category : ""), categoryEn: item.categoryEn ?? item.category ?? "", displayNameAr: ar.displayName ?? "", displayNameEn: en.displayName ?? "" };
  if (kind === "languages") return { languageCode: item.languageCode ?? "", proficiencyAr: item.proficiencyAr ?? item.proficiency ?? "", proficiencyEn: item.proficiencyEn ?? item.proficiency ?? "" };
  return {
    organizationAr: ar.organization ?? "", organizationEn: en.organization ?? "", jobTitleAr: ar.jobTitle ?? "", jobTitleEn: en.jobTitle ?? "",
    institutionAr: ar.institution ?? "", institutionEn: en.institution ?? "", degreeAr: ar.degree ?? "", degreeEn: en.degree ?? "",
    fieldOfStudyAr: ar.fieldOfStudy ?? "", fieldOfStudyEn: en.fieldOfStudy ?? "", locationAr: ar.location ?? "", locationEn: en.location ?? "",
    summaryAr: ar.summary ?? "", summaryEn: en.summary ?? "", notesAr: ar.notes ?? "", notesEn: en.notes ?? "",
    nameAr: ar.name ?? "", nameEn: en.name ?? "", issuerAr: ar.issuer ?? "", issuerEn: en.issuer ?? "",
    employmentType: item.employmentType ?? "full-time", startDate: (kind === "certifications" ? item.issuedOn : item.startDate) ?? "", endDate: (kind === "certifications" ? item.expiresOn : item.endDate) ?? "", current: item.isCurrent ?? false,
    credentialId: item.credentialId ?? "", url: item.organizationUrl ?? item.credentialUrl ?? ""
  };
}
