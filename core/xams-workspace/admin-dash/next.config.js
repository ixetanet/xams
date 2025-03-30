/** @type {import('next').NextConfig} */
const nextConfig = {
  output: "export",
  basePath: "/xams",
  images: {
    unoptimized: true,
  },
  trailingSlash: false,
  reactStrictMode: true,
  transpilePackages: ["@ixeta/xams"],
};

const withBundleAnalyzer = require("@next/bundle-analyzer")({
  enabled: process.env.ANALYZE === "true",
});
module.exports = withBundleAnalyzer(nextConfig);

// module.exports = nextConfig
