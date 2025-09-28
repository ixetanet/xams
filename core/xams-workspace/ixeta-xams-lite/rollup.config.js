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
    // {
    //   // dir: "./dist/cjs/",
    //   file: "dist/cjs/index.js",
    //   format: "cjs",
    //   sourcemap: true,
    //   name: "react-lib",
    // },
    {
      dir: "./dist/",
      format: "esm",
      sourcemap: true,
    },
  ],
  plugins: [
    external(),
    resolve(),
    commonjs(),
    typescript({ tsconfig: "./tsconfig.json" }),
    postcss(),
    terser(),
  ],
  external: ["react", "react-dom"], // Exclude peer dependencies
};
