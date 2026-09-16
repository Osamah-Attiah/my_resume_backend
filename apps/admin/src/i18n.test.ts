import { describe, expect, it } from "vitest";
import { localizeAdminError, localizeAdminMessage } from "./i18n";

const arabic = (ar: string, _en: string) => ar;
const english = (_ar: string, en: string) => en;

describe("admin localization", () => {
  it("translates validation failures in both interface languages", () => {
    const error = { code: "VALIDATION_FAILED", message: "Validation failed" };
    expect(localizeAdminError(error, arabic)).toContain("فشل التحقق");
    expect(localizeAdminError(error, english)).toContain("Validation failed");
  });

  it("does not expose an unknown error as an empty message", () => {
    expect(localizeAdminError({ code: "UNKNOWN", message: "Fallback message" }, english)).toBe("Fallback message");
    expect(localizeAdminError({}, arabic)).toContain("تعذر إكمال الطلب");
  });

  it("localizes publication history summaries", () => {
    const summary = "No final workflow callback arrived before the attempt lease expired.";
    expect(localizeAdminMessage(summary, arabic)).toContain("لم تصل النتيجة النهائية");
    expect(localizeAdminMessage("GitHub Actions completed with conclusion 'failure'.", arabic)).toContain("انتهى سير عمل GitHub");
  });
});
