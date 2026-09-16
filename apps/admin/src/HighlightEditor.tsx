import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Pencil, Plus, Save, Trash2, X } from "lucide-react";
import { api } from "./api";
import { localizeAdminError, useAdminI18n } from "./i18n";

export type Highlight = {
  id: string;
  version: number;
  sortOrder: number;
  translations: Array<{ locale: "ar" | "en"; text: string }>;
};

export function HighlightEditor({ basePath, highlights, queryKey, label }: { basePath: string; highlights: Highlight[]; queryKey: string[]; label?: string }) {
  const { l } = useAdminI18n();
  const client = useQueryClient();
  const [editing, setEditing] = useState<Highlight | "new" | null>(null);
  const current = editing === "new" ? undefined : editing ?? undefined;
  const [draft, setDraft] = useState({ textAr: "", textEn: "", sortOrder: 0 });
  const title = label ?? l("نقاط العمل", "Highlights");
  const begin = (item: Highlight | "new") => {
    setEditing(item);
    if (item === "new") setDraft({ textAr: "", textEn: "", sortOrder: highlights.length });
    else setDraft({ textAr: item.translations.find(x => x.locale === "ar")?.text ?? "", textEn: item.translations.find(x => x.locale === "en")?.text ?? "", sortOrder: item.sortOrder });
  };
  const refresh = async () => { setEditing(null); await client.invalidateQueries({ queryKey }); };
  const save = useMutation({ mutationFn: () => api<Highlight>(current ? `${basePath}/${current.id}` : basePath, { method: current ? "PUT" : "POST", body: JSON.stringify({ version: current?.version ?? 0, ...draft }) }), onSuccess: refresh });
  const remove = useMutation({ mutationFn: (id: string) => api<void>(`${basePath}/${id}`, { method: "DELETE" }), onSuccess: refresh });
  return <section className="nested-editor" aria-label={title}>
    <div className="toolbar"><div><h3>{title}</h3><p className="field-help">{l("اكتب حقائق قابلة للتحقق فقط؛ كل نقطة تحتاج النص العربي والإنجليزي.", "Write verifiable facts only; every highlight needs Arabic and English text.")}</p></div><button type="button" className="button button-secondary" onClick={() => begin("new")}><Plus size={17} aria-hidden="true" />{l("إضافة نقطة", "Add highlight")}</button></div>
    {(save.isError || remove.isError) && <div className="notice" role="alert">{localizeAdminError(save.error ?? remove.error, l)}</div>}
    {editing && <div className="highlight-form"><div className="form-grid"><div className="field"><label htmlFor={`${basePath}-highlight-ar`}>{l("النص بالعربية", "Arabic text")}</label><textarea id={`${basePath}-highlight-ar`} dir="rtl" rows={3} value={draft.textAr} onChange={event => setDraft({ ...draft, textAr: event.target.value })} /></div><div className="field"><label htmlFor={`${basePath}-highlight-en`}>{l("النص بالإنجليزية", "English text")}</label><textarea id={`${basePath}-highlight-en`} dir="ltr" rows={3} value={draft.textEn} onChange={event => setDraft({ ...draft, textEn: event.target.value })} /></div><div className="field"><label htmlFor={`${basePath}-highlight-order`}>{l("الترتيب", "Order")}</label><input id={`${basePath}-highlight-order`} type="number" min="0" value={draft.sortOrder} onChange={event => setDraft({ ...draft, sortOrder: Number(event.target.value) })} /></div></div><div className="toolbar-actions"><button type="button" className="button button-primary" disabled={save.isPending || !draft.textAr.trim() || !draft.textEn.trim()} onClick={() => save.mutate()}><Save size={17} aria-hidden="true" />{l("حفظ النقطة", "Save highlight")}</button><button type="button" className="button button-secondary" onClick={() => setEditing(null)}><X size={17} aria-hidden="true" />{l("إلغاء", "Cancel")}</button></div></div>}
    {highlights.length ? <div className="profile-list">{[...highlights].sort((a, b) => a.sortOrder - b.sortOrder).map(item => <div className="row" key={item.id}><div><div className="row-title">{item.translations.find(x => x.locale === "ar")?.text || item.translations.find(x => x.locale === "en")?.text}</div><div className="row-meta">{l("الترتيب", "Order")} {item.sortOrder}</div></div><div className="row-actions"><button type="button" className="icon-button" aria-label={l("تعديل النقطة", "Edit highlight")} onClick={() => begin(item)}><Pencil aria-hidden="true" /></button><button type="button" className="icon-button danger-icon" aria-label={l("حذف النقطة", "Delete highlight")} disabled={remove.isPending} onClick={() => { if (window.confirm(l("حذف هذه النقطة من المحتوى ومن جميع اختيارات البروفايلات؟", "Delete this highlight from the content and all profile selections?"))) remove.mutate(item.id); }}><Trash2 aria-hidden="true" /></button></div></div>)}</div> : <div className="empty-state compact-empty">{l("لا توجد نقاط بعد.", "No highlights yet.")}</div>}
  </section>;
}
