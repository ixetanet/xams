import React, {
  Ref,
  forwardRef,
  useEffect,
  useImperativeHandle,
  useMemo,
  useRef,
  useState,
} from "react";
import AutoSizer from "react-virtualized-auto-sizer";
import {
  CellLocation,
  CellRange,
  DataGridProps,
  MergedCell,
} from "./datagrid/DataGridTypes";
import DataGridValidation from "./datagrid/DataGridValidation";
import {
  GridContext,
  GridContextShape,
  VirtualizedContext,
} from "./datagrid/engine/GridContext";
import GridContainer from "./datagrid/engine/GridContainer";
import useVirtualized from "./datagrid/engine/useVirtualized";
import useResizing from "./datagrid/engine/useResizing";
import useMergedCells, {
  MergedCellsMap,
} from "./datagrid/engine/useMergedCells";
import useKeyboard from "./datagrid/engine/useKeyboard";
import useMouse from "./datagrid/engine/useMouse";
import useFill from "./datagrid/engine/useFill";
import useCopy from "./datagrid/engine/useCopy";
import usePaste from "./datagrid/engine/usePaste";

export interface DataGridRef {
  reset: () => void;
  activeCell?: CellLocation;
  setActiveCell: (cell: CellLocation) => void;
  isEditing?: boolean;
  editValue?: string;
  setEditValue: (value: string) => void;
  /**
   * Merges the given range into a single cell anchored at its top-left
   * (primary) cell. Optional because it was added after the initial
   * DataGridRef shape — existing structural implementations remain valid.
   *
   * A range may not cross a frozen-pane boundary (`snapRows`/`snapColumns`):
   * each pane renders independently, so such a range cannot be displayed as
   * one cell. Cross-pane ranges are rejected and `undefined` is returned.
   * Changing `snapRows`/`snapColumns` clears all merged regions.
   */
  mergeCells?: (range: CellRange) => MergedCell | undefined;
  /** Unmerges every merged region intersecting the given range. Optional —
   * added after the initial DataGridRef shape. */
  unmergeCells?: (range: CellRange) => MergedCell[];
}

const sameWidths = (a: number[], b: number[]) => {
  if (a.length !== b.length) return false;
  for (let i = 0; i < a.length; i++) {
    if (a[i] !== b[i]) return false;
  }
  return true;
};

type GridContextCore = Omit<GridContextShape, "width" | "height">;

// React 19 hands a forwardRef component a freshly spread props object on
// every render when the element carries a ref, so props identity alone can't
// key the memoized context value. Reuse the previous object while every prop
// is reference-equal — any real prop change still yields a new identity.
const useStableProps = (props: DataGridProps): DataGridProps => {
  const ref = useRef(props);
  const prev = ref.current;
  if (prev !== props) {
    const prevKeys = Object.keys(prev) as (keyof DataGridProps)[];
    const nextKeys = Object.keys(props) as (keyof DataGridProps)[];
    const same =
      prevKeys.length === nextKeys.length &&
      nextKeys.every((key) => prev[key] === props[key]);
    if (!same) {
      ref.current = props;
    }
  }
  return ref.current;
};

// Folds the AutoSizer-measured size into the memoized context value. This is
// its own component (AutoSizer's render callback can't call hooks) so the
// value keeps its identity across scroll-frame re-renders — that stability is
// what lets memoized cells bail out while scrolling.
interface SizedGridProviderProps {
  core: GridContextCore;
  width: number;
  height: number;
  scrollRef: React.RefObject<HTMLDivElement | null>;
}

const SizedGridProvider = (props: SizedGridProviderProps) => {
  const { core, width, height, scrollRef } = props;
  const value = useMemo<GridContextShape>(
    () => ({ ...core, width, height }),
    [core, width, height]
  );
  return (
    <GridContext.Provider value={value}>
      <GridContainer width={width} height={height} scrollRef={scrollRef} />
    </GridContext.Provider>
  );
};

const DataGrid = forwardRef((rawProps: DataGridProps, ref: Ref<DataGridRef>) => {
  const props = useStableProps(rawProps);
  const defaultRowHeight = props.defaultRowHeight ?? 30;
  const snapRows = props.snapRows ?? 0;
  const snapColumns = props.snapColumns ?? 0;

  const [activeCell, setActiveCell] = useState<undefined | CellLocation>(
    undefined
  );
  const [isEditing, setIsEditing] = useState(false);
  const [editValue, setEditValue] = useState("");
  const [selectedRange, setSelectedRange] = useState<CellRange | null>(null);
  const [copiedRange, setCopiedRange] = useState<CellRange | null>(null);
  // The ref mirrors the state synchronously so imperative merge/unmerge
  // calls batched into one task see each other's results (see useMergedCells)
  const mergedCellsMapRef = useRef<MergedCellsMap>(new Map());
  const [mergedCellsMap, setMergedCellsMap] = useState<MergedCellsMap>(
    () => mergedCellsMapRef.current
  );

  const divRef = useRef<HTMLDivElement>(null);
  const scrollRef = useRef<HTMLDivElement>(null);

  const propColumnWidths = useMemo(() => {
    if (typeof props.columnWidths === "number") {
      return props.rows.length > 0
        ? Array(props.rows[0].columns.length).fill(
            props.columnWidths
          )
        : ([] as number[]);
    }
    return props.columnWidths as number[];
  }, [props.rows, props.columnWidths]);

  const [columnWidths, setColumnWidths] = useState<number[]>(propColumnWidths);
  const [rowHeights, setRowHeights] = useState<number[]>(() =>
    props.rows.map((row) => row.rowHeight ?? defaultRowHeight)
  );

  const clearInteractionState = () => {
    setActiveCell(undefined);
    setIsEditing(false);
    setEditValue("");
    setSelectedRange(null);
    setCopiedRange(null);
  };

  // A columnWidths prop change resets user resizes and (like the old
  // remount-on-key behavior) clears selection/edit state and scroll position.
  const lastPropColumnWidths = useRef(propColumnWidths);
  useEffect(() => {
    if (!sameWidths(propColumnWidths, lastPropColumnWidths.current)) {
      lastPropColumnWidths.current = propColumnWidths;
      setColumnWidths(propColumnWidths);
      clearInteractionState();
      scrollRef.current?.scrollTo({ top: 0, left: 0 });
    }
  }, [propColumnWidths]);

  // A row-count change rebuilds rowHeights (async-loaded rows). With the
  // count unchanged, entries whose prop-derived height (Row.rowHeight or
  // defaultRowHeight) changed are reconciled individually, so a controlled
  // rowHeight update takes effect while user-resized rows whose prop height
  // did not change keep their resize.
  const lastPropRowHeights = useRef<number[]>(
    props.rows.map((row) => row.rowHeight ?? defaultRowHeight)
  );
  useEffect(() => {
    const nextPropHeights = props.rows.map(
      (row) => row.rowHeight ?? defaultRowHeight
    );
    if (nextPropHeights.length !== rowHeights.length) {
      lastPropRowHeights.current = nextPropHeights;
      setRowHeights(nextPropHeights);
      return;
    }
    const lastPropHeights = lastPropRowHeights.current;
    let changed = false;
    const reconciled = rowHeights.slice();
    for (let i = 0; i < nextPropHeights.length; i++) {
      if (nextPropHeights[i] !== lastPropHeights[i]) {
        reconciled[i] = nextPropHeights[i];
        changed = true;
      }
    }
    lastPropRowHeights.current = nextPropHeights;
    if (changed) {
      setRowHeights(reconciled);
    }
  }, [props.rows, rowHeights, defaultRowHeight]);

  // Frozen-pane count changes also cleared all state via the old remount key.
  // Merged regions are cleared too: they may not cross pane boundaries, and a
  // snap change can move a boundary through an existing region.
  const snapMounted = useRef(false);
  useEffect(() => {
    if (!snapMounted.current) {
      snapMounted.current = true;
      return;
    }
    clearInteractionState();
    if (mergedCellsMapRef.current.size > 0) {
      mergedCellsMapRef.current = new Map();
      setMergedCellsMap(mergedCellsMapRef.current);
    }
    scrollRef.current?.scrollTo({ top: 0, left: 0 });
  }, [props.snapColumns, props.snapRows]);

  const virtualized = useVirtualized({
    rows: props.rows,
    columnWidths,
    rowHeights,
    defaultRowHeight,
    scrollRef,
    overscanRows: props.overscanRows,
    overscanColumns: props.overscanColumns,
  });

  useEffect(() => {
    virtualized.columnVirtualizer.measure();
  }, [columnWidths]);

  useEffect(() => {
    virtualized.rowVirtualizer.measure();
  }, [rowHeights]);

  const resizing = useResizing({
    columnWidths,
    rowHeights,
    setColumnWidths,
    setRowHeights,
  });

  const mergedCells = useMergedCells({
    mergedCells: mergedCellsMap,
    mergedCellsRef: mergedCellsMapRef,
    setMergedCells: setMergedCellsMap,
    snapRows,
    snapColumns,
  });

  const keyboard = useKeyboard({
    props,
    activeCell,
    setActiveCell,
    isEditing,
    setIsEditing,
    editValue,
    setEditValue,
    mergedCells,
    columnWidths,
    rowHeights,
    snapRows,
    snapColumns,
    scrollRef,
  });

  const mouse = useMouse({
    props,
    activeCell,
    setActiveCell,
    isEditing,
    onEndEdit: keyboard.onEndEdit,
    selectedRange,
    setSelectedRange,
    mergedCells,
  });

  const fill = useFill({
    props,
    activeCell,
    selectedRange,
    setSelectedRange,
    isEditing,
    columnWidths,
    rowHeights,
    snapRows,
    snapColumns,
    scrollRef,
    mergedCells,
    expandRangeToIncludeMergedCells: mouse.expandRangeToIncludeMergedCells,
  });

  const copy = useCopy({
    props,
    activeCell,
    selectedRange,
    isEditing,
    copiedRange,
    setCopiedRange,
    mergedCells,
    expandRangeToIncludeMergedCells: mouse.expandRangeToIncludeMergedCells,
  });

  usePaste({
    props,
    activeCell,
    selectedRange,
    isEditing,
  });

  // Validation
  DataGridValidation(props);

  const selectionChangeMounted = useRef(false);
  useEffect(() => {
    if (!selectionChangeMounted.current) {
      selectionChangeMounted.current = true;
      return;
    }
    if (props.onSelectionChange != null) {
      props.onSelectionChange(activeCell, selectedRange);
    }
  }, [activeCell, selectedRange]);

  useEffect(() => {
    const handleClickOutside = (event: any) => {
      if (divRef.current && !divRef.current.contains(event.target)) {
        setActiveCell(undefined);
      }
    };
    document.addEventListener("click", handleClickOutside, true);
    return () => {
      document.removeEventListener("click", handleClickOutside, true);
    };
  }, []);

  useImperativeHandle(ref, () => ({
    reset: () => {
      virtualized.rowVirtualizer.measure();
      virtualized.columnVirtualizer.measure();
    },
    activeCell: activeCell,
    setActiveCell: (cell: CellLocation) => {
      setActiveCell(cell);
    },
    isEditing: isEditing,
    editValue: editValue,
    setEditValue: (value: string) => {
      setEditValue(value);
    },
    mergeCells: (range: CellRange) => {
      const mergedCell = mergedCells.mergeCells(range);
      if (mergedCell != null && props.onMergeCells != null) {
        props.onMergeCells(mergedCell);
      }
      return mergedCell;
    },
    unmergeCells: (range: CellRange) => {
      const unmerged = mergedCells.unmergeCells(range);
      if (props.onUnmergeCells != null) {
        for (const mergedCell of unmerged) {
          props.onUnmergeCells(mergedCell);
        }
      }
      return unmerged;
    },
  }));

  // Everything cells render from, memoized so scroll-frame re-renders (the
  // virtualizer updates state on every scroll) keep the same identity. Any
  // prop or interaction-state change lands in a dep here and invalidates the
  // whole grid, exactly like the old inline value did.
  const contextCore = useMemo<GridContextCore>(
    () => ({
      props,
      rows: props.rows,
      columnWidths,
      rowHeights,
      snapRows,
      snapColumns,
      activeCell:
        activeCell != null &&
        props.rows[activeCell.row] != null &&
        props.rows[activeCell.row].columns[activeCell.col] != null
          ? props.rows[activeCell.row].columns[activeCell.col]
          : undefined,
      activeCellLocation: activeCell,
      editValue,
      setEditValue,
      isEditing,
      selectedRange,
      onKeyDown: keyboard.onKeyDown,
      onEndEdit: keyboard.onEndEdit,
      onCellClick: mouse.onCellClick,
      isCellInRange: mouse.isCellInRange,
      getRangeEdges: mouse.getRangeEdges,
      resizing,
      mergedCells,
      fill,
      copy,
    }),
    [
      props,
      columnWidths,
      rowHeights,
      snapRows,
      snapColumns,
      activeCell,
      editValue,
      isEditing,
      selectedRange,
      keyboard,
      mouse,
      resizing,
      mergedCells,
      fill,
      copy,
    ]
  );

  return (
    <div ref={divRef} className="w-full h-full relative">
      <VirtualizedContext.Provider value={virtualized}>
        <AutoSizer>
          {({ height, width }) => (
            <SizedGridProvider
              core={contextCore}
              width={width}
              height={height}
              scrollRef={scrollRef}
            />
          )}
        </AutoSizer>
      </VirtualizedContext.Provider>
    </div>
  );
});

DataGrid.displayName = "DataGrid";
export default DataGrid;
