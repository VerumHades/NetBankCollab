import { UseMutationResult } from "@tanstack/react-query";
import { useEffect } from "react";

export namespace useMutationResult {
    export interface Props<T> {
        result: UseMutationResult<T, any, any, any>,
        onSuccess?: (data: T) => Promise<void>,
        onError?: () => void,
        onFetching?: () => void,
        onSettled?: () => void,
    }
}

/** Hook that lets you fetch async queries and push callbacks to it */
export const useMutationResult = <T>({ result, onFetching, onSuccess, onError, onSettled }: useMutationResult.Props<T>) => {
    useEffect(() => {
        if (result.isPending) {
            onFetching?.();
        }
    }, [result.isPending]);

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
        if (result.isSuccess || result.isError) {
            onSettled?.();
        }
    }, [result.isSuccess, result.isError]);
}