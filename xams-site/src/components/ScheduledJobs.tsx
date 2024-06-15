import {
  CloudArrowUpIcon,
  LockClosedIcon,
  ServerIcon,
  ChevronRightIcon,
} from "@heroicons/react/20/solid";
import Highlighter from "./common/Highlighter";

const features = [
  {
    name: "Intervals.",
    description:
      "Scheduled jobs can be set to run at intervals, such as every 5 minutes, every hour, or every day.",
    icon: ChevronRightIcon,
  },
  {
    name: "Time of Day.",
    description:
      "Scheduled jobs can be set to run at specific times of day, such as 3:00 AM or 8:00 PM.",
    icon: ChevronRightIcon,
  },
  {
    name: "Specific Days.",
    description:
      "Scheduled jobs can be set to run on specific days of the week, such as Monday, Wednesday, and Friday.",
    icon: ChevronRightIcon,
  },
];

const codeBlock = `[ServiceJob(nameof(MyScheduleJob), "Primary", "00:00:00:05", 
JobSchedule.Interval, 
DaysOfWeek.All)]
public class MyScheduleJob : IServiceJob
{
    public Task<Response<object?>> Execute(JobServiceContext context)
    {
       // Do something
        return Task.FromResult(ServiceResult.Success("Success!"));
    }
}`;

export default function ScheduledJobs() {
  return (
    <div className="overflow-hidden bg-neutral-800 py-24 sm:py-32">
      <div className="mx-auto max-w-7xl px-6 lg:px-8">
        <div className="mx-auto grid max-w-2xl grid-cols-1 gap-x-8 gap-y-16 sm:gap-y-20 lg:mx-0 lg:max-w-none lg:grid-cols-2">
          <div className="lg:ml-auto lg:pl-4 lg:pt-4">
            <div className="lg:max-w-lg">
              <h2 className="text-base font-semibold leading-7 text-green-500">
                Jobs
              </h2>
              <p className="mt-2 text-3xl font-bold tracking-tight text-gray-100 sm:text-4xl">
                Scheduled Jobs
              </p>
              <p className="mt-6 text-lg leading-8 text-gray-200">
                Use the IServiceJob interface and ServiceJob attribute to turn
                any class into a scheduled job.
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
          <div className="flex items-start justify-end lg:order-first">
            <Highlighter language="csharp" codeBlock={codeBlock} />
            {/* <img
              src="https://tailwindui.com/img/component-images/dark-project-app-screenshot.png"
              alt="Product screenshot"
              className="w-[48rem] max-w-none rounded-xl shadow-xl ring-1 ring-gray-400/10 sm:w-[57rem]"
              width={2432}
              height={1442}
            /> */}
          </div>
        </div>
      </div>
    </div>
  );
}
