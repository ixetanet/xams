import { MetadataField, MetadataResponse } from "../api/MetadataResponse";
import useAuthRequest from "../hooks/useAuthRequest";
import React, {
  Ref,
  forwardRef,
  useContext,
  useEffect,
  useImperativeHandle,
  useReducer,
  useRef,
  useState,
} from "react";
import TableShell from "./datatable/TableShell";
import DataForm, { DataFormRef } from "./datatable/DataForm";
import { usePermissionStore } from "../stores/usePermissionStore";
import { ReadRequest } from "../api/ReadRequest";
import {
  dataTableInitState,
  datatableReducer,
} from "../reducers/datatableReducer";
import {
  DataTableField,
  DataTableFieldInfo,
  DataTableProps,
  DataTableRef,
  DataTableShape,
  GetDataOptions,
  SetDataFunction,
  getDataOptions,
} from "./datatable/DataTableTypes";
import DataRows from "./datatable/DataRows";
import { FormContext } from "../contexts/FormContext";
import useGuid from "../hooks/useGuid";
import { isNotNull } from "../utils/Util";

export const DataTableContext = React.createContext<DataTableShape | null>(
  null
);

export const useDataTableContext = () => {
  const context = React.useContext(DataTableContext);
  if (context == null) {
    throw new Error(
      "useDataTableContext must be used within a DataTableContextProvider"
    );
  }
  return context;
};

const DataTable = forwardRef(
  (props: DataTableProps, ref: Ref<DataTableRef>) => {
    const authRequest = useAuthRequest();
    const formContext = useContext(FormContext);
    const permissionStore = usePermissionStore();
    const guid = useGuid();
    const dataFormRef = useRef<DataFormRef>(null);
    const [id, setId] = useState<string>(guid.get());
    const [state, dispatch] = useReducer(datatableReducer, {
      ...dataTableInitState,
      id: id,
    });

    const stateRef = useRef(state);

    if (props.fields != null && props.columnWidths != null) {
      if (props.fields.length !== props.columnWidths.length) {
        throw new Error(
          "Fields and columnWidths must be the same length in DataTable"
        );
      }
    }

    const onLoad = async () => {
      dispatch({
        type: "START_INITIAL_LOAD",
      });

      const metadata =
        state.metadata != null && state.metadata.tableName === props.tableName
          ? state.metadata
          : await authRequest.metadata(props.tableName);
      const joinMetadata: MetadataResponse[] = [];

      if (props.joins != null) {
        for (let join of props.joins) {
          const joinMeta = await authRequest.metadata(join.toTable);
          if (joinMeta != null) {
            joinMetadata.push(joinMeta);
          }
        }
      }

      if (metadata == null) {
        dispatch({
          type: "TABLE_NOT_FOUND",
        });

        return;
      }

      if (props.disabledMessage) {
        dispatch({
          type: "SET_METADATA",
          payload: {
            metadata: metadata,
            joinMetadata: joinMetadata,
          },
        });
        return;
      }

      const tablePermissions = await permissionStore.getTablePermissions(
        authRequest,
        props.tableName
      );

      if (tablePermissions.read === "NONE") {
        dispatch({
          type: "MISSING_READ_PERMISSIONS",
        });
        return;
      }

      const fields =
        props.fields ?? metadata.fields.map((f) => f.name).slice(0, 7);

      const dataResp = await getData({
        ...getDataOptions,
        metadata: metadata,
        joinMetadata: joinMetadata,
        fields: fields,
        page: 1,
        active: props.showActiveSwitch === true ? true : null,
        orderBy: props.orderBy,
        searchField: "",
        searchValue: "",
      });

      if (!dataResp?.succeeded) {
        return;
      }
      validateMetadata(metadata);

      if (props.onInitialLoad != null) {
        props.onInitialLoad(dataResp.data.results);
      }

      dispatch({
        type: "INITIAL_LOAD_COMPLETE",
        payload: {
          permissions: tablePermissions,
          metadata: metadata,
          joinMetadata: joinMetadata,
          data: dataResp.data,
          isLoadingData: false,
          activeSwitch: props.showActiveSwitch === true ? "Active" : null,
        },
      });
    };
    const validateMetadata = (metadata: MetadataResponse) => {
      if (props.fields !== undefined) {
        props.fields.forEach((f) => {
          if (typeof f === "function") {
            return;
          }
          if (
            metadata.fields.find((x: MetadataField) => x.name === f) ===
              undefined &&
            f !== props.tableName + "Id" &&
            typeof f !== "object" &&
            !f.includes(".") // Exclude joined fields
          ) {
            console.warn(
              `Field ${f} not found in metadata for table ${props.tableName}`
            );
          }
        });
      }
    };

    const getData = async (options?: GetDataOptions | null) => {
      if (
        options?.setData != null &&
        options.setData === true &&
        state.isLoadingData === false &&
        (options.showLoading == null || options.showLoading === true)
      ) {
        dispatch({
          type: "SET_IS_LOADING",
        });
      }

      const metadata =
        options?.metadata ??
        state.metadata ??
        (await authRequest.metadata(props.tableName));

      const fields = (props.fields?.filter(
        (f) => typeof f === "string" && !f.includes(".") // Exclude joined fields
      ) ??
        options?.fields?.filter(
          (f) => typeof f === "string" && !f.includes(".") // Exclude joined fields
        ) ??
        metadata?.fields.map((f) => f.name).slice(0, 7)) as string[];

      // For every field that ends with Id, check the metadata to see if it is of type "Lookup"
      // If it is, add the corresponding field to the fields array
      let lookupFields: string[] = [];
      fields?.forEach((f) => {
        if (f.endsWith("Id")) {
          const metadataField = metadata?.fields.find((x) => x.name === f);
          if (
            metadataField?.type === "Lookup" &&
            fields.find((x) => x === metadataField.lookupName) === undefined
          ) {
            lookupFields.push(metadataField.lookupName);
          }
        }
      });
      fields?.push(...lookupFields);

      // Add any additional fields to be queried
      if (props.additionalFields != null) {
        for (let f of props.additionalFields) {
          if (fields.find((x) => x === f) === undefined) {
            fields.push(f);
          }
        }
      }

      // Always make sure the Id field is included
      if (fields?.find((f) => f === props.tableName + "Id") === undefined) {
        fields?.push(props.tableName + "Id");
      }

      // If deactivate instead of delete is enabled, add the IsActive field to the fields
      if (props.deleteBehavior === "Deactivate") {
        if (fields.find((f) => f === "IsActive") === undefined) {
          fields.push("IsActive");
        }
      }

      const dataResp = await authRequest.read<any>({
        tableName: props.tableName as string,
        page: options == null ? 1 : options.page,
        fields: fields,
        maxResults: props.maxResults ?? 10,
        except: props.except,
        orderBy: options?.orderBy ?? state.data?.orderBy ?? [],
        filters: [
          {
            field:
              options == null
                ? ""
                : options.searchField ?? state.searchField ?? "",
            value:
              options == null
                ? ""
                : options.searchValue ?? state.searchValue ?? "",
          },
          ...(props.showActiveSwitch === true
            ? [
                {
                  field: "IsActive",
                  operator: "==",
                  value:
                    options?.active != null
                      ? options?.active.toString()
                      : "true",
                },
              ]
            : []),
          // If there are filters passed in, add them to the request
          ...(props.filters !== undefined
            ? props.filters.map((f) => {
                return {
                  field: f.field,
                  operator: f.operator,
                  value: f.value,
                };
              })
            : []),
        ],
        joins: props.joins !== undefined ? props.joins : [],
      } as ReadRequest);

      if (options?.setData != null && options.setData === true) {
        dispatch({
          type: "SET_DONE_LOADING",
          payload: dataResp.data,
        });
      }

      if (props.onDataLoaded != null) {
        props.onDataLoaded(dataResp.data);
      }
      return dataResp;
    };

    const refresh = async (showLoading?: boolean) => {
      if (props.disabledMessage == null) {
        let orderBy = state.data?.orderBy;
        if (orderBy == null || orderBy.length === 0) {
          orderBy = props.orderBy ?? [];
        }
        await getData({
          ...getDataOptions,
          page: stateRef.current.data.currentPage,
          showLoading: showLoading,
          searchField: stateRef.current.searchField as string,
          searchValue: stateRef.current.searchValue as string,
          orderBy: orderBy, //stateRef.current.data?.orderBy ?? [],
          active: stateRef.current.activeSwitch === "Active" ? true : false,
          setData: true,
        });
      }
    };

    const getWidth = (index: number) => {
      if (props.columnWidths != null && props.columnWidths.length >= index) {
        return props.columnWidths[index];
      }
      return `100%`;
    };
    const getFields = (): DataTableFieldInfo[] => {
      if (props.fields != null && state.metadata != null) {
        let results = props.fields
          .map((f, i) => {
            if (typeof f === "string") {
              let fieldName = f;
              let metadata = state.metadata;
              let alias = "";
              if (f.includes(".")) {
                const parts = f.split(".");
                alias = parts[0];
                fieldName = parts[1];
                const join = props.joins?.find((x) => x.alias === alias);
                if (join != null) {
                  metadata = state.joinMetadata.find(
                    (x) => x.tableName === join.toTable
                  );
                }
              }
              const m = metadata?.fields.find((x) => x.name === fieldName);
              if (m == null) {
                return null;
              }
              return {
                displayName: fieldName,
                metadataField: m,
                body: null,
                width: getWidth(i),
                alias: alias,
              };
            }
            if (typeof f === "object") {
              return {
                displayName: f.header,
                body: f.body,
                width: getWidth(i),
                alias: "",
              };
            }
            return null;
          })
          .filter(isNotNull);
        return results;
      }
      if (props.fields == null && state.metadata != null) {
        let results = state.metadata?.fields
          .map((f, i) => {
            if (i <= 6) {
              return {
                displayName: f.name,
                metadataField: f,
                body: null,
                width: getWidth(i),
                alias: "",
              };
            }
            return null;
          })
          .filter(isNotNull);
        return results;
      }
      return [];
    };

    const openForm = (
      recordData: any | undefined,
      triggerOnRowClick: boolean = true
    ) => {
      if (props.onRowClick !== undefined && triggerOnRowClick === true) {
        if (props.onRowClick(recordData) === false) {
          return;
        }
      }

      dispatch({
        type: "OPEN_FORM",
        payload: {
          editRecordId:
            recordData != null ? recordData[props.tableName + "Id"] : null,
        },
      });
    };

    const showLoading = () => {
      dispatch({
        type: "SET_IS_LOADING",
      });
    };

    const setRecords = (prevFunction: SetDataFunction) => {
      dispatch({
        type: "SET_DATA",
        payload: {
          setDataFunction: prevFunction,
        },
      });
    };

    const close = () => {
      dispatch({
        type: "CLOSE_FORM",
      });
    };

    const onSort = async (field: string) => {
      if (
        state.data.orderBy != null &&
        state.data.orderBy.length > 0 &&
        state.data.orderBy[0].field === field
      ) {
        if (state.data.orderBy[0].order === "asc") {
          getData({
            ...getDataOptions,
            page: state.data.currentPage,
            orderBy: [
              {
                field: field,
                order: "desc",
              },
            ],
            setData: true,
          });
        } else {
          getData({
            ...getDataOptions,
            page: state.data.currentPage,
            orderBy: [
              {
                field: field,
                order: "asc",
              },
            ],
            setData: true,
          });
        }
      } else {
        getData({
          ...getDataOptions,
          page: state.data.currentPage,
          orderBy: [
            {
              field: field,
              order: "asc",
            },
          ],
          setData: true,
        });
      }
    };

    useEffect(() => {
      stateRef.current = state;
    }, [state.data]);

    useEffect(() => {
      if (props.tableName !== undefined) {
        onLoad();
      }
    }, [props.tableName, props.disabledMessage]);

    useEffect(() => {
      if (state.type !== "OPEN_FORM" && props.refreshInterval != null) {
        const interval = setInterval(async () => {
          await refresh(false);
        }, props.refreshInterval);

        return () => clearInterval(interval);
      }
    }, [props.refreshInterval, state.type]);

    const refObject = {
      refresh: refresh,
      openForm: (recordData: any | undefined) => openForm(recordData, false),
      dataTableId: id,
      getRecords: () => state.data?.results,
      setRecords: setRecords,
      showLoading: showLoading,
      sort: onSort,
    } as DataTableRef;

    // FormContext may need to reload the datatables
    if (formContext != null) {
      formContext.formBuilder.addDataTable(refObject);
    }

    useImperativeHandle(ref, () => refObject);

    if (state.isTableNotFound === true) {
      return (
        <div className="w-full h-full flex justify-center items-center">
          Table not found.
        </div>
      );
    }

    return (
      <DataTableContext.Provider
        value={{
          props,
          state,
          refHandle: refObject,
          dispatch,
          openForm,
          refresh,
          formDisclosure: {
            opened: state.type === "OPEN_FORM",
            close,
          },
          getData,
          getFields,
          sort: onSort,
        }}
      >
        {state.type === "OPEN_FORM" && <DataForm ref={dataFormRef}></DataForm>}
        {state.data && (
          <TableShell>
            <DataRows></DataRows>
          </TableShell>
        )}
      </DataTableContext.Provider>
    );
  }
);
DataTable.displayName = "DataTable";
export default DataTable;
