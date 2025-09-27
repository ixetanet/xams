import { useEditor } from "@tiptap/react";
import { RichTextEditor, Link } from "@mantine/tiptap";
import Highlight from "@tiptap/extension-highlight";
import StarterKit from "@tiptap/starter-kit";
import Underline from "@tiptap/extension-underline";
import TextAlign from "@tiptap/extension-text-align";
import Superscript from "@tiptap/extension-superscript";
import SubScript from "@tiptap/extension-subscript";
import React, { useEffect } from "react";

interface RichTextProps {
  onChange?: (content: string) => void;
  value?: string;
}

const RichText = (props: RichTextProps) => {
  const editor = useEditor({
    extensions: [
      StarterKit,
      Underline,
      Link,
      Superscript,
      SubScript,
      Highlight,
      TextAlign.configure({ types: ["heading", "paragraph"] }),
    ],
    // content,
  });

  useEffect(() => {
    editor?.on("update", () => {
      if (props.onChange != null) {
        props.onChange(editor?.getHTML() ?? "");
      }
    });

    return () => {
      editor?.off("update");
    };
  }, [editor]);

  useEffect(() => {
    editor?.commands.setContent(props.value ?? "");
  }, [editor]);

  return (
    <RichTextEditor
      editor={editor}
      styles={{
        root: {
          height: "100%",
          display: "flex",
          flexDirection: "column",
        },
        toolbar: {
          position: "static",
        },
        content: {
          flexGrow: 1,
          display: "flex",
          flexDirection: "column",
          overflow: "auto",
        },
      }}
      onChange={(content) => {
        console.log(content);
      }}
    >
      <RichTextEditor.Toolbar sticky stickyOffset={60}>
        <RichTextEditor.ControlsGroup>
          <RichTextEditor.Bold />
          <RichTextEditor.Italic />
          <RichTextEditor.Underline />
          <RichTextEditor.Strikethrough />
          <RichTextEditor.ClearFormatting />
          <RichTextEditor.Highlight />
          <RichTextEditor.Code />
        </RichTextEditor.ControlsGroup>

        <RichTextEditor.ControlsGroup>
          <RichTextEditor.H1 />
          <RichTextEditor.H2 />
          <RichTextEditor.H3 />
          <RichTextEditor.H4 />
        </RichTextEditor.ControlsGroup>

        <RichTextEditor.ControlsGroup>
          <RichTextEditor.Blockquote />
          <RichTextEditor.Hr />
          <RichTextEditor.BulletList />
          <RichTextEditor.OrderedList />
          <RichTextEditor.Subscript />
          <RichTextEditor.Superscript />
        </RichTextEditor.ControlsGroup>

        <RichTextEditor.ControlsGroup>
          <RichTextEditor.Link />
          <RichTextEditor.Unlink />
        </RichTextEditor.ControlsGroup>

        <RichTextEditor.ControlsGroup>
          <RichTextEditor.AlignLeft />
          <RichTextEditor.AlignCenter />
          <RichTextEditor.AlignJustify />
          <RichTextEditor.AlignRight />
        </RichTextEditor.ControlsGroup>
      </RichTextEditor.Toolbar>

      <RichTextEditor.Content
        onChange={(content) => {
          console.log(content);
        }}
      />
    </RichTextEditor>
  );
};

export default RichText;
