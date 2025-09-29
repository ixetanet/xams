/** @type {import('next').NextConfig} */
const nextConfig = {
  output: "export",
  basePath: "/x",
  images: {
    unoptimized: true,
  },
  trailingSlash: false,
  reactStrictMode: true,
  transpilePackages: [
    "@ixeta/xams",
    "@ixeta/headless-auth-react",
    "@ixeta/headless-auth-react-firebase",
  ],
};

const withBundleAnalyzer = require("@next/bundle-analyzer")({
  enabled: process.env.ANALYZE === "true",
});
module.exports = withBundleAnalyzer(nextConfig);

// module.exports = nextConfig
