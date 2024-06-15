import {
  CloudArrowUpIcon,
  LockClosedIcon,
  ServerIcon,
  ChevronRightIcon,
} from "@heroicons/react/20/solid";
import Highlighter from "./common/Highlighter";

const features = [
  {
    name: "useFormBuilder.",
    description:
      "The `useFormBuilder` hook allows you to create forms for your entities without requiring binding logic.",
    icon: ChevronRightIcon,
  },
  {
    name: "DataTable.",
    description:
      "Create datatables for your entities with the `DataTable` component. It supports sorting, filtering, pagination, creating records, and a variety of options.",
    icon: ChevronRightIcon,
  },
  {
    name: "DataGrid.",
    description: "A simple grid component allows for Excel-like editing.",
    icon: ChevronRightIcon,
  },
];

const codeBlock = `const MyComponent = () => {
  const formBuilder = useFormBuilder({
    tableName: 'Widget',
  });

  return (
    <div className="h-full w-full p-6">
      <FormContainer formBuilder={formBuilder}>
        <div className="flex flex-col gap-4">
          <Grid>
            <Grid.Col span={6}>
              <Field name="Name" />
            </Grid.Col>
            <Grid.Col span={6}>
              <Field name="Price" />
            </Grid.Col>
          </Grid>
          <SaveButton />
        </div>
      </FormContainer>
    </div>
  )
}`;

export default function ReactComponents() {
  return (
    <div className="overflow-hidden bg-neutral-800 py-24 sm:py-32">
      <div className="mx-auto max-w-7xl md:px-6 lg:px-8">
        <div className="grid grid-cols-1 gap-x-8 gap-y-16 sm:gap-y-20 lg:grid-cols-2 lg:items-start">
          <div className="px-6 lg:px-0 lg:pr-4 lg:pt-4">
            <div className="mx-auto max-w-2xl lg:mx-0 lg:max-w-lg">
              <h2 className="text-base font-semibold leading-7 text-green-500">
                Components
              </h2>
              <p className="mt-2 text-3xl font-bold tracking-tight text-gray-100 sm:text-4xl">
                React Components
              </p>
              <p className="mt-6 text-lg leading-8 text-gray-200">
                Use the many provided components to facilitate interaction with
                your entities. React is not required to use Xams.
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
          <div className="sm:px-6 lg:px-0 relative">
            {/* <div className="relative isolate overflow-hidden bg-indigo-500 px-6 pt-8 sm:mx-auto sm:max-w-2xl sm:rounded-3xl sm:pl-16 sm:pr-0 sm:pt-16 lg:mx-0 lg:max-w-none">
              <div
                className="absolute -inset-y-px -left-3 -z-10 w-full origin-bottom-left skew-x-[-30deg] bg-indigo-100 opacity-20 ring-1 ring-inset ring-white"
                aria-hidden="true"
              />
              <div className="mx-auto max-w-2xl sm:mx-0 sm:max-w-none">
                <img
                  src="https://tailwindui.com/img/component-images/project-app-screenshot.png"
                  alt="Product screenshot"
                  width={2432}
                  height={1442}
                  className="-mb-12 w-[57rem] max-w-none rounded-tl-xl bg-gray-800 ring-1 ring-white/10"
                />
              </div>
              <div
                className="pointer-events-none absolute inset-0 ring-1 ring-inset ring-black/10 sm:rounded-3xl"
                aria-hidden="true"
              />
            </div> */}
            <Highlighter codeBlock={codeBlock} language="jsx" />
            <div className="absolute -bottom-14 -right-10 w-9/12">
              <img src="/images/widget_form.png"></img>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
