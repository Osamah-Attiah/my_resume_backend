"use client";

import { createContext, useCallback, useContext, useEffect, useLayoutEffect, useMemo, useRef, useState, type ReactNode, type CSSProperties } from "react";
import {
  AnimatePresence,
  MotionConfig,
  motion,
  useScroll,
  useSpring,
  useTransform,
  useReducedMotion,
  useMotionValue,
  useMotionValueEvent,
  useMotionTemplate,
  useVelocity,
  useAnimationFrame,
  useInView,
  type MotionValue,
} from "framer-motion";
import Lenis from "lenis";
import { ArrowUpRight, Download, Mail, Phone, Check, Wifi, WifiOff, CreditCard, Wallet, Database, Server, ShieldCheck, Smartphone } from "lucide-react";
import "./portfolio.css";
import { storyFor, type PublicProfile, type PublicProject } from "@resume/contracts";
import { useLiveProjects } from "../../lib/live-projects";
import { portfolioProject, profilePath, resumePath, type PortfolioProject } from "../../lib/portfolio";

/* ------------------------------------------------------------------ */
/* DATA                                                                */
/* ------------------------------------------------------------------ */

/* The same backend contract as the original site:                    */
/* - the API publication supplies the profile at build time            */
/* - projects are re-fetched live from the public API, every 15s       */

export type Lang = "en" | "ar";
type BackendProject = PublicProject;
type BackendProfile = PublicProfile;
type LiveStatus = "syncing" | "live" | "cached";

function useMedia(q: string) {
  const [m, setM] = useState(false);
  useEffect(() => {
    const mq = window.matchMedia(q);
    const f = () => setM(mq.matches);
    f();
    mq.addEventListener("change", f);
    return () => mq.removeEventListener("change", f);
  }, [q]);
  return m;
}

/* ------------------------------------------------------------------ */
/* UI STRINGS                                                          */
/* ------------------------------------------------------------------ */

const STRINGS = {
  en: {
    hint: "Just one tap. Scroll down to see what it sets off.",
    cv: "Download my CV",
    say: "Email me",
    write: "Email",
    call: "Call",
    view: "View",
    methodLabel: "How I work",
    method: [
      { a: "First, I try to", b: "understand the job.", body: "Before I write any code, I talk to the people who will use the app. How do they do this today, and where does it go wrong?" },
      { a: "Then I put the", b: "steps in order.", body: "I work out what happens at each step and why. That keeps the app easy to use, and easy for whoever works on the code after me." },
    ],
    steps: ["Understand", "Plan", "Build", "Test", "Ship"],
    layers: [
      { label: "Interface", note: "what's on the screen" },
      { label: "State", note: "what's going on right now" },
      { label: "Local data", note: "what's still there after the app closes" },
    ],
    tap: { payment: "Payment", choose: "Choose a method", bank: "Bank account", wallet: "Wallet", cont: "Continue", sending: "Sending\u2026", ok: "Confirmed", illus: "example screen", result: "200 OK / result shown" },
    ui: { bankPay: "Bank payment", walletPay: "Wallet payment", saved: "Draft saved on the phone", exploded: "the same screen, pulled apart" },
    confirmed: "Payment confirmed",
    online: "Online",
    offline: "No signal, saving locally",
    server: "Server",
    queued: "waiting",
    device: "field phone",
    queue: "waiting to send (WorkManager)",
    workEyebrow: "Things I've worked on",
    workTitle: "Real projects, and what I did on each.",
    live: { syncing: "updating\u2026", live: "LIVE \u00b7 up to date", cached: "saved copy" },
    swipe: "Swipe",
    expEyebrow: "Where I've worked",
    years: (n: number): [string, string] => [`${n}+ years`, "with Flutter."],
    contactFallback: "Contact",
    contactTitle: "Got a project in mind? I'd like to hear about it.",
    footer: "Thanks for scrolling this far.",
    present: "Present",
    hello: "Hello",
    chapters: { method: "Understand", build: "Build", connect: "Server", offline: "Offline", work: "Work", experience: "Experience", contact: "Contact", layers: "Layers", backend: "Backend", integration: "Integration" } as Record<string, string>,
  },
  ar: {
    hint: "ضغطة واحدة فقط. انزل قليلًا لترى ما يحدث بعدها.",
    cv: "حمّل سيرتي الذاتية",
    say: "راسلني",
    write: "راسلني",
    call: "اتصل",
    view: "عرض",
    methodLabel: "طريقتي في العمل",
    method: [
      { a: "أول ما أفعله", b: "أن أفهم العمل.", body: "قبل أن أكتب أي سطر، أتحدث مع من سيستخدم التطبيق: كيف ينجز عمله اليوم؟ وأين يتعطّل؟" },
      { a: "بعدها أرتّب", b: "الخطوات واحدة واحدة.", body: "أحدد ما يحدث في كل خطوة ولماذا، فيبقى التطبيق سهلًا لمن يستخدمه، وسهلًا لمن يعدّل عليه بعدي." },
    ],
    steps: ["أفهم", "أرتّب", "أبني", "أجرّب", "أسلّم"],
    layers: [
      { label: "الواجهة", note: "ما تراه على الشاشة" },
      { label: "الحالة", note: "ما يحدث في هذه اللحظة" },
      { label: "البيانات المحلية", note: "ما يبقى حتى لو أُغلق التطبيق" },
    ],
    tap: { payment: "الدفع", choose: "اختر طريقة الدفع", bank: "حساب بنكي", wallet: "محفظة", cont: "متابعة", sending: "جارٍ الإرسال\u2026", ok: "تم", illus: "شاشة للتوضيح", result: "200 OK / result shown" },
    ui: { bankPay: "دفع بنكي", walletPay: "دفع بالمحفظة", saved: "حُفظت المسودة على الجهاز", exploded: "الشاشة نفسها مفكّكة" },
    confirmed: "تم الدفع",
    online: "متصل",
    offline: "بلا اتصال، نحفظ محليًا",
    server: "الخادم",
    queued: "بالانتظار",
    device: "جهاز ميداني",
    queue: "بانتظار الإرسال (WorkManager)",
    workEyebrow: "أعمال شاركت فيها",
    workTitle: "مشاريع حقيقية، ودوري في كل منها.",
    live: { syncing: "جارٍ التحديث\u2026", live: "مباشر \u00b7 محدّث", cached: "آخر نسخة محفوظة" },
    swipe: "اسحب",
    expEyebrow: "أين عملت",
    years: (n: number): [string, string] => [`أكثر من ${n} ${n <= 10 ? "سنوات" : "سنة"}`, "مع Flutter."],
    contactFallback: "تواصل",
    contactTitle: "عندك مشروع؟ يسعدني أن نتحدث عنه.",
    footer: "شكرًا لأنك وصلت إلى هنا.",
    present: "حتى الآن",
    hello: "أهلًا",
    chapters: { method: "الفهم", build: "البناء", connect: "الخادم", offline: "بلا اتصال", work: "الأعمال", experience: "الخبرة", contact: "تواصل", layers: "طبقات التطبيق", backend: "الباك إند", integration: "التكامل" } as Record<string, string>,
  },
};
type Strings = (typeof STRINGS)["en"];

type Project = PortfolioProject;
type Job = { company: string; role: string; summary: string; products: string[]; dates: string; location?: string | null };
type Scene = { key: string; stage: string; title: string; body: string; detail?: string | null; label: string; project?: string };
type View = ReturnType<typeof toView>;

const EN_FMT = new Intl.DateTimeFormat("en-US", { month: "short", year: "numeric" });
const AR_FMT = new Intl.DateTimeFormat("ar-SA-u-ca-gregory-nu-latn", { month: "short", year: "numeric" });
function fmtMonth(v: string | null | undefined, lang: Lang) {
  if (!v) return STRINGS[lang].present;
  const [y, m] = v.split("-").map(Number);
  if (!y) return v;
  return (lang === "ar" ? AR_FMT : EN_FMT).format(new Date(y, (m || 1) - 1, 1));
}

type Env = { ready: boolean; profileSlug: string; isDefault: boolean; lang: Lang; mobile: boolean; fine: boolean; live: { status: LiveStatus; syncedAt: Date | null }; switchLang: (l: Lang) => void };

function toView(pr: BackendProfile, liveProjects: BackendProject[] | null, env: Env) {
  const { lang } = env;
  const t: Strings = STRINGS[lang];
  const parts = pr.headline.split("|").map((x) => x.trim()).filter(Boolean);
  const role = parts.find((x) => /senior|flutter/i.test(x)) ?? parts[0] ?? "";
  const roleExtra = parts.filter((x) => x !== role && !/^software developer$|^مطور برمجيات$/i.test(x)).join(" / ");
  const src = liveProjects ?? pr.projects;
  const projects = src.map((project) => portfolioProject(project, lang, pr.slug));
  const nameBySlug = new Map(src.map((p) => [p.slug, p.name]));
  const exps = [...pr.experiences].sort((a, b) => (b.startDate || "").localeCompare(a.startDate || ""));
  const experience: Job[] = exps.map((e) => {
    const text = [e.summary ?? "", ...(e.highlights ?? [])].join(" ").toLowerCase();
    return {
      company: e.organization,
      role: e.jobTitle,
      summary: e.summary ?? "",
      products: src.filter((p) => text.includes(p.name.toLowerCase())).map((p) => p.name),
      dates: `${fmtMonth(e.startDate, lang)} \u2013 ${fmtMonth(e.endDate, lang)}`,
      location: e.location,
    };
  });
  const starts = pr.experiences.map((e) => e.startDate).filter(Boolean).sort();
  const first = starts[0] ? new Date(`${starts[0].slice(0, 7)}-01T00:00:00Z`) : null;
  const years = first ? Math.max(1, Math.floor((Date.now() - first.getTime()) / (365.25 * 864e5))) : 0;
  const scenes = new Map<string, Scene>();
  (storyFor(pr, env.isDefault) ?? []).forEach((st) => {
    if (scenes.has(st.stage)) return;
    scenes.set(st.stage, { key: st.key, stage: st.stage, title: st.title, body: st.body, detail: st.detail, label: st.visualLabel || t.chapters[st.stage] || st.stage.replace("-", " "), project: st.projectSlug ? nameBySlug.get(st.projectSlug) : undefined });
  });
  const categories = Array.from(new Set(pr.skills.map((k) => k.category)));
  return {
    ...env,
    rtl: lang === "ar",
    t,
    name: pr.fullName,
    role,
    roleExtra,
    intro: pr.summary,
    email: pr.email ?? "",
    phone: pr.phone ?? "",
    cvUrl: resumePath(pr.slug, lang),
    workflowSteps: t.steps,
    projects,
    experience,
    years,
    about: scenes.get("intro")?.body ?? pr.seo?.description ?? "",
    teamScope: scenes.get("backend-focus")?.detail ?? "",
    scene: (stage: string) => scenes.get(stage),
    skills: pr.skills.map((k) => k.name),
    categories,
  };
}

const ProfileCtx = createContext<View | null>(null);
function useD(): View {
  const v = useContext(ProfileCtx);
  if (!v) throw new Error("ProfileCtx missing");
  return v;
}

function splitTitle(t: string): [string, string] {
  const w = t.replace(/\.$/, "").split(" ");
  if (w.length < 3) return [t, ""];
  const k = Math.ceil(w.length / 2);
  return [w.slice(0, k).join(" "), w.slice(k).join(" ") + (t.endsWith(".") ? "." : "")];
}

/** Renders whichever of the given story stages exist, spread evenly across the pinned scroll. */
function StoryBeats({ p, n, stages, dark, accent, size = "clamp(36px,4.6vw,64px)" }: { p: MotionValue<number>; n: string; stages: string[]; dark?: boolean; accent?: string; size?: string }) {
  const D = useD();
  const list = stages.map((x) => D.scene(x)).filter(Boolean) as Scene[];
  return (
    <>
      {list.map((sc, i) => {
        const [a, b] = splitTitle(sc.title);
        return (
          <Beat key={sc.key} p={p} at={[i / list.length, (i + 1) / list.length]} className="[grid-area:1/1] md:absolute md:inset-0">
            <Eyebrow n={n} dark={dark}>{sc.label}</Eyebrow>
            <h2 className="mt-5 font-bold leading-[1] tracking-[-0.04em]" style={{ fontSize: size, color: dark ? C.cream : C.ink }}>
              {a}
              {b && <><br /><span style={{ color: accent ?? (dark ? C.lime : C.forest) }}>{b}</span></>}
            </h2>
            <p className="mt-5 max-w-[440px] text-[16px] leading-relaxed" style={{ color: dark ? "rgba(242,245,239,0.7)" : C.muted }}>{sc.body}</p>
            {sc.project && (
              <span className="mt-4 inline-flex items-center gap-2 rounded-full border px-3 py-1 font-mono text-[11px]" style={{ borderColor: dark ? "rgba(191,216,197,0.35)" : C.soft, color: dark ? C.lime : C.forest }}>
                <ArrowUpRight size={12} /> {sc.project}
              </span>
            )}
          </Beat>
        );
      })}
    </>
  );
}

const hasAny = (D: View, stages: string[]) => stages.some((x) => D.scene(x));

const C = { cream: "#f2f5ef", paper: "#fcfdf9", ink: "#12251a", muted: "#5e6d64", forest: "#245c42", soft: "#bfd8c5", lime: "#d8ee75", night: "#14221b", coral: "#db684b" };
const ease = [0.22, 1, 0.36, 1] as const;
const MONO = "'JetBrains Mono Variable', ui-monospace, monospace";

/* ------------------------------------------------------------------ */
/* PRIMITIVES                                                          */
/* ------------------------------------------------------------------ */

function Pinned({ height = 300, id, dark, children }: { height?: number; id: string; dark?: boolean; children: (p: MotionValue<number>) => ReactNode }) {
  const ref = useRef<HTMLDivElement>(null);
  const reduce = useReducedMotion();
  const env = useContext(ProfileCtx);
  if (env?.mobile) height = Math.round(height * 0.72);
  const { scrollYProgress } = useScroll({ target: ref, offset: ["start start", "end end"] });
  const smooth = useSpring(scrollYProgress, { stiffness: 140, damping: 28, mass: 0.4 });
  const still = useMotionValue(0.5);
  return (
    <section id={id} ref={ref} style={{ height: reduce ? "auto" : `${height}vh`, background: dark ? C.night : "transparent" }} className="msf-pinned relative">
      <div className={reduce ? "relative min-h-[100dvh]" : "sticky top-0 h-[100dvh] overflow-hidden"}>{children(reduce ? still : smooth)}</div>
    </section>
  );
}

const GLYPHS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789<>/{}#$%&";

/** Decodes from random glyphs into the real text when it scrolls into view. Readable by default. */
function Scramble({ text, className, style, delay = 0 }: { text: string; className?: string; style?: CSSProperties; delay?: number }) {
  const ref = useRef<HTMLSpanElement>(null);
  const inView = useInView(ref, { once: true, margin: "-8% 0px" });
  const reduce = useReducedMotion();
  const [out, setOut] = useState(text);
  useEffect(() => {
    if (!inView || reduce || /[\u0600-\u06FF]/.test(text)) return;
    let frame = 0;
    const total = 18 + Math.min(text.length, 30);
    let id = 0;
    const start = window.setTimeout(() => {
      id = window.setInterval(() => {
        frame++;
        const revealed = ((frame - 4) / (total - 4)) * text.length;
        setOut(text.split("").map((ch, i) => (ch === " " || i < revealed ? ch : GLYPHS[(Math.random() * GLYPHS.length) | 0])).join(""));
        if (frame >= total) {
          window.clearInterval(id);
          setOut(text);
        }
      }, 32);
    }, delay * 1000);
    return () => {
      window.clearTimeout(start);
      window.clearInterval(id);
    };
  }, [inView, reduce, text, delay]);
  return (
    <span ref={ref} className={className} style={style} aria-label={text}>
      {out}
    </span>
  );
}

function Eyebrow({ n, children, dark }: { n: string; children: string; dark?: boolean }) {
  return (
    <div className="flex items-center gap-3 text-[11px] font-semibold uppercase tracking-[0.22em]" style={{ color: dark ? C.soft : C.forest }}>
      <span className="font-mono" style={{ color: dark ? C.lime : C.coral }}>{n}</span>
      <span className="h-px w-8" style={{ background: "currentColor", opacity: 0.4 }} />
      <Scramble text={children} />
    </div>
  );
}

function Beat({ p, at, children, className, style }: { p: MotionValue<number>; at: [number, number]; children: ReactNode; className?: string; style?: CSSProperties }) {
  const last = at[1] >= 1;
  const first = at[0] <= 0;
  const o = useTransform(p, [at[0], at[0] + 0.07, at[1] - 0.06, at[1]], [first ? 1 : 0, 1, 1, last ? 1 : 0]);
  const y = useTransform(p, [at[0], at[0] + 0.1, at[1] - 0.06, at[1]], [first ? 0 : 50, 0, 0, last ? 0 : -40]);
  const b = useTransform(p, [at[0], at[0] + 0.08, at[1] - 0.05, at[1]], [first ? 0 : 10, 0, 0, last ? 0 : 8]);
  const filter = useTransform(b, (v) => `blur(${v}px)`);
  return <motion.div className={`msf-beat ${className ?? ""}`} style={{ ...style, opacity: o, y, filter }}>{children}</motion.div>;
}

/** Pulls its child toward the cursor. */
function Magnetic({ children, strength = 0.35 }: { children: ReactNode; strength?: number }) {
  const ref = useRef<HTMLDivElement>(null);
  const x = useMotionValue(0);
  const y = useMotionValue(0);
  const sx = useSpring(x, { stiffness: 220, damping: 16, mass: 0.4 });
  const sy = useSpring(y, { stiffness: 220, damping: 16, mass: 0.4 });
  const env = useContext(ProfileCtx);
  if (env && !env.fine) return <div className="inline-block">{children}</div>;
  return (
    <motion.div
      ref={ref}
      style={{ x: sx, y: sy }}
      className="inline-block"
      onMouseMove={(e) => {
        const r = ref.current!.getBoundingClientRect();
        x.set((e.clientX - r.left - r.width / 2) * strength);
        y.set((e.clientY - r.top - r.height / 2) * strength);
      }}
      onMouseLeave={() => {
        x.set(0);
        y.set(0);
      }}
    >
      {children}
    </motion.div>
  );
}

/* ------------------------------------------------------------------ */
/* CURSOR                                                              */
/* ------------------------------------------------------------------ */

function Cursor() {
  const x = useMotionValue(-100);
  const y = useMotionValue(-100);
  const rx = useSpring(x, { stiffness: 380, damping: 32, mass: 0.5 });
  const ry = useSpring(y, { stiffness: 380, damping: 32, mass: 0.5 });
  const [hover, setHover] = useState<string | null>(null);
  const last = useRef<string | null>(null);
  useEffect(() => {
    const check = (el: Element | null) => {
      const t = (el as HTMLElement | null)?.closest?.("a,button,[data-cursor]") as HTMLElement | null;
      const next = t ? t.dataset.cursor ?? "" : null;
      if (next !== last.current) {
        last.current = next;
        setHover(next);
      }
    };
    const mv = (e: MouseEvent) => {
      x.set(e.clientX);
      y.set(e.clientY);
      check(e.target as Element);
    };
    const sc = () => check(document.elementFromPoint(x.get(), y.get()));
    window.addEventListener("mousemove", mv);
    window.addEventListener("scroll", sc, { passive: true });
    return () => {
      window.removeEventListener("mousemove", mv);
      window.removeEventListener("scroll", sc);
    };
  }, [x, y]);
  const size = hover === null ? 36 : hover ? 92 : 60;
  return (
    <>
      <motion.div className="pointer-events-none fixed left-0 top-0 z-[100]" style={{ x: rx, y: ry }}>
        <motion.div
          className="absolute grid -translate-x-1/2 -translate-y-1/2 place-items-center rounded-full border text-[10px] font-bold uppercase tracking-[0.14em]"
          animate={{ width: size, height: size, backgroundColor: hover !== null ? "rgba(216,238,117,0.92)" : "rgba(216,238,117,0)", borderColor: hover !== null ? "rgba(216,238,117,0)" : "rgba(36,92,66,0.55)" }}
          transition={{ type: "spring", stiffness: 300, damping: 24 }}
          style={{ color: C.night }}
        >
          {hover ? hover : null}
        </motion.div>
      </motion.div>
      <motion.div className="pointer-events-none fixed left-0 top-0 z-[101] h-1.5 w-1.5 -translate-x-1/2 -translate-y-1/2 rounded-full" style={{ x, y, background: C.forest }} />
    </>
  );
}

/* ------------------------------------------------------------------ */
/* HERO: the name is built from ~3,000 particles born from a single    */
/* tap. Move the cursor through it. Scroll and it collapses back into   */
/* the tap, which falls into chapter one.                              */
/* ------------------------------------------------------------------ */

/* HERO: the name rises letter by letter. Beside it, one tap on a      */
/* phone travels to the service and comes back as a clear result -     */
/* the whole story of the page, in four seconds.                       */

function TapDemo({ compact }: { compact?: boolean }) {
  const D = useD();
  const T = D.t.tap;
  // 0 idle -> 1 tap -> 2 sending -> 3 done ; loops
  const [phase, setPhase] = useState(0);
  useEffect(() => {
    const seq = [1400, 700, 1500, 2200];
    let i = 0;
    let t = 0;
    const step = () => {
      t = window.setTimeout(() => {
        i = (i + 1) % 4;
        setPhase(i);
        step();
      }, seq[i]);
    };
    step();
    return () => window.clearTimeout(t);
  }, []);
  const sending = phase === 2;
  const done = phase === 3;
  const body = (
    <div className="relative flex items-center gap-6">
      {/* phone */}
      <div className="relative h-[380px] w-[200px] rounded-[34px] border-[6px] p-4 shadow-[0_40px_80px_-40px_rgba(18,37,26,0.45)]" style={{ borderColor: C.ink, background: C.paper }}>
        <div className="mx-auto mb-5 h-1.5 w-14 rounded-full" style={{ background: C.ink, opacity: 0.85 }} />
        <div className="text-[10px] font-semibold uppercase tracking-[0.18em]" style={{ color: C.muted }}>{T.payment}</div>
        <div className="mt-1 text-[19px] font-bold leading-tight" style={{ color: C.ink }}>{T.choose}</div>
        {[T.bank, T.wallet].map((l, i) => (
          <div key={l} className="mt-3 flex items-center justify-between rounded-xl border px-3 py-2.5 text-[12px] font-semibold" style={{ borderColor: i === 0 ? C.forest : C.soft, color: C.ink }}>
            {l}
            {i === 0 && <Check size={13} style={{ color: C.forest }} />}
          </div>
        ))}
        <div className="relative mt-5">
          <motion.div
            animate={{ scale: phase === 1 ? 0.95 : 1, background: done ? C.lime : C.forest, color: done ? C.ink : C.cream }}
            transition={{ duration: 0.25 }}
            className="relative flex h-11 items-center justify-center overflow-hidden rounded-xl text-[13px] font-semibold"
          >
            <AnimatePresence mode="wait">
              <motion.span key={done ? "d" : sending ? "s" : "c"} initial={{ y: 14, opacity: 0 }} animate={{ y: 0, opacity: 1 }} exit={{ y: -14, opacity: 0 }} transition={{ duration: 0.25 }} className="flex items-center gap-1.5">
                {done ? <><Check size={14} /> {T.ok}</> : sending ? T.sending : T.cont}
              </motion.span>
            </AnimatePresence>
            {sending && <motion.span className="absolute bottom-0 left-0 h-[3px]" style={{ background: C.lime }} initial={{ width: "0%" }} animate={{ width: "100%" }} transition={{ duration: 1.4, ease: "linear" }} />}
          </motion.div>
          {/* finger tap */}
          <AnimatePresence>
            {phase === 1 && (
              <motion.span key="tap" className="pointer-events-none absolute left-1/2 top-1/2 h-10 w-10 -translate-x-1/2 -translate-y-1/2 rounded-full" style={{ background: C.coral }} initial={{ scale: 0.3, opacity: 0.7 }} animate={{ scale: 1.8, opacity: 0 }} exit={{ opacity: 0 }} transition={{ duration: 0.6, ease: "easeOut" }} />
            )}
          </AnimatePresence>
        </div>
        <div className="mt-4 text-center font-mono text-[9px]" style={{ color: C.muted }}>{T.illus}</div>
      </div>

      {/* the trip behind the tap */}
      <div className="flex flex-col gap-3">
        {["Flutter app", ".NET API", "PostgreSQL"].map((l, i) => {
          const lit = (sending && true) || done;
          return (
            <motion.div key={l} animate={{ opacity: lit ? 1 : 0.4, x: lit ? 0 : D.rtl ? 6 : -6 }} transition={{ delay: sending ? i * 0.4 : done ? 0 : 0, duration: 0.35 }} className="flex items-center gap-2 rounded-full border px-3 py-1.5 font-mono text-[11px]" style={{ borderColor: C.soft, background: C.paper, color: C.ink }}>
              <motion.span animate={{ background: lit ? (done ? C.forest : C.coral) : C.soft }} transition={{ delay: sending ? i * 0.4 : 0 }} className="h-2 w-2 rounded-full" />
              {l}
            </motion.div>
          );
        })}
        <motion.div dir="ltr" animate={{ opacity: done ? 1 : 0, y: done ? 0 : 6 }} className="font-mono text-[11px]" style={{ color: C.forest }}>{T.result}</motion.div>
      </div>
    </div>
  );
  if (!compact) return body;
  return (
    <div style={{ height: 300 }}>
      <div style={{ transform: "scale(0.78)", transformOrigin: D.rtl ? "top right" : "top left" }}>{body}</div>
    </div>
  );
}

function Hero() {
  const D = useD();
  const reduce = useReducedMotion();
  const { scrollY } = useScroll();
  const uiFade = useTransform(scrollY, [0, 480], [1, 0]);
  const lift = useTransform(scrollY, [0, 500], [0, -80]);
  const demoY = useTransform(scrollY, [0, 600], [0, 120]);
  const words = D.name.split(" ").filter(Boolean).slice(0, 3);
  return (
    <section className="relative flex min-h-[100dvh] items-center overflow-hidden px-[6vw] pb-16 pt-28 lg:py-0">
      <motion.div style={{ opacity: D.ready ? uiFade : 1, y: D.ready ? demoY : 0 }} initial={{ opacity: 0 }} animate={{ opacity: 1 }} transition={{ delay: 0.6, duration: 0.8 }} className="absolute end-[6vw] top-1/2 z-10 hidden -translate-y-1/2 lg:block">
        <TapDemo />
      </motion.div>

      <motion.div style={{ opacity: D.ready ? uiFade : 1, y: D.ready ? lift : 0 }} className="relative z-10 w-full max-w-[1000px]">
        <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.1, duration: 0.8, ease }}>
          <Eyebrow n="00">{D.scene("intro")?.title ?? D.roleExtra}</Eyebrow>
        </motion.div>
        <h1 className="mt-6 text-[clamp(60px,9vw,140px)] font-extrabold leading-[0.92] tracking-[-0.05em]" style={{ fontFamily: D.rtl ? AR_FONT : LATIN_FONT }} aria-label={D.name}>
          {words.map((w, wi) => (
            <span key={wi} className="block overflow-hidden pb-[0.06em]" style={{ color: wi % 2 ? C.forest : C.ink }} aria-hidden>
              {(D.rtl ? [w] : w.split("")).map((ch, ci) => (
                <motion.span key={ci} className="inline-block" initial={reduce ? false : { y: "105%" }} animate={{ y: "0%" }} transition={{ delay: 0.15 + wi * 0.18 + ci * 0.035, duration: 0.8, ease }}>
                  {ch}
                </motion.span>
              ))}
              {wi === words.length - 1 && (
                <motion.span className="inline-block" style={{ color: C.lime }} initial={reduce ? false : { scale: 0 }} animate={{ scale: 1 }} transition={{ delay: 0.9, type: "spring", stiffness: 400, damping: 14 }}>.</motion.span>
              )}
            </span>
          ))}
        </h1>
        <motion.p initial={{ opacity: 0, y: 16 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.75, duration: 0.8, ease }} className="mt-6 text-[clamp(18px,2vw,26px)] font-semibold tracking-[-0.01em] lg:max-w-[600px]" style={{ color: C.ink }}>
          {D.role} {D.roleExtra && <span className="font-medium" style={{ color: C.muted }}>+ {D.roleExtra}</span>}
        </motion.p>
        <motion.p initial={{ opacity: 0, y: 16 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.85, duration: 0.8, ease }} className="mt-3 line-clamp-3 max-w-[520px] text-[17px] leading-relaxed" style={{ color: C.muted }}>
          {D.intro}
        </motion.p>
        <motion.div initial={{ opacity: 0, y: 16 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.95, duration: 0.8, ease }} className="mt-8 flex flex-wrap items-center gap-3">
          <Magnetic>
            <a href={D.cvUrl} download target="_blank" rel="noreferrer" data-cursor="CV" className="inline-flex min-h-[44px] items-center gap-2 rounded-full px-6 py-3.5 text-sm font-semibold" style={{ background: C.forest, color: C.cream }}>
              <Download size={16} /> {D.t.cv}
            </a>
          </Magnetic>
          <Magnetic>
            <a href={`mailto:${D.email}`} data-cursor={D.t.say} className="inline-flex min-h-[44px] max-w-full items-center gap-2 rounded-full border px-6 py-3.5 text-sm font-semibold" style={{ borderColor: C.ink, color: C.ink }}>
              <Mail size={16} /> {D.email}
            </a>
          </Magnetic>
          <span dir="ltr" className="ms-2 font-mono text-[12px]" style={{ color: C.muted }}>{D.phone}</span>
        </motion.div>
        <div className="mt-10 lg:hidden"><TapDemo compact /></div>
      </motion.div>

      <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} transition={{ delay: 1.6 }} className="absolute bottom-8 start-[6vw] z-10 hidden items-center md:flex gap-3 text-[11px] font-semibold uppercase tracking-[0.2em]" style={{ color: C.muted }}>
        <motion.span animate={{ y: [0, 8, 0] }} transition={{ repeat: Infinity, duration: 1.8 }} className="block h-8 w-px" style={{ background: C.ink }} />
        {D.t.hint}
      </motion.div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* CHAPTER 1 - LISTEN                                                  */
/* ------------------------------------------------------------------ */

/** Phone scatter: zig-zag sides and tilts, rows kept in final order ~20% apart,
 *  so chips never collide at rest and never cross paths while they line up. */
const SCATTER_MOBILE = [
  { x: 10, y: 0, r: -7 },
  { x: 38, y: 19, r: 6 },
  { x: 2, y: 39, r: 5 },
  { x: 32, y: 58, r: -6 },
  { x: 14, y: 78, r: 8 },
  { x: 36, y: 90, r: -4 },
];

const SCATTER = [
  { x: 6, y: 10, r: -14 },
  { x: 60, y: 72, r: 11 },
  { x: 70, y: 6, r: 7 },
  { x: 16, y: 78, r: -6 },
  { x: 44, y: 36, r: 16 },
  { x: 80, y: 46, r: -9 },
];

function ChapterListen() {
  const D = useD();
  return (
    <Pinned id="method" height={260}>
      {(p) => (
        <div className="grid h-full grid-cols-1 content-center items-center gap-4 px-[6vw] md:grid-cols-[1fr_1.2fr] md:content-normal md:gap-10">
          <div className="relative grid md:block md:h-[360px]">
            {D.t.method.map((m, i) => (
              <Beat key={i} p={p} at={[i / D.t.method.length, (i + 1) / D.t.method.length]} className="[grid-area:1/1] md:absolute md:inset-0">
                <Eyebrow n="01">{D.t.methodLabel}</Eyebrow>
                <h2 className="mt-5 text-[clamp(36px,4.6vw,64px)] font-bold leading-[1] tracking-[-0.04em]" style={{ color: C.ink }}>
                  {m.a}
                  <br />
                  <span style={{ color: C.forest }}>{m.b}</span>
                </h2>
                <p className="mt-5 max-w-[440px] text-[16px] leading-relaxed" style={{ color: C.muted }}>{m.body}</p>
              </Beat>
            ))}
          </div>
          <div className="relative h-[330px] md:h-[420px]">
            {!D.mobile && <FlowLine p={p} />}
            {D.workflowSteps.map((s, i) => (
              <FlowStep key={s} p={p} i={i} n={D.workflowSteps.length} label={s} />
            ))}
            {!D.mobile && <FlowPulse p={p} />}
          </div>
        </div>
      )}
    </Pinned>
  );
}

function FlowStep({ p, i, n, label }: { p: MotionValue<number>; i: number; n: number; label: string }) {
  const D = useD();
  const s = (D.mobile ? SCATTER_MOBILE : SCATTER)[i % SCATTER.length];
  const sx = s.x;
  const tx = D.mobile ? 4 : (i / Math.max(1, n - 1)) * 80;
  const ty = D.mobile ? `${4 + i * (84 / Math.max(1, n))}%` : "44%";
  const start = 0.3 + i * 0.05;
  const left = useTransform(p, [0, start, start + 0.22], [`${sx}%`, `${sx}%`, `${tx}%`]);
  const top = useTransform(p, [0, start, start + 0.22], [`${s.y}%`, `${s.y}%`, ty]);
  const rotate = useTransform(p, [0, start, start + 0.22], [s.r, s.r, 0]);
  const done = useTransform(p, [0.76 + i * 0.04, 0.8 + i * 0.04], [0, 1]);
  const bg = useTransform(done, [0, 1], [C.paper, C.lime]);
  const drift = i % 2 ? 22 : -22;
  const floatY = useTransform(p, D.mobile ? [0, 0.3, start, start + 0.22] : [0, 0.3], D.mobile ? [0, drift * 0.35, drift * 0.35, 0] : [0, drift]);
  return (
    <motion.div className="absolute" style={{ [D.rtl ? "right" : "left"]: left, top, rotate: D.rtl ? useTransform(rotate, (v) => -v) : rotate, y: floatY }}>
      <motion.div className="flex items-center gap-2 whitespace-nowrap rounded-2xl border px-4 py-3 text-[15px] font-semibold shadow-[0_14px_40px_-18px_rgba(18,37,26,0.35)]" style={{ borderColor: C.soft, background: bg, color: C.ink }}>
        <span className="font-mono text-[11px]" style={{ color: C.forest }}>0{i + 1}</span>
        {label}
        <motion.span style={{ opacity: done, scale: done }}><Check size={14} /></motion.span>
      </motion.div>
    </motion.div>
  );
}

function FlowLine({ p }: { p: MotionValue<number> }) {
  const len = useTransform(p, [0.5, 0.76], [0, 1]);
  return (
    <svg className="absolute left-0 top-[50%] h-4 w-full overflow-visible" viewBox="0 0 100 4" preserveAspectRatio="none">
      <motion.line x1="4" y1="2" x2="92" y2="2" stroke={C.forest} strokeWidth="0.4" strokeDasharray="1.2 1" style={{ pathLength: len }} />
    </svg>
  );
}

function FlowPulse({ p }: { p: MotionValue<number> }) {
  const D = useD();
  const left = useTransform(p, [0.74, 0.96], ["3%", "90%"]);
  const o = useTransform(p, [0.72, 0.75, 0.95, 0.98], [0, 1, 1, 0]);
  return <motion.span className="absolute top-[50%] h-3 w-3 -translate-y-[2px] rounded-full" style={{ [D.rtl ? "right" : "left"]: left, opacity: o, background: C.coral, boxShadow: `0 0 0 5px rgba(219,104,75,0.18)` }} />;
}

/* ------------------------------------------------------------------ */
/* CHAPTER 2 - BUILD: the phone explodes into its layers in 3D         */
/* ------------------------------------------------------------------ */

function ChapterBuild() {
  const D = useD();
  if (!hasAny(D, ["layers"])) return null;
  return (
    <Pinned id="build" height={360}>
      {(p) => <BuildStage p={p} />}
    </Pinned>
  );
}

function BuildStage({ p }: { p: MotionValue<number> }) {
  const D = useD();
  const U = D.t.ui;
  const LAYERS = D.t.layers;
  const [stateIdx, setStateIdx] = useState(0);
  useMotionValueEvent(p, "change", (v) => setStateIdx(v < 0.4 ? 0 : v < 0.48 ? 1 : v < 0.6 ? 2 : 3));
  const rotX = useTransform(p, [0.08, 0.3, 0.74, 0.92], [0, 54, 54, 0]);
  const lower = useTransform(p, [0.08, 0.3, 0.74, 0.92], [0, 70, 70, 0]);
  const rz = D.rtl ? 34 : -34;
  const rotZ = useTransform(p, [0.08, 0.3, 0.74, 0.92], [0, rz, rz, 0]);
  const k = D.mobile ? 0.6 : 1;
  const scale = useTransform(p, [0.08, 0.3, 0.74, 0.92], [k, 0.7 * k, 0.7 * k, k]);
  const spread = useTransform(p, [0.14, 0.34, 0.72, 0.9], [0, 1, 1, 0]);
  const frame = useTransform(p, [0, 0.08], [0.2, 1]);
  return (
    <div className="grid h-full grid-cols-1 content-center items-center gap-2 px-[6vw] md:grid-cols-[0.9fr_1.1fr] md:content-normal md:gap-10">
      <div className="relative">
        <div className="relative grid md:block md:h-[330px]">
          <StoryBeats p={p} n="02" stages={["layers"]} size="clamp(34px,4vw,56px)" />
        </div>
        <div className="mt-6 hidden space-y-3 md:block">
          {LAYERS.map((l, i) => (
            <PieceRow key={l.label} p={p} at={0.36 + i * 0.12} i={i} label={l.label} note={l.note} />
          ))}
        </div>
      </div>
      <div className="relative flex h-[380px] items-center justify-center md:h-[640px]" style={{ perspective: 1800 }}>
        <motion.div style={{ rotateX: rotX, rotateZ: rotZ, scale, y: lower, transformStyle: "preserve-3d" }} className="relative h-[540px] w-[270px]">
          <Layer spread={spread} i={0} label="" note="">
            <motion.div style={{ opacity: frame }} className="absolute inset-0 rounded-[42px]" >
              <div className="absolute inset-0 rounded-[42px]" style={{ background: C.ink, boxShadow: "0 60px 90px -40px rgba(18,37,26,0.55)" }} />
              <div className="absolute inset-[10px] rounded-[34px] opacity-30" style={{ backgroundImage: `radial-gradient(${C.soft} 1px, transparent 1px)`, backgroundSize: "12px 12px" }} />
            </motion.div>
          </Layer>
          <Layer spread={spread} i={1} label={LAYERS[2].label} note="SQLite / Hive">
            <div className="absolute inset-[12px] rounded-[32px] border p-4" style={{ background: "rgba(242,245,239,0.94)", borderColor: C.soft }}>
              <div className="flex items-center gap-2 font-mono text-[10px] font-semibold" style={{ color: C.forest }}><Database size={12} /> drafts</div>
              <div className="mt-3 grid grid-cols-3 gap-1 font-mono text-[9px]" style={{ color: C.muted }}>
                <span>id</span><span>method</span><span>status</span>
              </div>
              {["d_01", "d_02", "d_03"].map((r, k) => (
                <motion.div key={r} animate={{ opacity: stateIdx >= 3 || k < 2 ? 1 : 0.15, x: stateIdx >= 3 || k < 2 ? 0 : -8 }} className="mt-1.5 grid grid-cols-3 gap-1 rounded-md px-1 py-1 font-mono text-[9px]" style={{ background: k === 2 ? C.lime : C.paper, color: C.ink }}>
                  <span>{r}</span><span>{k === 1 ? "wallet" : "bank"}</span><span>{k === 2 ? "saved" : "synced"}</span>
                </motion.div>
              ))}
            </div>
          </Layer>
          <Layer spread={spread} i={2} label={LAYERS[1].label} note="Riverpod / BLoC">
            <div className="absolute inset-[12px] rounded-[32px] p-4" style={{ background: "rgba(20,34,27,0.93)" }}>
              <div className="font-mono text-[10px]" style={{ color: C.soft }}>payment.state</div>
              <div className="mt-4 space-y-2">
                {["idle", "selecting", "ready", "saved"].map((s, k) => (
                  <motion.div key={s} animate={{ backgroundColor: stateIdx === k ? C.lime : "rgba(191,216,197,0.08)", color: stateIdx === k ? C.night : C.soft, scale: stateIdx === k ? 1.04 : 1 }} className="rounded-lg px-3 py-2 font-mono text-[11px] font-semibold">
                    {stateIdx === k ? "> " : "  "}{s}
                  </motion.div>
                ))}
              </div>
            </div>
          </Layer>
          <Layer spread={spread} i={3} label={LAYERS[0].label} note="Flutter widgets">
            <div className="absolute inset-[12px] overflow-hidden rounded-[32px] p-5" style={{ background: C.paper }}>
              <div className="mx-auto mb-6 h-5 w-24 rounded-full" style={{ background: C.ink }} />
              <div className="text-[10px] font-bold uppercase tracking-[0.2em]" style={{ color: C.forest }}>{D.t.tap.payment}</div>
              <div className="mt-1 text-[24px] font-bold leading-tight tracking-[-0.02em]" style={{ color: C.ink }}>{D.t.tap.choose}</div>
              <motion.div animate={{ borderColor: stateIdx >= 1 ? C.forest : C.soft }} className="mt-5 flex items-center gap-3 rounded-xl border-2 px-3 py-3 text-[13px] font-semibold" style={{ color: C.ink }}>
                <CreditCard size={16} /> {U.bankPay}
                <motion.span animate={{ scale: stateIdx >= 1 ? 1 : 0 }} className="ms-auto grid h-5 w-5 place-items-center rounded-full" style={{ background: C.forest, color: C.cream }}><Check size={12} /></motion.span>
              </motion.div>
              <div className="mt-3 flex items-center gap-3 rounded-xl border px-3 py-3 text-[13px] font-semibold" style={{ borderColor: C.soft, color: C.ink }}>
                <Wallet size={16} /> {U.walletPay}
              </div>
              <motion.div animate={{ opacity: stateIdx >= 2 ? 1 : 0.35 }} className="mt-6 rounded-xl py-3 text-center text-[13px] font-bold" style={{ background: C.forest, color: C.cream }}>{D.t.tap.cont}</motion.div>
              <motion.div animate={{ opacity: stateIdx >= 3 ? 1 : 0, y: stateIdx >= 3 ? 0 : 8 }} className="mt-5 flex items-center gap-2 text-[11px] font-semibold" style={{ color: C.muted }}>
                <span className="h-2 w-2 rounded-full" style={{ background: C.forest }} /> {U.saved}
              </motion.div>
            </div>
          </Layer>
        </motion.div>
        <motion.div style={{ opacity: spread }} className="absolute bottom-0 end-0 font-mono text-[11px]" >
          <span style={{ color: C.muted }}>{U.exploded}</span>
        </motion.div>
      </div>
    </div>
  );
}

function Layer({ spread, i, label, note, children }: { spread: MotionValue<number>; i: number; label: string; note: string; children: ReactNode }) {
  const z = useTransform(spread, (v) => v * i * 95);
  const lo = useTransform(spread, [0.55 + i * 0.08, 0.85 + i * 0.04], [0, 1]);
  const D = useD();
  return (
    <motion.div className="absolute inset-0" style={{ z, transformStyle: "preserve-3d" }}>
      {children}
      {label && !D.mobile && (
        <motion.div style={{ opacity: lo }} className="absolute start-[100%] top-[34%] flex items-center gap-3 whitespace-nowrap">
          <span className="h-px w-16" style={{ background: C.forest }} />
          <span className="rounded-lg px-3 py-1.5 text-[14px] font-bold" style={{ background: C.ink, color: C.lime }}>{label}</span>
          <span className="font-mono text-[11px]" style={{ color: C.muted }}>{note}</span>
        </motion.div>
      )}
    </motion.div>
  );
}

function PieceRow({ p, at, i, label, note }: { p: MotionValue<number>; at: number; i: number; label: string; note: string }) {
  const o = useTransform(p, [at - 0.04, at], [0.3, 1]);
  const w = useTransform(p, [at - 0.04, at + 0.08], [0, 1]);
  const D = useD();
  return (
    <motion.div style={{ opacity: o, borderColor: C.soft }} className="relative overflow-hidden rounded-2xl border px-5 py-4">
      <motion.div className="absolute inset-0" style={{ transformOrigin: D.rtl ? "right" : "left", scaleX: w, background: C.lime, opacity: 0.55 }} />
      <div className="relative flex items-baseline gap-4" style={{ color: C.ink }}>
        <span className="font-mono text-[11px]" style={{ color: C.forest }}>0{i + 1}</span>
        <span className="text-[18px] font-bold">{label}</span>
        <span className="text-[14px]" style={{ color: C.muted }}>{note}</span>
      </div>
    </motion.div>
  );
}

/* ------------------------------------------------------------------ */
/* CHAPTER 3 - CONNECT: the packet carries live code through the stack */
/* ------------------------------------------------------------------ */

const NODES = [
  { x: 8, y: 55, label: "Flutter app", icon: Smartphone },
  { x: 34, y: 18, label: ".NET API", icon: Server },
  { x: 64, y: 18, label: "Validation", icon: ShieldCheck },
  { x: 90, y: 55, label: "PostgreSQL", icon: Database },
];

const CODE = [
  { file: "payment_screen.dart", lines: ["final res = await api.post(", "  '/payments/initiate',", "  data: {'method': method, 'fields': form},", ");", "state = PaymentState.pending(res.id);"] },
  { file: "PaymentsController.cs", lines: ["[HttpPost(\"initiate\")]", "public async Task<IActionResult> Initiate(", "    InitiatePaymentRequest req) {", "  var result = await _payments.StartAsync(req);", "  return Ok(result);", "}"] },
  { file: "InitiateValidator.cs", lines: ["RuleFor(x => x.Method).IsInEnum();", "RuleFor(x => x.Fields)", "  .Must(MatchDynamicForm)", "  .WithMessage(\"Missing required field\");"] },
  { file: "payments.sql", lines: ["INSERT INTO payments (id, method, status)", "VALUES (@id, @method, 'pending')", "RETURNING id, status;"] },
  { file: "response.json", lines: ["HTTP/1.1 200 OK", "{", "  \"status\": \"confirmed\",", "  \"next\": \"show_result\"", "}"] },
];

const KW = /('[^']*'|"[^"]*"|\b(?:final|await|public|async|return|var|INSERT|INTO|VALUES|RETURNING|RuleFor|Task|HttpPost|HTTP)\b)/g;
function hl(line: string) {
  return line.split(KW).map((t, i) => {
    if (!t) return null;
    const isStr = /^['"]/.test(t);
    const isKw = !isStr && /^(final|await|public|async|return|var|INSERT|INTO|VALUES|RETURNING|RuleFor|Task|HttpPost|HTTP)$/.test(t);
    return (
      <span key={i} style={{ color: isStr ? C.lime : isKw ? C.coral : "rgba(242,245,239,0.86)" }}>
        {t}
      </span>
    );
  });
}

function TypedCode({ idx }: { idx: number }) {
  const block = CODE[idx];
  const full = block.lines.join("\n");
  const [n, setN] = useState(0);
  useEffect(() => {
    setN(0);
    const id = window.setInterval(() => setN((v) => (v >= full.length ? v : v + 3)), 14);
    return () => window.clearInterval(id);
  }, [idx, full.length]);
  const shown = full.slice(0, n).split("\n");
  return (
    <div dir="ltr" className="font-mono text-[11px] leading-[1.7] md:text-[12.5px] md:leading-[1.75]">
      {shown.map((l, i) => (
        <div key={i} className="flex">
          <span className="w-7 shrink-0 select-none" style={{ color: "rgba(191,216,197,0.3)" }}>{i + 1}</span>
          <span className="whitespace-pre">{hl(l)}{i === shown.length - 1 && <motion.span animate={{ opacity: [1, 0, 1] }} transition={{ repeat: Infinity, duration: 0.9 }} className="ml-px inline-block h-[14px] w-[7px] translate-y-[2px]" style={{ background: C.lime }} />}</span>
        </div>
      ))}
    </div>
  );
}

function ChapterConnect() {
  const D = useD();
  if (!hasAny(D, ["backend", "backend-focus", "integration"])) return null;
  return (
    <Pinned id="connect" height={340} dark>
      {(p) => <ConnectStage p={p} />}
    </Pinned>
  );
}

function ConnectStage({ p }: { p: MotionValue<number> }) {
  const D = useD();
  const go = [0.12, 0.28, 0.42, 0.56];
  const back = [0.64, 0.82];
  const keys = [...go, ...back];
  const xs = [8, 34, 64, 90, 50, 8];
  const ys = [55, 18, 18, 55, 92, 55];
  const px = useTransform(p, keys, xs);
  const py = useTransform(p, keys, ys);
  const [stage, setStage] = useState(0);
  useMotionValueEvent(p, "change", (v) => setStage(v < 0.2 ? 0 : v < 0.35 ? 1 : v < 0.49 ? 2 : v < 0.62 ? 3 : 4));
  const pathOut = useTransform(p, [0.1, 0.56], [0, 1]);
  const pathBack = useTransform(p, [0.6, 0.82], [0, 1]);
  const result = useTransform(p, [0.82, 0.88], [0, 1]);
  const ms = useTransform(p, [0.12, 0.82], [0, 184], { clamp: true });
  const msText = useTransform(ms, (v) => `${Math.round(v)} ms`);
  const back01 = useTransform(p, [0.56, 0.62], [0, 1]);
  const packetBg = useTransform(back01, [0, 1], [C.lime, C.coral]);
  const scopeO = useTransform(p, [0.4, 0.48], [0, 1]);
  const m = D.rtl;
  const mpx = useTransform(px, (v) => (m ? 100 - v : v));
  return (
    <div className="grid h-full grid-cols-1 content-center items-center gap-4 px-[6vw] pt-14 md:grid-cols-[1.25fr_1fr] md:content-normal md:gap-10 md:pt-16">
      <div className="flex flex-col">
      <div className="relative grid md:block md:h-[clamp(230px,30vh,280px)]">
        <StoryBeats p={p} n="03" dark stages={["backend", "backend-focus", "integration"]} size="clamp(28px,3.2vw,46px)" />
      </div>
        <div className="relative mt-3 h-[140px] md:mt-4 md:h-[clamp(240px,34vh,320px)]">
          <svg className="absolute inset-0 h-full w-full" viewBox="0 0 100 100" preserveAspectRatio="none">
            <g transform={m ? "translate(100 0) scale(-1 1)" : undefined}>
            <path d="M8 55 L34 18 L64 18 L90 55" fill="none" stroke={C.soft} strokeOpacity="0.15" strokeWidth="0.3" vectorEffect="non-scaling-stroke" />
            <path d="M90 55 Q50 128 8 55" fill="none" stroke={C.soft} strokeOpacity="0.15" strokeWidth="0.3" vectorEffect="non-scaling-stroke" />
            <motion.path d="M8 55 L34 18 L64 18 L90 55" fill="none" stroke={C.lime} strokeWidth="1.5" vectorEffect="non-scaling-stroke" style={{ pathLength: pathOut }} />
            <motion.path d="M90 55 Q50 128 8 55" fill="none" stroke={C.coral} strokeWidth="1.5" strokeDasharray="6 6" vectorEffect="non-scaling-stroke" style={{ pathLength: pathBack }} />
            </g>
          </svg>
          {NODES.map((n, i) => (
            <Node key={n.label} p={p} at={go[i]} {...n} x={m ? 100 - n.x : n.x} />
          ))}
          {[0, 1, 2, 3].map((i) => (
            <Trail key={i} px={mpx} py={py} stiffness={420 - i * 90} size={14 - i * 3} opacity={0.55 - i * 0.12} />
          ))}
          <Packet px={mpx} py={py} bg={packetBg} />
          <motion.div style={{ opacity: result, scale: result }} className="absolute start-[8%] top-[55%] z-30 ms-10 -translate-y-1/2 md:ms-12">
            <div className="flex items-center gap-2 whitespace-nowrap rounded-xl px-4 py-3 text-[14px] font-bold" style={{ background: C.lime, color: C.night }}><Check size={16} /> {D.t.confirmed}</div>
          </motion.div>
          <div dir="ltr" className="absolute -bottom-6 end-0 font-mono text-[10px] md:text-[11px]" style={{ color: C.soft }}>
            trace / POST /payments/initiate &nbsp;<motion.span style={{ color: C.lime }}>{msText}</motion.span>
          </div>
        </div>
      </div>
        <div dir="ltr" className="relative mt-8 overflow-hidden rounded-2xl border md:mt-0" style={{ borderColor: "rgba(191,216,197,0.16)", background: "rgba(10,20,15,0.6)" }}>
          <div className="flex items-center justify-between border-b px-4 py-3" style={{ borderColor: "rgba(191,216,197,0.12)" }}>
            <div className="flex gap-1.5">
              {[C.coral, C.lime, C.soft].map((c) => <span key={c} className="h-2.5 w-2.5 rounded-full" style={{ background: c, opacity: 0.7 }} />)}
            </div>
            <span className="font-mono text-[11px]" style={{ color: C.soft }}>{CODE[stage].file}</span>
            <span className="font-mono text-[10px]" style={{ color: C.lime }}>{stage + 1}/5</span>
          </div>
          <div className="min-h-[96px] p-3 md:min-h-[250px] md:p-4">
            <TypedCode idx={stage} />
          </div>
          <div className="flex gap-1 px-4 pb-4">
            {CODE.map((_, i) => (
              <motion.span key={i} animate={{ backgroundColor: i <= stage ? C.lime : "rgba(191,216,197,0.15)" }} className="h-1 flex-1 rounded-full" />
            ))}
          </div>
          <motion.div style={{ opacity: scopeO }} className="hidden border-t px-4 py-3 md:block font-mono text-[10.5px] leading-relaxed" >
            <span style={{ color: "rgba(191,216,197,0.6)" }}>// simplified example. {D.teamScope}</span>
          </motion.div>
        </div>
    </div>
  );
}

function Packet({ px, py, bg }: { px: MotionValue<number>; py: MotionValue<number>; bg: MotionValue<string> }) {
  const left = useTransform(px, (v) => `${v}%`);
  const top = useTransform(py, (v) => `${v}%`);
  return <motion.div className="absolute z-20 h-4 w-4 -translate-x-1/2 -translate-y-1/2 rounded-full" style={{ left, top, background: bg, boxShadow: "0 0 0 6px rgba(216,238,117,0.15)" }} />;
}

function Trail({ px, py, stiffness, size, opacity }: { px: MotionValue<number>; py: MotionValue<number>; stiffness: number; size: number; opacity: number }) {
  const sx = useSpring(px, { stiffness, damping: 30 });
  const sy = useSpring(py, { stiffness, damping: 30 });
  const left = useTransform(sx, (v) => `${v}%`);
  const top = useTransform(sy, (v) => `${v}%`);
  return <motion.div className="absolute z-10 -translate-x-1/2 -translate-y-1/2 rounded-full" style={{ left, top, width: size, height: size, background: C.lime, opacity }} />;
}

function Node({ p, at, x, y, label, icon: Icon }: { p: MotionValue<number>; at: number; x: number; y: number; label: string; icon: typeof Server }) {
  const D = useD();
  const hit = useTransform(p, [at - 0.03, at, at + 0.06], [0, 1, 0.35]);
  const scale = useTransform(hit, [0, 1], [1, 1.14]);
  const border = useTransform(hit, [0, 1], ["rgba(191,216,197,0.2)", C.lime]);
  const ring = useTransform(hit, [0, 1], [0.6, 1.9]);
  const ringO = useTransform(hit, [0, 0.5, 1], [0, 0.4, 0]);
  return (
    <div className="absolute z-10 -translate-x-1/2 -translate-y-1/2" style={{ left: `${x}%`, top: `${y}%` }}>
      <motion.span className="absolute inset-0 rounded-2xl border" style={{ scale: ring, opacity: ringO, borderColor: C.lime }} />
      <motion.div style={{ scale, borderColor: border, background: "rgba(20,34,27,0.9)" }} className="relative flex flex-col items-center gap-1 rounded-xl border px-2 py-2 md:gap-2 md:rounded-2xl md:px-5 md:py-4">
        <Icon size={D.mobile ? 15 : 22} color={C.cream} />
        <span dir="ltr" className="whitespace-nowrap font-mono text-[9px] md:text-[11px]" style={{ color: C.soft }}>{label}</span>
      </motion.div>
    </div>
  );
}

/* ------------------------------------------------------------------ */
/* CHAPTER 4 - BAD DAYS: live signal, offline queue, burst sync        */
/* ------------------------------------------------------------------ */

function ChapterOffline() {
  const D = useD();
  if (!hasAny(D, ["offline"])) return null;
  return (
    <Pinned id="offline" height={290}>
      {(p) => <OfflineStage p={p} />}
    </Pinned>
  );
}

function SignalWave({ online }: { online: boolean }) {
  const path = useRef<SVGPathElement>(null);
  const amp = useRef(1);
  useAnimationFrame((t) => {
    amp.current += ((online ? 1 : 0.04) - amp.current) * 0.06;
    let d = "M0 30";
    for (let x = 0; x <= 400; x += 6) {
      const y = 30 + Math.sin(x / 18 + t / 180) * 12 * amp.current + Math.sin(x / 7 + t / 90) * 4 * amp.current;
      d += ` L${x} ${y.toFixed(1)}`;
    }
    path.current?.setAttribute("d", d);
  });
  return (
    <svg viewBox="0 0 400 60" className="h-[60px] w-full" preserveAspectRatio="none">
      <path ref={path} fill="none" stroke={online ? C.forest : C.coral} strokeWidth="2" style={{ transition: "stroke .4s" }} />
    </svg>
  );
}

function OfflineStage({ p }: { p: MotionValue<number> }) {
  const [online, setOnline] = useState(true);
  useMotionValueEvent(p, "change", (v) => setOnline(v < 0.18 || v > 0.62));
  const records = ["beneficiary_0412", "document_capture", "signature", "receipt_0412", "beneficiary_0413"];
  const flash = useTransform(p, [0.62, 0.66, 0.74], [0, 1, 0]);
  const D = useD();
  const T = D.t;
  return (
    <div className="grid h-full grid-cols-1 content-center items-center gap-4 px-[6vw] pt-16 md:grid-cols-[1fr_1.1fr] md:content-normal md:gap-12 md:pt-0">
      <div>
        <div className="relative grid md:block md:h-[420px]"><StoryBeats p={p} n="04" stages={["offline"]} accent={C.coral} /></div>
      </div>
      <div className="relative h-[356px] overflow-hidden rounded-[28px] border p-5 md:h-[470px] md:p-7" style={{ borderColor: C.soft, background: C.paper }}>
        <motion.div className="pointer-events-none absolute inset-0" style={{ opacity: flash, background: `radial-gradient(circle at 85% 20%, ${C.lime}, transparent 60%)` }} />
        <div className="relative flex items-center justify-between">
          <motion.div key={String(online)} initial={{ opacity: 0, y: -6 }} animate={{ opacity: 1, y: 0 }} className="flex items-center gap-2 rounded-full px-3 py-1.5 text-[12px] font-bold" style={{ background: online ? C.lime : "#f6d9cf", color: online ? C.night : C.coral }}>
            {online ? <Wifi size={14} /> : <WifiOff size={14} />} {online ? T.online : T.offline}
          </motion.div>
          <span className="font-mono text-[11px]" style={{ color: C.muted }}>{T.device}</span>
        </div>
        <div className="relative mt-4 w-[60%]"><SignalWave online={online} /></div>
        <div className="absolute end-5 top-[62px] flex h-[52px] w-[110px] md:end-7 md:top-[86px] md:h-[70px] md:w-[120px] items-center justify-center rounded-2xl border border-dashed text-[12px] font-semibold" style={{ borderColor: C.forest, color: C.forest }}>
          <Server size={14} className="me-2" /> {T.server}
        </div>
        <div className="absolute bottom-5 left-5 right-5 md:bottom-7 md:left-7 md:right-7">
          <div className="mb-3 font-mono text-[11px]" style={{ color: C.muted }}>{T.queue}</div>
          <div className="relative h-[192px] md:h-[230px]">
            {records.map((r, i) => (
              <QueueCard key={r} p={p} i={i} label={r} />
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}

function QueueCard({ p, i, label }: { p: MotionValue<number>; i: number; label: string }) {
  const inAt = 0.2 + i * 0.07;
  const outAt = 0.64 + i * 0.04;
  const o = useTransform(p, [inAt, inAt + 0.04, outAt, outAt + 0.06], [0, 1, 1, 0]);
  const y = useTransform(p, [inAt, inAt + 0.06, outAt, outAt + 0.06], [-80, 0, 0, -270]);
  const D = useD();
  const x = useTransform(p, [outAt, outAt + 0.06], [0, D.rtl ? -240 : 240]);
  const rotate = useTransform(p, [inAt, inAt + 0.06], [i % 2 ? 6 : -6, 0]);
  const scale = useTransform(p, [outAt, outAt + 0.06], [1, 0.4]);
  return (
    <motion.div className="absolute left-0 right-0 flex items-center justify-between rounded-xl border px-4 py-2.5 text-[13px] font-semibold" style={{ bottom: i * (D.mobile ? 38 : 44), opacity: o, y, x, rotate, scale, borderColor: C.soft, background: C.cream, color: C.ink }}>
      <span dir="ltr" className="font-mono">{label}</span>
      <span className="text-[11px]" style={{ color: C.coral }}>{D.t.queued}</span>
    </motion.div>
  );
}

/* ------------------------------------------------------------------ */
/* VELOCITY MARQUEE: speed and direction follow your scroll            */
/* ------------------------------------------------------------------ */

const wrap = (min: number, max: number, v: number) => {
  const r = max - min;
  return ((((v - min) % r) + r) % r) + min;
};

function VelocityMarquee({ words, base = 3, dark }: { words: string[]; base?: number; dark?: boolean }) {
  const baseX = useMotionValue(0);
  const { scrollY } = useScroll();
  const vel = useVelocity(scrollY);
  const sv = useSpring(vel, { damping: 50, stiffness: 400 });
  const factor = useTransform(sv, [0, 1000], [0, 5], { clamp: false });
  const skew = useTransform(sv, [-2500, 2500], [8, -8]);
  const x = useTransform(baseX, (v) => `${wrap(-25, -50, v)}%`);
  const dir = useRef(1);
  const reduce = useReducedMotion();
  useAnimationFrame((_, delta) => {
    if (reduce) return;
    let m = dir.current * base * (delta / 1000);
    const f = factor.get();
    if (f < 0) dir.current = -1;
    else if (f > 0) dir.current = 1;
    m += dir.current * m * f;
    baseX.set(baseX.get() + m);
  });
  const row = (
    <span className="flex shrink-0 items-center">
      {words.map((w, i) => (
        <span key={i} className="flex items-center">
          <span className={i % 2 ? "msf-outline" : ""} style={{ color: dark ? C.lime : C.ink }}>{w}</span>
          <span className="mx-8 inline-block h-4 w-4 rounded-full" style={{ background: i % 2 ? C.coral : C.lime }} />
        </span>
      ))}
    </span>
  );
  return (
    <div dir="ltr" className="overflow-hidden py-10" style={{ background: dark ? C.night : "transparent" }} aria-hidden>
      <motion.div style={{ x, skewX: skew }} className="flex whitespace-nowrap text-[clamp(56px,9vw,128px)] font-extrabold leading-none tracking-[-0.04em]">
        {row}{row}{row}{row}
      </motion.div>
    </div>
  );
}

/* ------------------------------------------------------------------ */
/* CHAPTER 5 - WORK: horizontal reel, 3D tilt + spotlight cards        */
/* ------------------------------------------------------------------ */

function LiveBadge() {
  const D = useD();
  const st = D.live.status;
  const color = st === "live" ? C.forest : st === "cached" ? C.coral : C.muted;
  const time = D.live.syncedAt ? D.live.syncedAt.toLocaleTimeString("en-GB", { hour: "2-digit", minute: "2-digit", second: "2-digit" }) : null;
  return (
    <div className="flex flex-wrap items-center gap-x-3 gap-y-1 font-mono text-[11px]" role="status" style={{ color: C.muted }}>
      <span className="inline-flex items-center gap-2 rounded-full border px-2.5 py-1" style={{ borderColor: C.soft, background: C.paper, color }}>
        <span className="relative grid h-2 w-2 place-items-center">
          {st !== "cached" && <motion.span className="absolute h-2 w-2 rounded-full" style={{ background: color }} animate={{ scale: [1, 2.4], opacity: [0.5, 0] }} transition={{ duration: 1.6, repeat: Infinity }} />}
          <span className="relative h-2 w-2 rounded-full" style={{ background: color }} />
        </span>
        <span>{D.t.live[st]}</span>
      </span>
      {time && <bdi dir="ltr" className="hidden md:inline">@ {time}</bdi>}
    </div>
  );
}

function WorkHeader({ count }: { count?: ReactNode }) {
  const D = useD();
  return (
    <div className="flex flex-col items-start justify-between gap-4 px-[6vw] md:flex-row md:items-end">
      <div>
        <Eyebrow n="05">{D.t.workEyebrow}</Eyebrow>
        <h2 className="mt-5 text-[clamp(32px,4.6vw,64px)] font-bold leading-[1.05] tracking-[-0.04em]" style={{ color: C.ink }}>{D.t.workTitle}</h2>
        <div className="mt-4"><LiveBadge /></div>
      </div>
      {count}
    </div>
  );
}

function ChapterWork() {
  const D = useD();
  const reduce = useReducedMotion();
  const n = D.projects.length;
  if (n === 0) return <section id="work" className="py-20"><WorkHeader /><p className="mt-8 px-[6vw]">{D.rtl ? "لا توجد مشاريع منشورة حاليًا." : "No projects are published yet."}</p></section>;
  if (D.mobile || reduce) {
    return (
      <section id="work" className="py-20">
        <WorkHeader count={<span className="font-mono text-[12px]" style={{ color: C.muted }}>{D.t.swipe} {D.rtl ? "\u2190" : "\u2192"} {String(n).padStart(2, "0")}</span>} />
        <div className="msf-snap mt-8 flex snap-x snap-mandatory gap-4 overflow-x-auto scroll-px-[6vw] px-[6vw] pb-6">
          {D.projects.map((pr, i) => (
            <motion.div key={pr.id} className="snap-start" initial={{ opacity: 0, y: 30 }} whileInView={{ opacity: 1, y: 0 }} viewport={{ once: true }} transition={{ delay: Math.min(i, 3) * 0.06, duration: 0.6, ease }}>
              <TiltCard i={i} pr={pr} />
            </motion.div>
          ))}
        </div>
      </section>
    );
  }
  return <PinnedWork />;
}

/** The horizontal reel scrolls exactly as far as the cards overflow, then releases the page. */
function PinnedWork() {
  // How far (px) the card row overflows the screen. Measured from the real cards.
  const [overflow, setOverflow] = useState(0);
  const vh = typeof window !== "undefined" ? window.innerHeight : 900;
  // One screen to settle in, then ~1px of vertical scroll per 1px of horizontal travel.
  const height = 100 + Math.round((overflow / vh) * 100) + 20;
  return (
    <Pinned id="work" height={height}>
      {(p) => <WorkReel p={p} overflow={overflow} onMeasure={setOverflow} />}
    </Pinned>
  );
}

function WorkReel({ p, overflow, onMeasure }: { p: MotionValue<number>; overflow: number; onMeasure: (px: number) => void }) {
  const D = useD();
  const track = useRef<HTMLDivElement>(null);
  useLayoutEffect(() => {
    const el = track.current;
    if (!el) return;
    const measure = () => {
      const cards = Array.from(el.children) as HTMLElement[];
      if (!cards.length) return onMeasure(0);
      const cs = getComputedStyle(el);
      const gap = parseFloat(cs.columnGap) || 0;
      const padStart = parseFloat(D.rtl ? cs.paddingRight : cs.paddingLeft) || 0;
      const content = cards.reduce((a, c) => a + c.offsetWidth, 0) + gap * (cards.length - 1) + padStart;
      const endPad = window.innerWidth * 0.06;
      onMeasure(Math.max(0, Math.round(content + endPad - el.clientWidth)));
    };
    measure();
    const ro = new ResizeObserver(measure);
    ro.observe(el);
    Array.from(el.children).forEach((c) => ro.observe(c));
    return () => ro.disconnect();
  }, [D.projects, D.rtl, onMeasure]);
  const x = useTransform(p, [0.08, 0.92], [0, D.rtl ? overflow : -overflow]);
  const { scrollY } = useScroll();
  const vel = useSpring(useVelocity(scrollY), { stiffness: 200, damping: 40 });
  const skew = useTransform(vel, [-3000, 3000], [5, -5]);
  const count = useTransform(p, [0.08, 0.92], [1, D.projects.length]);
  const countText = useTransform(count, (v) => String(Math.round(v)).padStart(2, "0"));
  return (
    <div className="flex h-full flex-col justify-center">
      <WorkHeader
        count={
          <div dir="ltr" className="hidden font-mono text-[13px] md:block" style={{ color: C.muted }}>
            <motion.span style={{ color: C.ink }}>{countText}</motion.span> / {String(D.projects.length).padStart(2, "0")}
          </div>
        }
      />
      <motion.div ref={track} style={{ x, skewX: skew }} className="mt-10 flex gap-6 ps-[6vw]" onFocusCapture={(event) => {
        const card = (event.target as HTMLElement).closest("article");
        const section = track.current?.closest("section");
        if (!card || !section || !track.current) return;
        const bounds = card.getBoundingClientRect();
        if (bounds.left >= 0 && bounds.right <= window.innerWidth) return;
        const index = Array.from(track.current.children).indexOf(card);
        const progress = 0.08 + (index / Math.max(1, D.projects.length - 1)) * 0.84;
        const top = window.scrollY + section.getBoundingClientRect().top + progress * (section.offsetHeight - window.innerHeight);
        window.scrollTo({ top, behavior: "smooth" });
      }}>
        {D.projects.map((pr, i) => (
          <TiltCard key={pr.id} i={i} pr={pr} />
        ))}
      </motion.div>
    </div>
  );
}

function TiltCard({ i, pr }: { i: number; pr: Project }) {
  const D = useD();
  const rx = useMotionValue(0);
  const ry = useMotionValue(0);
  const mx = useMotionValue(50);
  const my = useMotionValue(30);
  const srx = useSpring(rx, { stiffness: 220, damping: 20 });
  const sry = useSpring(ry, { stiffness: 220, damping: 20 });
  const dark = i < 3;
  const spot = useMotionTemplate`radial-gradient(380px circle at ${mx}% ${my}%, ${dark ? "rgba(216,238,117,0.22)" : "rgba(36,92,66,0.12)"}, transparent 65%)`;
  const bg = dark ? (i === 0 ? C.forest : i === 1 ? C.night : C.ink) : C.paper;
  return (
    <motion.article
      onMouseMove={(e) => {
        const r = e.currentTarget.getBoundingClientRect();
        const px = (e.clientX - r.left) / r.width;
        const py = (e.clientY - r.top) / r.height;
        ry.set((px - 0.5) * 16);
        rx.set(-(py - 0.5) * 16);
        mx.set(px * 100);
        my.set(py * 100);
      }}
      onMouseLeave={() => {
        rx.set(0);
        ry.set(0);
      }}
      style={{ rotateX: srx, rotateY: sry, transformPerspective: 900, background: bg, border: dark ? "none" : `1px solid ${C.soft}` }}
      className="relative flex min-h-[420px] w-[min(430px,82vw)] shrink-0 flex-col justify-between overflow-hidden rounded-[28px] p-8"
    >
      <motion.div className="pointer-events-none absolute inset-0" style={{ background: spot }} />
      <span dir="ltr" className="msf-outline pointer-events-none absolute -bottom-10 -end-2 text-[150px] md:text-[180px] font-extrabold leading-none tracking-[-0.06em]" style={{ color: dark ? "rgba(216,238,117,0.18)" : "rgba(36,92,66,0.14)" }}>0{i + 1}</span>
      <div className="relative" style={{ transform: "translateZ(40px)" }}>
        <div className="flex items-center justify-between text-[11px] font-semibold uppercase tracking-[0.2em]" style={{ color: dark ? C.lime : C.forest }}>
          <span>{pr.kicker}</span>
          <ArrowUpRight size={16} />
        </div>
        <h3 className="mt-6 text-[28px] md:text-[34px] font-bold leading-[1.02] tracking-[-0.035em]" style={{ color: dark ? C.cream : C.ink }}><a href={pr.href} data-cursor={D.t.view}>{pr.name}</a></h3>
        <p className="mt-4 line-clamp-4 text-[15px] leading-relaxed" style={{ color: dark ? "rgba(242,245,239,0.72)" : C.muted }}>{pr.summary}</p>
      </div>
      <div className="relative">
        <div className="flex flex-wrap gap-2">
          {pr.tags.map((t) => (
            <span key={t} className="rounded-full border px-3 py-1 text-[11px] font-semibold" style={{ borderColor: dark ? "rgba(242,245,239,0.25)" : C.soft, color: dark ? C.cream : C.ink }}>{t}</span>
          ))}
        </div>
        {pr.scope && <div className="mt-4 line-clamp-2 font-mono text-[11px]" style={{ color: dark ? C.soft : C.muted }}>{pr.scope}</div>}
        <div className="mt-4 flex flex-wrap gap-3 text-[12px] font-semibold" style={{ color: dark ? C.lime : C.forest }}>
          <a href={pr.href} className="inline-flex min-h-[44px] items-center gap-1" data-cursor={D.t.view}>{D.rtl ? "تفاصيل المشروع" : "View project"}<ArrowUpRight size={14} /></a>
          {pr.links.map(link => <a key={link.url} href={link.url} target="_blank" rel="noopener noreferrer" className="inline-flex min-h-[44px] items-center gap-1">{link.label}<ArrowUpRight size={14} /></a>)}
        </div>
      </div>
    </motion.article>
  );
}

/* ------------------------------------------------------------------ */
/* CHAPTER 6 - EXPERIENCE                                              */
/* ------------------------------------------------------------------ */

function ChapterExperience() {
  const D = useD();
  const ref = useRef<HTMLDivElement>(null);
  const { scrollYProgress } = useScroll({ target: ref, offset: ["start 70%", "end 60%"] });
  const line = useSpring(scrollYProgress, { stiffness: 120, damping: 30 });
  const headY = useTransform(scrollYProgress, [0, 1], [0, D.mobile ? 0 : 60]);
  const [y1, y2] = D.t.years(D.years);
  if (!D.experience.length) return null;
  return (
    <section id="experience" className="px-[6vw] py-20 md:py-32">
      <div className="grid gap-12 md:grid-cols-[1fr_2fr]">
        <motion.div style={{ y: headY }} className="md:sticky md:top-32 md:self-start">
          <Eyebrow n="06">{D.t.expEyebrow}</Eyebrow>
          <h2 className="mt-5 text-[clamp(36px,4.6vw,64px)] font-bold leading-[1] tracking-[-0.04em]" style={{ color: C.ink }}>{y1}<br />{y2}</h2>
          <p className="mt-6 max-w-[360px] text-[15px] leading-relaxed" style={{ color: C.muted }}>{D.about}</p>
          <div className="mt-6 rounded-2xl border px-4 py-3 font-mono text-[11.5px] leading-relaxed" style={{ borderColor: C.soft, color: C.forest }}>{D.teamScope}</div>
        </motion.div>
        <div ref={ref} className="relative ps-10">
          <div className="absolute bottom-2 start-[7px] top-2 w-[2px]" style={{ background: C.soft, opacity: 0.5 }} />
          <motion.div className="absolute bottom-2 start-[7px] top-2 w-[2px] origin-top" style={{ background: C.forest, scaleY: line }} />
          {D.experience.map((j, i) => (
            <motion.div key={`${j.company}-${j.dates}`} initial={{ opacity: 0, x: D.rtl ? -40 : 40 }} whileInView={{ opacity: 1, x: 0 }} viewport={{ once: true, margin: "-12% 0px" }} transition={{ duration: 0.8, ease }} className="group relative mb-14 last:mb-0">
              <motion.span initial={{ scale: 0 }} whileInView={{ scale: 1 }} viewport={{ once: true, margin: "-12% 0px" }} transition={{ delay: 0.2, type: "spring", stiffness: 300, damping: 15 }} className="absolute -start-10 top-2 block h-4 w-4 rounded-full" style={{ background: C.lime, boxShadow: `0 0 0 4px ${C.forest}` }} />
              <div className="flex flex-wrap items-baseline gap-x-4">
                <h3 className="text-[24px] font-bold md:text-[28px] tracking-[-0.025em]" style={{ color: C.ink }}><Scramble text={j.company} delay={0.1 + i * 0.02} /></h3>
                <span className="text-[13px] font-semibold uppercase tracking-[0.16em]" style={{ color: C.forest }}>{j.role}</span>
              </div>
              <div className="mt-2 font-mono text-[12px] tracking-[0.04em]" style={{ color: C.coral }}>{j.dates}{j.location ? <span style={{ color: C.muted }}> / {j.location}</span> : null}</div>
              <p className="mt-3 max-w-[600px] text-[15px] leading-relaxed" style={{ color: C.muted }}>{j.summary}</p>
              <div className="mt-4 flex flex-wrap gap-2">
                {j.products.map((pr, k) => (
                  <motion.span key={pr} initial={{ opacity: 0, y: 10, scale: 0.9 }} whileInView={{ opacity: 1, y: 0, scale: 1 }} viewport={{ once: true }} transition={{ delay: 0.3 + k * 0.06, type: "spring", stiffness: 260, damping: 18 }} whileHover={{ y: -3, backgroundColor: C.lime }} className="rounded-full px-3 py-1 text-[12px] font-semibold" style={{ background: C.paper, border: `1px solid ${C.soft}`, color: C.ink }}>{pr}</motion.span>
                ))}
              </div>
            </motion.div>
          ))}
        </div>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* CONTACT: letters lean toward the cursor                             */
/* ------------------------------------------------------------------ */

function MagneticHeadline({ text }: { text: string }) {
  const D = useD();
  const host = useRef<HTMLHeadingElement>(null);
  useEffect(() => {
    const el = host.current;
    if (!el) return;
    const letters = Array.from(el.querySelectorAll<HTMLSpanElement>("[data-l]"));
    let raf = 0;
    let mx = -9999;
    let my = -9999;
    const apply = () => {
      raf = 0;
      letters.forEach((l) => {
        const r = l.getBoundingClientRect();
        const dx = mx - (r.left + r.width / 2);
        const dy = my - (r.top + r.height / 2);
        const d = Math.hypot(dx, dy);
        const k = Math.max(0, 1 - d / 220);
        l.style.transform = `translate(${(dx * k * 0.12).toFixed(1)}px, ${(-k * 22 + dy * k * 0.06).toFixed(1)}px) scale(${(1 + k * 0.12).toFixed(3)})`;
        l.style.color = k > 0.55 ? C.forest : "";
      });
    };
    const mv = (e: MouseEvent) => {
      mx = e.clientX;
      my = e.clientY;
      if (!raf) raf = requestAnimationFrame(apply);
    };
    window.addEventListener("mousemove", mv);
    return () => {
      window.removeEventListener("mousemove", mv);
      cancelAnimationFrame(raf);
    };
  }, []);
  return (
    <h2 ref={host} aria-label={text} className="mt-6 max-w-[980px] text-[clamp(44px,6.6vw,100px)] font-extrabold leading-[0.95] tracking-[-0.05em]" style={{ color: C.night }}>
      {text.split(" ").map((w, wi) => (
        <span key={wi} className="me-[0.25em] inline-block whitespace-nowrap" aria-hidden>
          {(D.rtl ? [w] : w.split("")).map((ch, ci) => (
            <span key={ci} data-l className="inline-block" style={{ transition: "transform .25s cubic-bezier(.22,1,.36,1), color .2s" }}>{ch}</span>
          ))}
        </span>
      ))}
    </h2>
  );
}

function Contact() {
  const D = useD();
  return (
    <section id="contact" className="px-[4vw] pb-10">
      <motion.div initial={{ opacity: 0, y: 80, scale: 0.95 }} whileInView={{ opacity: 1, y: 0, scale: 1 }} viewport={{ once: true, margin: "-10% 0px" }} transition={{ duration: 1.1, ease }} className="relative overflow-hidden rounded-[28px] px-[6vw] py-16 md:rounded-[36px] md:px-[5vw] md:py-24" style={{ background: C.lime }}>
        <div dir="ltr" className="absolute inset-x-0 top-8 overflow-hidden whitespace-nowrap opacity-[0.08]">
          <motion.div animate={{ x: ["0%", "-50%"] }} transition={{ duration: 30, repeat: Infinity, ease: "linear" }} className="inline-block text-[120px] font-extrabold tracking-[-0.04em]" style={{ color: C.night }}>
            {[...D.skills.slice(0, 6), ...D.skills.slice(0, 6)].join(" / ")} /&nbsp;
          </motion.div>
        </div>
        <div className="relative">
          <Eyebrow n="07">{D.scene("return")?.title ?? D.t.contactFallback}</Eyebrow>
          <MagneticHeadline text={D.t.contactTitle} />
          <div className="mt-12 flex flex-wrap gap-3">
            <Magnetic>
              <a href={`mailto:${D.email}`} data-cursor={D.t.write} className="inline-flex min-h-[44px] max-w-full items-center gap-2 break-all rounded-full px-6 py-4 text-[15px] font-semibold" style={{ background: C.night, color: C.cream }}><Mail size={16} /> {D.email} <ArrowUpRight size={16} /></a>
            </Magnetic>
            <Magnetic>
              <a href={`tel:${D.phone}`} data-cursor={D.t.call} className="inline-flex min-h-[44px] items-center gap-2 rounded-full border px-6 py-4 text-[15px] font-semibold" style={{ borderColor: C.night, color: C.night }}><Phone size={16} /> <bdi dir="ltr">{D.phone}</bdi></a>
            </Magnetic>
            <Magnetic>
              <a href={D.cvUrl} download target="_blank" rel="noreferrer" data-cursor="CV" className="inline-flex min-h-[44px] items-center gap-2 rounded-full border px-6 py-4 text-[15px] font-semibold" style={{ borderColor: C.night, color: C.night }}><Download size={16} /> {D.t.cv}</a>
            </Magnetic>
          </div>
        </div>
      </motion.div>
      <div className="mt-8 flex flex-col gap-2 px-[2vw] text-[12px] md:flex-row md:justify-between" style={{ color: C.muted }}>
        <span>{D.name}, {D.role}</span>
        <span className="font-mono">{D.t.footer}</span>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* CHROME                                                              */
/* ------------------------------------------------------------------ */

const CHAPTERS = ["method", "build", "connect", "offline", "work", "experience", "contact"];

function LangToggle() {
  const D = useD();
  const opts: { id: Lang; label: string }[] = [
    { id: "en", label: "EN" },
    { id: "ar", label: "عربي" },
  ];
  return (
    <span dir="ltr" role="group" aria-label="Language" className="relative flex rounded-full p-0.5 text-[12px] font-bold" style={{ background: C.cream }}>
      {opts.map((o) => {
        const on = D.lang === o.id;
        return (
          <a
            key={o.id}
            href={profilePath(D.profileSlug, o.id, D.isDefault)}
            hrefLang={o.id}
            onClick={(event) => {
              if (on || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return;
              event.preventDefault();
              D.switchLang(o.id);
            }}
            aria-current={on ? "page" : undefined}
            data-cursor={o.id === "ar" ? "AR" : "EN"}
            className="relative inline-flex min-h-[44px] min-w-[48px] items-center justify-center rounded-full px-3"
            style={{ color: on ? C.lime : C.muted, fontFamily: o.id === "ar" ? AR_FONT : undefined }}
          >
            {on && <motion.span layoutId="msf-lang-pill" className="absolute inset-0 rounded-full" style={{ background: C.ink }} transition={{ type: "spring", stiffness: 420, damping: 34 }} />}
            <span className="relative">{o.label}</span>
          </a>
        );
      })}
    </span>
  );
}

function Chrome() {
  const D = useD();
  const chapters = useMemo(() => CHAPTERS.filter(id =>
    ["method", "work", "contact"].includes(id)
    || (id === "build" && hasAny(D, ["layers"]))
    || (id === "connect" && hasAny(D, ["backend", "backend-focus", "integration"]))
    || (id === "offline" && hasAny(D, ["offline"]))
    || (id === "experience" && D.experience.length > 0)), [D]);
  const { scrollYProgress } = useScroll();
  const bar = useSpring(scrollYProgress, { stiffness: 120, damping: 30 });
  const [active, setActive] = useState("");
  useEffect(() => {
    const onScroll = () => {
      const mid = window.innerHeight * 0.5;
      let cur = "";
      chapters.forEach((id) => {
        const el = document.getElementById(id);
        if (el && el.getBoundingClientRect().top < mid) cur = id;
      });
      setActive(cur);
    };
    onScroll();
    window.addEventListener("scroll", onScroll, { passive: true });
    return () => window.removeEventListener("scroll", onScroll);
  }, [chapters]);
  const label = (active && D.t.chapters[active]) || D.t.hello;
  return (
    <>
      <motion.div className="fixed left-0 right-0 top-0 z-50 h-[3px]" style={{ scaleX: bar, background: C.forest, transformOrigin: D.rtl ? "right" : "left" }} />
      <motion.nav initial={{ y: -40, opacity: 0 }} animate={{ y: 0, opacity: 1 }} transition={{ delay: 0.2, duration: 0.8, ease }} className="fixed left-1/2 top-3 z-50 flex w-max max-w-[94vw] -translate-x-1/2 items-center gap-3 rounded-full border py-1.5 pe-1.5 ps-2 backdrop-blur-md md:top-4 md:gap-6 md:px-3 md:py-2" style={{ borderColor: "rgba(191,216,197,0.6)", background: "rgba(252,253,249,0.78)" }}>
        <a href="#top" className="flex min-h-[44px] items-center gap-2 ps-1 text-[13px] font-bold md:min-h-0" style={{ color: C.ink }}>
          <span dir="ltr" className="grid h-7 w-7 place-items-center rounded-full font-mono text-[10px]" style={{ background: C.ink, color: C.lime }}>oa</span>
          <span className="whitespace-nowrap">{D.name}</span>
        </a>
        <span className="relative hidden h-5 w-[96px] overflow-hidden text-[12px] font-semibold md:inline-block" style={{ color: C.muted }}>
          <motion.span key={label} initial={{ y: 16, opacity: 0 }} animate={{ y: 0, opacity: 1 }} transition={{ duration: 0.35, ease }} className="absolute inset-0">{label}</motion.span>
        </span>
        <LangToggle />
      </motion.nav>
      <div className="fixed start-6 top-1/2 z-40 hidden -translate-y-1/2 flex-col gap-3 lg:flex">
        {chapters.map((id) => (
          <a key={id} href={`#${id}`} aria-label={D.t.chapters[id]} className="group flex items-center gap-3 text-[11px] font-semibold uppercase tracking-[0.16em]" style={{ color: active === id ? C.ink : C.muted }}>
            <motion.span animate={{ width: active === id ? 28 : 10, backgroundColor: active === id ? C.forest : C.soft }} className="block h-[2px]" />
            <span className="rounded-full px-2 py-0.5 opacity-0 transition-opacity group-hover:opacity-100" style={{ background: "rgba(252,253,249,0.92)" }}>{D.t.chapters[id]}</span>
          </a>
        ))}
      </div>
    </>
  );
}

/* ------------------------------------------------------------------ */

const LATIN_FONT = "'Manrope', system-ui, sans-serif";
const AR_FONT = "'Readex Pro Variable', 'Manrope', system-ui, sans-serif";

function Page() {
  const D = useD();
  const reduce = useReducedMotion();

  useEffect(() => {
    if (reduce || D.mobile) return;
    const lenis = new Lenis({ duration: 1.1, smoothWheel: true });
    let raf = 0;
    const loop = (t: number) => {
      lenis.raf(t);
      raf = requestAnimationFrame(loop);
    };
    raf = requestAnimationFrame(loop);
    return () => {
      cancelAnimationFrame(raf);
      lenis.destroy();
    };
  }, [reduce, D.mobile]);

  const cursorOn = D.fine && !reduce && !D.mobile;
  return (
    <main id="top" tabIndex={-1} className={`msf-root relative min-h-[100dvh] overflow-x-clip ${cursorOn ? "msf-cursor" : ""}`} style={{ background: C.cream, fontFamily: D.rtl ? AR_FONT : LATIN_FONT, color: C.ink }}>
      <div className="pointer-events-none fixed inset-0 z-[60] opacity-[0.05] mix-blend-multiply" style={{ backgroundImage: "url(\"data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='160' height='160'%3E%3Cfilter id='n'%3E%3CfeTurbulence baseFrequency='0.8'/%3E%3C/filter%3E%3Crect width='100%25' height='100%25' filter='url(%23n)'/%3E%3C/svg%3E\")" }} />
      {cursorOn && <Cursor />}
      <Chrome />
      <Hero />
      <ChapterListen />
      <ChapterBuild />
      <ChapterConnect />
      <ChapterOffline />
      <VelocityMarquee words={D.skills} />
      <ChapterWork />
      <ChapterExperience />
      <VelocityMarquee words={D.categories} base={-2.5} />
      <Contact />
    </main>
  );
}

/** Keeps the approved language transition while navigating real localized URLs. */
function LangWipe({ to }: { to: Lang }) {
  return <motion.div className="pointer-events-none fixed inset-0 z-[200] grid place-items-center" style={{ background: C.forest }}
    initial={{ x: to === "ar" ? "100%" : "-100%" }} animate={{ x: "0%" }} transition={{ duration: 0.42, ease: [0.76, 0, 0.24, 1] }}>
    <span className="text-[clamp(48px,10vw,140px)] font-extrabold" style={{ color: C.lime, fontFamily: to === "ar" ? AR_FONT : LATIN_FONT }}>{to === "ar" ? "عربي" : "English"}</span>
  </motion.div>;
}

export function PortfolioPage({ profile, baseUrl, isDefault = false, siteId, apiBaseUrl }: {
  profile: PublicProfile;
  baseUrl: string;
  isDefault?: boolean;
  siteId?: string;
  apiBaseUrl: string;
}) {
  const lang = profile.locale;
  const [ready, setReady] = useState(false);
  useEffect(() => { setReady(true); }, []);
  const [wipeTo, setWipeTo] = useState<Lang | null>(null);
  const navigationTimer = useRef<number | null>(null);
  const reduce = useReducedMotion();
  useEffect(() => () => { if (navigationTimer.current !== null) window.clearTimeout(navigationTimer.current); }, []);
  const mobile = useMedia("(max-width: 767px)");
  const fine = useMedia("(pointer: fine)");
  const { projects, status, syncedAt } = useLiveProjects({ profile, siteId, apiBaseUrl });
  const switchLang = useCallback((locale: Lang) => {
    if (navigationTimer.current !== null || locale === lang) return;
    if (reduce) return window.location.assign(profilePath(profile.slug, locale, isDefault));
    setWipeTo(locale);
    navigationTimer.current = window.setTimeout(() => window.location.assign(profilePath(profile.slug, locale, isDefault)), 450);
  }, [profile.slug, isDefault, lang, reduce]);
  const view = useMemo(() => toView(profile, projects, { ready, profileSlug: profile.slug, isDefault, lang, mobile, fine, live: { status, syncedAt }, switchLang }), [ready, profile, projects, isDefault, lang, mobile, fine, status, syncedAt, switchLang]);
  const canonical = profile.seo.canonical ?? `${baseUrl}${profilePath(profile.slug, lang, isDefault)}`;
  const jsonLd = { "@context": "https://schema.org", "@graph": [
    { "@type": "WebSite", "@id": `${baseUrl}/#website`, url: `${baseUrl}/`, name: profile.fullName, inLanguage: ["ar", "en"] },
    { "@type": "ProfilePage", "@id": `${canonical}#profile-page`, url: canonical, inLanguage: lang, isPartOf: { "@id": `${baseUrl}/#website` }, mainEntity: { "@id": `${baseUrl}/#person` } },
    { "@type": "Person", "@id": `${baseUrl}/#person`, name: profile.fullName, jobTitle: profile.headline, description: profile.summary, knowsAbout: profile.skills.map(skill => skill.name), sameAs: profile.links.map(link => link.url) }
  ] };
  return <MotionConfig reducedMotion="user">
    <ProfileCtx.Provider value={view}>
      <div dir={view.rtl ? "rtl" : "ltr"} lang={lang}>
        <a className="skip-link" href="#work">{view.rtl ? "انتقل إلى المشاريع" : "Skip to projects"}</a>
        <Page />
      </div>
      {wipeTo && <LangWipe to={wipeTo} />}
      <noscript><style>{`
        .msf-root [style] { opacity: 1 !important; transform: none !important; filter: none !important; }
        .msf-root .msf-beat { position: relative !important; inset: auto !important; grid-area: auto !important; margin-block: 24px; }
        .msf-root .msf-pinned { height: auto !important; padding-block: 48px; }
        .msf-root .msf-pinned > div { position: relative !important; height: auto !important; min-height: auto !important; overflow: visible !important; }
        .msf-root .msf-pinned > div > div { height: auto !important; min-height: 360px; }
        .msf-root #work { overflow-x: auto; }
        .msf-root .msf-h1-ghost { color: inherit; }
      `}</style></noscript>
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd).replace(/</g, "\\u003c") }} />
    </ProfileCtx.Provider>
  </MotionConfig>;
}
