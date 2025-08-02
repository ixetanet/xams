import { useState, useEffect, useCallback, useRef } from "react";

interface UseFetchOptions<T> {
  fetcher: () => Promise<T>;
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

  // To handle race conditions, we use a request identifier
  const activeRequestId = useRef<number>(0);

  const fetchData = useCallback(async () => {
    // Increment the request ID to identify this specific request
    const requestId = ++activeRequestId.current;

    setIsLoading(true);
    setIsError(false);
    setError(null);

    try {
      const result = await fetcher();

      if (requestId === activeRequestId.current) {
        setData(result);
        setIsSuccess(true);
        setIsLoading(false);
      }
    } catch (err) {
      if (requestId === activeRequestId.current) {
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
    setIsSuccess(false);

    if (enabled) {
      fetchData();
    }

    // Cleanup function to handle component unmounting or refetching
    return () => {
      // No explicit abort needed as we're using the requestId approach
    };
  }, [fetchData, enabled]);

  return { data, isLoading, isError, isSuccess, error, refetch };
}
