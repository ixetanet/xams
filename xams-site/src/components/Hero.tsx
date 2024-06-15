import React from "react";
import Image from "next/image";
import Logo from "../../public/logo_cropped.svg"; // 2533 x 800
import Link from "next/link";

const Hero = () => {
  return (
    <div className="relative isolate overflow-hidden  bg-neutral-900 text-neutral-100">
      <svg
        className="absolute inset-0 -z-10 h-full w-full stroke-green-500/30 [mask-image:radial-gradient(100%_100%_at_top_right,white,transparent)]"
        aria-hidden="true"
      >
        <defs>
          <pattern
            id="0787a7c5-978c-4f66-83c7-11c213f99cb7"
            width={200}
            height={200}
            x="50%"
            y={-1}
            patternUnits="userSpaceOnUse"
          >
            <path d="M.5 200V.5H200" fill="none" />
          </pattern>
        </defs>
        <rect
          width="100%"
          height="100%"
          strokeWidth={0}
          fill="url(#0787a7c5-978c-4f66-83c7-11c213f99cb7)"
        />
      </svg>
      <div className="px-6 pb-24 pt-16 sm:pb-32 flex lg:pb-40 justify-center">
        <div className="">
          {/* <img
            className="h-11"
            src="https://tailwindui.com/img/logos/mark.svg?color=green&shade=600"
            alt="Your Company"
          /> */}
          <div className=" mt-4 sm:mt-12 lg:mt-16">
            <a href="#" className="flex justify-between space-x-6">
              <span className="rounded-full shrink-0 flex items-center h-8 bg-green-500/10 px-3 py-1 text-sm font-semibold leading-6 text-green-500 ring-1 ring-inset ring-green-500/10">
                Early Release - v0.9
              </span>
              <span className="inline-flex items-center space-x-2 text-sm font-medium leading-6 text-gray-100">
                {/* <span>Just shipped v1.0</span> */}
                <span>A React + ASP.NET Core Framework</span>
                {/* <ChevronRightIcon
                  className="h-5 w-5 text-gray-400"
                  aria-hidden="true"
                /> */}
              </span>
            </a>
          </div>
          <h1 className="text-4xl font-bold tracking-tight text-gray-100 sm:text-6xl flex justify-center pt-10">
            <Image
              src={Logo}
              width={407}
              // height={160}
              alt="logo"
              // className=" stroke-slate-200 bg-slate-300 rounded-lg"
            />
            {/* <img src="/logo_temp.png" className=" h-60"></img> */}
          </h1>
          <p className="mt-6 text-lg leading-8 text-gray-200">
            Deliver applications faster with the Xams Framework
          </p>
          {/* <div className="w-full flex justify-end">
            <div className=" text-gray-600 mt-2">
              The React + ASP.NET Framework
            </div>
          </div> */}
          <div className="mt-10 flex justify-center items-center gap-x-6">
            <Link
              href="https://docs.xams.io"
              className="rounded-md bg-green-600 px-3.5 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-green-500 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-green-500"
            >
              Get started
            </Link>
            {/* <Link
              href="/why"
              className="text-sm font-semibold leading-6 text-gray-100"
            >
              Watch Intro Video <span aria-hidden="true">→</span>
            </Link> */}
          </div>
        </div>
        {/* <div className="mx-auto mt-16 flex max-w-2xl sm:mt-24 lg:ml-10 lg:mr-0 lg:mt-0 lg:max-w-none lg:flex-none xl:ml-16">
          <div className="max-w-3xl flex-none sm:max-w-5xl lg:max-w-none">
            <div className="-m-2 rounded-xl bg-gray-900/5 p-2 ring-1 ring-inset ring-gray-900/10 lg:-m-4 lg:rounded-2xl lg:p-4">
              <img
                src="./images/dashboard.png"
                alt="App screenshot"
                className="w-[44rem] rounded-md shadow-2xl ring-1 ring-gray-900/10"
              />
            </div>
          </div>
        </div> */}
      </div>
      <div className="w-full h-4 bg-neutral-600"></div>
    </div>
  );
};

export default Hero;
