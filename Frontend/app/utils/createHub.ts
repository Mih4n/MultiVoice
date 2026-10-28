import * as signalR from '@microsoft/signalr'
import { MessagePackHubProtocol } from '@microsoft/signalr-protocol-msgpack'

export function createHub(path: string, options: signalR.IHttpConnectionOptions = {}) {
  return new signalR.HubConnectionBuilder()
    .withUrl(serverUrl.read() + path, options)
    .withHubProtocol(new MessagePackHubProtocol())
    // never give up: in the Android app nothing else would bring a dropped background connection back
    .withAutomaticReconnect({ nextRetryDelayInMilliseconds: ({ previousRetryCount }) => Math.min(previousRetryCount * 2000, 10000) })
    .build()
}
