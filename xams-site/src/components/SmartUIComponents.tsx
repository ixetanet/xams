import {
  LockClosedIcon,
  CubeIcon,
  CpuChipIcon,
} from "@heroicons/react/20/solid";
import { useState } from "react";
import { Prism as SyntaxHighlighter } from "react-syntax-highlighter";
import { vscDarkPlus } from "react-syntax-highlighter/dist/cjs/styles/prism";
const features = [
  {
    name: "Code First Entities",
    description:
      "Leverage the Entity Framework with Code First Entities to define and generate your databases and tables programmatically.",
    icon: CubeIcon,
  },
  {
    name: "Smart UI Components",
    description:
      "Simply add a React component to your page, and it will automatically bind with your data model.",
    icon: CpuChipIcon,
  },
  {
    name: "Security Model",
    description:
      "UI Component interactions are governed by the underlying security model to ensure proper access control.",
    icon: LockClosedIcon,
  },
];

type codeTab = "TodoList.cs" | "App.tsx";

export default function SmartUIComponents() {
  const [codeTab, setCodeTab] = useState<codeTab>("TodoList.cs");

  return (
    <div className="overflow-hidden bg-white py-24 sm:py-32">
      <div className="mx-auto max-w-7xl md:px-6 lg:px-8">
        <div className="grid grid-cols-1 gap-x-8 gap-y-16 sm:gap-y-20 lg:grid-cols-2 lg:items-end">
          <div className="px-6 md:px-0 lg:pr-4 lg:pt-4">
            <div className="mx-auto max-w-2xl lg:mx-0 lg:max-w-lg">
              <h2 className="text-base font-semibold leading-7 text-green-500">
                Develop faster
              </h2>
              <p className="mt-2 text-3xl font-bold tracking-tight text-gray-900 sm:text-4xl">
                Smart UI Components
              </p>
              <p className="mt-6 text-lg leading-8 text-gray-600">
                React components are designed for direct binding with the Entity
                Framework to facilitate data display.
              </p>
              <dl className="mt-10 max-w-xl space-y-8 text-base leading-7 text-gray-600 lg:max-w-none">
                {features.map((feature) => (
                  <div key={feature.name} className="relative pl-9">
                    <dt className="inline font-semibold text-gray-900">
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
            <div className="relative isolate overflow-hidden bg-green-500 px-6 pt-8 sm:mx-auto sm:max-w-2xl sm:rounded-3xl sm:pl-16 sm:pr-0 sm:pt-16 lg:mx-0 lg:max-w-none">
              <div
                className="absolute -inset-y-px -left-3 -z-10 w-full origin-bottom-left skew-x-[-30deg] bg-purple-100 opacity-20 ring-1 ring-inset ring-white"
                aria-hidden="true"
              />
              <div className="mx-auto max-w-2xl sm:mx-0 sm:max-w-none">
                <div className="w-screen overflow-hidden rounded-tl-xl bg-gray-900 ring-1 ring-white/10 h-80">
                  <div className="flex bg-gray-800/40 ring-1 ring-white/5">
                    <div className="-mb-px flex text-sm font-medium leading-6 text-gray-400">
                      <div
                        className={`${
                          codeTab === "TodoList.cs"
                            ? `border-b border-r border-b-white/20 border-r-white/10 bg-white/5 text-white`
                            : `cursor-pointer`
                        }  px-4 py-2 `}
                        onClick={() => setCodeTab("TodoList.cs")}
                      >
                        TodoList.cs{" "}
                      </div>
                      <div
                        className={`border-r px-4 py-2 
                        ${
                          codeTab === "TodoList.cs"
                            ? `cursor-pointer border-gray-600/10`
                            : `text-white border-b-white/20 border-r-white/10 border-l-white/10 bg-white/5 border-b border-r border-l`
                        }`}
                        onClick={() => setCodeTab("App.tsx")}
                      >
                        App.jsx
                      </div>
                    </div>
                  </div>
                  <div className="px-6 pb-14 pt-6">
                    {codeTab === "TodoList.cs" ? (
                      <SyntaxHighlighter
                        language="csharp"
                        style={vscDarkPlus}
                        customStyle={{ fontSize: "14px", background: "none" }}
                      >{`[Table("TodoList")]
public class TodoList : BaseRecord
{
    public Guid TodoListId { get; set; }
    public string Name { get; set; }
    
    [UIDisplayName("Due Date")]
    public DateTime DueDate { get; set; }
}`}</SyntaxHighlighter>
                    ) : (
                      <SyntaxHighlighter
                        language="jsx"
                        style={vscDarkPlus}
                        customStyle={{ fontSize: "14px", background: "none" }}
                      >{`const App = () => {
  return (
    <DataTable tableName="TodoList" />
  )
}

export default App`}</SyntaxHighlighter>
                    )}
                  </div>
                </div>
              </div>

              <div
                className="pointer-events-none absolute inset-0 ring-1 ring-inset ring-black/10 sm:rounded-3xl"
                aria-hidden="true"
              />
            </div>
            {/* <div className=" absolute ml-16 -mt-9 w-[35rem] shadow-2xl bg-white rounded-3xl p-2">
              <img
                src="/images/todo_datatable.png"
                className=" rounded-3xl"
                alt="datatable"
              />
            </div> */}
          </div>
        </div>
      </div>
    </div>
  );
}
