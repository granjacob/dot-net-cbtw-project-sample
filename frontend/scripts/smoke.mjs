import * as signalR from "@microsoft/signalr";

const baseUrl = (process.env.SERVICEFLOW_URL ?? "http://localhost:3000").replace(/\/$/, "");

async function api(path, token, init = {}) {
  const response = await fetch(`${baseUrl}${path}`, {
    ...init,
    headers: {
      ...(init.body ? { "Content-Type": "application/json" } : {}),
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...init.headers
    }
  });
  if (!response.ok) throw new Error(`${init.method ?? "GET"} ${path} returned ${response.status}: ${await response.text()}`);
  return response.status === 204 ? undefined : response.json();
}

async function login(email, password) {
  return api("/api/auth/login", null, {
    method: "POST",
    body: JSON.stringify({ email, password })
  });
}

const employee = await login("employee@serviceflow.local", "Employee123!");
const agent = await login("agent@serviceflow.local", "Agent123!");
const requests = await api("/api/requests?page=1&pageSize=100", agent.token);
const request = requests.items.find((item) => item.status !== "Closed");
if (!request) throw new Error("No mutable seeded request was found.");

const nextStatus = {
  Open: "InProgress",
  InProgress: "Pending",
  Pending: "InProgress",
  Resolved: "InProgress"
}[request.status];
if (!nextStatus) throw new Error(`No smoke transition is configured for ${request.status}.`);

let resolveEvent;
let rejectEvent;
const received = new Promise((resolve, reject) => {
  resolveEvent = resolve;
  rejectEvent = reject;
});
const timeout = setTimeout(() => rejectEvent(new Error("Timed out waiting for RequestStatusChanged over SignalR.")), 20_000);

const connection = new signalR.HubConnectionBuilder()
  .withUrl(`${baseUrl}/hubs/notifications`, { accessTokenFactory: () => employee.token })
  .withAutomaticReconnect()
  .configureLogging(signalR.LogLevel.None)
  .build();

connection.on("RequestStatusChanged", (event) => {
  if (Number(event.requestId) === Number(request.id)) resolveEvent(event);
});

try {
  await connection.start();
  await api(`/api/requests/${request.id}/status`, agent.token, {
    method: "PATCH",
    body: JSON.stringify({ status: nextStatus })
  });
  const event = await received;
  const notifications = await api("/api/notifications?page=1&pageSize=100", employee.token);
  const notification = notifications.items.find((item) => item.eventId === event.eventId);
  if (!notification) throw new Error("SignalR event arrived but its durable notification was not found.");

  console.log(JSON.stringify({
    ok: true,
    requestId: request.id,
    transition: `${request.status}->${nextStatus}`,
    signalREvent: event.eventType,
    notificationId: notification.id,
    notificationUser: notification.userId
  }, null, 2));
} finally {
  clearTimeout(timeout);
  await connection.stop();
}
