import resolve from "@rollup/plugin-node-resolve";
import commonjs from "@rollup/plugin-commonjs";
import typescript from "@rollup/plugin-typescript";
import external from "rollup-plugin-peer-deps-external";
import postcss from "rollup-plugin-postcss";
import terser from "@rollup/plugin-terser";

// import packageJson from "./package.json" assert { type: "json" };
// import { dir } from "console";
// const packageJson = require("./package.json");

export default {
  input: "src/index.ts",
  output: [
    {
      dir: "./dist/",
      format: "esm",
      sourcemap: true,
      preserveModules: true, // Keep module structure for tree-shaking
      preserveModulesRoot: "src", // Maintain clean import paths
    },
  ],
  plugins: [
    external(), // Automatically externalize peerDependencies
    resolve(),
    commonjs(),
    typescript({ tsconfig: "./tsconfig.json" }),
    postcss(),
    // Removed terser() - libraries shouldn't be minified (let consuming apps decide)
  ],
  external: [
    "react",
    "react-dom",
    "@microsoft/signalr", // Externalize SignalR - consuming apps will install it
  ],
};
