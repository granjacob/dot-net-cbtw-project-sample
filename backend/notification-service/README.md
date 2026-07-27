# ServiceFlow Notification Service

Microservicio ASP.NET Core 10 encargado de consumir eventos de solicitudes,
persistir notificaciones idempotentes y publicarlas en tiempo real mediante SignalR.

## Estructura

- `Domain`: entidades `Notification` y `ProcessedEvent`.
- `Application`: contratos, repositorio y casos de uso de consulta/lectura.
- `Infrastructure`: EF Core SQL Server, inicialización con reintentos y consumidor RabbitMQ.
- `Api`: endpoints JWT, Problem Details, SignalR, CORS, OpenAPI y health checks.
- `tests`: pruebas unitarias xUnit.

## Contrato RabbitMQ

- Exchange topic: `serviceflow.events`
- Queue durable: `serviceflow.notifications`
- Binding: `request.#` (incluye el routing key `request.event`)

Envelope esperado (camelCase):

```json
{
  "eventId": "6fb7c6a0-4204-49bb-9817-475d89928118",
  "eventType": "RequestStatusChanged",
  "occurredAt": "2026-07-21T20:30:00Z",
  "requestId": 148,
  "userId": "employee@serviceflow.local",
  "title": "Estado actualizado",
  "message": "La solicitud #148 cambió a Resolved",
  "correlationId": "request-correlation-id",
  "data": {
    "previousStatus": "InProgress",
    "newStatus": "Resolved"
  }
}
```

`eventId` es la clave idempotente. `ProcessedEvent` y `Notification` se guardan en
una misma transacción SQL. Si `userId` no llega en el envelope ni en `data`, se usa
`employee@serviceflow.local`.

## API

Todos los endpoints funcionales exigen JWT con issuer `ServiceFlow`, audience
`ServiceFlow.Client` y una clave compartida configurada en `Jwt__Key`.

- `GET /api/notifications?page=1&pageSize=20&isRead=false`
- `GET /api/notifications/unread?page=1&pageSize=20`
- `GET /api/notifications/unread-count`
- `PATCH /api/notifications/{id}/read`
- `PATCH /api/notifications/read-all`

Las listas devuelven:

```json
{
  "items": [],
  "total": 0,
  "page": 1,
  "pageSize": 20,
  "totalPages": 0
}
```

El DTO de cada item contiene `id`, `userId`, `type`, `title`, `message`, `isRead`,
`createdAt`, `eventId` y `requestId`.

El hub está en `/hubs/notifications` y acepta el JWT por `access_token` durante el
handshake WebSocket. Publica el nombre exacto recibido en `eventType`
(`RequestCreated`, `RequestUpdated`, `RequestAssigned`, `RequestStatusChanged` o
`CommentAdded`) y luego `NotificationCreated`. Con `SignalR__BroadcastToAll=true`
se transmite a todos para la demo; en `false`, se usa el grupo del usuario destino.

## Configuración principal

Las claves admiten el formato de variables de entorno de .NET (`__`):

- `ConnectionStrings__NotificationsDatabase`
- `Jwt__Issuer`, `Jwt__Audience`, `Jwt__Key`
- `RabbitMq__HostName`, `RabbitMq__Port`, `RabbitMq__UserName`, `RabbitMq__Password`
- `RabbitMq__Exchange`, `RabbitMq__Queue`, `RabbitMq__RoutingKey`
- `Cors__Origins__0`
- `SignalR__BroadcastToAll`
- `DatabaseInitialization__Enabled`, `DatabaseInitialization__MaxRetries`
- `OpenApi__Enabled`

La base `ServiceFlowNotifications` se crea con `EnsureCreatedAsync` y reintentos al
arrancar. Los probes son `/health/live` y `/health/ready`.

## OpenAPI y Swagger UI

OpenAPI y Swagger UI se habilitan automáticamente en `Development`. En cualquier
otro entorno permanecen deshabilitados, salvo que se configure
`OpenApi__Enabled=true` explícitamente.

Con los puertos predeterminados de Docker Compose están disponibles en:

- Documento OpenAPI: `http://localhost:5002/openapi/v1.json`
- Swagger UI: `http://localhost:5002/swagger`

Los endpoints de notificaciones requieren un JWT. Se puede obtener uno iniciando
sesión en Request Service:

```powershell
$login = Invoke-RestMethod `
  -Method Post `
  -Uri http://localhost:5001/api/auth/login `
  -ContentType application/json `
  -Body '{"email":"employee@serviceflow.local","password":"Employee123!"}'

$login.token
```

En Swagger UI, selecciona **Authorize**, pega únicamente el valor de
`$login.token` y confirma. No agregues manualmente el prefijo `Bearer`; Swagger UI
lo agrega al encabezado `Authorization`. La autorización se conserva al recargar
la página, por lo que debe cerrarse desde **Authorize > Logout** al terminar en un
equipo compartido.

## Verificación

```bash
dotnet restore ServiceFlow.Notifications.slnx
dotnet build ServiceFlow.Notifications.slnx -c Release --no-restore
dotnet test ServiceFlow.Notifications.slnx -c Release --no-build
docker build -t serviceflow-notifications .
```
