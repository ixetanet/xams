import { DataTableProps, DataTableRef } from "./datatable/DataTableTypes";
import React, {
  useState,
  useEffect,
  forwardRef,
  useImperativeHandle,
} from "react";
import { Loader } from "@mantine/core";
const DataTableImp = React.lazy(() => import("./DataTableImp"));

const DataTable = forwardRef((props: DataTableProps, ref) => {
  const [isClient, setIsClient] = useState(false);
  const dataTableRef = React.useRef<DataTableRef>(null);

  useImperativeHandle(ref, () => dataTableRef.current);

  useEffect(() => {
    setIsClient(true);
  }, []);

  if (!isClient) {
    return (
      <div className="w-full h-full flex justify-center items-center">
        <Loader />
      </div>
    );
  }

  const key =
    props.tableName +
    JSON.stringify(props.filters) +
    JSON.stringify(props.joins) +
    JSON.stringify(props.orderBy) +
    JSON.stringify(props.except) +
    JSON.stringify(props.additionalFields) +
    JSON.stringify(props.fields) +
    JSON.stringify(props.maxResults);

  return <DataTableImp key={key} ref={dataTableRef} {...props} />;
});

export default DataTable;
