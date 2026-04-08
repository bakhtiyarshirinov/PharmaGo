'use client'

export default function Error({ reset }: { error: Error; reset: () => void }) {
  return (
    <div className="mx-auto max-w-xl p-10 text-center">
      <h2 className="text-2xl font-semibold text-slate-950">Something went wrong</h2>
      <p className="mt-3 text-sm text-slate-500">The page failed to load. Try again.</p>
      <button className="mt-6 rounded-2xl bg-slate-950 px-4 py-2 text-sm text-white" onClick={reset}>
        Retry
      </button>
    </div>
  )
}

