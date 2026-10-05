# النشر المعتمد

يُنشَر الموقع العام وملفات Resume PDF التابعة له عبر **Azure DevOps Pipeline** في `azure-pipelines.yml` فقط. يقرأ خط النشر نسخة ثابتة من API، يولّد PDF والموقع، يشغّل الفحوص، ثم يرفع `apps/public/out` إلى مشروع Cloudflare Pages المحدد داخل النسخة. لا تستخدم GitHub Actions لنشر الموقع العام؛ ملف `.github/workflows/ci.yml` للفحوص فقط.

البنية الحالية: Neon لقاعدة PostgreSQL، Render لخدمة `resume.API`، وCloudflare Pages للموقع العام ولوحة الإدارة. بيانات المشاريع وروابطها والخبرات الوظيفية تُحدّث من API عند الزيارة وكل ١٥ ثانية ما دامت الصفحة مفتوحة. يعيد طلب المشاريع نفسه قائمة `experiences` المنتقاة للموقع، مع استبعاد المؤرشف وخبرات PDF فقط؛ وتُقبل القائمة الفارغة لإزالة القسم عند حذف آخر خبرة. يبقى آخر محتوى متاحًا عند تعذر الاتصال، بينما يتطلب تحديث PDF والصفحات الثابتة طلب نشر جديد.

التصميم الجديد المنقول من `apps/Portfolio-redesign` يعمل داخل `apps/public/components/portfolio` وفي مسارات الموقع الحالية. تأتي بيانات البروفايل والقصة والمهارات من نسخة النشر التي ينشئها API، وتأتي هوية الموقع والبروفايل منها أيضًا. لا تُستخدم ملفات بيانات النموذج الأولي أو عناوينه الثابتة في الإنتاج. تنتقل اللغة بين مسارات العربية والإنجليزية الفعلية، وتفتح بطاقات المشاريع صفحة التفاصيل التي تدعم المشاريع المضافة بعد النشر، مع روابطها العامة. ملفات PDF وروابط SEO تبقى ضمن عملية نشر Azure نفسها.

مجلد `apps/Portfolio-redesign` نسخة المصدر المستوردة من Replit؛ حزم الإنتاج التي يثبتها npm هي `apps/admin` و`apps/public` والحزم المشتركة فقط، لتجنب تشغيل إعدادات pnpm وExpress التجريبية عند النشر.

## إعداد Render

- `ConnectionStrings__DefaultConnection`: سلسلة اتصال قاعدة البيانات مع SSL.
- `Auth__SigningKey`: مفتاح عشوائي لا يقل عن 32 بايت.
- `Auth__Issuer=resume-api` و`Auth__Audience=resume-admin`.
- `Cors__AdminOrigin`: أصل لوحة الإدارة HTTPS.
- `Publishing__CallbackSecret`: سر مشترك مع Azure DevOps لاسترجاع نسخة النشر وتسجيل النتيجة.
- `Publishing__Provider=azure-devops` للتوثيق التشغيلي؛ يوجّه الكود نشر الموقع إلى Azure DevOps حتى إذا غاب هذا المتغير.
- `AzureDevOps__Organization`, `AzureDevOps__Project`, `AzureDevOps__PipelineId`, `AzureDevOps__Token`, `AzureDevOps__Ref=main`.

يوثق `render.yaml` أسماء المتغيرات بلا قيمها. يطبّق API migrations المعلقة عند بدء التشغيل في Production؛ راجع النسخ الاحتياطية قبل نشر تغييرات مخطط قاعدة البيانات.

## إعداد Azure DevOps

اربط Pipeline بالمستودع وملف `azure-pipelines.yml`. اضبط متغيرات/أسرار خط النشر التالية:

- `RESUME_API_ORIGIN`: أصل API بـHTTPS دون شرطة أخيرة.
- `Publishing__CallbackSecret`: نفس القيمة المضبوطة في Render.
- `CLOUDFLARE_API_TOKEN`: صلاحية محدودة لنشر Pages.
- `CLOUDFLARE_ACCOUNT_ID`: معرّف حساب Cloudflare.

ينشئ API عند طلب النشر `PUBLICATION_ID` و`ATTEMPT_ID` ويرسلهما إلى Azure DevOps. لا تضع لهما قيمًا ثابتة داخل YAML، لأن ذلك سيطغى على قيم كل عملية نشر. يأخذ خط النشر `deploymentTargetKey` من نسخة النشر، ويرفع إلى Cloudflare Pages مع `--branch main` كي يصل التحديث إلى نطاق الإنتاج.

## طريقة النشر والتحقق

1. حدّث بيانات الموقع و`BaseUrl` وSEO و`deploymentTargetKey` من لوحة الإدارة.
2. من **المواقع والنشر** اختر **مراجعة ونشر**. لا تشغّل GitHub Actions لهذا الغرض.
3. راقب Azure DevOps Pipeline: جلب النسخة الثابتة، توليد PDF، `pdf:qa`، البناء والاختبارات، ورفع Pages.
4. تأكد من ظهور حالة **ناجح** للإصدار في لوحة الإدارة، ثم افتح العربية والإنجليزية وPDF من نطاق Pages الفعلي.
5. اختبر مشروعًا ورابطًا مضافًا من اللوحة؛ تُرجع المشاريع من API، أما PDF فيتجدد مع كل نشر.

إذا تعطل Azure DevOps، أصلح سبب التعطل وأعد المحاولة من لوحة الإدارة. لا تنتقل إلى GitHub Actions أو ترفع نسخة محلية مبنية من بيانات تجريبية بدل نسخة API الحالية.

## تصدير PDF الخاص

تصدير PDF الخاص عملية منفصلة عن نشر الموقع العام. يمكن أن يستخدم مستودع عمليات خاصًا وGitHub Actions عبر `deploy/private-operations/export-private-pdf.yml`. لا تمنحه صلاحيات نشر Cloudflare للموقع العام. ثبّت `GitHub__ProductRef` على commit SHA كامل بدل `main`، واحفظ رموز الوصول في الخادم فقط.

## النسخ الاحتياطي

أنشئ `pg_dump -Fc` مشفر التخزين خارج المستودع، واختبر `pg_restore` دوريًا في قاعدة جديدة. لا تضع dump أو snapshot خاصة داخل Git أو artifacts عامة.
