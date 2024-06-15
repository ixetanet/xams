import {
  CloudArrowUpIcon,
  LockClosedIcon,
  ServerIcon,
  ChevronRightIcon,
} from "@heroicons/react/20/solid";

const features = [
  {
    name: "CRUD operations.",
    description:
      "Create, Read, Update, Delete, Upsert, and Bulk—are used to interact with all entities. Data can be sent in batches or as a single bulk transaction.",
    icon: ChevronRightIcon,
  },
  {
    name: "Metadata.",
    description:
      "The metadata endpoint provides information about your models, including fields, types, and attributes.",
    icon: ChevronRightIcon,
  },
  {
    name: "Permissions.",
    description:
      "The permissions endpoint enables you to check whether a user has access to a specific entity or action. This is useful for determining what should be visible in the UI.",
    icon: ChevronRightIcon,
  },
];

export default function UnifiedEndpoint() {
  return (
    <div className="overflow-hidden bg-neutral-800 py-24 sm:py-32">
      <div className="mx-auto max-w-7xl px-6 lg:px-8">
        <div className="mx-auto grid max-w-2xl grid-cols-1 gap-x-8 gap-y-16 sm:gap-y-20 lg:mx-0 lg:max-w-none lg:grid-cols-2">
          <div className="lg:ml-auto lg:pl-4 lg:pt-4">
            <div className="lg:max-w-lg">
              <h2 className="text-base font-semibold leading-7 text-green-500">
                CRUD
              </h2>
              <p className="mt-2 text-3xl font-bold tracking-tight text-gray-100 sm:text-4xl">
                Unified Endpoint
              </p>
              <p className="mt-6 text-lg leading-8 text-gray-200">
                Xams has 10 endpoints that perform all CRUD operations on your
                data, provide entity metadata, and retrieve permissions. There
                is no need to scaffold new endpoints.
              </p>
              <dl className="mt-10 max-w-xl space-y-8 text-base leading-7 text-gray-200 lg:max-w-none">
                {features.map((feature) => (
                  <div key={feature.name} className="relative pl-9">
                    <dt className="inline font-semibold text-gray-100">
                      <feature.icon
                        className="absolute left-1 top-1 h-5 w-5 text-green-500"
                        aria-hidden="true"
                      />
                      {feature.name}
                    </dt>{" "}
                    <dd className="inline">{feature.description}</dd>
                  </div>
                ))}
              </dl>
            </div>
          </div>
          <div className="flex items-start justify-center lg:order-first">
            <img
              src="/images/endpoints.png"
              alt="Endpoints Screenshot"
              className="w-[24rem] max-w-none rounded-xl sm:w-[24rem]"
            />
          </div>
        </div>
      </div>
    </div>
  );
}
