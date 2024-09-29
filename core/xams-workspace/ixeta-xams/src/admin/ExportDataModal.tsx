import { API_DATA_ACTION } from "../apiurls";
import useAuthRequest from "../hooks/useAuthRequest";
import { Button, Checkbox, Loader, Modal, ScrollArea } from "@mantine/core";
import React, { useEffect } from "react";
import { IconKey } from "@tabler/icons-react";

interface ExportDataModalProps {
  opened: boolean;
  close: () => void;
}

interface ExportDependencyInfo {
  Name: string;
  Dependencies: ExportDependency[];
  checked: boolean;
  disabled: boolean;
}

interface ExportDependency {
  Name: string;
  Optional: boolean;
  checked: boolean;
  disabled: boolean;
}

const ExportDataModal = (props: ExportDataModalProps) => {
  const authRequest = useAuthRequest();
  const [exportDependencies, setExportDependencies] = React.useState<
    ExportDependencyInfo[]
  >([]);
  const [isLoading, setIsLoading] = React.useState<boolean>(true);

  const onExport = async () => {
    setIsLoading(true);
    const resp = await authRequest.execute({
      url: API_DATA_ACTION,
      method: "POST",
      body: {
        name: "ADMIN_ExportData",
        parameters: {
          tables: exportDependencies
            .filter((r) => r.checked === true)
            .map((r) => {
              return {
                name: r.Name,
                dependencies: r.Dependencies.filter(
                  (d) => d.checked === true
                ).map((d) => {
                  return {
                    name: d.Name,
                  };
                }),
              };
            }),
        },
      },
      fileName: `export_${new Date()
        .toLocaleString()
        .replaceAll(" ", "")}.json`,
    });
    props.close();
    // setIsLoading(false);
  };

  const getDependencyGraph = async () => {
    setIsLoading(true);
    const resp = await authRequest.execute({
      url: API_DATA_ACTION,
      method: "POST",
      body: {
        name: "ADMIN_ExportDependencies",
      },
    });
    if (resp?.succeeded === true) {
      const exportDependencies = resp.data as ExportDependencyInfo[];
      for (let root of exportDependencies) {
        root.checked = false;
        for (let dependent of root.Dependencies) {
          dependent.checked = false;
          dependent.disabled = false;
        }
      }
      setExportDependencies(exportDependencies);
    }
    setIsLoading(false);
  };

  const onTableClick = (
    exportDependencies: ExportDependencyInfo[],
    tableName: string
  ) => {
    let checked = false;
    for (let root of exportDependencies) {
      if (root.Name === tableName) {
        checked = !root.checked;
        break;
      }
    }

    const newExportDependencies = exportDependencies.map((r) => r);

    addTable(tableName, newExportDependencies, [], checked);
    setExportDependencies(newExportDependencies);
  };

  const addTable = (
    tableName: string,
    exportDependencies: ExportDependencyInfo[],
    tables: string[],
    checked: boolean
  ) => {
    if (tables.includes(tableName)) {
      return;
    }
    tables.push(tableName);
    for (let dependent of exportDependencies) {
      if (dependent.Name === tableName) {
        dependent.checked = checked;
        dependent.disabled = false;
        for (let dep of dependent.Dependencies) {
          if (dep.Optional === false) {
            dep.checked = checked;
            dep.disabled = checked;
            addTable(dep.Name, exportDependencies, tables, checked);
          }
        }
      } else {
        for (let dep of dependent.Dependencies) {
          if (dep.Name === tableName) {
            dep.checked = checked;
            dep.disabled = checked;
          }
        }
      }
    }
  };

  useEffect(() => {
    if (props.opened === true) {
      setIsLoading(false);
      setExportDependencies([]);
      getDependencyGraph();
    }
  }, [props.opened]);

  return (
    <Modal
      opened={props.opened}
      onClose={props.close}
      title="Export Data"
      styles={{
        body: {
          overflow: "hidden",
          position: "relative",
          padding: "0px",
        },
      }}
      centered
    >
      {isLoading === true && (
        <div className="absolute w-full h-full flex justify-center items-center overflow-hidden z-30">
          <Loader />
        </div>
      )}
      <div className={`p-4 ${isLoading ? `invisible` : ``}`}>
        <ScrollArea h={650}>
          <div className=" pl-2 flex flex-col gap-2">
            {exportDependencies.map((root: ExportDependencyInfo) => {
              return (
                <div key={root.Name}>
                  <div>
                    <Checkbox
                      styles={{
                        input: {
                          cursor: "pointer",
                        },
                        label: {
                          cursor: "pointer",
                        },
                      }}
                      checked={root.checked}
                      disabled={root.disabled}
                      onChange={() => {}}
                      onClick={() => {
                        // onRootCheckboxChange(true, root, exportDependencies);
                        onTableClick(exportDependencies, root.Name);
                      }}
                      label={root.Name}
                    />
                  </div>
                  <div className="flex flex-col gap-1">
                    {root.Dependencies.map((dependent: ExportDependency) => {
                      return (
                        <div
                          key={`${IconKey.name}-${dependent.Name}`}
                          className="ml-8"
                        >
                          <Checkbox
                            styles={{
                              input: {
                                cursor: "pointer",
                              },
                              label: {
                                cursor: "pointer",
                              },
                            }}
                            label={dependent.Name}
                            checked={dependent.checked}
                            onChange={() => {}}
                            disabled={dependent.disabled}
                            onClick={() =>
                              onTableClick(exportDependencies, dependent.Name)
                            }
                          ></Checkbox>
                        </div>
                      );
                    })}
                  </div>
                </div>
              );
            })}
          </div>
        </ScrollArea>

        <div className="w-full flex justify-end mt-4">
          <Button onClick={onExport}>Export</Button>
        </div>
      </div>
    </Modal>
  );
};

export default ExportDataModal;
