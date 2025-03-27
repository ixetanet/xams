import { Grid, Loader, Modal } from "@mantine/core";
import React, { Ref, forwardRef, useEffect, useImperativeHandle } from "react";
import Field from "../Field";
import useFormBuilder, { useFormBuilderType } from "../../hooks/useFormBuilder";
import SaveButton from "../SaveButton";
import FormContainer from "../FormContainer";
import { useDataTableContext } from "../DataTableImp";
import { getDataOptions } from "./DataTableTypes";

export interface DataFormRef {
  formBuilder: useFormBuilderType;
}

interface DataFormProps {}

const DataForm = forwardRef((props: DataFormProps, ref: Ref<DataFormRef>) => {
  const ctx = useDataTableContext();
  const formBuilder = useFormBuilder({
    tableName: ctx.state.metadata?.tableName ?? "",
    id: ctx.state.editRecordId,
    metadata: ctx.state.metadata,
    defaults: ctx.props.formFieldDefaults,
    lookupExclusions: ctx.props.formLookupExclusions,
    lookupQueries: ctx.props.formLookupQueries,
    canCreate: ctx.state.permissions.create !== "NONE",
    canUpdate: ctx.props.canUpdate,
    onPreSave: async (submissionData) => {
      if (
        ctx.props.formCloseOnCreate === true ||
        ctx.props.formCloseOnCreate === undefined
      ) {
      }
      if (ctx.props.formOnPreSave != null) {
        ctx.props.formOnPreSave(submissionData);
      }
      return {
        continue: true,
      };
    },
    onPostSave: async (operation, id, data) => {
      if (operation === "FAILED") {
      } else {
        if (operation === "CREATE" && ctx.props.formCloseOnCreate === false) {
          formBuilder.setSnapshot(data);
        }
        if (operation === "UPDATE" && ctx.props.formCloseOnUpdate === false) {
          formBuilder.setSnapshot(data);
        }

        if (ctx.props.formOnPostSave != null) {
          ctx.props.formOnPostSave(operation, formBuilder.data);
        }

        if (
          (operation === "CREATE" &&
            (ctx.props.formCloseOnCreate === true ||
              ctx.props.formCloseOnCreate === undefined)) ||
          (operation === "UPDATE" &&
            (ctx.props.formCloseOnUpdate === true ||
              ctx.props.formCloseOnUpdate === undefined))
        ) {
          ctx.closeForm();
          ctx.getData({
            ...getDataOptions,
            page: ctx.state.data?.currentPage,
            orderBy: ctx.state.data?.orderBy,
            setData: true,
          });
        }
      }
    },
  });
  const getTitle = () => {
    if (ctx.props.formTitle != null) {
      return ctx.props.formTitle;
    }

    let prefix = "";
    if (
      formBuilder.operation === "UPDATE" &&
      formBuilder.data != null &&
      (formBuilder.data as any)._ui_info_.canUpdate === true
    ) {
      prefix = "Edit";
    }
    if (
      formBuilder.operation === "CREATE" &&
      ctx.state.permissions.create !== "NONE"
    ) {
      prefix = "Create";
    }

    const displayName =
      ctx.state.metadata === undefined
        ? ctx.props.tableName
        : ctx.state.metadata?.displayName;

    if (prefix !== "") {
      return `${prefix} ${displayName}`;
    }

    return displayName;
  };

  const onClose = () => {
    if (ctx.props.formOnClose != null) {
      ctx.props.formOnClose();
    }
    // If the record has been saved since being opened, we need to reload the data
    if (formBuilder.isSubmitted) {
      ctx.closeForm();
      ctx.getData({
        ...getDataOptions,
        page: ctx.state.data?.currentPage,
        orderBy: ctx.state.data?.orderBy,
        setData: true,
      });
    } else {
      ctx.formDisclosure.close();
    }
  };

  const calculateSize = () => {
    if (ctx.props.formMaxWidth != null) {
      return `${ctx.props.formMaxWidth}rem`;
    }
    if (ctx.state.metadata !== undefined) {
      const fields = ctx.state.metadata.fields.filter(
        (field) =>
          ctx.props.formFields === undefined ||
          ctx.props.formFields.includes(field.name)
      );
      if (fields.length <= 3) {
        const size = (fields.length / 3) * 72;
        if (
          ctx.props.formMaxWidth !== undefined &&
          size < ctx.props.formMaxWidth
        ) {
          return `${ctx.props.formMaxWidth}rem`;
        }
        return `${size}rem`;
      }
    }
    if (ctx.props.formMaxWidth !== undefined && ctx.props.formMaxWidth > 72) {
      return `${ctx.props.formMaxWidth}rem`;
    }
    return `72rem`;
  };

  const calculateSpan = () => {
    if (ctx.state.metadata !== undefined) {
      const fields = ctx.state.metadata.fields.filter(
        (field) =>
          ctx.props.formFields === undefined ||
          ctx.props.formFields.includes(field.name)
      );
      if (fields.length <= 3) {
        return 12 / fields.length;
      }
    }
    return 4;
  };

  useImperativeHandle(ref, () => ({
    formBuilder,
  }));

  const showSaveButton =
    ctx.props.formHideSaveButton !== true &&
    ((formBuilder.snapshot !== undefined && formBuilder.canUpdate === true) ||
      (formBuilder.snapshot === undefined && formBuilder.canCreate === true));

  return (
    <>
      {/* This creates an overlay while the record from a clicked row is loading */}
      <Modal
        opened={formBuilder.stateType === "START_INITIAL_LOAD"}
        onClose={() => {}}
        withCloseButton={false}
        size="auto"
        closeOnClickOutside={false}
        closeOnEscape={false}
        overlayProps={{
          blur: 3,
        }}
        styles={{
          content: {
            display: "none",
          },
        }}
        centered
      ></Modal>
      <Modal
        title={getTitle()}
        opened={
          ctx.formDisclosure.opened &&
          formBuilder.metadata != null &&
          formBuilder.stateType !== "START_INITIAL_LOAD"
        }
        onClose={onClose}
        transitionProps={{ transition: "fade", duration: 100 }}
        closeOnClickOutside={false}
        closeOnEscape={ctx.props.formCloseOnEscape ?? true}
        size={calculateSize()}
        overlayProps={{
          blur: 3,
        }}
        styles={{
          root: {
            overflow: "visible",
          },
          body: {
            ...(formBuilder.isLoading === true && {
              position: "relative",
              overflow: "hidden",
              paddingLeft: 0,
              paddingRight: 0,
            }),
          },
          // overlay: {
          //   ...(ctx.props.formZIndex !== undefined && {
          //     zIndex: ctx.props.formZIndex,
          //   }),
          // },
          // inner: {
          //   ...(ctx.props.formZIndex !== undefined && {
          //     zIndex: ctx.props.formZIndex + 1,
          //   }),
          // },
        }}
        centered
      >
        <FormContainer formBuilder={formBuilder}>
          {ctx.props.customForm !== undefined
            ? ctx.props.customForm(formBuilder, ctx.formDisclosure)
            : ctx.state.metadata !== undefined &&
              (ctx.props.formFields !== undefined
                ? ctx.props.formFields
                : ctx.state.metadata.fields
              ).map((field, i) => {
                if (i % 3 === 0) {
                  return (
                    <Grid key={i}>
                      {ctx.state.metadata !== undefined &&
                        (ctx.props.formFields !== undefined
                          ? ctx.props.formFields
                          : ctx.state.metadata?.fields.map(
                              (field) => field.name
                            )
                        )
                          ?.slice(i, i + 3)
                          .map((fieldName, j) => {
                            const field = ctx.state.metadata?.fields.find(
                              (field) => field.name === fieldName
                            );
                            if (field === undefined) {
                              if (
                                typeof fieldName !== "function" &&
                                typeof fieldName !== "object"
                              ) {
                                console.warn(
                                  `Couldn't find field named ${fieldName}.`
                                );
                              }

                              return (
                                <div key={`${i}${j}`} className="hidden"></div>
                              );
                            }
                            if (typeof fieldName === "function") {
                              <div key={`${i}${j}`} className="hidden"></div>;
                            }
                            return (
                              <Grid.Col
                                key={`${field.name}${i}${j}}`}
                                span={calculateSpan()}
                              >
                                <Field
                                  focus={i === 0 && j === 0}
                                  name={field.name}
                                ></Field>
                              </Grid.Col>
                            );
                          })}
                    </Grid>
                  );
                }
                return <div key={i} className="hidden"></div>;
              })}
          {ctx.props.appendCustomForm !== undefined ? (
            <div className="mt-2">
              {ctx.props.appendCustomForm(formBuilder)}
            </div>
          ) : (
            <></>
          )}
          {ctx.props.customForm == null && (
            <div
              className={`w-full flex justify-end ${
                ctx.props.formAppendButton != null || showSaveButton
                  ? `mt-3`
                  : ``
              }`}
            >
              {ctx.props.formAppendButton != null ? (
                <>{ctx.props.formAppendButton(formBuilder)}</>
              ) : (
                <></>
              )}
              {ctx.props.formHideSaveButton !== true ? (
                (formBuilder.snapshot !== undefined &&
                  formBuilder.canUpdate === true) ||
                (formBuilder.snapshot === undefined &&
                  formBuilder.canCreate === true) ? (
                  <div>
                    <SaveButton></SaveButton>
                  </div>
                ) : (
                  <></>
                )
              ) : (
                <></>
              )}
            </div>
          )}
        </FormContainer>
      </Modal>
    </>
  );
});

DataForm.displayName = "DataForm";
export default DataForm;
