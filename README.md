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
| Swagger UI Requests | http://localhost:5001/swagger |
| Swagger UI Notifications | http://localhost:5002/swagger |
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

## Despliegue propuesto en AWS

Docker Compose se conserva para desarrollo y demostraciones locales. En AWS, el resultado estático de React se almacenaría en un bucket S3 privado y se distribuiría con CloudFront; únicamente las dos imágenes .NET se publicarían en Amazon ECR y se ejecutarían en Amazon EKS. Esta es la **arquitectura objetivo**: el repositorio todavía no incluye `deploy/kubernetes/`, `deploy/terraform/` ni un pipeline de entrega que efectúe el despliegue.

```text
 GitHub Actions
    ├── npm run build ──► S3 privado ────────────────┐
    └── docker build ──► ECR ──► EKS                 │
                                   ▲                 │
 Internet ──► Route 53 ──► CloudFront + ACM ◄────────┘
                              │
                              ├── /* ────────────────► S3: frontend/dist
                              │
                              └── /api/*, /hubs/* ──► ALB privado (VPC origin)
                                                        ├── Request Service ──► RDS: ServiceFlowRequests
                                                        │          │
                                                        │          └──────────► Amazon MQ for RabbitMQ
                                                        │                              │
                                                        └── Notification Service ◄─────┘
                                                                   │
                                                                   └──────────► RDS: ServiceFlowNotifications
```

| Componente | Código de origen | Repositorio o almacenamiento propuesto | Ejecución en AWS | Responsabilidad |
|---|---|---|---|---|
| Frontend React | `frontend/` | Bucket S3 privado con el contenido de `frontend/dist/` | Amazon CloudFront con Origin Access Control | Distribuye HTML, CSS y JavaScript desde CDN. Nginx y la imagen `frontend/Dockerfile` se conservan solo para Docker Compose. |
| Request Service | `backend/request-service/` | ECR `serviceflow/request-service:<commit-sha>` | `Deployment` y `Service` en EKS | Atiende `/api/auth/*` y `/api/requests/*`, accede a su base y publica el Outbox. |
| Notification Service | `backend/notification-service/` | ECR `serviceflow/notification-service:<commit-sha>` | `Deployment` y `Service` en EKS | Atiende `/api/notifications/*` y `/hubs/*`, consume eventos y mantiene SignalR. |
| Base de solicitudes | `db/` y `backend/request-service/src/ServiceFlow.Requests.Infrastructure/` | Base `ServiceFlowRequests` | Amazon RDS for SQL Server en subredes privadas | Propiedad exclusiva de Request Service. Para la demo puede compartir instancia RDS con la otra base. |
| Base de notificaciones | `db/` y `backend/notification-service/src/ServiceFlow.Notifications.Infrastructure/` | Base `ServiceFlowNotifications` | Amazon RDS for SQL Server en subredes privadas | Propiedad exclusiva de Notification Service. No se permiten consultas cruzadas. |
| Mensajería asíncrona | `backend/*/src/*Infrastructure/Messaging/` | Exchanges y colas `serviceflow.*` | Amazon MQ for RabbitMQ privado | Sustituye el contenedor local de RabbitMQ conservando AMQP, routing keys, reintento y dead-letter exchange. |
| Entrada HTTP/HTTPS | Futuros `deploy/terraform/` y `deploy/kubernetes/` | Distribución CloudFront y ALB | Route 53 + CloudFront + ACM + ALB creado por AWS Load Balancer Controller | CloudFront es el único punto público; usa S3 como origen predeterminado y el ALB privado como VPC origin para las rutas dinámicas. |
| Migraciones | `db/` y futuras migraciones EF Core | Artefactos incluidos en las imágenes o un paquete de migración | `Job` de Kubernetes con acceso privado a RDS | Versiona el esquema antes del rollout; en producción reemplaza `EnsureCreated`. |
| Secretos | Configuración de los servicios | AWS Secrets Manager | Secrets Store CSI Driver para EKS | Monta o sincroniza claves JWT y credenciales de SQL/RabbitMQ como secretos de Kubernetes, sin guardarlas en Git ni en las imágenes. |
| Logs y métricas | Salida estándar de todos los contenedores | CloudWatch Logs y métricas de Container Insights | Agentes/add-ons del clúster EKS | Centraliza logs, reinicios, health checks, errores y alarmas de la DLQ. |
| CI/CD | `.github/workflows/ci.yml` | Bundle React en S3 y dos imágenes versionadas en ECR | GitHub Actions autenticado con AWS mediante IAM OIDC | Prueba todo, publica `dist/`, construye los backends con el SHA y actualiza los dos `Deployment`. El workflow actual termina en la construcción local. |
| Entorno local | `docker-compose.yml` | Volúmenes Docker locales | No se despliega en AWS | Continúa levantando los cinco contenedores para desarrollo sin depender de servicios cloud. |

Los dos `Service` de Kubernetes serían internos (`ClusterIP`). Un `Ingress` administrado por AWS Load Balancer Controller crearía un ALB interno, registrado como VPC origin de CloudFront. La distribución aplicaría estos comportamientos:

| Ruta pública | Origen y destino | Caché |
|---|---|---|
| `/*` | S3 → archivos de `frontend/dist/` | Habilitada; assets con hash e `immutable`, `index.html` con TTL corto |
| `/api/auth/*`, `/api/requests*` | ALB → Request Service | Deshabilitada; reenvía métodos, query strings y `Authorization` |
| `/api/notifications*` | ALB → Notification Service | Deshabilitada; reenvía métodos, query strings y `Authorization` |
| `/hubs/*` | ALB → Notification Service / SignalR | Deshabilitada; reenvía query strings y encabezados de WebSocket |

CloudFront y Application Load Balancer soportan WebSockets, por lo que SignalR puede atravesar el mismo dominio usando `wss://`. La política del origen debe reenviar los encabezados WebSocket y la query string `access_token`. Una CloudFront Function reescribiría rutas de la SPA sin extensión, como `/requests/123`, hacia `/index.html`, excluyendo `/api/*` y `/hubs/*`. S3 se protege con Origin Access Control; el ALB, RDS, Amazon MQ, los nodos y los pods permanecen en subredes privadas.

### RabbitMQ actual y alternativa con Amazon SQS

La implementación actual depende de `RabbitMQ.Client`, exchanges, routing keys, bindings y dead-letter exchanges. Por ello, la ruta de menor cambio para el primer despliegue es **Amazon MQ for RabbitMQ**, habilitando AMQPS/TLS en ambos microservicios y almacenando sus credenciales en Secrets Manager.

La propuesta original plantea **Amazon SQS**. SQS no es un reemplazo configurable del protocolo RabbitMQ: para adoptarlo deben crearse adaptadores como `SqsEventPublisher` y `SqsNotificationConsumer` con AWS SDK. Request Service tendría permiso IAM únicamente para enviar; Notification Service podría recibir y eliminar; una redrive policy movería los fallos a una DLQ. El dominio, los casos de uso, el Outbox y el control idempotente mediante `ProcessedEvents` se conservarían.

### Secuencia de aprovisionamiento y entrega

1. Terraform aprovisiona VPC, subredes en al menos dos zonas, EKS, dos repositorios ECR, el bucket S3 privado, RDS, Amazon MQ, Secrets Manager y CloudWatch, e instala AWS Load Balancer Controller.
2. El bootstrap aplica el `Ingress`; el controlador crea el ALB interno y una segunda etapa de infraestructura lo registra como VPC origin de CloudFront, configura ACM y publica el dominio en Route 53.
3. GitHub Actions restaura dependencias y ejecuta las pruebas de .NET y React.
4. El pipeline ejecuta `npm run build`, sincroniza `frontend/dist/` con S3 e invalida `/index.html` en CloudFront.
5. El pipeline construye las dos imágenes .NET, las etiqueta con el SHA inmutable del commit y las publica en ECR.
6. Un `Job` controlado aplica las migraciones de ambas bases.
7. Helm o `kubectl` actualiza los dos `Deployment` y EKS realiza el rolling update utilizando `/health/live` y `/health/ready`.

Antes de aumentar réplicas, Notification Service necesita un backplane de SignalR, por ejemplo Amazon ElastiCache for Redis, y afinidad de sesión en el ALB; el comportamiento `/hubs/*` de CloudFront también debe reenviar la cookie de afinidad. Inicialmente debe ejecutarse con una réplica. Cada réplica de Request Service necesita un `RequestId__NodeId` diferente. Para producción deben eliminarse los usuarios demo y valores predeterminados, sustituir el proveedor JWT simulado por un proveedor como Amazon Cognito, usar `ASPNETCORE_ENVIRONMENT=Production`, permisos IAM mínimos y credenciales SQL distintas por microservicio.

Referencias: [sitio estático seguro con S3 y CloudFront](https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/getting-started-secure-static-website-cloudformation-template.html), [comportamientos y múltiples orígenes](https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/DownloadDistValuesCacheBehavior.html), [WebSockets en CloudFront](https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/distribution-working-with.websockets.html), [VPC origins privados](https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/private-content-vpc-origins.html), [AWS Load Balancer Controller](https://docs.aws.amazon.com/eks/latest/userguide/aws-load-balancer-controller.html), [Amazon ECR](https://docs.aws.amazon.com/AmazonECR/latest/userguide/docker-push-ecr-image.html), [Amazon RDS for SQL Server](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/CHAP_SQLServer.html), [Amazon MQ for RabbitMQ](https://docs.aws.amazon.com/amazon-mq/latest/developer-guide/working-with-rabbitmq.html), [DLQ de Amazon SQS](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/sqs-dead-letter-queues.html), [Secrets Manager con EKS](https://docs.aws.amazon.com/eks/latest/userguide/manage-secrets.html), [IAM OIDC](https://docs.aws.amazon.com/IAM/latest/UserGuide/id_roles_providers_oidc.html) y [CloudWatch Container Insights](https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/ContainerInsights.html).

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

### Swagger UI

En el entorno `Development`, ambos microservicios exponen documentación interactiva:

- Request Service: http://localhost:5001/swagger
- Notification Service: http://localhost:5002/swagger

Para probar endpoints protegidos, ejecuta primero `POST /api/auth/login` en Request Service, copia el valor `token`, pulsa **Authorize** y pega únicamente el JWT, sin escribir el prefijo `Bearer`. El esquema OpenAPI aplica el requisito de seguridad solo a las operaciones que tienen autorización. Swagger conserva la autorización al recargar; en un equipo compartido usa **Authorize > Logout** cuando termines.

Swagger UI y el documento OpenAPI están deshabilitados por defecto en `Production`. Para habilitarlos explícitamente en un entorno controlado, configura `OpenApi__Enabled=true`; no se recomienda publicar estas rutas sin protección en internet.

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
