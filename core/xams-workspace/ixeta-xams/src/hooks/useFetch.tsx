import { useState, useEffect, useCallback, useRef, useMemo } from "react";

interface UseFetchOptions<T> {
  fetcher: (signal?: AbortSignal) => Promise<T>;
  enabled?: boolean;
  initialData?: T;
}

interface UseFetchResult<T> {
  data: T | undefined;
  isLoading: boolean;
  isError: boolean;
  isSuccess: boolean;
  error: Error | null;
  refetch: () => Promise<void>;
}

export function useFetch<T>({
  fetcher,
  enabled = true,
  initialData,
}: UseFetchOptions<T>): UseFetchResult<T> {
  const [data, setData] = useState<T | undefined>(initialData);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [isError, setIsError] = useState<boolean>(false);
  const [isSuccess, setIsSuccess] = useState<boolean>(false);
  const [error, setError] = useState<Error | null>(null);

  // Store fetcher in ref to avoid stale closures
  const fetcherRef = useRef(fetcher);
  useEffect(() => {
    fetcherRef.current = fetcher;
  }, [fetcher]);

  // Track if component is mounted to prevent state updates after unmount
  const isMountedRef = useRef(true);

  // To handle race conditions, we use a request identifier
  const activeRequestId = useRef<number>(0);

  // Store current AbortController to cancel in-flight requests
  const abortControllerRef = useRef<AbortController | null>(null);

  const fetchData = useCallback(async () => {
    // Abort any in-flight request
    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
    }

    // Create new AbortController for this request
    const abortController = new AbortController();
    abortControllerRef.current = abortController;

    // Increment the request ID to identify this specific request
    const requestId = ++activeRequestId.current;

    if (isMountedRef.current) {
      setIsLoading(true);
      setIsError(false);
      setError(null);
    }

    try {
      const result = await fetcherRef.current(abortController.signal);

      // Only update state if this is still the active request, component is mounted, and not aborted
      if (
        requestId === activeRequestId.current &&
        isMountedRef.current &&
        !abortController.signal.aborted
      ) {
        setData(result);
        setIsSuccess(true);
        setIsLoading(false);
      }
    } catch (err) {
      // Don't treat abort errors as real errors
      const isAbortError =
        err instanceof Error && err.name === "AbortError";

      // Only update state if this is still the active request, component is mounted, and not aborted
      if (
        requestId === activeRequestId.current &&
        isMountedRef.current &&
        !isAbortError
      ) {
        setIsError(true);
        setError(err instanceof Error ? err : new Error(String(err)));
        setIsSuccess(false);
        setIsLoading(false);
      }
    }
  }, []);

  const refetch = useCallback(async () => {
    await fetchData();
  }, [fetchData]);

  useEffect(() => {
    if (enabled) {
      fetchData();
    }

    // Cleanup function to handle component unmounting
    return () => {
      isMountedRef.current = false;
      // Abort in-flight request on unmount or when dependencies change
      if (abortControllerRef.current) {
        abortControllerRef.current.abort();
      }
    };
  }, [fetchData, enabled]);

  // Memoize return value to prevent unnecessary re-renders
  return useMemo(
    () => ({ data, isLoading, isError, isSuccess, error, refetch }),
    [data, isLoading, isError, isSuccess, error, refetch]
  );
}
