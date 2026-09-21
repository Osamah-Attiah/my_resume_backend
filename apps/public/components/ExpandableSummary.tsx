import { ChevronDown } from "lucide-react";

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
  const contentId = "about-more-content";

  return <div className="about-summary-block" data-expandable-summary>
    <p className="about-lead">{preview}</p>
    {remainder && <>
      <div id={contentId} className="about-more-content" aria-hidden="true">
        <div className="about-more-content-inner">
          <p className="about-lead about-lead-more">{remainder}</p>
        </div>
      </div>
      <button
        type="button"
        className="about-more-toggle"
        aria-expanded="false"
        aria-controls={contentId}
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
