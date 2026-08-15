import React, { CSSProperties, useLayoutEffect, useState } from "react";
import { useGridContext } from "./GridContext";
import VirtualGrid from "./VirtualGrid";
import StickyColumn from "./StickyColumn";
import StickyHeader from "./StickyHeader";
import StickyCorner from "./StickyCorner";

interface GridContainerProps {
  width: number;
  height: number;
  scrollRef: React.RefObject<HTMLDivElement | null>;
}

const cornerRadius = (
  style: CSSProperties | undefined,
  corner:
    | "borderTopLeftRadius"
    | "borderTopRightRadius"
    | "borderBottomLeftRadius"
    | "borderBottomRightRadius"
) => {
  return style?.[corner] ?? style?.borderRadius;
};

const useSnapSize = () => {
  const gridContext = useGridContext();
  const { snapRows, snapColumns, columnWidths, rowHeights } = gridContext;

  let snapWidth = 0;
  for (let c = 0; c < snapColumns; c++) {
    snapWidth += columnWidths[c] ?? 0;
  }
  let snapHeight = 0;
  for (let r = 0; r < snapRows; r++) {
    snapHeight += rowHeights[r] ?? 0;
  }
  return { snapWidth, snapHeight };
};

const chromeBoxClassName = "absolute border border-solid border-gray-500";

// Main-pane chrome: carries the public `style` prop. Rendered *below* the
// body cells (before VirtualGrid, no z-index) so an opaque consumer
// background sits behind cell content — matching the old implementation,
// where `style` was applied to the pane element behind its children.
const BodyChromeLayer = (props: { width: number; height: number }) => {
  const gridContext = useGridContext();
  const { snapWidth, snapHeight } = useSnapSize();

  return (
    <div className="sticky top-0 left-0" style={{ width: 0, height: 0 }}>
      <div
        className={chromeBoxClassName}
        style={{
          pointerEvents: "none",
          top: snapHeight,
          left: snapWidth,
          width: Math.max(0, props.width - snapWidth),
          height: Math.max(0, props.height - snapHeight),
          ...gridContext.props.style,
        }}
      ></div>
    </div>
  );
};

// The body chrome box renders below the cells, so opaque cell backgrounds
// (disabled fill, range fill) swallow its border wherever a cell touches a
// pane boundary — e.g. a disabled column thinned the line under a frozen
// header row. Redraw just the border above the cells; the consumer `style`
// minus its background comes along so border overrides stay consistent.
const BodyBorderLayer = (props: { width: number; height: number }) => {
  const gridContext = useGridContext();
  const { snapWidth, snapHeight } = useSnapSize();
  const { background, backgroundColor, ...styleSansBackground } =
    gridContext.props.style ?? {};

  return (
    <div className="sticky top-0 left-0 z-10" style={{ width: 0, height: 0 }}>
      <div
        className={chromeBoxClassName}
        style={{
          pointerEvents: "none",
          top: snapHeight,
          left: snapWidth,
          width: Math.max(0, props.width - snapWidth),
          height: Math.max(0, props.height - snapHeight),
          ...styleSansBackground,
        }}
      ></div>
    </div>
  );
};

// Frozen-pane chrome: the styletr/stylebl boxes (styletl → StickyCorner).
// These stay layered above the body grid and are opaque so body cells
// scrolling underneath the sticky overlays don't bleed through the
// transparent cell backgrounds.
const FrozenChromeLayer = (props: { width: number; height: number }) => {
  const gridContext = useGridContext();
  const { snapRows, snapColumns } = gridContext;
  const { snapWidth, snapHeight } = useSnapSize();

  const paneBackground = "var(--mantine-color-body, transparent)";

  if (snapRows === 0 && snapColumns === 0) {
    return null;
  }

  return (
    <div className="sticky top-0 left-0 z-10" style={{ width: 0, height: 0 }}>
      {snapRows > 0 && (
        <div
          className={chromeBoxClassName}
          style={{
            pointerEvents: "none",
            top: 0,
            left: snapWidth,
            width: Math.max(0, props.width - snapWidth),
            height: snapHeight,
            backgroundColor: paneBackground,
            ...gridContext.props.styletr,
          }}
        ></div>
      )}
      {snapColumns > 0 && (
        <div
          className={chromeBoxClassName}
          style={{
            pointerEvents: "none",
            top: snapHeight,
            left: 0,
            width: snapWidth,
            height: Math.max(0, props.height - snapHeight),
            backgroundColor: paneBackground,
            ...gridContext.props.stylebl,
          }}
        ></div>
      )}
    </div>
  );
};

const GridContainer = (props: GridContainerProps) => {
  const gridContext = useGridContext();
  const { totalWidth, totalHeight } = gridContext.virtualized;
  const gridProps = gridContext.props;

  // The chrome boxes must span the scrollport (client area), not the outer
  // width/height — sizing them to the outer box would extend the scrollable
  // area under the scrollbars, forcing a phantom scrollbar on the other axis
  // and pushing the right/bottom pane borders out of view.
  const [clientSize, setClientSize] = useState<{
    width: number;
    height: number;
  } | null>(null);

  const lastClientSize = React.useRef<{ width: number; height: number } | null>(
    null
  );
  useLayoutEffect(() => {
    const el = props.scrollRef.current;
    if (el == null) return;
    const update = () => {
      const width = el.clientWidth;
      const height = el.clientHeight;
      const last = lastClientSize.current;
      if (last != null && last.width === width && last.height === height) {
        return;
      }
      lastClientSize.current = { width, height };
      setClientSize({ width, height });
      // The virtualizers do not recover their range on their own after the
      // scroll element was hidden (display:none in an inactive tab collapses
      // its rect to 0) and shown again — force a re-measure on size change.
      gridContext.virtualized.rowVirtualizer.measure();
      gridContext.virtualized.columnVirtualizer.measure();
    };
    update();
    // content-box observation also fires when a scrollbar appears/disappears
    const observer = new ResizeObserver(update);
    observer.observe(el);
    return () => observer.disconnect();
  }, [totalWidth, totalHeight, props.width, props.height]);

  const chromeWidth = clientSize?.width ?? props.width;
  const chromeHeight = clientSize?.height ?? props.height;

  return (
    <div
      ref={props.scrollRef}
      className="thin-scrollbar-x overflow-auto relative isolate"
      style={{
        width: props.width,
        height: props.height,
        // Clip scrolled content to the outer rounded corners of the panes
        borderTopLeftRadius: cornerRadius(
          gridProps.styletl,
          "borderTopLeftRadius"
        ),
        borderTopRightRadius: cornerRadius(
          gridProps.styletr,
          "borderTopRightRadius"
        ),
        borderBottomLeftRadius: cornerRadius(
          gridProps.stylebl,
          "borderBottomLeftRadius"
        ),
        borderBottomRightRadius: cornerRadius(
          gridProps.style,
          "borderBottomRightRadius"
        ),
      }}
    >
      <div
        className="relative"
        style={{ width: totalWidth, height: totalHeight }}
      >
        <BodyChromeLayer width={chromeWidth} height={chromeHeight} />
        <VirtualGrid />
        <BodyBorderLayer width={chromeWidth} height={chromeHeight} />
        <FrozenChromeLayer width={chromeWidth} height={chromeHeight} />
        <StickyColumn />
        <StickyHeader />
        <StickyCorner />
      </div>
    </div>
  );
};

export default GridContainer;
