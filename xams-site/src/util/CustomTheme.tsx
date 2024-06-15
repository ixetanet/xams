export const vsCustomTheme = {
  plain: {
    color: "#9CDCFE",
    backgroundColor: "#1E1E1E",
  },
  styles: [
    {
      types: ["comment", "prolog", "doctype", "cdata"],
      style: {
        color: "#608B4E",
      },
    },
    {
      types: ["namespace"],
      style: {
        opacity: 0.7,
      },
    },
    {
      types: ["string", "attr-value"],
      style: {
        color: "#CE9178",
      },
    },
    {
      types: ["punctuation", "operator"],
      style: {
        color: "#D4D4D4",
      },
    },
    {
      // Add other types as needed
    },
  ],
};
