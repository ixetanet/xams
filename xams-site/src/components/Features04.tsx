import {
  ClockIcon,
  ServerIcon,
  Cog8ToothIcon,
} from "@heroicons/react/20/solid";

const features = [
  {
    name: "Service Layer",
    description:
      "Clean service layer makes it easy to write business logic that executes on any CRUD operations.",
    href: "#",
    icon: ServerIcon,
  },
  {
    name: "Scheduled Jobs",
    description:
      "Easily and quickly write scheduled jobs that run in the background.",
    href: "#",
    icon: ClockIcon,
  },
  {
    name: "Admin Dashboard",
    description:
      "Manage data and security from the Admin Dashboard. Create users, roles, and permissions.",
    href: "#",
    icon: Cog8ToothIcon,
  },
];

export default function Features04() {
  return (
    <div className="bg-white py-24 sm:py-32">
      <div className="mx-auto max-w-7xl px-6 lg:px-8">
        <div className="mx-auto max-w-2xl lg:text-center">
          <h2 className="text-base font-semibold leading-7 text-green-500">
            Deploy faster
          </h2>
          <p className="mt-2 text-3xl font-bold tracking-tight text-gray-900 sm:text-4xl">
            Everything you need to build your app
          </p>
          <p className="mt-6 text-lg leading-8 text-gray-600">
            Use an architecture that makes writing business logic and managing
            data easy and enjoyable. Xams abstracts away the complexity of
            common application features so you can focus on building your app.
          </p>
        </div>
        <div className="mx-auto mt-16 max-w-2xl sm:mt-20 lg:mt-24 lg:max-w-none">
          <dl className="grid max-w-xl grid-cols-1 gap-x-8 gap-y-16 lg:max-w-none lg:grid-cols-3">
            {features.map((feature) => (
              <div key={feature.name} className="flex flex-col">
                <dt className="flex items-center gap-x-3 text-base font-semibold leading-7 text-gray-900">
                  <feature.icon
                    className="h-5 w-5 flex-none text-green-500"
                    aria-hidden="true"
                  />
                  {feature.name}
                </dt>
                <dd className="mt-4 flex flex-auto flex-col text-base leading-7 text-gray-600">
                  <p className="flex-auto">{feature.description}</p>
                  <p className="mt-6">
                    <a
                      href={feature.href}
                      className="text-sm font-semibold leading-6 text-green-500"
                    >
                      Learn more <span aria-hidden="true">→</span>
                    </a>
                  </p>
                </dd>
              </div>
            ))}
          </dl>
        </div>
      </div>
    </div>
  );
}
