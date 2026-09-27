import { createReadStream } from "node:fs";
import { stat } from "node:fs/promises";
import { createServer } from "node:http";
import { extname, relative, resolve, sep } from "node:path";
import { fileURLToPath } from "node:url";

const root = fileURLToPath(new URL("../out/", import.meta.url));
const port = Number(process.env.PORT || 4180);
const types = new Map([
  [".html", "text/html; charset=utf-8"],
  [".css", "text/css; charset=utf-8"],
  [".js", "text/javascript; charset=utf-8"],
  [".json", "application/json; charset=utf-8"],
  [".txt", "text/plain; charset=utf-8"],
  [".xml", "application/xml; charset=utf-8"],
  [".svg", "image/svg+xml"],
  [".png", "image/png"],
  [".jpg", "image/jpeg"],
  [".jpeg", "image/jpeg"],
  [".webp", "image/webp"],
  [".avif", "image/avif"],
  [".ico", "image/x-icon"],
  [".woff", "font/woff"],
  [".woff2", "font/woff2"],
  [".pdf", "application/pdf"],
]);

createServer(async (request, response) => {
  response.setHeader("Cache-Control", "no-store, max-age=0");
  response.setHeader("Pragma", "no-cache");
  response.setHeader("Expires", "0");

  if (request.method !== "GET" && request.method !== "HEAD") {
    response.writeHead(405, { Allow: "GET, HEAD" }).end();
    return;
  }

  let pathname;
  try {
    pathname = decodeURIComponent(new URL(request.url ?? "/", "http://localhost").pathname);
  } catch {
    response.writeHead(400).end();
    return;
  }

  const path = resolve(root, `.${pathname}`);
  const rel = relative(root, path);
  if (rel === ".." || rel.startsWith(`..${sep}`)) {
    response.writeHead(403).end();
    return;
  }

  try {
    const info = await stat(path);
    if (info.isDirectory() && !pathname.endsWith("/")) {
      response.writeHead(308, { Location: `${pathname}/${new URL(request.url ?? "/", "http://localhost").search}` }).end();
      return;
    }

    const file = info.isDirectory() ? resolve(path, "index.html") : path;
    const fileInfo = info.isDirectory() ? await stat(file) : info;
    if (!fileInfo.isFile()) throw new Error("Not a file");

    response.writeHead(200, {
      "Content-Type": types.get(extname(file).toLowerCase()) ?? "application/octet-stream",
      "Content-Length": fileInfo.size,
    });
    if (request.method === "HEAD") response.end();
    else createReadStream(file).pipe(response);
  } catch {
    response.writeHead(404).end();
  }
}).listen(port, "127.0.0.1", () => {
  console.log(`Public preview: http://127.0.0.1:${port}/ar/`);
});
