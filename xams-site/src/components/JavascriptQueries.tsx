import {
  CloudArrowUpIcon,
  LockClosedIcon,
  ServerIcon,
} from "@heroicons/react/20/solid";
import Highlighter from "./common/Highlighter";

const features = [
  {
    name: "CRUD operations.",
    description:
      "Create, Read, Update, Delete, Upsert and Bulk operations are used to interact with all entities. Data can be sent in batches or a single bulk transaction.",
    icon: CloudArrowUpIcon,
  },
  {
    name: "Metadata.",
    description:
      "The metadata endpoint provides information about your models, including the fields, types, and attributes.",
    icon: LockClosedIcon,
  },
  {
    name: "Permissions.",
    description:
      "The permissions endpoint allows you to check if a user has access to a specific entity or action. This is useful for determining what should be visible in the UI.",
    icon: ServerIcon,
  },
];

const codeBlock = `// Select all columns from the User table
let readRequest = new Query(["*"])
.from("User").top(10).page(1).distinct()
.join("User.UserId", "Account.OwnerId", "acc", ["AccountName"])
.where("PurchaseCount", ">=", "10")
.toReadRequest();`;

export default function JavascriptQueries() {
  return (
    <div className="overflow-hidden bg-neutral-800 py-24 sm:py-32">
      <div className="mx-auto max-w-7xl px-6 lg:px-8">
        <div className="mx-auto grid max-w-2xl grid-cols-1 gap-x-8 gap-y-16 sm:gap-y-20 lg:mx-0 lg:max-w-none lg:grid-cols-2">
          <div className="lg:ml-auto lg:pl-4 lg:pt-4">
            <div className="lg:max-w-lg">
              <h2 className="text-base font-semibold leading-7 text-green-500">
                Query
              </h2>
              <p className="mt-2 text-3xl font-bold tracking-tight text-gray-100 sm:text-4xl">
                Javascript Queries
              </p>
              <p className="mt-6 text-lg leading-8 text-gray-200">
                You can query your data from the frontend using SQL-like syntax.
                This allows for complex filters, ordering, joins, and left
                joins. Enable denormalization to return the data in a nested
                format.
              </p>
              {/* <dl className="mt-10 max-w-xl space-y-8 text-base leading-7 text-gray-200 lg:max-w-none">
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
              </dl> */}
            </div>
          </div>
          <div className="sm:px-6 lg:px-0 lg:order-first lg:mt-8">
            <Highlighter codeBlock={codeBlock} language="javascript" />
          </div>
          {/* <div className="flex items-start justify-end lg:order-first mt-4">
            <Highlighter
              language="javascript"
              codeBlock={codeBlock}
            ></Highlighter>
          </div> */}
        </div>
      </div>
    </div>
  );
}
