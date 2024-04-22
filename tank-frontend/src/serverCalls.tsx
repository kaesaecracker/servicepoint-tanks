import {Guid} from './Guid.ts';

export function makeApiUrl(path: string, protocol: 'http' | 'ws' = 'http') {
    return new URL(`${protocol}://${window.location.hostname}${path}`);
}

export type ServerResponse<T> = {
    ok: boolean;
    statusCode: number;
    statusText: string;
    additionalErrorText?: string;
    successResult?: T;
}

export type Scores = {
    readonly kills: number;
    readonly deaths: number;
    readonly wallsDestroyed: number;
    readonly shotsFired: number;
    readonly overallScore: number;
};

export type Player = {
    readonly name: string;
    readonly id: Guid;
    readonly scores: Scores;
};

export type NameId = {
    name: string,
    id: Guid
};

export async function fetchTyped<T>({url, method}: { url: URL; method: string; }): Promise<ServerResponse<T>> {
    const response = await fetch(url, {method});
    const result: ServerResponse<T> = {
        ok: response.ok,
        statusCode: response.status,
        statusText: response.statusText
    };

    if (response.ok)
        result.successResult = await response.json();
    else
        result.additionalErrorText = await response.text();
    return result;
}

export function postPlayer({name, id}: NameId) {
    const url = makeApiUrl('/player');
    url.searchParams.set('name', name);
    url.searchParams.set('id', id);

    return fetchTyped<NameId>({url, method: 'POST'});
}
