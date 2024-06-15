import {
  CloudArrowUpIcon,
  LockClosedIcon,
  ServerIcon,
  UserIcon,
  UserGroupIcon,
  KeyIcon,
} from "@heroicons/react/20/solid";

const features = [
  {
    name: "Users.",
    description:
      "Users can be assigned to roles and teams. They inherit the permissions of the roles and teams to which they are assigned.",
    icon: UserIcon,
  },

  {
    name: "Teams.",
    description:
      "Users can be assigned to teams. Roles can be configured to allow users to see only the records for their teams.",
    icon: UserGroupIcon,
  },
  {
    name: "Roles.",
    description:
      "Roles are assigned permissions. These permissions determine which actions can be performed on specific entities.",
    icon: KeyIcon,
  },
];

export default function Permissions() {
  return (
    <div className="overflow-hidden bg-neutral-800 py-24 sm:py-32">
      <div className="mx-auto max-w-7xl md:px-6 lg:px-8">
        <div className="grid grid-cols-1 gap-x-8 gap-y-16 sm:gap-y-20 lg:grid-cols-2 lg:items-start">
          <div className="px-6 lg:px-0 lg:pr-4 lg:pt-4">
            <div className="mx-auto max-w-2xl lg:mx-0 lg:max-w-lg">
              <h2 className="text-base font-semibold leading-7 text-green-500">
                Security
              </h2>
              <p className="mt-2 text-3xl font-bold tracking-tight text-gray-100 sm:text-4xl">
                Permissions
              </p>
              <p className="mt-6 text-lg leading-8 text-gray-200">
                Permissions can be set from the Admin Dashboard. You can define
                who can access which data and what actions they are allowed to
                perform.
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
            <img
              src="/images/permissions.png"
              alt="Permissions Screenshot"
              className="w-[54rem] max-w-xl sm:max-w-none rounded-xl sm:w-[54rem]"
            />
          </div>
        </div>
      </div>
    </div>
  );
}
