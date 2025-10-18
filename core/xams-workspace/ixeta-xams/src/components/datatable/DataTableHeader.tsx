import React from "react";
import { IconCaretDown, IconCaretUp } from "@tabler/icons-react";
import { useMantineTheme, useMantineColorScheme, Table } from "@mantine/core";
import { useDataTableContext } from "./DataTableContext";

interface DataTableHeaderProps {}

const DataTableHeader = (props: DataTableHeaderProps) => {
  const theme = useMantineTheme();
  const ctx = useDataTableContext();
  const { colorScheme, toggleColorScheme } = useMantineColorScheme();
  const dark = colorScheme === "dark";
  const color = dark ? theme.colors.gray[4] : theme.colors.dark[7];

  let orderByName = "";
  let orderByOrder = "" as undefined | string;
  if (ctx.state.data.orderBy != null && ctx.state.data.orderBy.length > 0) {
    orderByName = ctx.state.data.orderBy[0].field;
    orderByOrder = ctx.state.data.orderBy[0].order;
  }

  const fields = ctx.getFields();

  return (
    <>
      {fields.map((f, i) => {
        if (f.metadataField == null) {
          return (
            <Table.Th
              key={i}
              style={{
                width: f.width,
              }}
              className={`${
                f.width.endsWith("px") ? `flex-none` : ``
              } truncate`}
            >
              <div className="flex items-center">
                {typeof f.displayName === "string"
                  ? f.displayName
                  : f.displayName(ctx.refHandle)}
              </div>
            </Table.Th>
          );
        }

        const m = f.metadataField;
        const fieldName = m?.lookupName != null ? m.lookupName : m.name;
        const withAlias = `${f.alias}${f.alias !== "" ? `.` : ``}${fieldName}`;
        if (m == null) {
          return;
        }
        return (
          <Table.Th
            key={withAlias}
            style={{
              width: f.width,
            }}
            className={`${f.width.endsWith("px") ? `flex-none` : ``} truncate`}
          >
            <div className="flex items-center">
              <span
                className="cursor-pointer"
                onClick={() => {
                  ctx.sort(withAlias);
                }}
              >
                {m.displayName}
              </span>
              {orderByName === withAlias && orderByOrder === "desc" && (
                <IconCaretDown
                  size={16}
                  strokeWidth={2}
                  color={color}
                  className=" ml-2"
                />
              )}
              {orderByName === withAlias && orderByOrder === "asc" && (
                <IconCaretUp
                  size={16}
                  strokeWidth={2}
                  color={color}
                  className="ml-2"
                />
              )}
            </div>
          </Table.Th>
        );
      })}
    </>
  );
};

export default DataTableHeader;
