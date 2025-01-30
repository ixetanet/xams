"use client";

import { Component, use, useEffect, useMemo, useState } from "react";
import FormBuilder from "@/examples/FormBuilder";
import { Button, Drawer } from "@mantine/core";
import FormBuilderUpdate from "@/examples/FormBuilderUpdate";
import Navigation, { Example } from "@/components/Navigation";
import FormBuilderValidation from "@/examples/FormBuilderValidation";
import { useRouter } from "next/router";
import { ToggleMode, getQueryParam, useColor } from "@ixeta/xams";
import FormBuilderDefaults from "@/examples/FormBuilderDefaults";
import DataTables from "@/examples/DataTables";
import Image from "next/image";
import CodeExample from "@/components/CodeExample";
import { Prism } from "prism-react-renderer";
import FormBuilderCode from "@/examples/FormBuilderCode";
import FormBuilderUpdateCode from "@/examples/FormBuilderUpdateCode";
import FormBuilderValidationCode from "@/examples/FormBuilderValidationCode";
import FormBuilderDefaultsCode from "@/examples/FormBuilderDefaultsCode";
import DataTablesCode from "@/examples/DataTablesCode";
import DataTableFields from "@/examples/DataTableFields";
import DataTableFieldsCode from "@/examples/DataTableFieldsCode";
import DataTableFilters from "@/examples/DataTableFilters";
import DataTableFiltersCode from "@/examples/DataTableFiltersCode";
import DataTableOrder from "@/examples/DataTableOrder";
import DataTableOrderCode from "@/examples/DataTableOrderCode";
import DataTableMaxResults from "@/examples/DataTableMaxResults";
import DataTableMaxResultsCode from "@/examples/DataTableMaxResultsCode";
import DataTableDisabled from "@/examples/DataTableDisabled";
import DataTableDisabledCode from "@/examples/DataTableDisabledCode";
import DataTableColumnWidths from "@/examples/DataTableColumnWidths";
import DataTableColumnWidthsCode from "@/examples/DataTableColumnWidthsCode";
import DataTableSearchable from "@/examples/DataTableSearchable";
import DataTableSearchableCode from "@/examples/DataTableSearchableCode";
import DataTableTitle from "@/examples/DataTableTitle";
import DataTableTitleCode from "@/examples/DataTableTitleCode";
import DataTableConfirmDelete from "@/examples/DataTableConfirmDelete";
import DataTableConfirmDeleteCode from "@/examples/DataTableConfirmDeleteCode";
import DataTableConfirmDeleteMessage from "@/examples/DataTableConfirmDeleteMessage";
import DataTableConfirmDeleteMessageCode from "@/examples/DataTableConfirmDeleteMessageCode";
import DataTableScrollable from "@/examples/DataTableScrollable";
import DataTableScrollableCode from "@/examples/DataTableScrollableCode";
import DataTableEvents from "@/examples/DataTableEvents";
import DataTableEventsCode from "@/examples/DataTableEventsCode";
import DataTableCUD from "@/examples/DataTableCUD";
import DataTableCUDCode from "@/examples/DataTableCUDCode";
import DataTableCustomFields from "@/examples/DataTableCustomFields";
import DataTableCustomFieldsCode from "@/examples/DataTableCustomFieldsCode";
import DTFormFields from "@/examples/DTFormFields";
import DTFormFieldsCode from "@/examples/DTFormFieldsCode";
import DTFormTitle from "@/examples/DTFormTitle";
import DTFormTitleCode from "@/examples/DTFormTitleCode";
import DTFormDefaults from "@/examples/DTFormDefaults";
import DTFormDefaultsCode from "@/examples/DTFormDefaultsCode";
import DTFormCloseOptions from "@/examples/DTFormCloseOptions";
import DTFormCloseOptionsCode from "@/examples/DTFormCloseOptionsCode";
import DTFormZIndex from "@/examples/DTFormZIndex";
import DTFormZIndexCode from "@/examples/DTFormZIndexCode";
import DTFormMaxWidth from "@/examples/DTFormMaxWidth";
import DTFormMaxWidthCode from "@/examples/DTFormMaxWidthCode";
import DTFormHideSaveButton from "@/examples/DTFormHideSaveButton";
import DTFormHideSaveButtonCode from "@/examples/DTFormHideSaveButtonCode";
import DTFormEvents from "@/examples/DTFormEvents";
import DTFormEventsCode from "@/examples/DTFormEventsCode";
import DTFormCustom from "@/examples/DTFormCustom";
import DTFormCustomCode from "@/examples/DTFormCustomCode";
import DataTableCreateButton from "@/examples/DataTableCreateButton";
import DataTableCreateButtonCode from "@/examples/DataTableCreateButtonCode";
import FormBuilderSnapshot from "@/examples/FormBuilderSnapshot";
import FormBuilderSnapshotCode from "@/examples/FormBuilderSnapshotCode";
import FormBuilderData from "@/examples/FormBuilderData";
import FormBuilderDataCode from "@/examples/FormBuilderDataCode";
import FieldTextarea from "@/examples/FieldTextarea";
import FieldTextareaCode from "@/examples/FieldTextareaCode";
import FieldRichText from "@/examples/FieldRichText";
import FieldRichTextCode from "@/examples/FieldRichTextCode";
import FieldOnChange from "@/examples/FieldOnChange";
import FieldOnChangeCode from "@/examples/FieldOnChangeCode";
import FormBuilderSetField from "@/examples/FormBuilderSetField";
import FormBuilderSetFieldCode from "@/examples/FormBuilderSetFieldCode";
(typeof global !== "undefined" ? global : window).Prism = Prism;
require("prismjs/components/prism-csharp");

const formExamples = [
  {
    title: "FormBuilder",
    component: <FormBuilder />,
    codeComponent: <FormBuilderCode />,
    id: 1000,
  },
  {
    title: "Update",
    component: <FormBuilderUpdate />,
    codeComponent: <FormBuilderUpdateCode />,
    id: 2000,
  },
  {
    title: "Set Field",
    component: <FormBuilderSetField />,
    codeComponent: <FormBuilderSetFieldCode />,
    id: 2500,
  },
  {
    title: "Validation",
    component: <FormBuilderValidation />,
    codeComponent: <FormBuilderValidationCode />,
    id: 3000,
  },
  {
    title: "Default Fields",
    component: <FormBuilderDefaults />,
    codeComponent: <FormBuilderDefaultsCode />,
    id: 4000,
  },
  {
    title: "Snapshot",
    component: <FormBuilderSnapshot />,
    codeComponent: <FormBuilderSnapshotCode />,
    id: 5000,
  },
  {
    title: "Data",
    component: <FormBuilderData />,
    codeComponent: <FormBuilderDataCode />,
    id: 6000,
  },
];

const formFieldExamples = [
  {
    title: "Textarea",
    component: <FieldTextarea />,
    codeComponent: <FieldTextareaCode />,
    id: 7000,
  },
  {
    title: "Rich Text",
    component: <FieldRichText />,
    codeComponent: <FieldRichTextCode />,
    id: 7100,
  },
  {
    title: "On Change",
    component: <FieldOnChange />,
    codeComponent: <FieldOnChangeCode />,
    id: 7200,
  },
] as Example[];

const dataTableExamples = [
  {
    title: "DataTable",
    component: <DataTables />,
    codeComponent: <DataTablesCode />,
    id: 10000,
  },
  {
    title: "Title",
    component: <DataTableTitle />,
    codeComponent: <DataTableTitleCode />,
    id: 10500,
  },
  {
    title: "Fields",
    component: <DataTableFields />,
    codeComponent: <DataTableFieldsCode />,
    id: 11000,
  },
  {
    title: "Custom Columns",
    component: <DataTableCustomFields />,
    codeComponent: <DataTableCustomFieldsCode />,
    id: 11250,
  },
  {
    title: "Column Widths",
    component: <DataTableColumnWidths />,
    codeComponent: <DataTableColumnWidthsCode />,
    id: 11500,
  },
  {
    title: "Custom Create Button",
    component: <DataTableCreateButton />,
    codeComponent: <DataTableCreateButtonCode />,
    id: 11750,
  },
  {
    title: "Filters",
    component: <DataTableFilters />,
    codeComponent: <DataTableFiltersCode />,
    id: 12000,
  },
  {
    title: "Order",
    component: <DataTableOrder />,
    codeComponent: <DataTableOrderCode />,
    id: 13000,
  },
  {
    title: "Max Results",
    component: <DataTableMaxResults />,
    codeComponent: <DataTableMaxResultsCode />,
    id: 14000,
  },
  {
    title: "Disable",
    component: <DataTableDisabled />,
    codeComponent: <DataTableDisabledCode />,
    id: 15000,
  },
  {
    title: "Searchable",
    component: <DataTableSearchable />,
    codeComponent: <DataTableSearchableCode />,
    id: 16000,
  },
  {
    title: "Confirm Delete",
    component: <DataTableConfirmDelete />,
    codeComponent: <DataTableConfirmDeleteCode />,
    id: 17000,
  },
  {
    title: "Confirm Delete Message",
    component: <DataTableConfirmDeleteMessage />,
    codeComponent: <DataTableConfirmDeleteMessageCode />,
    id: 18000,
  },
  {
    title: "Scrollable",
    component: <DataTableScrollable />,
    codeComponent: <DataTableScrollableCode />,
    id: 19000,
  },
  {
    title: "Events",
    component: <DataTableEvents />,
    codeComponent: <DataTableEventsCode />,
    id: 20000,
  },
  {
    title: "Disable Create  Update  Delete",
    component: <DataTableCUD />,
    codeComponent: <DataTableCUDCode />,
    id: 21000,
  },
] as Example[];

const dataTableFormExamples = [
  {
    title: "Fields",
    component: <DTFormFields />,
    codeComponent: <DTFormFieldsCode />,
    id: 30000,
  },
  {
    title: "Title",
    component: <DTFormTitle />,
    codeComponent: <DTFormTitleCode />,
    id: 31000,
  },
  {
    title: "Defaults",
    component: <DTFormDefaults />,
    codeComponent: <DTFormDefaultsCode />,
    id: 32000,
  },
  {
    title: "Close Options",
    component: <DTFormCloseOptions />,
    codeComponent: <DTFormCloseOptionsCode />,
    id: 33000,
  },
  {
    title: "Z Index",
    component: <DTFormZIndex />,
    codeComponent: <DTFormZIndexCode />,
    id: 34000,
  },
  {
    title: "Maximum Width",
    component: <DTFormMaxWidth />,
    codeComponent: <DTFormMaxWidthCode />,
    id: 35000,
  },
  {
    title: "Hide Save Button",
    component: <DTFormHideSaveButton />,
    codeComponent: <DTFormHideSaveButtonCode />,
    id: 36000,
  },
  {
    title: "Events",
    component: <DTFormEvents />,
    codeComponent: <DTFormEventsCode />,
    id: 37000,
  },
  {
    title: "Custom Form",
    component: <DTFormCustom />,
    codeComponent: <DTFormCustomCode />,
    id: 38000,
  },
] as Example[];

export default function Home() {
  const router = useRouter();
  const exampleId = parseInt(getQueryParam("id", router.asPath) ?? "1000");
  const color = useColor();
  const examples = useMemo(
    () => [
      ...formExamples,
      ...formFieldExamples,
      ...dataTableExamples,
      ...dataTableFormExamples,
    ],
    []
  );
  const currentExample = examples.find((e) => e.id === exampleId);

  useEffect(() => {
    if (currentExample === undefined) {
      router.push(`/?id=${examples[0].id}`);
    }
  }, [currentExample, examples, router]);

  if (!currentExample) {
    return null;
  }

  const prev = () => {
    // Get the previous example by id
    const prevExample = examples
      .slice()
      .reverse()
      .find((e) => e.id < exampleId);
    if (prevExample) {
      router.push(`/?id=${prevExample.id}`);
    }
  };

  const next = () => {
    // Get the next example by id
    const nextExample = examples.find((e) => e.id > exampleId);
    if (nextExample) {
      router.push(`/?id=${nextExample.id}`);
    } else {
      router.push(`/?id=${examples[0].id}`);
    }
  };

  return (
    <>
      <div className="w-full flex justify-between p-2">
        <Navigation
          formExamples={formExamples}
          formFieldsExamples={formFieldExamples}
          dataTableExamples={dataTableExamples}
          dataTableFormExamples={dataTableFormExamples}
        />
        <div className="flex gap-4 items-center mr-2">
          <ToggleMode />
          <a href="https://xams.io">
            {color.colorScheme === "dark" ? (
              <Image
                src={`/logo_dark.svg`}
                alt="logo"
                width={128}
                height={40}
              />
            ) : (
              <Image
                src={`/logo_light.svg`}
                alt="logo"
                width={128}
                height={40}
              />
            )}
          </a>
        </div>
      </div>
      <div className="w-full flex justify-center items-center">
        <div className="flex flex-col self-start px-2 max-w-lg">
          <div className="w-full flex justify-between items-center">
            <h1 className="text-xl font-bold">{currentExample.title}</h1>
            <CodeExample example={currentExample.codeComponent} />
          </div>
          <div className=" w-full">{currentExample.component}</div>
          <div className="w-full flex justify-between mt-2">
            <Button variant="subtle" onClick={prev}>
              Prev
            </Button>
            <Button variant="subtle" onClick={next}>
              Next
            </Button>
          </div>
        </div>
      </div>
    </>
  );
}
