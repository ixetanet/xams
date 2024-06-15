import {
  CloudArrowUpIcon,
  LockClosedIcon,
  ServerIcon,
  ChevronRightIcon,
} from "@heroicons/react/20/solid";
import Highlighter from "./common/Highlighter";

const features = [
  {
    name: "Push to deploy.",
    description:
      "Lorem ipsum, dolor sit amet consectetur adipisicing elit. Maiores impedit perferendis suscipit eaque, iste dolor cupiditate blanditiis ratione.",
    icon: CloudArrowUpIcon,
  },
  {
    name: "SSL certificates.",
    description:
      "Anim aute id magna aliqua ad ad non deserunt sunt. Qui irure qui lorem cupidatat commodo.",
    icon: LockClosedIcon,
  },
  {
    name: "Database backups.",
    description:
      "Ac tincidunt sapien vehicula erat auctor pellentesque rhoncus. Et magna sit morbi lobortis.",
    icon: ServerIcon,
  },
];

const codeBlock = `[Table("Widget")]
public class Widget : BaseEntity
{
    public Guid WidgetId { get; set; }
    [UIRequired] // This will be required by the UI and server
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    [UIHide] // This will never be available in the UI
    public string CouponCode { get; set; }
}`;

export default function DataModel() {
  return (
    <div className="overflow-hidden bg-neutral-800 py-24 sm:py-32">
      <div className="mx-auto max-w-7xl md:px-6 lg:px-8">
        <div className="grid grid-cols-1 gap-x-8 gap-y-16 sm:gap-y-20 lg:grid-cols-2 lg:items-start">
          <div className="px-6 lg:px-0 lg:pr-4 lg:pt-4">
            <div className="mx-auto max-w-2xl lg:mx-0 lg:max-w-lg">
              <h2 className="text-base font-semibold leading-7 text-green-500">
                Entity Framework
              </h2>
              <p className="mt-2 text-3xl font-bold tracking-tight text-gray-100 sm:text-4xl">
                A Single Model
              </p>
              <p className="mt-6 text-lg leading-8 text-gray-200">
                Xams uses attributes on your model classes to determine whats
                exposed to the client. This allows you to have a single model
                that is used for both the view and controller.
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
          <div className="sm:px-6 lg:px-0">
            <Highlighter codeBlock={codeBlock} language="csharp" />
          </div>
        </div>
      </div>
    </div>
  );
}
