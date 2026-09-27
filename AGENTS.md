# Project deployment instructions

- Public site publication, including its generated resume PDFs, always runs through Azure DevOps using `azure-pipelines.yml`.
- Start a public publication from the admin dashboard's **Sites and publishing** page. The API creates an immutable snapshot and queues the Azure pipeline with `PUBLICATION_ID` and `ATTEMPT_ID`.
- The Azure pipeline validates the snapshot, generates the PDFs and static site, and deploys `apps/public/out` to the production Cloudflare Pages project. Verify the Azure run, the admin publication status, the live site, and the live PDFs before reporting success.
- Never use GitHub Actions to publish the public site or its PDFs. The GitHub `CI` workflow is for checks only. A separate private PDF export workflow may use GitHub Actions; it does not deploy the public site.
- If Azure publishing is unavailable, report the blocker. Do not switch the public deployment to GitHub Actions or upload a local fixture snapshot as a substitute.
