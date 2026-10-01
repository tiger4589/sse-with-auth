import { fetchEventSource } from 'https://cdn.jsdelivr.net/npm/@microsoft/fetch-event-source@2.0.1/+esm';

const connections = {};

export async function connect(id, url, dotNetObject, messageCallback, errorCallback, token) {

    await fetchEventSource(url, {
        headers: {
            'Authorization': `Bearer ${token}`,
        },
        onmessage(event) {
            dotNetObject.invokeMethodAsync(messageCallback, id, event.data);
        },
        onerror() {
            dotNetObject.invokeMethodAsync(errorCallback, id, `Connection failed or was closed for ${url}.`);
        }
    });
}
