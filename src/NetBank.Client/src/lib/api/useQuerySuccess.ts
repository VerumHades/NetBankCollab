import { UseQueryResult } from "@tanstack/react-query";
import { useQueryResult } from "./useQueryResult.ts";

export const useQuerySuccess = <T>(result: UseQueryResult<T>, callback: (data: T) => Promise<void>) => {
    useQueryResult({ result, onSuccess: callback });
}