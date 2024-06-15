import { UsersIcon, CogIcon, ArrowPathIcon } from "@heroicons/react/24/outline";

const features = [
  {
    name: "Robust yet Intuitive Framework",
    description:
      "Seamlessly integrates modern Javascript technologies for a state-of-the-art development experience.",
    href: "#",
    icon: CogIcon,
  },
  {
    name: "Streamlined Project Handover",
    description:
      "Architecturally designed for organized, maintainable projects that simplify knowledge transfer and minimize technical debt.",
    href: "#",
    icon: UsersIcon,
  },
  {
    name: "Accelerated Development Cycle",
    description:
      "Streamlines innovation with pre-built components and boilerplate-free architecture for rapid, efficient development from the outset.",
    href: "#",
    icon: ArrowPathIcon,
  },
];

export default function Features03() {
  return (
    <div className="bg-white py-24 sm:py-32">
      <div className="mx-auto max-w-7xl px-6 lg:px-8">
        <div className="mx-auto max-w-2xl lg:max-w-none lg:mx-0">
          <h2 className="text-3xl font-bold tracking-tight text-gray-900 sm:text-4xl">
            Unleash Developer Potential
          </h2>
          <p className="mt-6 text-lg leading-8 text-gray-600">
            Xams redefines software development by enhancing developer expertise
            with an advanced framework that excels where low code falls short.
            This platform facilitates rapid, flexible development without
            forgoing the sophisticated features of modern Javascript ecosystems.
            Elevate your team with Xams and turn complex challenges into
            innovative solutions.
          </p>
        </div>
        <div className="mx-auto mt-16 max-w-2xl sm:mt-20 lg:mt-24 lg:max-w-none">
          <dl className="grid max-w-xl grid-cols-1 gap-x-8 gap-y-16 lg:max-w-none lg:grid-cols-3">
            {features.map((feature) => (
              <div key={feature.name} className="flex flex-col">
                <dt className="text-base font-semibold leading-7 text-gray-900">
                  <div className="mb-6 flex h-10 w-10 items-center justify-center rounded-lg bg-green-500">
                    <feature.icon
                      className="h-6 w-6 text-white"
                      aria-hidden="true"
                    />
                  </div>
                  {feature.name}
                </dt>
                <dd className="mt-1 flex flex-auto flex-col text-base leading-7 text-gray-600">
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
