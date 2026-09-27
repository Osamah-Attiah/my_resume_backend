(() => {
  const root = document.documentElement;
  const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)");
  const storySequence = document.querySelector("[data-story-sequence]");
  if (storySequence && !reducedMotion.matches) {
    const beats = Array.from(storySequence.querySelectorAll("[data-story-beat]"));
    const anchors = beats.map(beat => beat.querySelector("[data-story-anchor]"));
    const visual = storySequence.querySelector("[data-story-visual]");
    const frames = anchors.map(anchor => JSON.parse(anchor?.dataset.storyFrame || "{}"));
    const keys = Object.keys(frames[0] || {});
    const stageLabels = JSON.parse(visual?.dataset.storyLabels || "{}");
    const clamp = value => Math.min(1, Math.max(0, value));
    const ramp = (value, start, end) => {
      const amount = clamp((value - start) / Math.max(end - start, .001));
      return amount * amount * (3 - 2 * amount);
    };
    const setSceneInteraction = (stage, progress) => {
      const state = {
        introPulse: 0, actionCue: 0, actionFill: 0, actionPress: 0, actionSending: 0, actionComplete: 0,
        layerStep1: 0, layerStep2: 0, layerStep3: 0, offlineReveal: 0, offlineSync: 0,
        requestProgress: 0, responseProgress: 0, backendStep1: 0, backendStep2: 0, backendStep3: 0, backendStep4: 0,
        integrationStep1: 0, integrationStep2: 0, integrationStep3: 0,
        operationStep1: 0, operationStep2: 0, operationStep3: 0, operationStep4: 0
      };
      if (stage === "intro") state.introPulse = ramp(progress, .18, .78);
      if (stage === "action") {
        state.actionFill = ramp(progress, .04, .28);
        state.actionCue = ramp(progress, .18, .36) * (1 - ramp(progress, .45, .62));
        state.actionPress = ramp(progress, .37, .48) * (1 - ramp(progress, .53, .62));
        state.actionSending = ramp(progress, .43, .56) * (1 - ramp(progress, .62, .73));
        state.actionComplete = ramp(progress, .62, .8);
      }
      if (stage === "layers") {
        state.layerStep1 = ramp(progress, .04, .27);
        state.layerStep2 = ramp(progress, .2, .47);
        state.layerStep3 = ramp(progress, .37, .64);
      }
      if (stage === "offline") {
        state.offlineReveal = ramp(progress, .06, .32);
        state.offlineSync = ramp(progress, .28, .86);
        state.requestProgress = state.offlineSync;
      }
      if (stage === "backend" || stage === "backend-focus") {
        state.requestProgress = ramp(progress, .08, .39);
        state.backendStep1 = ramp(progress, .19, .38);
        state.backendStep2 = ramp(progress, .36, .55);
        state.backendStep3 = ramp(progress, .53, .72);
        state.backendStep4 = ramp(progress, .7, .88);
        state.responseProgress = ramp(progress, .65, .94);
      }
      if (stage === "integration") {
        state.integrationStep1 = ramp(progress, .04, .28);
        state.integrationStep2 = ramp(progress, .28, .56);
        state.integrationStep3 = ramp(progress, .56, .84);
      }
      if (stage === "operations") {
        state.operationStep1 = ramp(progress, .04, .22);
        state.operationStep2 = ramp(progress, .22, .42);
        state.operationStep3 = ramp(progress, .42, .62);
        state.operationStep4 = ramp(progress, .62, .82);
      }
      if (stage === "return") state.actionComplete = ramp(progress, .1, .5);
      Object.entries(state).forEach(([name, value]) => {
        const cssName = name.replace(/[A-Z]/g, char => "-" + char.toLowerCase()).replace(/(\D)(\d)/g, "$1-$2");
        visual.style.setProperty(`--${cssName}`, String(value));
      });
    };
    let storyFrame = 0;
    let activeIndex = -1;
    let transitionTimer = 0;
    const updateStory = () => {
      storyFrame = 0;
      const height = window.innerHeight;
      if (!visual || !beats.length) return;
      const beatBounds = beats.map(beat => beat.getBoundingClientRect());
      beats.forEach((beat, at) => {
        const bounds = beatBounds[at];
        const entered = height - bounds.top;
        beat.style.setProperty("--beat-reveal", String(ramp(entered, height * .12, height * .65)));
        beat.style.setProperty("--beat-detail", String(ramp(entered, height * .23, height * .78)));
      });
      const marker = height * .5;
      let index = beatBounds.findIndex(bounds => bounds.top <= marker && bounds.bottom >= marker);
      if (index < 0) {
        let nearest = Infinity;
        beatBounds.forEach((bounds, at) => {
          const distance = Math.abs(bounds.top + bounds.height * .5 - marker);
          if (distance < nearest) { nearest = distance; index = at; }
        });
      }
      let travelFrom = index;
      let travelTo = index;
      let travelMix = 1;
      for (let next = 1; next < beats.length; next++) {
        const boundary = beatBounds[next].top;
        if (boundary > height * 1.1 || boundary < height * .05) continue;
        travelFrom = next - 1;
        travelTo = next;
        travelMix = ramp(height * 1.1 - boundary, 0, height * 1.05);
        const threshold = activeIndex === next ? .44 : activeIndex === next - 1 ? .56 : .5;
        index = travelMix >= threshold ? next : next - 1;
        break;
      }
      const anchor = anchors[index];
      const beat = beats[index];
      if (!anchor || !beat) return;
      const fromAnchor = anchors[travelFrom]?.getBoundingClientRect() ?? anchor.getBoundingClientRect();
      const toAnchor = anchors[travelTo]?.getBoundingClientRect() ?? fromAnchor;
      const mix = (from, to) => from + (to - from) * travelMix;
      const travelArc = travelFrom === travelTo ? 0 : 4 * travelMix * (1 - travelMix);
      const mixedTop = mix(fromAnchor.top, toAnchor.top);
      const compactTravel = window.innerWidth <= 900;
      const arrivalDrop = compactTravel ? 1 - ramp(travelMix, .45, .82) : 1;
      const lift = Math.min(height * .25 * travelArc * arrivalDrop, Math.max(0, mixedTop - 24));
      const titleClearance = compactTravel ? height * .075 * ramp(travelMix, .65, .8) * (1 - ramp(travelMix, .93, 1)) : 0;
      const travelBounds = {
        left: mix(fromAnchor.left, toAnchor.left),
        top: mixedTop - lift + titleClearance,
        width: mix(fromAnchor.width, toAnchor.width),
        height: mix(fromAnchor.height, toAnchor.height)
      };
      const bounds = beatBounds[index];
      const progress = clamp((height - bounds.top) / Math.max(height + bounds.height, 1));
      if (index !== activeIndex) {
        activeIndex = index;
        visual.classList.add("is-transitioning");
        if (transitionTimer) window.clearTimeout(transitionTimer);
        transitionTimer = window.setTimeout(() => visual.classList.remove("is-transitioning"), 740);
      }
      visual.classList.add("is-travelling");
      visual.style.width = `${travelBounds.width}px`;
      visual.style.height = `${travelBounds.height}px`;
      visual.style.transform = `translate3d(${travelBounds.left}px,${travelBounds.top}px,0)`;
      const stage = anchor.dataset.storyStage || "intro";
      visual.dataset.storyStage = stage;
      visual.querySelector("[data-story-stage-label]").textContent = stageLabels[stage] || "";
      visual.querySelector("[data-story-counter]").textContent = `${String(index + 1).padStart(2, "0")} / ${String(beats.length).padStart(2, "0")}`;
      visual.querySelector("[data-story-caption]").textContent = anchor.dataset.storyLabel || stageLabels[stage] || "";
      visual.querySelector("[data-story-progress]").style.width = `${((index + 1) / beats.length) * 100}%`;
      const routeLabel = visual.querySelector("[data-story-route-label]");
      if (routeLabel) routeLabel.textContent = stage === "offline" ? (document.documentElement.lang === "ar" ? "مزامنة عند عودة الاتصال" : "Sync when connected") : (document.documentElement.lang === "ar" ? "طلب إلى الخدمة" : "Request to service");
      const from = frames[travelFrom];
      const to = frames[travelTo];
      keys.forEach(key => {
        const value = from[key] + (to[key] - from[key]) * travelMix;
        visual.style.setProperty("--" + key.replace(/[A-Z]/g, char => "-" + char.toLowerCase()), String(value));
      });
      const shellTop = visual.querySelector(".story-phone")?.getBoundingClientRect().top ?? travelBounds.top;
      const interactionProgress = clamp((height * .75 - shellTop) / (height * .65));
      setSceneInteraction(stage, interactionProgress);
      visual.style.setProperty("--interaction-progress", String(interactionProgress));
      visual.style.setProperty("--scene-progress", String(progress));
      visual.style.setProperty("--travel-mix", String(travelFrom === travelTo ? 0 : travelMix));
      const travelGhost = travelFrom === travelTo ? 0 : ramp(travelMix, .03, .18) * (1 - ramp(travelMix, .72, .96));
      visual.style.setProperty("--travel-ghost", String(travelGhost));
      const driftDirection = document.documentElement.dir === "rtl" ? -1 : 1;
      visual.style.setProperty("--travel-scale", String(1 - (compactTravel ? .66 : .14) * travelArc));
      visual.style.setProperty("--travel-drift", `${compactTravel ? driftDirection * Math.min(window.innerWidth * .22, 96) * travelArc : 0}px`);
      if (visual.classList.contains("is-transitioning")) requestStory();
    };
    const requestStory = () => { if (!storyFrame) storyFrame = requestAnimationFrame(updateStory); };
    window.addEventListener("scroll", requestStory, { passive: true });
    window.addEventListener("resize", requestStory, { passive: true });
    requestStory();
  }
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
