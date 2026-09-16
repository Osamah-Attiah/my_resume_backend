// Development uses Vite's same-origin proxy. Production sets VITE_API_BASE_URL
// to the deployed API origin, keeping the public static site API-independent.
const baseUrl = (import.meta.env.VITE_API_BASE_URL ?? "").replace(/\/$/, "");
const accessTokenKey = "resume-admin.access-token";
export const authExpiredEvent = "resume-admin.auth-expired";
let accessToken: string | null = readStoredAccessToken();

export class ApiError extends Error {
  constructor(public status: number, public code: string, message: string, public errors?: Array<{ path: string; message: string }>) { super(message); }
}

export function getAccessToken() { return accessToken; }

export function setAccessToken(value: string | null) {
  accessToken = value;
  try {
    if (value) window.sessionStorage.setItem(accessTokenKey, value);
    else window.sessionStorage.removeItem(accessTokenKey);
  } catch {
    // Storage can be unavailable in privacy-restricted browser contexts. The
    // in-memory token still keeps the current page usable in that case.
  }
}

export async function downloadApiFile(path: string): Promise<Blob> {
  const response = await fetch(`${baseUrl}${path}`, { headers: accessToken ? { Authorization: `Bearer ${accessToken}` } : {} });
  if (!response.ok) throw await toApiError(response, "DOWNLOAD_FAILED", "تعذر تنزيل الملف");
  return response.blob();
}

export async function uploadApiFile<T>(path: string, form: FormData): Promise<T> {
  const response = await fetch(`${baseUrl}${path}`, { method: "POST", body: form, headers: accessToken ? { Authorization: `Bearer ${accessToken}` } : {} });
  const body = await response.json().catch(() => ({}));
  if (!response.ok) {
    if (response.status === 401) expireAccessToken();
    throw new ApiError(response.status, body.code ?? "UPLOAD_FAILED", body.title ?? "تعذر رفع الصورة", body.errors);
  }
  return body as T;
}

export async function api<T>(path: string, options: RequestInit = {}): Promise<T> {
  const controller = new AbortController();
  const timeout = window.setTimeout(() => controller.abort(), 35_000);
  try {
    const response = await fetch(`${baseUrl}${path}`, {
      ...options,
      signal: controller.signal,
      headers: { "Content-Type": "application/json", ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}), ...options.headers }
    });
    if (response.status === 204) return undefined as T;
    const body = await response.json().catch(() => ({}));
    if (!response.ok) {
      if (response.status === 401) expireAccessToken();
      throw new ApiError(response.status, body.code ?? "REQUEST_FAILED", body.title ?? "تعذر إكمال الطلب", body.errors);
    }
    return body as T;
  } catch (error) {
    if (error instanceof ApiError) throw error;
    if (error instanceof DOMException && error.name === "AbortError") throw new ApiError(408, "API_WAKE_TIMEOUT", "استغرق اتصال الخادم وقتًا أطول من المتوقع. حاول مجددًا؛ قد تكون الخدمة المجانية في طور الاستيقاظ.");
    throw new ApiError(0, "NETWORK_ERROR", "تعذر الاتصال بالخادم. احتفظنا بما كتبته في هذه الصفحة؛ تحقق من الاتصال وحاول مجددًا.");
  } finally { window.clearTimeout(timeout); }
}

function readStoredAccessToken(): string | null {
  try { return window.sessionStorage.getItem(accessTokenKey); }
  catch { return null; }
}

function expireAccessToken() {
  setAccessToken(null);
  window.dispatchEvent(new Event(authExpiredEvent));
}

async function toApiError(response: Response, fallbackCode: string, fallbackMessage: string) {
  const body = await response.json().catch(() => ({}));
  if (response.status === 401) expireAccessToken();
  return new ApiError(response.status, body.code ?? fallbackCode, body.title ?? fallbackMessage, body.errors);
}
