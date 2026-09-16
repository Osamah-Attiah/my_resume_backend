# الهوية المهنية وSEO — متطلبات تنفيذ Terra

تاريخ المراجعة: 11 سبتمبر 2026. يكمل هذا الملف [تحليل النظام](./TERRA_IMPLEMENTATION_SPEC_AR.md) و[التصميم](./TERRA_VISUAL_DESIGN_AR.md). SEO جزء مطلوب من التنفيذ والاختبارات، وليس إضافة لاحقة. هذه مواصفات جاهزة للتنفيذ، وليست ادعاء بأن موقعًا نُشر أو فُهرس.

## 1. الهوية المعتمدة من الآن

المسمى الأساسي: **Software Engineer**. التخصص: **Flutter & .NET Backend**.

الصيغة الإنجليزية العامة:

> Software Engineer | Flutter & .NET Backend

الصيغة العربية: «مهندس برمجيات» مع وصف تخصص «تطوير تطبيقات Flutter والخدمات الخلفية باستخدام .NET».

Software Engineering اسم المجال، وSoftware Engineer المسمى الشخصي. لا تضف Senior أو سنوات خبرة أو درجة تعليمية أو شركة دون بيانات من المالك. لا تعدّل المسميات التاريخية للوظائف إن أضيفت لاحقًا؛ هذه حقائق مستقلة عن العنوان التسويقي العام.

| الموضع | التطبيق |
|---|---|
| الموقع الأساسي | الاسم ثم Software Engineer وتخصص Flutter/.NET |
| السيرة الإنجليزية | Software Engineer، مع إبراز التخصص حسب البروفايل |
| السيرة العربية | مهندس برمجيات والتخصصات بصيغها التقنية الصحيحة |
| HTML title وOG | الاسم والهوية ضمن عنوان موجز مناسب للصفحة |
| Person JSON-LD | `jobTitle: Software Engineer` و`knowsAbout` للتخصصات الحقيقية |
| README | يشرح المشروع وصاحبه بهذه الهوية دون تضخيم خبرته |
| LinkedIn/GitHub bio | صيغة متسقة موصى بها؛ تعديل الحسابات الخارجية ليس جزءًا من تحرير هذه الملفات |

لا تستبدل جميع العناوين بقيمة واحدة في الكود؛ الهوية بيانات مترجمة قابلة للتحرير في لوحة التحكم. جميع الافتراضات والـseed الإنتاجية الجديدة تتبع هذا التحديث، بينما بيانات demo تُوسم كاختبار.

## 2. أهداف البحث وحدود الالتزام

الأولوية أن يجد من يبحث عن اسم المالك صفحة واضحة تربطه بهندسة البرمجيات وFlutter و.NET ومشاريعه. الكلمات الموضوعية تُستخدم طبيعيًا في وصف العمل، مثل Software Engineer وFlutter وASP.NET Core وREST APIs حيث تدعمها مشاريع فعلية. لا صفحات مدن أو وظائف متكررة ولا حشو كلمات أو نص مخفي.

SEO التقني يسهل الاكتشاف والفهم، لكنه لا يضمن الفهرسة أو المركز الأول. لا تُقدَّم Lighthouse SEO 100 بوصفها ضمان ترتيب. كذلك `meta keywords` لا تؤثر في ترتيب Google ولا نضيف محررًا لها. [وسوم Google المدعومة](https://developers.google.com/search/docs/crawling-indexing/special-tags)

## 3. مصدر HTML والروابط

- React عبر Next static export كما في التحليل؛ الاسم والمسمى والملخص والمشاريع والروابط في HTML الناتج، حتى عند تعطيل JavaScript والـAPI.
- عنوان H1 واضح واحد لكل صفحة كقاعدة تحريرية لهذا المشروع، وعناوين أقسام H2 ثم H3 دون استخدامها للمظهر فقط. هذه قاعدة تنظيم وليست ادعاء شرط ترتيب لدى Google.
- روابط انتقال حقيقية `<a href>`؛ لا onClick وحده للوصول للمشاريع أو اللغات.
- عنوان إنتاج HTTPS ثابت لكل Site؛ لا تُشتق canonical أو OG من عنوان localhost أو preview أو query parameter.
- سياسة موحدة لمسارات trailing slash تتوافق مع الاستضافة، وتُختبر redirect chain الفعلية.
- sitemap وrobots و404 وheaders وredirects تُبنى من نفس snapshot/route manifest، ولا تُجمع من قاعدة بيانات حية أثناء الزيارة.
- صفحات المشاريع المنتقاة تتبع حدود بروفايلها. لا SEO endpoint يسرب مشاريع مخفية أو مسودات.

## 4. Metadata بالعربية والإنجليزية

كل صفحة لها title وdescription وcanonical وفق سياستها وOG وTwitter card/summary_large_image وصورة مشاركة ثابتة عند توفرها. تستخرج القيم في build لا بعد hydration.

أمثلة قوالب وليست بيانات المالك الفعلية:

| الصفحة | English title | العربية |
|---|---|---|
| الأساسية | `{Name} — Software Engineer, Flutter & .NET` | `{الاسم} — مهندس برمجيات، Flutter و.NET` |
| بروفايل متخصص | `{Name} — Software Engineer, Flutter` | `{الاسم} — مهندس برمجيات متخصص في Flutter` |
| مشروع | `{Project} — {Name}` | `{المشروع} — {الاسم}` |

title مستهدف تحريريًا نحو 45–65 حرفًا وdescription نحو 120–170، مع تحذير فقط؛ عرض نتائج البحث يعتمد على المساحة ويمكن لمحرك البحث إعادة صياغتها. لا تمنع نشر اسم طويل بسبب عدد ثابت. الوصف يلخص المحتوى الحقيقي لكل لغة.

صور OG مقترحة 1200×630، مولدة وقت البناء من الاسم والمسمى أو المشروع، بخط عربي صحيح؛ لا تضف image generation service. `og:url` عنوان الصفحة المختار، وصورة OG وalt والعناوين لا تتضمن بيانات غير منشورة. `lang/dir` صحيحة، ويمكن استخدام `ar_AR/en_US` للـOG conventions دون اختلاق استهداف بلد في hreflang.

## 5. Canonical والبروفايلات والمواقع المتعددة

| الحالة | السياسة |
|---|---|
| الموقع الأساسي وبروفايله العام | indexable وself-canonical |
| ترجمة عربية وإنجليزية متكافئتان | كل لغة self-canonical، مع hreflang متبادل |
| بروفايل مخصص لتقديم معين | noindex افتراضيًا، ويمكن للمالك إتاحته للبحث إذا كان محتواه مستقلًا ومفيدًا |
| نسختان متماثلتان بنفس اللغة | اختيار URL أصلي؛ redirect إن لم نحتج النسخة، أو canonical للنسخة الأصلية مع إبقاء النسخة متاحة |
| صفحة مختلفة فعلًا في محتواها | canonical لنفسها إذا كانت indexable؛ لا canonical تعسفي لكل شيء إلى الرئيسية |
| admin أو preview أو تصدير خاص | غير قابل للفهرسة، مع حماية الوصول للمحتوى الخاص |

canonical إشارة تفضيل وليست ضمان اختيار Google. لا نستخدم noindex لمجرد إجبار اختيار canonical بين صفحتين نريد بقاء محتواهما في البحث، ولا نخلط canonical إلى صفحة أخرى مع سياسة الاستبعاد دون سبب. [إرشادات canonical](https://developers.google.com/search/docs/crawling-indexing/consolidate-duplicate-urls)

تجنب تكرار البروفايل الافتراضي: `/en/` هو الصفحة الأصلية له، و`/en/p/{defaultSlug}/` يُعاد توجيهه 301 إليها على مستوى الاستضافة؛ نفس القاعدة للعربية. تُولّد تفاصيل المشاريع بمساراتها المحددة في التحليل. تغيير البروفايل الافتراضي يعيد حساب المسارات والروابط والredirects مع كشف التعارضات.

عند إضافة نطاق شخصي لاحقًا، حدّث base_url والروابط وcanonical وsitemap وتحقق من توجيه نطاق الاستضافة القديم إلى المقابل الجديد حيث تدعم إعدادات المزود ذلك. لا تترك نسختين أصلًا دائمًا دون سياسة محددة.

## 6. اللغات وhreflang

- مسارات ثابتة `/ar/` و`/en/`، بلا توجيه إجباري اعتمادًا على IP أو لغة المتصفح.
- `/` صفحة لغة بسيطة مع روابط HTML حقيقية إلى الصفحتين، وتصلح `x-default` للصفحة الرئيسية فقط. قرار يزيل غموض الصفحة الجذرية في التحليل الأساسي.
- لكل زوج indexable من نفس البروفايل أو المشروع: روابط alternate تشمل الصفحة نفسها والبديل، وتكون متبادلة وabsolute.
- لا hreflang إلى لغة ناقصة أو noindex أو 404 أو redirect؛ استعمل الوجهة الأصلية النهائية.
- لا تربط بروفايل Flutter بالعربية ببروفايل مختلف عن .NET بالإنجليزية باعتبارهما ترجمة.
- استخدم `ar/en`، ولا `ar-SA` دون استهداف جغرافي حقيقي. الصفحة العربية لا تشير canonical إلى الإنجليزية.

[توثيق النسخ المحلية لدى Google](https://developers.google.com/search/docs/specialty/international/localized-versions)

## 7. Sitemap وrobots وPDF

`/sitemap.xml` UTF-8 يتضمن absolute canonical URLs ذات 200 والقابلة للفهرسة فقط. يستبعد admin والمسودات وpreview وnoindex وredirects وPDF وفق السياسة أدناه. `lastmod` تاريخ آخر تغيير جوهري بالمحتوى، وليس وقت كل build؛ خزّنه أو احسبه بمقارنة content hash مع snapshot السابقة. لا حاجة priority/changefreq. [Google sitemap](https://developers.google.com/search/docs/crawling-indexing/sitemaps/build-sitemap)

`robots.txt` يعلن موقع sitemap الصحيح ولا يمنع crawl لملفات CSS/JS أو صفحات نحتاج أن يقرأ crawler وسم noindex فيها. robots.txt ليس وسيلة خصوصية؛ قد يبقى URL محجوب الزحف معروفًا في البحث.

سياسة المنتج لملفات Resume PDF العامة: تبقى قابلة للتنزيل، لكن `X-Robots-Tag: noindex` حتى تكون صفحة الهوية HTML هي النتيجة الأساسية وتقل فرص ظهور PDF قديمة. لا تحجب `/resumes/` في robots.txt حتى يستطيع crawler رؤية الهيدر. هذه السياسة لا تغيّر توافق الملف مع ATS. private PDF تبقى مصادقًا عليها ولا تكفيها noindex.

في Cloudflare Pages استخدم `_headers` للأصول الثابتة، مثال تولَّد قواعده وقت النشر:

```text
/resumes/*
  X-Robots-Tag: noindex
```

admin deployment يحمل noindex لجميع الصفحات. تحقق من preview deployment header: Cloudflare يضعه افتراضيًا، لكن يجب فحص الاستجابة النهائية. لا تضع noindex global على إنتاج الموقع العام بطريق الخطأ. [Cloudflare headers](https://developers.cloudflare.com/pages/configuration/headers/)، [Preview deployments](https://developers.cloudflare.com/pages/configuration/preview-deployments/)

## 8. البيانات المنظمة

أنشئ JSON-LD من snapshot العامة، مع escaping آمن لا يسمح للنص بإغلاق script.

- `Person`: الاسم، `jobTitle: Software Engineer`، الوصف، الروابط المهنية الموثقة في sameAs، والصورة عند وجود صورة حقيقية.
- `ProfilePage` للصفحات التي تركز فعلًا على الشخص مع mainEntity Person؛ لا تطبقه تلقائيًا على تفاصيل كل مشروع.
- `WebSite` للهوية العامة للموقع؛ وBreadcrumbList في صفحات التفاصيل إذا ظهرت breadcrumbs فعلية.
- لا Organization لصاحب الموقع لمجرد أنه مطور مستقل، ولا مراجعات أو aggregateRating أو موظفين أو إحصائيات وهمية.
- استخدم Person `@id` ثابتًا مثل `{primaryOrigin}/#person` بين اللغات والبروفايلات عند إعداد الموقع الأساسي، مع بقاء بيانات الموقع صحيحة إذا كان منفصلًا.
- احتفظ بالحقول التي تطابق محتوى ظاهرًا. لا تضف الشهادات أو الخبرات الناقصة إلى schema لتحسين الظهور.

اختبر Schema Markup Validator، واستخدم Rich Results Test للأنواع المدعومة. عدم وجود rich result لنوع Person وحده لا يعني JSON-LD فاسدًا. اجتياز التحقق لا يضمن ظهورًا خاصًا. [ProfilePage لدى Google](https://developers.google.com/search/docs/appearance/structured-data/profile-page)، [سياسات البيانات المنظمة](https://developers.google.com/search/docs/appearance/structured-data/sd-policies)

## 9. 404 والتحويلات وتغيير الروابط

- صفحة غير موجودة تعيد HTTP 404 حقيقية، ولا SPA fallback يعيد 200 لكل URL. وجود top-level `404.html` مهم على Cloudflare Pages. [Serving Pages](https://developers.cloudflare.com/pages/configuration/serving-pages/)
- المسارات القديمة ذات بديل مباشر تتحول إلى البديل بـ301/308 على الاستضافة؛ لا client-side redirect فقط.
- حذف مشروع دون بديل لا يوجّه إلى الصفحة الرئيسية لتجنب إخفاء 404.
- جدول redirects يقبل مسارات داخلية فقط، ويمنع الحلقات والسلاسل والتعارض مع صفحة حية. old → final مباشرة.
- `trailingSlash` وملفات `_redirects` وسياسة المزود يجب أن تتفق؛ اختبرها بعد deployment ولا تعتمد على local preview لإثبات HTTP status.
- إضافة redirect تُراجع قبل نشرها مع تأثير تغيّر البروفايل الافتراضي؛ لا تحول مسار بروفايل جديد بطريق الخطأ بسبب قاعدة قديمة.

## 10. إعدادات SEO في لوحة التحكم والبيانات

أضف داخل إعداد الموقع تبويب SEO يعرض base URL وpreview لنتيجة بحث تقريبية، وصفحات الموقع وحالة فهرستها، وحقول title/description/OG لكل لغة، وتحذيرات canonical والترجمة والروابط. التغيير يحتاج publish مثل بقية المحتوى.

الامتدادات المكملة لجدولَي التحليل الأساسي:

- `sites`: أضف `is_primary_identity_site boolean` بفهرس جزئي يسمح بموقع أساسي واحد للمالك، و`search_verification_token?` لقيمة Google HTML verification فقط، لا HTML عشوائي. token التحقق العام ليس credential؛ لا تضع مفاتيح حساب Google هنا.
- `seo_page_settings`: UUID وحقول الزمن/version المشتركة، وFK site/profile/project/media؛ locale ar/en؛ page_kind Home/Profile/Project. Home بلا profile/project، Profile يفرض profile فقط، Project يفرض الاثنين. تحقق أن project مختار في البروفايل وأن البروفايل مرتبط بالموقع.
- فهارس جزئية unique: `(site_id, locale)` لـHome، `(site_id, locale, profile_id)` لـProfile، `(site_id, locale, profile_id, project_id)` لـProject. هذه تمنع تكرار الصفوف رغم null.
- `index_override` nullable: null يرث site_profile؛ false يستبعد؛ true لا يتجاوز parent noindex أو preview أو draft. الصفحة الرئيسية تتبع البروفايل الافتراضي. الجذر `/` يشتق إعداداته تلقائيًا من الموقع.
- `canonical_override_url` nullable للمحتوى المتكافئ فقط؛ https وhost ضمن مواقع المالك المهيأة، ولا query/fragment. قبل النشر تحقق الوجهة واللغة والتكافؤ، ولا server-side fetch تعسفي من حقل المستخدم.
- أولوية metadata: page override ثم profile SEO fields الموجودة ثم title/summary-derived defaults المناسبة للصفحة واللغة. لا قيم عامة متكررة لكل مشروع.
- `site_redirects`: source/target paths تبدأ `/`، بلا بروتوكول أو hostname أو CRLF، والرمز 301 أو 308؛ unique source داخل site. تحفظ مع snapshot ويولد `_redirects` منها.

API المكمل: `GET/PUT /api/v1/admin/sites/{id}/seo`، وCRUD `/api/v1/admin/sites/{id}/redirects`. كل العمليات مصادق عليها وتستعمل validation/version/audit نفسها. تحديث site-level SEO لا يغير محتوى سيرة PDF إلا إذا عدّل المالك نص الهوية نفسه.

## 11. الأداء والمحتوى

أهداف Core Web Vitals عند توفر بيانات زيارات فعلية: LCP ≤2.5s وINP ≤200ms وCLS ≤0.1 عند المئين 75، مع فصل الجوال والكمبيوتر. بيانات ميدانية قليلة قد لا تكفي لإصدار حكم؛ Lighthouse اختبار مختبري ولا يثبت INP ميدانيًا. [Core Web Vitals](https://web.dev/articles/vitals)

نفذ صورًا responsive بأبعاد ثابتة، لا lazy-load للصورة الرئيسية المؤثرة في LCP دون سبب، وخطوطًا محلية محدودة، وJS تفاعليًا بالقدر المطلوب. لا يظهر النص فقط بعد animation أو JavaScript. أهداف Lighthouse في التحليل الأساسي تبقى قائمة، وأضف فحوص SEO المحددة التالية بدل الاكتفاء بدرجة عامة.

المالك يكتب لكل مشروع وصفًا مفيدًا يشرح ما بناه ودوره والتقنيات والروابط. لا تؤلف Terra سنوات أو أرقامًا أو أسماء عملاء لإكمال الصفحة. تحديث الهوية لا ينشئ تلقائيًا صفحات Flutter و.NET وSoftware Engineering متشابهة بلا محتوى مستقل.

## 12. اختبارات القبول قبل التسليم

- [ ] Software Engineer والتخصصات متسقة في الصفحة وPDF وmetadata والبيانات الافتراضية.
- [ ] نص الهوية والمشروع موجود في HTML عند تعطيل JavaScript وAPI.
- [ ] title/description وcanonical في head الناتج من build، بلا localhost/preview/placeholder.
- [ ] كل canonical indexable يشير لصفحة 200 صحيحة؛ لا canonical إنجليزية للترجمة العربية.
- [ ] hreflang متبادل لنفس المحتوى وباللغات المنشورة فقط.
- [ ] sitemap يستبعد noindex وPDF وredirects ويستخدم lastmod حقيقيًا.
- [ ] robots لا يمنع قراءة noindex، وإنتاج الموقع العام غير محجوب بطريق الخطأ.
- [ ] PDF العام يحمل noindex header وقابل للتنزيل؛ الخاص يرفض anonymous download.
- [ ] صفحة مجهولة 404، وتغيير slug ينتج redirect صحيحًا دون loop أو soft 404.
- [ ] JSON-LD صالح ويطابق المحتوى، وروابط sameAs للمالك فقط.
- [ ] OG العربية والإنجليزية سليمة الخطوط والاتجاه، وملف الصورة URL متاح.
- [ ] البروفايل الافتراضي لا يظهر كنسختين indexable تحت مسارين.
- [ ] تشغيل موقعين لا ينتج canonical يشير لنطاق خاطئ بسبب إعداد مشترك.
- [ ] الفرع/preview وadmin noindex؛ فحص headers بعد النشر موثق.
- [ ] تقرير Lighthouse وفحص الروابط والـHTML وheaders مرفق؛ لا ادعاء ترتيب بحث مضمون.

قسّم الفحوص إلى CI على build ثابت، ثم smoke tests لروابط deployment الفعلية. استخدم fixture ar/en مع default profile وبروفايل noindex ومشروع مشترك ولغة ناقصة وredirect قديم.

## 13. الإعداد بعد الاستضافة وجاهزية العمل

بعد وجود الموقع وحساب المالك: التحقق من Google Search Console عبر URL-prefix property على نطاق الخدمة أو DNS عند امتلاك دومين، ثم تقديم sitemap وفحص URL للرئيسية العربية والإنجليزية ومشروع. لا تنفذ تسجيل دخول أو تحقق ملكية نيابة عن المالك دون صلاحياته الفعلية، ولا تعتبر عدم توفر الحساب مانعًا لإنهاء الكود والوثائق.

Search Console لتشخيص الفهرسة والزيارات القادمة من البحث؛ إضافة Google Analytics أو أدوات تتبع الزوار ليست مطلوبة لهذا الإصدار. لا تنشئ أتمتة مراقبة دون طلب مستقل. عند الحاجة للمتابعة يسجل الدليل كيفية مراجعة الصفحات المكتشفة والمفهرسة والأخطاء؛ نتيجة الفهرسة قد تتأخر ولا تُضمن بتقديم sitemap.

الحالة الحالية: حزمة التحليل والتصميم وSEO جاهزة لبدء Terra التنفيذ. تنفيذ التطبيق والنشر وفحص PDF والواجهات وربط Search Console لم يُنجز بمجرد كتابة هذه الملفات. القيم التي تُملأ عند الإعداد: الاسم الفعلي باللغتين، بيانات السيرة والصور، روابط المالك، وحسابات وأسرار الاستضافة. لا يمنع غيابها بدء التطوير ببيانات اختبار صريحة.

يعتبر التنفيذ مكتملًا عندما تنجح معايير الملفات الثلاثة، وتُعرض الروابط/الملفات الفعلية ونتائج التحقق والقيود المتبقية. كلمة «جاهز للعمل» في تسليم التحليل تعني جاهزًا للتطوير؛ «جاهز للاستخدام العام» تتطلب نشرًا وفحوصًا فعلية.
