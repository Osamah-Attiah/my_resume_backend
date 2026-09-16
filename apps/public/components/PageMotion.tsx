"use client";

import { useEffect } from "react";

export function PageMotion() {
  useEffect(() => {
    const root = document.documentElement;
    const elements = Array.from(document.querySelectorAll<HTMLElement>("[data-reveal]"));
    const sections = Array.from(document.querySelectorAll<HTMLElement>("[data-section]"));
    const railLinks = Array.from(document.querySelectorAll<HTMLAnchorElement>("[data-index-target]"));
    const sideIndex = document.querySelector<HTMLElement>(".side-index");
    const darkSections = new Set(["intro"]);
    const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)");
    let frame = 0;

    const setActiveSection = (activeSection: string | undefined) => {
      railLinks.forEach(link => {
        const isActive = link.dataset.indexTarget === activeSection;
        link.classList.toggle("is-active", isActive);
        if (isActive) link.setAttribute("aria-current", "location");
        else link.removeAttribute("aria-current");
      });
    };

    const updateScrollState = () => {
      frame = 0;
      const scrollableHeight = Math.max(document.documentElement.scrollHeight - window.innerHeight, 1);
      const progress = Math.min(Math.max(window.scrollY / scrollableHeight, 0), 1);
      root.style.setProperty("--scroll-progress", String(progress));
      document.querySelectorAll<HTMLElement>("[data-scroll-value]").forEach(value => {
        value.textContent = Math.round(progress * 100) + "%";
      });

      const marker = window.innerHeight * 0.34;
      let activeSection = sections[0]?.dataset.section;
      sections.forEach(section => {
        if (section.getBoundingClientRect().top <= marker) activeSection = section.dataset.section;
      });
      sideIndex?.classList.toggle("is-dark", Boolean(activeSection && darkSections.has(activeSection)));
      setActiveSection(activeSection);
    };

    const onScroll = () => {
      if (frame) return;
      frame = window.requestAnimationFrame(updateScrollState);
    };

    root.dataset.motion = "ready";

    if (reducedMotion.matches || !("IntersectionObserver" in window)) {
      elements.forEach(element => element.classList.add("is-visible"));
    } else {
      const observer = new IntersectionObserver(entries => {
        entries.forEach(entry => {
          if (!entry.isIntersecting) return;
          entry.target.classList.add("is-visible");
          observer.unobserve(entry.target);
        });
      }, { rootMargin: "0px 0px -10% 0px", threshold: 0.08 });
      elements.forEach(element => observer.observe(element));

      window.addEventListener("scroll", onScroll, { passive: true });
      window.addEventListener("resize", onScroll, { passive: true });
      updateScrollState();

      return () => {
        observer.disconnect();
        window.removeEventListener("scroll", onScroll);
        window.removeEventListener("resize", onScroll);
        if (frame) window.cancelAnimationFrame(frame);
        root.style.removeProperty("--scroll-progress");
        delete root.dataset.motion;
      };
    }

    window.addEventListener("scroll", onScroll, { passive: true });
    window.addEventListener("resize", onScroll, { passive: true });
    updateScrollState();

    return () => {
      window.removeEventListener("scroll", onScroll);
      window.removeEventListener("resize", onScroll);
      if (frame) window.cancelAnimationFrame(frame);
      root.style.removeProperty("--scroll-progress");
      delete root.dataset.motion;
    };
  }, []);

  return null;
}
