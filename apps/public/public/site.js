(() => {
  const root = document.documentElement;
  const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)");
  const revealElements = Array.from(document.querySelectorAll("[data-reveal]"));
  const sections = Array.from(document.querySelectorAll("[data-section]"));
  const railLinks = Array.from(document.querySelectorAll("[data-index-target]"));
  const sideIndex = document.querySelector(".side-index");
  const darkSections = new Set(["intro"]);
  let frame = 0;

  document.querySelectorAll("[data-expandable-summary]").forEach(block => {
    const button = block.querySelector(".about-more-toggle");
    const content = block.querySelector(".about-more-content");
    if (!button || !content) return;

    button.addEventListener("click", () => {
      const expanded = button.getAttribute("aria-expanded") !== "true";
      block.classList.toggle("is-expanded", expanded);
      button.setAttribute("aria-expanded", String(expanded));
      content.setAttribute("aria-hidden", String(!expanded));
    });
  });

  const setActiveSection = activeSection => {
    railLinks.forEach(link => {
      const active = link.dataset.indexTarget === activeSection;
      link.classList.toggle("is-active", active);
      if (active) link.setAttribute("aria-current", "location");
      else link.removeAttribute("aria-current");
    });
  };

  const activeSectionAtMarker = () => {
    const marker = Math.min(window.innerHeight * 0.34, 280);
    let activeSection;
    let passedSection;

    sections.forEach(section => {
      const sectionName = section.dataset.section;
      if (!sectionName) return;
      const bounds = section.getBoundingClientRect();
      if (bounds.top <= marker && bounds.bottom >= marker) activeSection = sectionName;
      if (bounds.top <= marker) passedSection = sectionName;
    });

    return activeSection || passedSection || sections[0]?.dataset.section;
  };

  const updateScrollState = () => {
    frame = 0;
    const scrollableHeight = Math.max(document.documentElement.scrollHeight - window.innerHeight, 1);
    const progress = Math.min(Math.max(window.scrollY / scrollableHeight, 0), 1);
    root.style.setProperty("--scroll-progress", String(progress));
    document.querySelectorAll("[data-scroll-value]").forEach(value => {
      value.textContent = Math.round(progress * 100) + "%";
    });

    const activeSection = activeSectionAtMarker();
    sideIndex?.classList.toggle("is-dark", Boolean(activeSection && darkSections.has(activeSection)));
    setActiveSection(activeSection);
  };

  const onScroll = () => {
    if (frame) return;
    frame = window.requestAnimationFrame(updateScrollState);
  };

  railLinks.forEach(link => link.addEventListener("click", () => {
    setActiveSection(link.dataset.indexTarget);
    onScroll();
  }));

  root.dataset.motion = "ready";
  if (reducedMotion.matches || !("IntersectionObserver" in window)) {
    revealElements.forEach(element => element.classList.add("is-visible"));
  } else {
    const observer = new IntersectionObserver(entries => {
      entries.forEach(entry => {
        if (!entry.isIntersecting) return;
        entry.target.classList.add("is-visible");
        observer.unobserve(entry.target);
      });
    }, { rootMargin: "0px 0px -10% 0px", threshold: 0.08 });
    revealElements.forEach(element => observer.observe(element));
  }

  window.addEventListener("scroll", onScroll, { passive: true });
  window.addEventListener("resize", onScroll, { passive: true });
  updateScrollState();
})();
