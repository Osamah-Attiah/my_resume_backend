"use client";

import { ChevronDown } from "lucide-react";
import { useId, useState } from "react";

export function ExpandableSummary({
  preview,
  remainder,
  moreLabel,
  lessLabel
}: {
  preview: string;
  remainder: string;
  moreLabel: string;
  lessLabel: string;
}) {
  const [expanded, setExpanded] = useState(false);
  const contentId = useId();

  return <div className={"about-summary-block" + (expanded ? " is-expanded" : "")}>
    <p className="about-lead">{preview}</p>
    {remainder && <>
      <div id={contentId} className="about-more-content" aria-hidden={!expanded}>
        <div className="about-more-content-inner">
          <p className="about-lead about-lead-more">{remainder}</p>
        </div>
      </div>
      <button
        type="button"
        className="about-more-toggle"
        aria-expanded={expanded}
        aria-controls={contentId}
        onClick={() => setExpanded(value => !value)}
      >
        <span className="about-more-control">
          <span className="about-more-labels">
            <span className="about-more-label about-more-label-more">{moreLabel}</span>
            <span className="about-more-label about-more-label-less">{lessLabel}</span>
          </span>
          <span className="about-more-icon" aria-hidden="true"><ChevronDown size={16} strokeWidth={1.8} /></span>
        </span>
      </button>
    </>}
  </div>;
}
