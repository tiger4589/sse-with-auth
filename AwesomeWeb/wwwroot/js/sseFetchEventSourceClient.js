import { fetchEventSource } from 'https://cdn.jsdelivr.net/npm/@microsoft/fetch-event-source@2.0.1/+esm';

const connections = {};

export async function connect(id, url, dotNetObject, messageCallback, errorCallback, token) {
    disconnect(id);

    const controller = new AbortController();
    connections[id] = controller;

    try {
        await fetchEventSource(url, {
            headers: {
                Authorization: `Bearer ${token}`
            },
            signal: controller.signal,
            onmessage(event) {
                dotNetObject.invokeMethodAsync(messageCallback, id, event.data);
            },
            onerror() {
                dotNetObject.invokeMethodAsync(errorCallback, id, `Connection failed or was closed for ${url}.`);
                controller.abort();
            }
        });
    } finally {
        if (connections[id] === controller) {
            delete connections[id];
        }
    }
}

export function disconnect(id) {
    const controller = connections[id];
    if (!controller) {
        return;
    }

    controller.abort();
    delete connections[id];
}
