import * as signalR from '@microsoft/signalr'
import { MessagePackHubProtocol } from '@microsoft/signalr-protocol-msgpack'

export function createHub(path: string, options: signalR.IHttpConnectionOptions = {}) {
  return new signalR.HubConnectionBuilder()
    .withUrl(useRuntimeConfig().public.serverUrl + path, options)
    .withHubProtocol(new MessagePackHubProtocol())
    .withAutomaticReconnect()
    .build()
}
