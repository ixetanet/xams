export default function CTA() {
  return (
    <div className="bg-purple-700">
      <div className="px-6 py-24 sm:px-6 sm:py-32 lg:px-8">
        <div className="mx-auto max-w-2xl text-center">
          <h2 className="text-3xl font-bold tracking-tight text-white sm:text-4xl">
            Drive Innovation Faster
          </h2>
          <p className="mx-auto mt-6 max-w-xl text-lg leading-8 text-purple-200">
            Powerful development made surprisingly simple.
          </p>
          <div className="mt-10 flex items-center justify-center gap-x-6">
            <a
              href="https://docs.xams.io"
              className="rounded-md bg-white px-3.5 py-2.5 text-sm font-semibold text-green-500 shadow-sm hover:bg-purple-50 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-white"
            >
              Get started
            </a>
            <a href="#" className="text-sm font-semibold leading-6 text-white">
              Learn more <span aria-hidden="true">→</span>
            </a>
          </div>
        </div>
      </div>
    </div>
  );
}
