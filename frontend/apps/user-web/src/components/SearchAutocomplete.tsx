'use client'

import { useEffect, useId, useMemo, useRef, useState } from 'react'
import { AlertCircle, LoaderCircle, Search } from 'lucide-react'
import { Button, Input, cn } from '@pharmago/ui'

export interface SearchAutocompleteProps<TItem> {
  value: string
  onValueChange: (value: string) => void
  onSubmit: (value: string) => void
  placeholder: string
  suggestions: TItem[]
  isLoading?: boolean
  isError?: boolean
  onRetry?: () => void
  getKey: (item: TItem) => string
  getTitle: (item: TItem) => string
  getDescription?: (item: TItem) => string | undefined
  onSelect: (item: TItem) => void
  emptyTitle?: string
  loadingTitle?: string
  errorTitle?: string
}

export function SearchAutocomplete<TItem>({
  value,
  onValueChange,
  onSubmit,
  placeholder,
  suggestions,
  isLoading = false,
  isError = false,
  onRetry,
  getKey,
  getTitle,
  getDescription,
  onSelect,
  emptyTitle = 'No suggestions found',
  loadingTitle = 'Loading suggestions...',
  errorTitle = 'Unable to load suggestions',
}: SearchAutocompleteProps<TItem>) {
  const listboxId = useId()
  const containerRef = useRef<HTMLDivElement | null>(null)
  const [isOpen, setIsOpen] = useState(false)
  const [activeIndex, setActiveIndex] = useState(-1)

  const shouldShowSuggestions = value.trim().length >= 2
  const visibleItems = useMemo(() => suggestions.slice(0, 8), [suggestions])

  useEffect(() => {
    if (!shouldShowSuggestions) {
      setIsOpen(false)
      setActiveIndex(-1)
      return
    }

    setIsOpen(true)
    setActiveIndex((current) => {
      if (!visibleItems.length) {
        return -1
      }

      return current >= visibleItems.length ? 0 : current
    })
  }, [shouldShowSuggestions, visibleItems])

  useEffect(() => {
    function handlePointerDown(event: MouseEvent) {
      if (!containerRef.current?.contains(event.target as Node)) {
        setIsOpen(false)
        setActiveIndex(-1)
      }
    }

    window.addEventListener('mousedown', handlePointerDown)
    return () => {
      window.removeEventListener('mousedown', handlePointerDown)
    }
  }, [])

  function handleSelect(item: TItem) {
    onSelect(item)
    setIsOpen(false)
    setActiveIndex(-1)
  }

  function handleKeyDown(event: React.KeyboardEvent<HTMLInputElement>) {
    if (!isOpen && event.key === 'Enter') {
      event.preventDefault()
      onSubmit(value)
      return
    }

    if (!shouldShowSuggestions) {
      if (event.key === 'Enter') {
        event.preventDefault()
        onSubmit(value)
      }

      return
    }

    if (event.key === 'ArrowDown') {
      event.preventDefault()
      if (!visibleItems.length) {
        return
      }

      setIsOpen(true)
      setActiveIndex((current) => (current + 1) % visibleItems.length)
      return
    }

    if (event.key === 'ArrowUp') {
      event.preventDefault()
      if (!visibleItems.length) {
        return
      }

      setIsOpen(true)
      setActiveIndex((current) => (current <= 0 ? visibleItems.length - 1 : current - 1))
      return
    }

    if (event.key === 'Escape') {
      setIsOpen(false)
      setActiveIndex(-1)
      return
    }

    if (event.key === 'Enter') {
      event.preventDefault()

      if (activeIndex >= 0 && visibleItems[activeIndex]) {
        handleSelect(visibleItems[activeIndex])
        return
      }

      onSubmit(value)
    }
  }

  return (
    <div ref={containerRef} className="relative flex-1">
      <div className="flex flex-col gap-3 md:flex-row">
        <div className="relative flex-1">
          <Search className="pointer-events-none absolute left-4 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
          <Input
            value={value}
            onChange={(event) => onValueChange(event.target.value)}
            onKeyDown={handleKeyDown}
            onFocus={() => {
              if (shouldShowSuggestions) {
                setIsOpen(true)
              }
            }}
            placeholder={placeholder}
            className="pl-11"
            role="combobox"
            aria-expanded={isOpen}
            aria-controls={listboxId}
            aria-autocomplete="list"
          />
        </div>
        <Button type="submit" variant="secondary">
          Search
        </Button>
      </div>

      {isOpen ? (
        <div className="absolute left-0 right-0 top-[calc(100%+0.75rem)] z-30 overflow-hidden rounded-[1.5rem] border border-slate-200 bg-white shadow-[0_24px_80px_-32px_rgba(15,23,42,0.35)]">
          <div id={listboxId} role="listbox" className="max-h-96 overflow-y-auto p-2">
            {isLoading ? (
              <SuggestionMessage
                icon={<LoaderCircle className="h-4 w-4 animate-spin text-emerald-700" />}
                title={loadingTitle}
              />
            ) : isError ? (
              <SuggestionMessage
                icon={<AlertCircle className="h-4 w-4 text-red-600" />}
                title={errorTitle}
                action={onRetry ? (
                  <Button variant="outline" size="sm" onClick={onRetry}>
                    Retry
                  </Button>
                ) : null}
              />
            ) : visibleItems.length ? (
              visibleItems.map((item, index) => (
                <button
                  key={getKey(item)}
                  type="button"
                  role="option"
                  aria-selected={index === activeIndex}
                  className={cn(
                    'flex w-full flex-col items-start gap-1 rounded-2xl px-4 py-3 text-left transition',
                    index === activeIndex ? 'bg-emerald-50 text-slate-950' : 'hover:bg-slate-50',
                  )}
                  onMouseEnter={() => setActiveIndex(index)}
                  onMouseDown={(event) => {
                    event.preventDefault()
                    handleSelect(item)
                  }}
                >
                  <span className="text-sm font-medium">{getTitle(item)}</span>
                  {getDescription ? (
                    <span className="text-sm text-slate-500">{getDescription(item)}</span>
                  ) : null}
                </button>
              ))
            ) : (
              <SuggestionMessage
                icon={<Search className="h-4 w-4 text-slate-400" />}
                title={emptyTitle}
              />
            )}
          </div>
        </div>
      ) : null}
    </div>
  )
}

function SuggestionMessage({
  icon,
  title,
  action,
}: {
  icon: React.ReactNode
  title: string
  action?: React.ReactNode
}) {
  return (
    <div className="flex items-center justify-between gap-3 rounded-2xl px-4 py-4">
      <div className="flex items-center gap-3">
        {icon}
        <span className="text-sm text-slate-600">{title}</span>
      </div>
      {action}
    </div>
  )
}
