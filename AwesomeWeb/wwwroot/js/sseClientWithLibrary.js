import { fetchEventSource } from '@microsoft/fetch-event-source';
export async function connect(id, url, dotNetObject, messageCallback, errorCallback, token) {
    disconnect(id);

    await fetchEventSource(url, {
        headers: {
            'Authorization': token,
        },
        onmessage(event) {
            dotNetObject.invokeMethodAsync(messageCallback, id, event.data);
        },
        onerror() {
            dotNetObject.invokeMethodAsync(errorCallback, id, `Connection failed or was closed for ${url}.`);
        }
    });
}
