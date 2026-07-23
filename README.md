# ServiceFlow

Plataforma web para registrar, asignar y seguir solicitudes empresariales. El proyecto combina **ASP.NET Core .NET 10**, **React 19 + TypeScript**, SQL Server, RabbitMQ y SignalR en una arquitectura de microservicios instalable con Docker Compose.

## Inicio rápido

Requisitos: Docker Desktop o Docker Engine + Compose en un equipo x86-64, con al menos 4 GB de memoria disponibles. SQL Server 2022 para Linux no ofrece una imagen ARM64 oficial; en equipos Apple Silicon se requiere emulación `linux/amd64`, con menor rendimiento.

```bash
git clone <url-del-repositorio>
cd dot-net-cbtw-project-sample
docker compose up --build
```

No es obligatorio crear un archivo `.env`; Compose incluye valores seguros para una demostración local. Para personalizarlos:

```bash
cp .env.example .env
docker compose up --build
```

En PowerShell, el primer comando es `Copy-Item .env.example .env`.

La primera ejecución descarga SQL Server y compila las imágenes, por lo que puede tardar unos minutos. Cuando los contenedores estén listos:

| Recurso | Dirección |
|---|---|
| Aplicación React | http://localhost:3000 |
| Request Service | http://localhost:5001 |
| Notification Service | http://localhost:5002 |
| OpenAPI Requests | http://localhost:5001/openapi/v1.json |
| OpenAPI Notifications | http://localhost:5002/openapi/v1.json |
| RabbitMQ Management | http://localhost:15672 |

Puertos y credenciales de infraestructura se pueden cambiar en `.env`.

### Usuarios de demostración

| Rol | Correo | Contraseña |
|---|---|---|
| Employee | `employee@serviceflow.local` | `Employee123!` |
| Agent | `agent@serviceflow.local` | `Agent123!` |
| Administrator | `admin@serviceflow.local` | `Admin123!` |

El arranque inicial crea tres solicitudes de ejemplo para que el dashboard no aparezca vacío.

## Arquitectura

```text
 Navegador
    │ HTTP + WebSocket
    ▼
 React / Nginx :3000
    ├── /api/auth, /api/requests ─────► Request Service :8080
    │                                      │
    │                                EF Core + Outbox
    │                                      │ eventos
    │                                      ▼
    │                               RabbitMQ topic
    │                                      │
    └── /api/notifications, /hubs ──► Notification Service :8080
                                           │       │
                                     EF Core    SignalR
                                           │       │
                                           └───────┘

 SQL Server
    ├── ServiceFlowRequests       (solo Request Service)
    └── ServiceFlowNotifications  (solo Notification Service)
```

Request Service guarda el cambio de negocio y su evento en la misma transacción mediante el patrón **Outbox**. Un proceso en segundo plano publica el evento persistente en RabbitMQ. Notification Service lo consume de forma idempotente, guarda `ProcessedEvent`, crea la notificación y publica por SignalR. React recibe el evento a través de un `EventBus`; un store observado con `useSyncExternalStore` actualiza las vistas e invalida la caché de TanStack Query.

Esto permite que una ventana de empleado vea de inmediato los cambios realizados por un agente en otra ventana, sin recargar la página.

## Estructura

```text
.
├── frontend/                         React, TypeScript, Vite y Nginx
├── backend/
│   ├── request-service/              dominio de solicitudes + outbox
│   │   ├── src/                      Api, Application, Domain, Infrastructure
│   │   └── tests/                    pruebas unitarias
│   └── notification-service/         consumidor idempotente + SignalR
│       ├── src/                      Api, Application, Domain, Infrastructure
│       └── tests/                    pruebas unitarias
├── db/                               scripts/documentación de las bases
├── docker-compose.yml
└── ServiceFlow.slnx                  solución .NET completa
```

Cada microservicio aplica Clean Architecture y posee su propia base de datos. No hay consultas cruzadas entre servicios.

## Funcionalidad incluida

- Inicio de sesión JWT y autorización por roles `Employee`, `Agent` y `Administrator`.
- Dashboard con métricas, distribución por estado y solicitudes recientes.
- Listado paginado con búsqueda, filtros y ordenamiento.
- Creación, edición, asignación y cambio de estado de solicitudes.
- Cálculo de vencimiento SLA mediante Strategy + Factory.
- Comentarios e historial auditado de estados.
- Outbox persistente, entrega asíncrona e idempotent consumer.
- Reintento controlado y dead-letter queue `serviceflow.notifications.dead-letter`.
- Notificaciones persistidas, contador, marcar una o todas como leídas.
- Actualización en tiempo real con SignalR, reconexión automática y patrón Observer en React.
- Aislamiento por propietario: empleados solo ven sus casos y SignalR entrega eventos a su grupo de usuario.
- Correlation ID propagado, logs estructurados, Problem Details, OpenAPI y health checks.
- Datos y colas persistentes en volúmenes Docker.
- Interfaces responsive para escritorio, tableta y móvil.
- Integración continua para compilar, probar y construir las tres imágenes en cada cambio.

## API principal

Todos los endpoints salvo el login y los health checks requieren `Authorization: Bearer <token>`.

### Request Service

| Método | Endpoint | Acceso |
|---|---|---|
| `POST` | `/api/auth/login` | Público |
| `GET` / `POST` | `/api/requests` | Autenticado |
| `GET` / `PUT` | `/api/requests/{id}` | GET autenticado; PUT Agent/Admin |
| `PATCH` | `/api/requests/{id}/status` | Agent/Admin |
| `PATCH` | `/api/requests/{id}/assignment` | Agent/Admin |
| `POST` | `/api/requests/{id}/comments` | Autenticado |
| `GET` | `/api/requests/{id}/history` | Autenticado |

### Notification Service

| Método | Endpoint |
|---|---|
| `GET` | `/api/notifications` |
| `GET` | `/api/notifications/unread` |
| `GET` | `/api/notifications/unread-count` |
| `PATCH` | `/api/notifications/{id}/read` |
| `PATCH` | `/api/notifications/read-all` |
| WebSocket | `/hubs/notifications` |

Health checks: `/health/live` comprueba el proceso y `/health/ready` incluye la base de datos.

## Demostración en tiempo real

1. Abre dos ventanas del navegador (una puede ser incógnito).
2. Inicia sesión como `Employee` en la primera y abre una solicitud.
3. Inicia sesión como `Agent` en la segunda.
4. Asigna la solicitud, cambia su estado o agrega un comentario.
5. La ventana del empleado recibe el evento, muestra el indicador de sincronización y actualiza detalle, historial y notificaciones automáticamente.

## Desarrollo y pruebas

Con .NET 10 SDK y Node.js 24 instalados:

```bash
dotnet restore ServiceFlow.slnx
dotnet build ServiceFlow.slnx --configuration Release
dotnet test ServiceFlow.slnx --configuration Release --no-build

cd frontend
npm ci
npm run build
npm test
```

Para ejecutar Vite durante desarrollo, inicia primero SQL Server, RabbitMQ y los dos servicios, y luego usa `npm run dev`. Vite redirige Requests a `localhost:5001`, Notifications/SignalR a `localhost:5002`.

Comandos operativos útiles:

```bash
docker compose ps
docker compose logs -f request-service notification-service
docker compose down
```

`docker compose down` conserva las bases y mensajes. Para reiniciar completamente los datos de desarrollo se puede usar `docker compose down --volumes`; esta última operación elimina de forma irreversible los volúmenes locales de ServiceFlow.

## Configuración y seguridad

Las claves de `.env.example` son exclusivamente de desarrollo. Antes de un despliegue real:

- configura secretos desde el gestor de secretos de la plataforma;
- cambia `MSSQL_SA_PASSWORD`, `RABBITMQ_PASSWORD` y `JWT_KEY`;
- usa un proveedor de identidad real en lugar de los usuarios demo;
- habilita HTTPS y restringe CORS;
- crea usuarios SQL independientes con permisos mínimos por base;
- reemplaza `EnsureCreated` por migraciones EF Core versionadas.

Si Request Service se escala a varias réplicas, configura un valor `RequestId__NodeId` distinto (0–63) en cada instancia para conservar la unicidad de sus identificadores Snowflake de 53 bits.

## Solución de problemas

- **Docker no responde:** inicia Docker Desktop y espera a que el motor Linux esté listo.
- **SQL Server queda `unhealthy`:** asigna al menos 4 GB de memoria a Docker y revisa `docker compose logs sqlserver`.
- **Un puerto ya está ocupado:** cambia el puerto correspondiente en `.env`.
- **La interfaz abre pero aún no carga datos:** revisa `docker compose ps`; ambos servicios deben haber terminado sus reintentos de conexión a SQL Server.
- **No llegan eventos:** abre RabbitMQ Management y comprueba la cola `serviceflow.notifications`, o revisa los logs de ambos microservicios.
