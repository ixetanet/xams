import {
  CloudArrowUpIcon,
  LockClosedIcon,
  ServerIcon,
  ChevronRightIcon,
} from "@heroicons/react/20/solid";
import Highlighter from "./common/Highlighter";

const features = [
  {
    name: "Entity Name.",
    description:
      "Specify the entity name for the service logic to execute on, or use '*' for all entities.",
    icon: ChevronRightIcon,
  },
  {
    name: "Data Operation.",
    description:
      "Specify whether the service logic should execute on a Create, Read, Update, or Delete operation.",
    icon: ChevronRightIcon,
  },
  {
    name: "Logic Stage.",
    description:
      "Specify whether the logic should run before or after the data operation.",
    icon: ChevronRightIcon,
  },
];

const codeBlock = `[ServiceLogic(nameof(Widget), 
DataOperation.Create | DataOperation.Update, 
LogicStage.PreOperation)]
public class WidgetService : IServiceLogic
{
    public async Task<Response<object?>> Execute(ServiceContext context)
    {
        // Get the widget to be created or updated
        Widget widget = context.GetEntity<Widget>();
        // Do some stuff
        return ServiceResult.Success();
    }
}`;

export default function Logic() {
  return (
    <div className="overflow-hidden bg-neutral-800 py-24 sm:py-32">
      <div className="mx-auto max-w-7xl md:px-6 lg:px-8">
        <div className="grid grid-cols-1 gap-x-8 gap-y-16 sm:gap-y-20 lg:grid-cols-2 lg:items-start">
          <div className="px-6 lg:px-0 lg:pr-4 lg:pt-4">
            <div className="mx-auto max-w-2xl lg:mx-0 lg:max-w-lg">
              <h2 className="text-base font-semibold leading-7 text-green-500">
                Logic
              </h2>
              <p className="mt-2 text-3xl font-bold tracking-tight text-gray-100 sm:text-4xl">
                Service Logic
              </p>
              <p className="mt-6 text-lg leading-8 text-gray-200">
                Easily write service logic for specific entities by using the
                IServiceLogic interface and the ServiceLogic attribute.
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
          <div className="sm:px-6 lg:px-0">
            <Highlighter codeBlock={codeBlock} language="csharp" />
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
          </div>
        </div>
      </div>
    </div>
  );
}
