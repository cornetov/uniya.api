/** React custom hooks
 * 
 * USING
 * const r = useFetch(`https://dog.ceo/api/breeds/image/random`);
 * if (!r.response) { <div>Loading...</div> }
 */

import { useEffect, useState } from "react";
//import { useEffect, useReducer } from "react";

/**
 * The React hook that using fetch function (see : fetch).
 * @param input {RequestInfo} The information for fetch request.
 * @param init {RequestInit} The added initialization information.
 */
export function useFetch(input: RequestInfo, init?: RequestInit) {
    const [response, setResponse] = useState<any | null>(null);
    const [error, setError] = useState<Error | null>(null);

    useEffect(() => {
        const FetchData = async () => {
            try {
                const r = await fetch(input, init);
                if (!r.ok) {
                    throw Error(r.statusText);
                }
                const json = await r.json();
                setResponse(json);
            } catch (error) {
                if (error !== undefined) {
                    console.error('Unhandled error', error)
                    setError(error as Error);
                }
            }
        };
        FetchData();
    }, []);
    return { response, error };
}