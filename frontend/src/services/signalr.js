import * as signalR from '@microsoft/signalr';

const API_BASE = import.meta.env.VITE_API_URL || 'http://localhost:5170/api';
const DEFAULT_HUB = API_BASE.replace(/\/api\/?$/, '') + '/hubs/canteen';
const HUB_URL = import.meta.env.VITE_HUB_URL || DEFAULT_HUB;

let connection = null;

export const getSignalRConnection = () => {
  if (!connection) {
    connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL, {
        accessTokenFactory: () => localStorage.getItem('drdo_access_token') || '',
        skipNegotiation: false,
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling,
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();
  }
  return connection;
};

export const startSignalRConnection = async () => {
  const conn = getSignalRConnection();
  if (conn.state === signalR.HubConnectionState.Disconnected) {
    try {
      await conn.start();
      console.log('SignalR Hub Connected.');
    } catch (err) {
      console.warn('SignalR Connection Error, will retry:', err);
    }
  }
  return conn;
};
