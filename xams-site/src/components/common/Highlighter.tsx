import { Highlight, themes } from "prism-react-renderer";
import React from "react";

interface HighlighterProps {
  codeBlock: string;
  language: "csharp" | "jsx" | "javascript" | "typescript";
}

const Highlighter = (props: HighlighterProps) => {
  return (
    <Highlight
      theme={themes.vsDark}
      language={props.language}
      code={props.codeBlock}
    >
      {({ className, style, tokens, getLineProps, getTokenProps }) => (
        <pre style={style} className=" rounded-xl p-4 text-sm shadow-xl">
          <div className=" overflow-x-scroll code-scrollbar">
            {tokens.map((line, i) => (
              <div key={i} {...getLineProps({ line })}>
                {line.map((token, key) => (
                  <span key={key} {...getTokenProps({ token })} />
                ))}
              </div>
            ))}
          </div>
        </pre>
      )}
    </Highlight>
  );
};

export default Highlighter;
