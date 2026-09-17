import { createContext, useContext, type ReactNode } from "react";

export type AdminLocale = "ar" | "en";

const apiErrorCopy: Record<string, [string, string]> = {
  REQUEST_FAILED: ["تعذر إكمال الطلب.", "Unable to complete the request."],
  VALIDATION_FAILED: ["فشل التحقق من البيانات. راجع الحقول المطلوبة.", "Validation failed. Review the required fields."],
  VALIDATION_ERROR: ["فشل التحقق من البيانات. راجع الحقول المطلوبة.", "Validation failed. Review the required fields."],
  DOWNLOAD_FAILED: ["تعذر تنزيل الملف.", "Unable to download the file."],
  UPLOAD_FAILED: ["تعذر رفع الملف.", "Unable to upload the file."],
  NETWORK_ERROR: ["تعذر الاتصال بالخادم. تحقق من الاتصال وحاول مجددًا.", "Unable to reach the server. Check your connection and try again."],
  API_WAKE_TIMEOUT: ["استغرق اتصال الخادم وقتًا أطول من المتوقع. حاول مجددًا.", "The server took longer than expected to respond. Try again."],
  MEDIA_STORAGE_NOT_CONFIGURED: ["رفع الصور غير مهيأ بعد؛ أضف إعدادات تخزين الصور للخادم.", "Image uploads are not configured yet; add the server image-storage settings."],
  PRIVATE_EXPORT_NOT_CONFIGURED: ["التصدير الخاص غير مهيأ بعد؛ المعاينة المحلية ما زالت متاحة.", "Private export is not configured yet; the local preview is still available."],
  VERSION_CONFLICT: ["تغيّرت البيانات منذ فتح المحرر. حدّث الصفحة ثم راجعها واحفظ مرة أخرى.", "The data changed while the editor was open. Refresh, review it, and save again."],
  UNAUTHORIZED: ["انتهت الجلسة. سجّل الدخول مرة أخرى.", "Your session has expired. Sign in again."],
  FORBIDDEN: ["ليست لديك صلاحية لتنفيذ هذا الإجراء.", "You do not have permission to perform this action."],
  INVALID_CREDENTIALS: ["البريد الإلكتروني أو كلمة المرور غير صحيحة.", "The email or password is incorrect."],
  INVALID_CURRENT_PASSWORD: ["كلمة المرور الحالية غير صحيحة.", "The current password is incorrect."],
  PASSWORD_TOO_SHORT: ["كلمة المرور الجديدة يجب أن تتكون من 14 حرفًا على الأقل.", "The new password must be at least 14 characters long."],
  INVALID_SLUG: ["استخدم حروفًا لاتينية صغيرة وأرقامًا وشرطات فقط في المسار.", "Use lowercase Latin letters, numbers, and hyphens in the slug."],
  INVALID_LOCALE: ["اللغة غير صالحة. استخدم العربية أو الإنجليزية.", "The locale is invalid. Use Arabic or English."],
  INVALID_LOCALES: ["قائمة اللغات غير صالحة.", "The locale list is invalid."],
  INVALID_LANGUAGE_CODE: ["استخدم رمز لغة بصيغة BCP 47، مثل ar أو en.", "Use a BCP 47 language code, such as ar or en."],
  REQUIRED: ["أكمل الحقل المطلوب.", "Complete the required field."],
  INVALID_PDF_SETTINGS: ["إعدادات PDF غير صالحة. استخدم حجم خط من 9.5 إلى 12 وهوامش من 10 إلى 25 مم وصفحة أو صفحتين.", "The PDF settings are invalid. Use a 9.5–12 font size, 10–25 mm margins, and one or two target pages."],
  ONGOING_END_DATE: ["لا يمكن للسجل المستمر أن يحتوي على تاريخ نهاية.", "An ongoing record cannot have an end date."],
  DATE_ORDER: ["تاريخ النهاية لا يمكن أن يسبق تاريخ البداية.", "The end date cannot be before the start date."],
  INVALID_SEARCH_VERIFICATION: ["أدخل رمز تحقق Google فقط، دون وسم HTML.", "Enter the Google verification token only, without an HTML tag."],
  INVALID_PAGE_KIND: ["نوع الصفحة غير صالح.", "The page type is invalid."],
  INVALID_SEO_TARGET: ["اختيار الصفحة يحتاج إلى بروفايل أو مشروع مطابق.", "The selected page type requires a matching profile or project."],
  SITE_PROFILE_REQUIRED: ["اربط بروفايلًا واحدًا على الأقل بالموقع.", "Attach at least one profile to the site."],
  ONE_DEFAULT_REQUIRED: ["يجب تحديد بروفايل افتراضي واحد فقط.", "Exactly one default profile is required."],
  DUPLICATE_PROFILE: ["لا يمكن ربط البروفايل نفسه أكثر من مرة.", "A profile can be attached only once."],
  DUPLICATE_PATH: ["يجب أن تكون مسارات البروفايلات مختلفة داخل الموقع.", "Profile paths must be unique within the site."],
  PROFILE_NOT_ON_SITE: ["هذا البروفايل غير مرتبط بالموقع.", "This profile is not attached to the site."],
  PROJECT_NOT_ON_PROFILE: ["هذا المشروع غير موجود في البروفايل المحدد.", "This project is not included in the selected profile."],
  OWNERSHIP_MISMATCH: ["العنصر المحدد لا ينتمي إلى هذا الحساب.", "The selected item does not belong to this account."],
  PROFILE_OWNERSHIP_MISMATCH: ["البروفايل المحدد لا ينتمي إلى هذا الحساب.", "The selected profile does not belong to this account."],
  MEDIA_OWNERSHIP_MISMATCH: ["الصورة المحددة لا تنتمي إلى هذا الحساب.", "The selected image does not belong to this account."],
  HIGHLIGHT_OWNERSHIP_MISMATCH: ["النقطة المحددة لا تنتمي إلى هذا المحتوى.", "The selected highlight does not belong to this content."],
  PROFILE_IN_USE: ["لا يمكن حذف بروفايل مرتبط بموقع.", "A profile attached to a site cannot be archived."],
  MEDIA_IN_USE: ["افصل الصورة عن المشاريع قبل أرشفتها.", "Detach the image from projects before archiving it."],
  COVER_NOT_ATTACHED: ["صورة الغلاف يجب أن تكون مرفقة بالمشروع أولًا.", "The cover image must be attached to the project first."],
  ONE_IMAGE_REQUIRED: ["أضف صورة واحدة على الأقل.", "Add at least one image."],
  DUPLICATE_MEDIA: ["لا يمكن إضافة الصورة نفسها أكثر من مرة.", "The same image cannot be added more than once."],
  INVALID_IDEMPOTENCY_KEY: ["أدخل مفتاح طلب غير فارغ بطول لا يتجاوز 100 حرف.", "Provide a non-empty request key up to 100 characters."],
  EXPORT_ACTIVE: ["يوجد تصدير PDF قيد التنفيذ لهذا البروفايل.", "A private PDF export is already active for this profile."],
  ARTIFACT_EXPIRED: ["انتهت صلاحية ملف PDF الخاص؛ أنشئه مرة أخرى.", "The private PDF artifact expired; generate it again."],
  ARTIFACT_DOWNLOAD_FAILED: ["تعذر تنزيل ملف PDF الخاص.", "The private PDF artifact could not be downloaded."],
  WORKFLOW_DISPATCH_FAILED: ["تعذر إرسال سير عمل النشر.", "The publishing workflow could not be dispatched."],
  RECONCILE_EVIDENCE_INVALID: ["فشل التحقق من نتيجة سير العمل.", "The workflow result evidence failed validation."],
  RETRY_NOT_ALLOWED: ["لا يمكن إعادة محاولة هذا النشر في حالته الحالية.", "This publication cannot be retried in its current state."],
  STALE_PUBLICATION_RESULT: ["يوجد نشر أحدث لهذا الموقع.", "A newer publication already exists for this site."],
  INVALID_ROLLBACK_SOURCE: ["يمكن الرجوع إلى نسخة ناجحة من هذا الموقع فقط.", "You can roll back only to a successful publication of this site."],
  TRANSLATION_REQUIRED: ["أكمل الحقول المطلوبة بالعربية والإنجليزية.", "Complete the required fields in Arabic and English."],
  ALT_TRANSLATION_REQUIRED: ["النص البديل للصورة مطلوب بالعربية والإنجليزية.", "Image alt text is required in Arabic and English."],
  INVALID_ARTIFACT: ["ملف النشر غير صالح.", "The publication artifact is invalid."],
  ARTIFACTS_REQUIRED: ["ملفات النشر المطلوبة غير موجودة.", "The required publication artifacts are missing."],
  DEPLOYMENT_REQUIRED: ["بيانات النشر مطلوبة.", "Deployment data is required."],
  INVALID_DEPLOYMENT: ["بيانات النشر غير صالحة.", "The deployment data is invalid."],
  INVALID_REDIRECT: ["إعداد التحويل غير صالح.", "The redirect settings are invalid."],
  OG_MEDIA_INVALID: ["صورة المشاركة OG غير صالحة.", "The OG image is invalid."],
  IMAGE_DIMENSIONS: ["أبعاد الصورة غير صالحة.", "The image dimensions are invalid."],
  IMAGE_SIZE: ["حجم الصورة يتجاوز الحد المسموح.", "The image exceeds the allowed size."],
  UNSUPPORTED_IMAGE: ["صيغة الصورة غير مدعومة.", "The image format is not supported."],
  MULTIPART_REQUIRED: ["أرسل الصورة بصيغة الرفع المطلوبة.", "Send the image using the required upload format."]
};

const apiMessageCopy: Record<string, [string, string]> = {
  "Validation failed": ["فشل التحقق من البيانات. راجع الحقول المطلوبة.", "Validation failed. Review the required fields."],
  "Unable to complete the request": ["تعذر إكمال الطلب.", "Unable to complete the request."],
  "The request is invalid.": ["الطلب غير صالح.", "The request is invalid."],
  "No final workflow callback arrived before the attempt lease expired.": ["لم تصل النتيجة النهائية لسير العمل قبل انتهاء مهلة المحاولة.", "No final workflow result arrived before the attempt timed out."]
};

export function localizeAdminError(error: unknown, l: (ar: string, en: string) => string) {
  const candidate = error as { code?: unknown; message?: unknown } | null;
  const code = typeof candidate?.code === "string" ? candidate.code.toUpperCase() : "";
  if (apiErrorCopy[code]) return l(...apiErrorCopy[code]);
  const message = typeof candidate?.message === "string" ? candidate.message : "";
  if (apiMessageCopy[message]) return l(...apiMessageCopy[message]);
  return message || l("تعذر إكمال الطلب.", "Unable to complete the request.");
}

export function localizeAdminMessage(message: string | undefined, l: (ar: string, en: string) => string) {
  if (!message) return "";
  if (apiMessageCopy[message]) return l(...apiMessageCopy[message]);
  if (message.startsWith("GitHub Actions completed with conclusion")) {
    return l("انتهى سير عمل GitHub دون نجاح.", "The GitHub Actions workflow finished without success.");
  }
  return message;
}

type CountForms = {
  en: { one: string; other: string };
  ar: { zero?: string; one: string; two: string; few: string; many: string; other: string };
};

const localeContext = createContext<AdminLocale>("ar");

export function AdminLocaleProvider({ locale, children }: { locale: AdminLocale; children: ReactNode }) {
  return <localeContext.Provider value={locale}>{children}</localeContext.Provider>;
}

export function useAdminI18n() {
  const locale = useContext(localeContext);
  const isArabic = locale === "ar";
  const direction = isArabic ? "rtl" : "ltr";
  const l = (ar: string, en: string) => isArabic ? ar : en;
  const count = (value: number, forms: CountForms) => {
    if (!isArabic) return `${value} ${value === 1 ? forms.en.one : forms.en.other}`;
    const word = value === 0 && forms.ar.zero
      ? forms.ar.zero
      : value === 1
        ? forms.ar.one
        : value === 2
          ? forms.ar.two
          : value >= 3 && value <= 10
            ? forms.ar.few
            : value >= 11 && value <= 99
              ? forms.ar.many
              : forms.ar.other;
    return `${value} ${word}`;
  };
  return { locale, direction, isArabic, l, count };
}
