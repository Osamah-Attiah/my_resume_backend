# Personal Resume Platform

A bilingual platform for managing resume facts once and selecting them for multiple profiles and websites. The professional identity represented by the platform is **Software Engineer | Flutter & .NET Backend**. All data included in this repository is clearly marked as test data and does not represent real experience or achievements.

## Components

- `resume.API`: An ASP.NET Core API for a single owner, with short-lived JWTs, rate limiting, audit logging, idempotent publishing, and health checks.
- `resume.Core` and `resume.infrastructure`: The PostgreSQL and EF Core model for translations, profiles, websites, SEO, and static publication snapshots.
- `apps/admin`: A React/Vite dashboard in Arabic and English, with RTL/LTR layouts and loading, error, and empty states.
- `apps/public`: A Next.js site exported as static files. Project lists and detail pages refresh from the public API; the published snapshot remains available if the API is offline.
- `packages/resume-template`: An HTML preview and ATS-friendly, single-column PDF template, with automated text and font checks.

## Local development

Requirements: .NET SDK 10, Node.js 24, Docker, Python 3.12, and Poppler (`pdftotext`, `pdffonts`, `pdfinfo`, and `pdftoppm`).

```bash
cp .env.example .env
docker compose up -d
dotnet tool restore
dotnet ef database update --project resume.infrastructure --startup-project resume.API
BOOTSTRAP_ADMIN_EMAIL='owner@example.invalid' BOOTSTRAP_ADMIN_PASSWORD='replace-with-a-strong-local-password' dotnet run --project resume.API -- --bootstrap-owner
dotnet run --project resume.API --urls http://127.0.0.1:5080
```

In two other terminals, start the admin dashboard and public site:

```bash
npm ci
npm --workspace @resume/admin run dev
PUBLIC_API_BASE_URL=http://127.0.0.1:5080 npm --workspace @resume/public run dev
```

The admin dashboard runs at `http://127.0.0.1:5173`, and the public site runs at `http://127.0.0.1:3000`. The Vite proxy routes `/api` to port 5080 locally.

## Production publishing

Publish the public site and its resume PDFs from the admin dashboard through `azure-pipelines.yml` in Azure DevOps. The pipeline builds from the current immutable publication snapshot and deploys to Cloudflare Pages. GitHub Actions runs CI checks only; it does not deploy the public site. See [Deployment](docs/DEPLOYMENT_AR.md) and [project instructions](AGENTS.md).

## Full verification

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

To generate PDFs locally without creating a virtual environment, set `PDF_PYTHON_BIN` to a Python interpreter with the packages listed in `requirements.txt` installed. CI installs these requirements explicitly and uses Node.js 24.

For more on the design, deployment, security, PDF checks, and acceptance results, see [Architecture](docs/ARCHITECTURE_AR.md), [Deployment](docs/DEPLOYMENT_AR.md), [Security](docs/SECURITY_AR.md), [PDF QA](docs/PDF_QA_AR.md), and [QA Results](docs/QA_RESULTS_AR.md). These documents are currently written in Arabic.
