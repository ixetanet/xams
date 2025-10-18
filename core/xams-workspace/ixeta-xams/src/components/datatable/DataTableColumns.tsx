import { Button, Checkbox, Divider, Modal, Table } from "@mantine/core";
import React, { useState } from "react";
import { useDataTableContext } from "./DataTableContext";
import { IconCaretDown } from "@tabler/icons-react";

interface DataTableColumnsProps {
  opened: boolean;
  close: () => void;
}

const DataTableColumns = (props: DataTableColumnsProps) => {
  const ctx = useDataTableContext();
  const visibleFields = ctx.getFields();
  const [fields, setFields] = useState(
    ctx.state.metadata?.fields
      .filter((f) => f.name !== ctx.state.metadata?.primaryKey)
      .map((f) => ({
        metadataField: f,
        isVisible:
          visibleFields.find((vf) => vf.metadataField?.name === f.name) != null,
      })) ?? []
  );

  const onSetColumns = () => {
    ctx.dispatch({
      type: "SET_VISIBLE_FIELDS",
      payload: {
        visibleFields: fields
          .filter((f) => f.isVisible)
          .map((f) => f.metadataField.name),
      },
    });
    props.close();
  };

  const onChecked = (fieldName: string, value: boolean) => {
    setFields((prevFields) =>
      prevFields.map((field) => {
        if (field.metadataField.name === fieldName) {
          return {
            ...field,
            isVisible: value,
          };
        }
        return field;
      })
    );
  };

  return (
    <Modal
      opened={props.opened}
      onClose={props.close}
      title="Columns"
      size="xs"
      // closeOnEscape={!isLoading}
      // closeOnClickOutside={!isLoading}
      // withCloseButton={!isLoading}
      centered
      styles={{
        overlay: {
          zIndex: 4000,
        },
        inner: {
          zIndex: 4001,
        },
      }}
    >
      <div className="w-full flex flex-col max-h-[80vh]">
        <div className="w-full overflow-y-scroll flex flex-col gap-2">
          {fields.map((field) => (
            <div key={field.metadataField.name} className="flex gap-2">
              {/* <div className="flex items-center">
                  <IconCaretDown size={16} />
                </div> */}
              <Checkbox
                label={field.metadataField.displayName}
                checked={field.isVisible}
                onChange={(e) =>
                  onChecked(field.metadataField.name, e.currentTarget.checked)
                }
              />
            </div>
          ))}
        </div>
        <div className="w-full flex justify-end py-1">
          <Button onClick={onSetColumns}>Set</Button>
        </div>
      </div>
    </Modal>
  );
};

export default DataTableColumns;
