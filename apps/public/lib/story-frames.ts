import type { CSSProperties } from "react";
import type { StoryScene } from "@resume/contracts";

type Stage = StoryScene["stage"];
export type StoryFrame = Record<"phoneX" | "phoneY" | "phoneRotate" | "phoneTilt" | "phoneScale" | "phoneOpacity" | "layersOpacity" | "layerSpread" | "layerRise" | "localOpacity" | "routeOpacity" | "packetPosition" | "backendOpacity" | "backendX" | "backendScale" | "backendFocus" | "returnOpacity" | "integrationOpacity" | "operationsOpacity", number>;

const base: StoryFrame = { phoneX: 0, phoneY: 0, phoneRotate: 0, phoneTilt: -4, phoneScale: .96, phoneOpacity: 1, layersOpacity: 0, layerSpread: 0, layerRise: 0, localOpacity: 0, routeOpacity: 0, packetPosition: 0, backendOpacity: 0, backendX: 0, backendScale: 1, backendFocus: 0, returnOpacity: 0, integrationOpacity: 0, operationsOpacity: 0 };

export const storyFrames: Record<Stage, StoryFrame> = {
  intro: { ...base },
  action: { ...base, phoneTilt: -7, phoneScale: .98 },
  layers: { ...base, phoneTilt: -1, phoneScale: 1, layersOpacity: 1 },
  offline: { ...base, phoneTilt: 2, phoneScale: .98, localOpacity: 1 },
  backend: { ...base, phoneTilt: 0, phoneScale: .96, routeOpacity: 1, packetPosition: 1, backendOpacity: 1 },
  "backend-focus": { ...base, phoneTilt: 0, phoneScale: .96, phoneOpacity: 1, routeOpacity: 1, packetPosition: 1, backendOpacity: 1, backendFocus: 1 },
  integration: { ...base, phoneTilt: 3, phoneScale: .98, integrationOpacity: 1 },
  operations: { ...base, phoneTilt: -2, phoneScale: 1, operationsOpacity: 1 },
  return: { ...base, phoneTilt: 0, phoneScale: .98, returnOpacity: 1 }
};

export function storyFrameStyle(stage: Stage): CSSProperties {
  return Object.fromEntries(Object.entries(storyFrames[stage]).map(([key, value]) => [`--${key.replace(/[A-Z]/g, char => `-${char.toLowerCase()}`)}`, value])) as CSSProperties;
}
