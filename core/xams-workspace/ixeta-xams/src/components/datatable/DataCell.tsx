import { AppContext } from "../../contexts/AppContext";
import useAuthRequest from "../../hooks/useAuthRequest";
import React, { useContext } from "react";
import { IconCheck, IconCircleOff, IconTrash } from "@tabler/icons-react";
import {
  MantineColorShade,
  Table,
  Tooltip,
  useMantineColorScheme,
  useMantineTheme,
} from "@mantine/core";
import { useDataTableContext } from "../DataTableImp";
import dayjs from "dayjs";
import { DataTableFieldInfo } from "./DataTableTypes";

interface DataCellProps {
  record: any;
  fieldInfo: DataTableFieldInfo;
}

const DataCell = (props: DataCellProps) => {
  const authRequest = useAuthRequest();
  const appContext = useContext(AppContext);
  const ctx = useDataTableContext();
  const theme = useMantineTheme();
  const { colorScheme, toggleColorScheme } = useMantineColorScheme();

  const isActive =
    props.record["IsActive"] != null && props.record["IsActive"] === true;

  const iconColor =
    colorScheme === "dark"
      ? theme.colors[theme.primaryColor][
          (theme.primaryShade as MantineColorShade) - 3
        ]
      : theme.colors[theme.primaryColor][
          theme.primaryShade as MantineColorShade
        ];

  const showDelete =
    props.record["_ui_info_"]["canDelete"] === true &&
    (ctx.props.canDelete === undefined || ctx.props.canDelete === true);

  const showDeactivate =
    props.record["_ui_info_"]["canUpdate"] === true &&
    ctx.state.metadata?.fields.find((f) => f.name === "IsActive") != null &&
    ctx.props.canDeactivate === true;

  const getDataValue = (fieldType: string, value: any) => {
    if (fieldType === "Boolean") {
      return value ? "Yes" : "No";
    }

    if (fieldType === "DateTime") {
      if (value == null) {
        return "";
      }
      if (value === "0001-01-01T00:00:00") {
        return "";
      }
      if (props.fieldInfo.metadataField?.dateFormat != null) {
        return dayjs(Date.parse(value)).format(
          props.fieldInfo.metadataField.dateFormat
        );
      }
      const date = new Date(value.replace("Z", ""));
      const mm = String(date.getMonth() + 1).padStart(2, "0"); // getUTCMonth() returns months from 0-11, so we add 1
      const dd = String(date.getDate()).padStart(2, "0");
      const yyyy = date.getFullYear();

      return `${mm}/${dd}/${yyyy}`;
    }

    return value;
  };

  const onDelete = async (record: any) => {
    const id = record[ctx.state.metadata?.primaryKey ?? ""];
    if (
      ctx.props.confirmDelete === undefined ||
      ctx.props.confirmDelete === true
    ) {
      let title = "Confirm";
      let message = "Are you sure you want to delete this record?";
      let showPrompt = true;
      if (ctx.props.deleteConfirmation != null) {
        const deletePrompt = await ctx.props.deleteConfirmation(record);
        title = deletePrompt.title ?? title;
        message = deletePrompt.message ?? message;
        showPrompt =
          deletePrompt.showPrompt === undefined
            ? true
            : deletePrompt.showPrompt;
      }
      if (!showPrompt) {
        await deleteRecord(record, ctx.props.tableName, id);
      } else {
        appContext?.showConfirm(
          message,
          async () => {
            await deleteRecord(record, ctx.props.tableName, id);
          },
          () => {},
          title
        );
      }
    } else {
      await deleteRecord(record, ctx.props.tableName, id);
    }
  };

  const onDeactivate = async (record: any) => {
    const id = record[ctx.state.metadata?.primaryKey ?? ""];
    await updateRecord(ctx.props.tableName, id, !isActive);
  };

  const updateRecord = async (
    tableName: string,
    id: string,
    activate: boolean
  ) => {
    ctx.dispatch({
      type: "SET_IS_LOADING",
    });
    await authRequest.update(tableName, {
      [`${ctx.props.tableName}Id`]: id,
      IsActive: activate,
    });
    await ctx.refresh();
  };

  const deleteRecord = async (record: any, tableName: string, id: string) => {
    ctx.dispatch({
      type: "SET_IS_LOADING",
    });
    await authRequest.delete(tableName, id);
    if (ctx.props.onPostDelete != null) {
      ctx.props.onPostDelete(record);
    }
    await ctx.refresh();
  };

  const ActionBox = ({ children }: { children: React.ReactElement }) => {
    return (
      <div
        className={`absolute right-0 top-0 bottom-0 flex items-center cursor-pointer xams_delete`}
      >
        <div
          className="flex items-center p-0.5 rounded gap-1"
          style={{
            backgroundColor:
              colorScheme === "dark"
                ? theme.colors.gray[
                    (theme.primaryShade as MantineColorShade) + 2
                  ]
                : theme.colors.gray[
                    (theme.primaryShade as MantineColorShade) - 5
                  ],
          }}
        >
          {children}
        </div>
      </div>
    );
  };

  const Delete = ({ record }: { record: any }) => {
    return (
      <Tooltip label="Delete" withArrow>
        <IconTrash
          onClick={(e) => {
            e.stopPropagation();
            onDelete(record);
          }}
          size={26}
          strokeWidth={2}
          color={iconColor}
        />
      </Tooltip>
    );
  };

  const Deactivate = ({ record }: { record: any }) => {
    return isActive ? (
      <Tooltip label="Deactivate" withArrow>
        <IconCircleOff
          onClick={(e) => {
            e.stopPropagation();
            onDeactivate(record);
          }}
          size={26}
          strokeWidth={2}
          color={iconColor}
        />
      </Tooltip>
    ) : (
      <Tooltip label="Activate" withArrow>
        <IconCheck
          onClick={(e) => {
            e.stopPropagation();
            onDeactivate(record);
          }}
          size={26}
          strokeWidth={2}
          color={iconColor}
        />
      </Tooltip>
    );
  };

  let cellValue = "";
  if (props.fieldInfo.metadataField != null) {
    const fieldName = `${props.fieldInfo.alias}${
      props.fieldInfo.alias !== "" ? `.` : ``
    }${props.fieldInfo.metadataField.name}`;
    if (props.record[props.fieldInfo.metadataField.lookupName] !== undefined) {
      cellValue = props.record[props.fieldInfo.metadataField.lookupName];
    } else if (props.record[fieldName] !== undefined) {
      cellValue = getDataValue(
        props.fieldInfo.metadataField.type,
        props.record[fieldName]
      );
    }
  }
  return (
    <Table.Td
      className={`${
        ctx.props.tableStyle?.truncate === true ||
        ctx.props.tableStyle?.truncate == null
          ? `truncate`
          : ``
      } cursor-pointer relative ${
        props.fieldInfo.width.endsWith("px") ? `flex-none` : ``
      }`}
      style={{
        width: props.fieldInfo.width,
        height:
          ctx.props.tableStyle?.truncate === true ||
          ctx.props.tableStyle?.truncate == null
            ? "36.7px"
            : ``,
      }}
    >
      {props.fieldInfo.body != null
        ? props.fieldInfo.body(props.record, ctx.refHandle)
        : cellValue}
      {(showDeactivate || showDelete) && (
        <ActionBox>
          <>
            {showDeactivate && <Deactivate record={props.record} />}
            {showDelete && <Delete record={props.record} />}
          </>
        </ActionBox>
      )}
    </Table.Td>
  );
};

export default DataCell;
