# النشر المجاني

البنية المقترحة: Neon Free لـPostgreSQL، Render Free لـ`resume.API`، ومشروع Cloudflare Pages مستقل لكل Site عام. لوحة الإدارة يمكن أن تكون مشروع Pages منفصلًا. الموقع العام وPDF ملفات ثابتة ولا ينتظران استيقاظ Render أو Neon.

## 1. مستودعات GitHub

1. أنشئ مستودع المنتج الذي يحوي هذا المشروع وWorkflow العام `.github/workflows/publish-site.yml`.
2. أنشئ مستودع عمليات **خاصًا** وانسخ إليه `deploy/private-operations/export-private-pdf.yml` تحت `.github/workflows/export-private-pdf.yml`.
3. ثبّت `GitHub__ProductRef` على commit SHA كامل (40 أو 64 محرفًا سداسيًا)، وليس `main`. يرفض API تفعيل التصدير الخاص بمرجع متحرك.
4. استخدم fine-grained token محدودًا بالمستودعين وبصلاحيات Actions المطلوبة فقط. يفضّل فصل credential النشر العام عن الخاص عند الإعداد الفعلي.

## 2. متغيرات Render

- `ConnectionStrings__DefaultConnection`: سلسلة Neon مع SSL.
- `Auth__SigningKey`: 32 بايت عشوائية على الأقل؛ يبدأ Production بالفشل إذا غابت.
- `Auth__Issuer=resume-api` و`Auth__Audience=resume-admin`.
- `Cors__AdminOrigin`: أصل لوحة الإدارة HTTPS فقط.
- `Publishing__CallbackSecret`: قيمة عشوائية تطابق سر GitHub أدناه.
- `GitHub__Owner`, `GitHub__PublishRepository`, `GitHub__PublishWorkflow=publish-site.yml`, `GitHub__PublishRef=main`.
- `GitHub__PrivateExportRepository`, `GitHub__PrivateExportWorkflow=export-private-pdf.yml`, `GitHub__PrivateExportRef=main`.
- `GitHub__ProductRepository=owner/product-repository`, و`GitHub__ProductRef=<pinned-commit-sha>`.
- `GitHub__Token`: يبقى على الخادم فقط.
- اختياري للصور العامة: `Cloudinary__CloudName`, `Cloudinary__ApiKey`, `Cloudinary__ApiSecret`.

يوفر `render.yaml` أسماء هذه المتغيرات بلا قيم سرية. نفّذ migrations من بيئة موثوقة، ثم bootstrap للمالك مرة واحدة بمتغيري `BOOTSTRAP_ADMIN_EMAIL` و`BOOTSTRAP_ADMIN_PASSWORD` واحذفهما فورًا.

## 3. أسرار GitHub Actions

في مستودع المنتج والمستودع الخاص، حسب الحاجة:

- `RESUME_API_ORIGIN`: أصل API بـHTTPS دون شرطة أخيرة.
- `RESUME_PUBLISHING_CALLBACK_SECRET`: يطابق `Publishing__CallbackSecret`.
- في مستودع المنتج فقط: `CLOUDFLARE_API_TOKEN` محدود إلى Pages و`CLOUDFLARE_ACCOUNT_ID`.

اسم مشروع Pages يؤخذ من `deploymentTargetKey` داخل snapshot الموقع. كل Site يحتاج مشروع Pages مطابقًا، ولا توجد credential داخل snapshot.

## 4. الموقع ولوحة الإدارة

- ابنِ لوحة الإدارة مع `VITE_API_BASE_URL=https://api.example.com` و`VITE_PUBLIC_SITE_URL=https://site.example.com`. هذه عناوين عامة وليست أسرارًا.
- حدّث `BaseUrl` وSEO و`deploymentTargetKey` لكل Site من اللوحة، ثم اطلب النشر.
- Workflow يسحب snapshot ثابتًا، يولّد PDF، يشغّل QA والبناء وماسح الأسرار، ينشر output كاملًا، ثم يسجل hashes وأدلة النشر.
- يسجل Workflow رقم GitHub run وحالات `building/validating/deploying`. إذا نجح الرفع وضاع callback، زر «تحديث الحالة» يسترجع النتيجة من evidence artifact المحمي ذي retention سبعة أيام، دون إعادة نشر عمياء.
- تغيير slug أو سحب محتوى عام يولّد output كاملًا جديدًا. راجع واحذف preview deployments القديمة من حساب Cloudflare عند الحاجة.

## 5. فحص الإنتاج الإلزامي

بعد أول نشر حقيقي: أوقف API مؤقتًا، وافتح العربية والإنجليزية وPDF و404 وrobots وsitemap من نطاق Pages. افحص headers، canonical وhreflang وredirects، ثم اربط Search Console وقدم sitemap. لا يُنفذ تحقق الملكية دون حساب المالك.

## النسخ الاحتياطي

أنشئ `pg_dump -Fc` مشفر التخزين خارج المستودع، واختبر `pg_restore` دوريًا في قاعدة جديدة قبل الاعتماد عليه. لا تضع dump أو snapshot خاصة داخل Git أو artifacts عامة.
