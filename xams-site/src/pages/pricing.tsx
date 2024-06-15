import { useState } from "react";
import { RadioGroup } from "@headlessui/react";
import { CheckIcon } from "@heroicons/react/20/solid";
import MainLayout from "@/components/MainLayout";

const frequencies = [
  { value: "monthly", label: "Monthly", priceSuffix: "/month" },
  { value: "annually", label: "Annually", priceSuffix: "/year" },
];
const tiers = [
  {
    name: "Community",
    id: "tier-freelancer",
    href: "https://docs.xams.io",
    price: { monthly: "$0", annually: "$0" },
    description:
      "Use the community edition for free, exclusively for non-commercial purposes.",
    buyText: "Start Now",
    features: [
      "Security Model",
      "React Components",
      "Service Layer",
      "Admin Dashboard",
      "Community Support",
    ],
    mostPopular: false,
  },
  //   {
  //     name: "Startup",
  //     id: "tier-startup",
  //     href: "#",
  //     price: { monthly: "$30", annually: "$288" },
  //     description:
  //       "Suitable for small commercial projects with community support.",
  //   buyText: "Buy Now",
  //     features: [
  //       "Everything in Community",
  //       "1 Developer License",
  //       "1 Project",
  //       "Scheduled Jobs",
  //       "Integrated Logging",
  //       "XQuery JavaScript Library",
  //     ],
  //     mostPopular: true,
  //   },
  {
    name: "Enterprise",
    id: "tier-enterprise",
    href: "/contact",
    price: { monthly: "$60", annually: "$5044" },
    description: "Suitable for commercial projects with dedicated support.",
    buyText: "Contact Us",
    features: [
      //   "Everything in Startup",
      "Everything in Community",
      "20 Developer Licenses",
      "3 Projects",
      //   "Auditing",
      //   "Analytics",
      "Scheduled Jobs",
      "XQuery JavaScript Library",
      "Private ticket support",
    ],
    mostPopular: true,
  },
];

function classNames(...classes: any) {
  return classes.filter(Boolean).join(" ");
}

export default function Example() {
  const [frequency, setFrequency] = useState(frequencies[1]);

  return (
    <MainLayout>
      <div className="bg-white py-24 sm:py-32">
        <div className="mx-auto max-w-7xl px-6 lg:px-8">
          <div className="mx-auto max-w-4xl text-center">
            <h2 className="text-base font-semibold leading-7 text-green-500">
              Pricing
            </h2>
            <p className="mt-2 text-4xl font-bold tracking-tight text-gray-900 sm:text-5xl">
              Our Pricing & Plans
            </p>
          </div>
          {/* <p className="mx-auto mt-6 max-w-2xl text-center text-lg leading-8 text-gray-600">
            Choose an affordable plan that’s packed with the best features for
            engaging your audience, creating customer loyalty, and driving
            sales.
          </p> */}
          <div className="mt-16 flex justify-center">
            {/* <RadioGroup
              value={frequency}
              onChange={setFrequency}
              className="grid grid-cols-2 gap-x-1 rounded-full p-1 text-center text-xs font-semibold leading-5 ring-1 ring-inset ring-gray-200"
            >
              <RadioGroup.Label className="sr-only">
                Payment frequency
              </RadioGroup.Label>
              {frequencies.map((option) => (
                <RadioGroup.Option
                  key={option.value}
                  value={option}
                  className={({ checked }) =>
                    classNames(
                      checked ? "bg-green-500 text-white" : "text-gray-500",
                      "cursor-pointer rounded-full px-2.5 py-1"
                    )
                  }
                >
                  <span>{option.label}</span>
                </RadioGroup.Option>
              ))}
            </RadioGroup> */}
          </div>
          <div className="flex justify-center">
            <div
              className={`isolate mx-auto mt-10 grid max-w-md grid-cols-1 gap-8 lg:mx-0  ${
                tiers.length == 2
                  ? `lg:grid-cols-2 lg:max-w-3xl`
                  : `lg:grid-cols-3 lg:max-w-none`
              } `}
            >
              {tiers.map((tier: any) => (
                <div
                  key={tier.id}
                  className={classNames(
                    tier.mostPopular
                      ? "ring-2 ring-green-500"
                      : "ring-1 ring-gray-200",
                    "rounded-3xl p-8 xl:p-10 shadow-xl"
                  )}
                >
                  <div className="flex items-center justify-between gap-x-4">
                    <h3
                      id={tier.id}
                      className={classNames(
                        tier.mostPopular ? "text-green-500" : "text-gray-900",
                        "text-lg font-semibold leading-8"
                      )}
                    >
                      {tier.name}
                    </h3>
                    {/* {tier.mostPopular ? (
                      <p className="rounded-full bg-green-500/10 px-2.5 py-1 text-xs font-semibold leading-5 text-green-500">
                        Most popular
                      </p>
                    ) : null} */}
                  </div>
                  <p className="mt-4 text-sm leading-6 text-gray-600">
                    {tier.description}
                  </p>
                  {/* <p className="mt-6 flex items-baseline gap-x-1">
                  <span className="text-4xl font-bold tracking-tight text-gray-900">
                    {tier.price[frequency.value]}
                  </span>
                  <span className="text-sm font-semibold leading-6 text-gray-600">
                    {frequency.priceSuffix}
                  </span>
                </p> */}
                  <a
                    href={tier.href}
                    aria-describedby={tier.id}
                    className={classNames(
                      tier.mostPopular
                        ? "bg-green-500 text-white shadow-sm hover:bg-green-500"
                        : "text-green-500 ring-1 ring-inset ring-purple-200 hover:ring-purple-300",
                      "mt-6 block rounded-md py-2 px-3 text-center text-sm font-semibold leading-6 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-green-500"
                    )}
                  >
                    {tier.buyText}
                  </a>
                  <ul
                    role="list"
                    className="mt-8 space-y-3 text-sm leading-6 text-gray-600 xl:mt-10"
                  >
                    {tier.features.map((feature: any) => (
                      <li key={feature} className="flex gap-x-3">
                        <CheckIcon
                          className="h-6 w-5 flex-none text-green-500"
                          aria-hidden="true"
                        />
                        {feature}
                      </li>
                    ))}
                  </ul>
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>
    </MainLayout>
  );
}
