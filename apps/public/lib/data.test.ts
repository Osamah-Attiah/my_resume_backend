import { describe, expect, it } from "vitest";
import { demoSnapshot } from "@resume/contracts";
import { browserProfile } from "./data";

describe("browser profile", () => {
  it("does not serialize PDF-only content into the hydrated public page", () => {
    const profile = { ...demoSnapshot.profiles.en, pdfDocument: { ...demoSnapshot.profiles.en, summary: "PDF_ONLY_PRIVATE_COPY" } };
    const browser = browserProfile(profile);
    expect(browser).not.toHaveProperty("pdfDocument");
    expect(JSON.stringify(browser)).not.toContain("PDF_ONLY_PRIVATE_COPY");
  });
});
