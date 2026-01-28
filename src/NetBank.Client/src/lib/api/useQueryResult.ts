import { UseQueryResult } from "@tanstack/react-query";
import { useEffect } from "react";

export namespace useQueryResult {
    export interface Props<T> {
        result: UseQueryResult<T>,
        onSuccess?: (data: T) => Promise<void>,
        onError?: () => void,
        onFetching?: () => void,
        onSettled?: () => void,
    }
}

/** Hook that lets you fetch async queries and push callbacks to it */
export const useQueryResult = <T>({ result, onFetching, onSuccess, onError, onSettled }: useQueryResult.Props<T>) => {
    useEffect(() => {
        if (result.isFetching) {
            onFetching?.();
        }
    }, [result.isFetching]);

    useEffect(() => {
        if (result.isSuccess) {
            onSuccess?.(result.data);
        }
    }, [result.isSuccess, JSON.stringify(result.data)]);

    useEffect(() => {
        if (result.isError) {
            onError?.();
        }
    }, [result.isError]);

    useEffect(() => {
        if (result.fetchStatus === "idle") {
            onSettled?.();
        }
    }, [result.fetchStatus]);
}