# منصة السيرة الشخصية

منصة ثنائية اللغة لإدارة حقائق السيرة مرة واحدة، ثم انتقائها داخل عدة بروفايلات ومواقع. الهوية المهنية الأساسية: **Software Engineer | Flutter & .NET Backend**. كل البيانات المضمّنة في المستودع بيانات اختبار موسومة بوضوح، ولا تمثل خبرة أو إنجازًا حقيقيًا.

## المكونات

- `resume.API`: ASP.NET Core API محمي بمالك واحد، JWT قصير العمر، rate limiting، سجل تدقيق، نشر idempotent وhealth checks.
- `resume.Core` و`resume.infrastructure`: نموذج PostgreSQL وEF Core يدعم الترجمات، البروفايلات، المواقع، SEO ونسخ النشر الثابتة.
- `apps/admin`: لوحة React/Vite عربية/إنجليزية، RTL/LTR، وحالات تحميل/خطأ/فراغ.
- `apps/public`: موقع Next.js مُصدّر بالكامل كملفات ثابتة؛ لا يتصل بالـAPI وقت زيارة الجمهور.
- `packages/resume-template`: معاينة HTML وقالب PDF أحادي العمود مناسب لـATS مع فحص آلي للنص والخطوط.

## التشغيل المحلي

المتطلبات: .NET SDK 10، Node.js 24، Docker، Python 3.12 وPoppler (`pdftotext`, `pdffonts`, `pdfinfo`, `pdftoppm`).

```bash
cp .env.example .env
docker compose up -d
dotnet tool restore
dotnet ef database update --project resume.infrastructure --startup-project resume.API
BOOTSTRAP_ADMIN_EMAIL='owner@example.invalid' BOOTSTRAP_ADMIN_PASSWORD='replace-with-a-strong-local-password' dotnet run --project resume.API -- --bootstrap-owner
dotnet run --project resume.API --urls http://127.0.0.1:5080
```

في نافذتين أخريين:

```bash
npm ci
npm --workspace @resume/admin run dev
npm --workspace @resume/public run dev
```

لوحة التحكم: `http://127.0.0.1:5173`، والموقع: `http://127.0.0.1:3000`. وكيل Vite يربط `/api` بالمنفذ 5080 محليًا.

## التحقق الكامل

```bash
dotnet build my_resume_backend.sln --no-restore --maxcpucount:1 -p:UseSharedCompilation=false
dotnet test tests/resume.Tests/resume.Tests.csproj --no-build --no-restore --maxcpucount:1
python3 -m venv .venv
.venv/bin/pip install -r packages/resume-template/requirements.txt
npm run pdf:generate
npm run pdf:qa
npm run build
npm run test
```

للتوليد المحلي دون إنشاء venv يمكن ضبط `PDF_PYTHON_BIN` إلى Python يحتوي حزم `requirements.txt`. في CI تُثبت المتطلبات صراحةً ويُستخدم Node 24.

تفاصيل التصميم والنشر والأمان وفحص PDF ونتائج القبول في [docs/ARCHITECTURE_AR.md](docs/ARCHITECTURE_AR.md)، [docs/DEPLOYMENT_AR.md](docs/DEPLOYMENT_AR.md)، [docs/SECURITY_AR.md](docs/SECURITY_AR.md)، [docs/PDF_QA_AR.md](docs/PDF_QA_AR.md)، و[docs/QA_RESULTS_AR.md](docs/QA_RESULTS_AR.md).
