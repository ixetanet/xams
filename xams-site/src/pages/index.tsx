import { Inter } from "next/font/google";
import Hero from "@/components/Hero";
import MainLayout from "@/components/MainLayout";
import Features05 from "@/components/Features05";
import DataModel from "@/components/DataModel";
import Permissions from "@/components/Permissions";
import UnifiedEndpoint from "@/components/UnifiedEndpoint";
import UIQuery from "@/components/JavascriptQueries";
import Logic from "@/components/Logic";
import ScheduledJobs from "@/components/ScheduledJobs";
import ReactComponents from "@/components/ReactComponents";
import { Highlight, Prism } from "prism-react-renderer";

(typeof global !== "undefined" ? global : window).Prism = Prism;
require("prismjs/components/prism-csharp");

const inter = Inter({ subsets: ["latin"] });

export default function Home() {
  return (
    <MainLayout>
      <Hero />
      {/* <Features03 /> */}
      <DataModel />
      <UnifiedEndpoint />
      {/* <Permissions /> */}
      <UIQuery />
      <Logic />
      {/* <ScheduledJobs /> */}
      <ReactComponents />
      <Features05 />

      {/* <CTA /> */}
    </MainLayout>
  );
}
