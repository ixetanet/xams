import {
  CheckIcon,
  ClockIcon,
  Cog8ToothIcon,
  CogIcon,
  CpuChipIcon,
  CubeIcon,
  LockClosedIcon,
  ServerIcon,
  RocketLaunchIcon,
} from "@heroicons/react/20/solid";

const features = [
  {
    name: "Code First Entities",
    description: "Use the Entity Framework to create your database and tables.",
    icon: CubeIcon,
    href: "https://docs.xams.io/",
  },
  {
    name: "Smart UI Components",
    description:
      "React components know how to bind to the Entity Framework and display your data.",
    icon: CpuChipIcon,
    href: "https://examples.xams.io/",
  },
  // {
  //   name: "Security Model",
  //   description:
  //     "A flexible security model defines who can access your data and what they can do with it.",
  //   icon: LockClosedIcon,
  // },
  {
    name: "Service Layer",
    description:
      "Clean service layer makes it easy to write business logic that executes on any CRUD operations.",
    icon: ServerIcon,
    href: "https://docs.xams.io/servicelogic",
  },
  {
    name: "Unified Endpoint",
    description:
      "Never write another API endpoint. Xams uses a single endpoint for all your data.",
    icon: RocketLaunchIcon,
    href: "https://docs.xams.io/architecture",
  },
  // {
  //   name: "Scheduled Jobs",
  //   description:
  //     "Easily and quickly write scheduled jobs that run in the background.",
  //   icon: ClockIcon,
  // },
  // {
  //   name: "Admin Dashboard",
  //   description:
  //     "Manage data and security from the Admin Dashboard. Create users, roles, and permissions.",
  //   icon: Cog8ToothIcon,
  // },
];

export default function Features05() {
  return (
    <div className=" bg-neutral-800 py-24 sm:py-32">
      <div className="mx-auto max-w-7xl px-6 lg:px-8">
        <div className="mx-auto grid max-w-2xl grid-cols-1 gap-x-8 gap-y-16 sm:gap-y-20 lg:mx-0 lg:max-w-none lg:grid-cols-3">
          <div>
            <h2 className="text-base font-semibold leading-7 text-green-500">
              All in One
            </h2>
            <p className="mt-2 text-3xl font-bold tracking-tight text-gray-100 sm:text-4xl">
              Unified Coding Framework
            </p>
            <p className="mt-6 text-base leading-7 text-gray-200">
              Use an architecture that makes writing business logic and managing
              data easy and enjoyable. Xams abstracts away the complexity of
              common application features so you can focus on building your app.
            </p>
          </div>
          <dl className="col-span-2 grid grid-cols-1 gap-x-8 gap-y-10 text-base leading-7 text-gray-200 sm:grid-cols-2 lg:gap-y-16">
            {features.map((feature) => (
              <div key={feature.name} className="relative pl-9">
                <dt className="font-semibold text-gray-100">
                  <feature.icon
                    className="absolute left-0 top-1 h-5 w-5 text-green-500"
                    aria-hidden="true"
                  />
                  {feature.name}
                </dt>
                <dd className="mt-2">{feature.description}</dd>
                <p className="mt-1">
                  <a
                    href={feature.href}
                    className="text-sm font-semibold leading-6 text-green-500"
                  >
                    Learn more <span aria-hidden="true">→</span>
                  </a>
                </p>
              </div>
            ))}
          </dl>
        </div>
      </div>
    </div>
  );
}
