import MainLayout from "@/components/MainLayout";
import {
  CheckCircleIcon,
  InformationCircleIcon,
  XCircleIcon,
} from "@heroicons/react/20/solid";

export default function Why() {
  return (
    <MainLayout>
      <div className="bg-white px-6 py-16 lg:px-8">
        <div className="mx-auto max-w-3xl text-base leading-7 text-gray-700">
          <p className="text-base font-semibold leading-7 text-green-500">
            Introduction
          </p>
          <h1 className="mt-2 text-3xl font-bold tracking-tight text-gray-900 sm:text-4xl">
            Why Xams?
          </h1>
          <p className="mt-6 text-xl leading-8">
            Xams accelerates your development by providing a full-featured
            framework for building web apps. It&apos;s designed to be simple and
            straightforward, so you can get up and running quickly.
          </p>
          <div className="mt-10 max-w-2xl">
            <p>
              In our quest for a solution to supercharge our development
              process, we explored renowned platforms like Firebase, Supabase,
              and Pocketbase. These platforms stand out for their simplicity,
              efficiency, and ability to provide a significant boost to
              application development. However, each has its set of limitations:
            </p>
            <ul role="list" className="mt-8 max-w-xl space-y-8 text-gray-600">
              <li className="flex gap-x-3">
                <XCircleIcon
                  className="mt-1 h-5 w-5 flex-none text-red-600"
                  aria-hidden="true"
                />
                <span>
                  <strong className="font-semibold text-gray-900">
                    {/* Data types. */}
                  </strong>{" "}
                  None are natively integrated with the .NET ecosystem.
                </span>
              </li>
              <li className="flex gap-x-3">
                <XCircleIcon
                  className="mt-1 h-5 w-5 flex-none text-red-600"
                  aria-hidden="true"
                />
                <span>
                  <strong className="font-semibold text-gray-900">
                    {/* Loops. */}
                  </strong>{" "}
                  Firebase lacks a relational database, complicating certain
                  data modeling and queries.
                </span>
              </li>
              <li className="flex gap-x-3">
                <XCircleIcon
                  className="mt-1 h-5 w-5 flex-none text-red-600"
                  aria-hidden="true"
                />
                <span>
                  <strong className="font-semibold text-gray-900">
                    {/* Events. */}
                  </strong>{" "}
                  Pocketbase, while visually appealing, employs SQLite,
                  presenting scalability challenges.
                </span>
              </li>
              <li className="flex gap-x-3">
                <XCircleIcon
                  className="mt-1 h-5 w-5 flex-none text-red-600"
                  aria-hidden="true"
                />
                <span>
                  <strong className="font-semibold text-gray-900">
                    {/* Events. */}
                  </strong>{" "}
                  With Supabase, business logic is often penned in SQL Triggers.
                </span>
              </li>
            </ul>
            <p className="mt-8">
              On the other side, there are ASP.NET frameworks such as ABP,
              Serenity, and ASP.NET Zero. Perfect for expansive projects, these
              frameworks promote an opinionated architecture that ensures
              project organization. While they expedite certain processes
              because of their predefined implementations, they also confine
              developers, often leading to cumbersome boilerplate code and
              scenarios where developers feel they&apos;re wrestling with the
              framework. Not to mention, none of these frameworks are inherently
              tailored for React, despite its expansive ecosystem which reigns
              supreme among web frameworks.
            </p>
            <p className="mt-8">
              So, how do you harness the agility of platforms like Firebase
              while leveraging the .NET ecosystem, all without being shackled by
              a rigid framework?
            </p>
            <h2 className="mt-16 text-2xl font-bold tracking-tight text-gray-900">
              Enter Xams
            </h2>
            <p className="mt-6">
              Xams brilliantly bridges this gap. Think of it as having the best
              of both worlds: it offers the advantages you&apos;d get from
              Firebase, Supabase, or Pocketbase, but tailored for C# and the
              .NET framework. Designed akin to frameworks like ABP, Serenity,
              and ASP.NET Zero, Xams remains substantially more lightweight and
              flexible. It upholds an architecture that fosters code structure
              and purity without becoming overbearing.
            </p>
            <p className="mt-6">
              In essence, Xams glows with the nimbleness of Firebase and carries
              many benefits akin to ABP. If you’re keen on jump-starting your
              ASP.NET project without being bogged down by an excessively
              opinionated structure, Xams is your answer.
            </p>
            {/* <figure className="mt-10 border-l border-green-500 pl-9">
              <blockquote className="font-semibold text-gray-900">
                <p>
                  “Vel ultricies morbi odio facilisi ultrices accumsan donec
                  lacus purus. Lectus nibh ullamcorper ac dictum justo in
                  euismod. Risus aenean ut elit massa. In amet aliquet eget
                  cras. Sem volutpat enim tristique.”
                </p>
              </blockquote>
              <figcaption className="mt-6 flex gap-x-4">
                <img
                  className="h-6 w-6 flex-none rounded-full bg-gray-50"
                  src="https://images.unsplash.com/photo-1502685104226-ee32379fefbe?ixlib=rb-1.2.1&ixid=eyJhcHBfaWQiOjEyMDd9&auto=format&fit=facearea&facepad=2&w=256&h=256&q=80"
                  alt=""
                />
                <div className="text-sm leading-6">
                  <strong className="font-semibold text-gray-900">
                    Maria Hill
                  </strong>{" "}
                  – Marketing Manager
                </div>
              </figcaption>
            </figure>
            <p className="mt-10">
              Faucibus commodo massa rhoncus, volutpat. Dignissim sed eget risus
              enim. Mattis mauris semper sed amet vitae sed turpis id. Id dolor
              praesent donec est. Odio penatibus risus viverra tellus varius sit
              neque erat velit.
            </p> */}
          </div>
          {/* <figure className="mt-16">
            <img
              className="aspect-video rounded-xl bg-gray-50 object-cover"
              src="https://images.unsplash.com/photo-1500648767791-00dcc994a43e?ixlib=rb-1.2.1&auto=format&fit=facearea&w=1310&h=873&q=80&facepad=3"
              alt=""
            />
            <figcaption className="mt-4 flex gap-x-2 text-sm leading-6 text-gray-500">
              <InformationCircleIcon
                className="mt-0.5 h-5 w-5 flex-none text-gray-300"
                aria-hidden="true"
              />
              Faucibus commodo massa rhoncus, volutpat.
            </figcaption>
          </figure>
          <div className="mt-16 max-w-2xl">
            <h2 className="text-2xl font-bold tracking-tight text-gray-900">
              Everything you need to get up and running
            </h2>
            <p className="mt-6">
              Purus morbi dignissim senectus mattis adipiscing. Amet, massa quam
              varius orci dapibus volutpat cras. In amet eu ridiculus leo
              sodales cursus tristique. Tincidunt sed tempus ut viverra
              ridiculus non molestie. Gravida quis fringilla amet eget dui
              tempor dignissim. Facilisis auctor venenatis varius nunc, congue
              erat ac. Cras fermentum convallis quam.
            </p>
            <p className="mt-8">
              Faucibus commodo massa rhoncus, volutpat. Dignissim sed eget risus
              enim. Mattis mauris semper sed amet vitae sed turpis id. Id dolor
              praesent donec est. Odio penatibus risus viverra tellus varius sit
              neque erat velit.
            </p>
          </div> */}
        </div>
      </div>
    </MainLayout>
  );
}
