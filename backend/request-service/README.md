# ServiceFlow Request Service

Microservicio ASP.NET Core 10 organizado con Clean Architecture. Es propietario de la base de datos `ServiceFlowRequests` y publica sus cambios mediante un outbox transaccional a Apache Kafka.

## Configuración

Las claves se pueden sobreescribir con variables de entorno usando `__` como separador:

- `ConnectionStrings__RequestsDatabase`
- `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience`, `Jwt__ExpirationMinutes`
- `RequestId__NodeId` (0-63; debe ser distinto por réplica)
- `Kafka__BootstrapServers`, `Kafka__Topic`, `Kafka__ClientId`
- `Kafka__PollingSeconds`, `Kafka__BatchSize`, `Kafka__MessageTimeoutSeconds`

La API escucha en el puerto `8080` dentro del contenedor. En desarrollo crea el esquema y datos de demostración con `EnsureCreated`, reintentando mientras SQL Server termina de iniciar.

## Usuarios de demostración

| Rol | Correo | Contraseña |
| --- | --- | --- |
| Employee | `employee@serviceflow.local` | `Employee123!` |
| Agent | `agent@serviceflow.local` | `Agent123!` |
| Administrator | `admin@serviceflow.local` | `Admin123!` |

En `Development`, el documento OpenAPI se expone en `/openapi/v1.json` y Swagger UI en `/swagger`. La interfaz incluye autenticación JWT: ejecuta `POST /api/auth/login`, pulsa **Authorize** y pega solamente el valor `token`. Swagger conserva la autorización al recargar; en un equipo compartido usa **Authorize > Logout** cuando termines. En `Production`, ambas rutas se habilitan únicamente si `OpenApi__Enabled=true`. Las sondas están en `/health/live` y `/health/ready`.

## API

| Método | Ruta | Acceso |
| --- | --- | --- |
| POST | `/api/auth/login` | Público |
| POST | `/api/requests` | Employee, Agent, Administrator |
| GET | `/api/requests` | Autenticado; filtros, búsqueda, orden y paginación |
| GET | `/api/requests/{id}` | Autenticado |
| PUT | `/api/requests/{id}` | Agent, Administrator |
| PATCH | `/api/requests/{id}/status` | Agent, Administrator |
| PATCH | `/api/requests/{id}/assignment` | Agent, Administrator |
| POST | `/api/requests/{id}/comments` | Autenticado |
| GET | `/api/requests/{id}/history` | Autenticado |

Todas las respuestas JSON usan camelCase y los enums se representan como texto. La API acepta y devuelve `X-Correlation-ID`; si no se envía, genera uno y lo propaga al evento de integración.

## Desarrollo

```bash
dotnet restore ServiceFlow.Requests.slnx
dotnet test ServiceFlow.Requests.slnx
```
