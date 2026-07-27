# Exposición del proyecto ServiceFlow

## 1. Resumen ejecutivo

**ServiceFlow** es una plataforma web para registrar, asignar y hacer seguimiento a solicitudes internas de una organización. Centraliza casos como soporte técnico, mantenimiento, acceso a sistemas, compras e incidentes operativos, y permite que empleados y personal de atención colaboren sobre una misma solicitud.

El proyecto demuestra una solución distribuida de extremo a extremo: una interfaz web en React, dos microservicios en ASP.NET Core, persistencia independiente en SQL Server, mensajería asíncrona con RabbitMQ y actualizaciones en tiempo real mediante SignalR. Todo el entorno puede ejecutarse localmente con Docker Compose.

## 2. Problema que resuelve

En muchas organizaciones, las solicitudes internas se gestionan mediante correos, chats o archivos separados. Esto dificulta conocer quién atiende cada caso, cuál es su estado, cuánto tiempo lleva abierto y qué acciones se han realizado.

ServiceFlow propone un flujo único y trazable que permite:

- Registrar solicitudes con categoría, prioridad y descripción.
- Asignar responsables y controlar los cambios de estado.
- Calcular automáticamente la fecha límite según el nivel de servicio (SLA).
- Mantener comentarios e historial de cambios.
- Notificar al usuario sin necesidad de recargar la página.
- Consultar métricas, filtros y solicitudes recientes desde un dashboard.

## 3. Usuarios y capacidades

| Rol | Capacidades principales |
|---|---|
| **Employee** | Crea solicitudes, consulta sus propios casos, agrega comentarios y recibe notificaciones. |
| **Agent** | Consulta y atiende solicitudes, actualiza información, asigna responsables y cambia estados. |
| **Administrator** | Dispone de las capacidades operativas de administración y modificación de solicitudes. |

La autenticación se realiza con JWT. La versión actual incluye usuarios de demostración; por tanto, este mecanismo sirve para evaluación local y debe sustituirse por un proveedor de identidad real antes de un despliegue productivo.

## 4. Funcionalidades principales

1. Inicio de sesión y autorización por roles.
2. Dashboard con indicadores y solicitudes recientes.
3. Listado paginado con búsqueda, filtros y ordenamiento.
4. Creación y consulta detallada de solicitudes.
5. Edición, asignación y cambio controlado de estado.
6. Comentarios e historial auditable.
7. Cálculo de vencimiento según la prioridad.
8. Notificaciones persistentes y contador de no leídas.
9. Sincronización en tiempo real entre sesiones mediante SignalR.
10. Documentación OpenAPI, health checks, logs y correlación de peticiones.

## 5. Arquitectura de la solución

```text
Usuario
  │ HTTP / WebSocket
  ▼
React + Nginx (:3000)
  ├── /api/auth y /api/requests ──► Request Service (:5001)
  │                                      │
  │                              SQL Server + Outbox
  │                                      │ eventos
  │                                      ▼
  │                                  RabbitMQ
  │                                      │
  └── /api/notifications y /hubs ─► Notification Service (:5002)
                                         ├── SQL Server
                                         └── SignalR ──► Usuario
```

La solución se divide en los siguientes componentes:

| Componente | Tecnología | Responsabilidad |
|---|---|---|
| Frontend | React 19, TypeScript 5.9, Vite 7, TanStack Query | Interfaz, navegación, formularios, caché y reacción a eventos en tiempo real. |
| Request Service | ASP.NET Core sobre .NET 10, EF Core | Autenticación demo y ciclo de vida completo de las solicitudes. |
| Notification Service | ASP.NET Core sobre .NET 10, EF Core, SignalR | Consume eventos, persiste notificaciones y las entrega al usuario conectado. |
| SQL Server | SQL Server 2022 | Mantiene una base independiente para cada microservicio. |
| RabbitMQ | RabbitMQ 4 | Transporta eventos de solicitudes de forma asíncrona. |
| Nginx | Nginx en el contenedor frontend | Sirve la aplicación compilada y enruta API y WebSocket. |

Cada microservicio usa una organización inspirada en **Clean Architecture**:

- **Domain:** entidades, enumeraciones y reglas esenciales.
- **Application:** casos de uso, contratos, resultados y abstracciones.
- **Infrastructure:** EF Core, repositorios, mensajería y servicios externos.
- **Api:** controladores, autenticación, middleware, OpenAPI y configuración.

Esta separación reduce el acoplamiento entre las reglas de negocio y los detalles técnicos. Además, cada servicio es propietario de su base de datos y no realiza consultas directas sobre la base del otro.

## 6. Flujo principal de una solicitud

1. El empleado inicia sesión y obtiene un token JWT.
2. Desde React registra una solicitud con título, descripción, categoría y prioridad.
3. Request Service valida las reglas, calcula el SLA y guarda la solicitud.
4. En la misma transacción guarda un mensaje en la tabla Outbox.
5. Un proceso en segundo plano publica el mensaje pendiente en RabbitMQ.
6. Notification Service consume el evento y verifica que no se haya procesado antes.
7. El servicio guarda la notificación y registra el evento como procesado.
8. SignalR envía la actualización al grupo del usuario correspondiente.
9. El frontend recibe el evento, actualiza su store e invalida los datos en caché.

El patrón **Transactional Outbox** evita perder un evento si la operación de negocio se guardó pero RabbitMQ no estaba disponible. El consumidor idempotente evita crear notificaciones duplicadas cuando un mensaje se entrega más de una vez.

## 7. Reglas de negocio relevantes

### Estados

Una solicitud comienza en `Open`. El dominio controla las transiciones permitidas entre `Open`, `Pending`, `InProgress`, `Resolved` y `Closed`. Una solicitud cerrada ya no puede editarse ni asignarse.

### Acuerdos de nivel de servicio

La fecha de vencimiento se calcula mediante los patrones Strategy y Factory:

| Prioridad | Tiempo objetivo |
|---|---:|
| Low | 7 días |
| Medium | 3 días |
| High | 1 día |
| Critical | 4 horas |

### Trazabilidad y aislamiento

Cada cambio de estado genera un registro histórico. Las peticiones reciben un identificador de correlación para seguir una operación en los logs. Los empleados solo acceden a sus solicitudes, y las notificaciones de SignalR se dirigen al grupo del usuario autenticado.

## 8. Decisiones técnicas destacadas

- **Microservicios con bases separadas:** delimita responsabilidades y propiedad de datos.
- **Outbox:** coordina persistencia y publicación sin depender de una transacción distribuida.
- **Consumidor idempotente:** tolera la semántica de entrega al menos una vez.
- **Dead-letter queue:** aparta mensajes que no pudieron procesarse después de los reintentos.
- **SignalR:** entrega cambios al navegador en tiempo real.
- **Observer en frontend:** desacopla la recepción de eventos de las vistas que deben actualizarse.
- **Strategy para SLA:** permite variar el cálculo sin llenar el caso de uso de condicionales.
- **Problem Details y OpenAPI:** estandarizan errores y facilitan probar las APIs.
- **Health checks:** distinguen si el proceso está vivo y si sus dependencias están listas.

## 9. API resumida

| Servicio | Operaciones |
|---|---|
| Autenticación | `POST /api/auth/login` |
| Solicitudes | `GET/POST /api/requests`, `GET/PUT /api/requests/{id}` |
| Gestión | `PATCH .../status`, `PATCH .../assignment`, `POST .../comments`, `GET .../history` |
| Notificaciones | Listar, consultar no leídas, contar y marcar como leídas bajo `/api/notifications` |
| Tiempo real | Hub `/hubs/notifications` |
| Operación | `/health/live`, `/health/ready` y OpenAPI/Swagger en desarrollo |

## 10. Calidad y operación

El repositorio incluye pruebas unitarias para dominio, servicios de aplicación, SLA, generación de identificadores, JWT, modelos de EF Core, idempotencia y creación de notificaciones. En el frontend se prueban componentes, formularios, el bus de eventos y el store.

El flujo de integración continua de GitHub Actions:

1. Restaura, compila y prueba la solución .NET.
2. Instala, compila y prueba el frontend.
3. Valida Docker Compose.
4. Construye las imágenes de los dos servicios y del frontend.

## 11. Ejecución local

### Requisitos

- Docker Desktop, o Docker Engine con Docker Compose.
- Al menos 4 GB de memoria disponible.
- Arquitectura x86-64; SQL Server 2022 para Linux no dispone de imagen ARM64 oficial.

### Inicio

```bash
docker compose up --build
```

No es obligatorio crear `.env`, porque la composición incluye valores para demostración. Para personalizar puertos o secretos locales, se puede copiar `.env.example` a `.env`.

| Recurso | URL |
|---|---|
| Aplicación | http://localhost:3000 |
| Request Service | http://localhost:5001 |
| Notification Service | http://localhost:5002 |
| Swagger Requests | http://localhost:5001/swagger |
| Swagger Notifications | http://localhost:5002/swagger |
| RabbitMQ Management | http://localhost:15672 |

### Usuarios de demostración

| Rol | Correo | Contraseña |
|---|---|---|
| Employee | `employee@serviceflow.local` | `Employee123!` |
| Agent | `agent@serviceflow.local` | `Agent123!` |
| Administrator | `admin@serviceflow.local` | `Admin123!` |

## 12. Guion sugerido para una demostración

1. Presentar el problema y los tres roles.
2. Iniciar sesión como empleado y mostrar dashboard, filtros y solicitudes existentes.
3. Crear una solicitud y explicar el cálculo del SLA.
4. Abrir una segunda ventana, iniciar sesión como agente y asignar el caso.
5. Cambiar el estado y agregar un comentario.
6. Volver a la ventana del empleado y mostrar la actualización automática y la notificación.
7. Explicar el recorrido técnico: Outbox, RabbitMQ, consumidor idempotente y SignalR.
8. Mostrar Swagger, health checks y el pipeline de integración continua.

La demostración evidencia el principal valor técnico del proyecto: dos usuarios observan un flujo distribuido consistente y actualizado en tiempo real, aun cuando la publicación de eventos ocurre de forma asíncrona.

## 13. Alcance actual y evolución

La implementación actual está preparada para desarrollo y demostración local. El repositorio documenta una arquitectura objetivo en AWS con frontend estático en S3 y CloudFront, imágenes backend en ECR, servicios en EKS, RDS para SQL Server, Amazon MQ for RabbitMQ, Secrets Manager y CloudWatch. Esa infraestructura es una **propuesta**: todavía no se incluyen manifiestos Kubernetes, Terraform ni un pipeline de despliegue a AWS.

Antes de producción también sería necesario:

- Sustituir usuarios demo y JWT local por un proveedor de identidad.
- Gestionar secretos fuera del repositorio y aplicar credenciales distintas por servicio.
- Reemplazar `EnsureCreated` por migraciones de base de datos controladas.
- Añadir observabilidad, alertas, copias de seguridad y políticas de recuperación.
- Configurar TLS, restricciones de red y permisos mínimos.
- Incorporar un backplane de SignalR antes de escalar Notification Service horizontalmente.
- Ejecutar pruebas de integración, seguridad, carga y resiliencia.

## 14. Conclusión

ServiceFlow no solo resuelve el seguimiento de solicitudes empresariales; también presenta prácticas relevantes para sistemas modernos: separación por dominios, consistencia eventual, entrega fiable de eventos, idempotencia, autorización, tiempo real, pruebas automatizadas y contenerización.

Su principal fortaleza es integrar estas decisiones en un recorrido funcional visible: una acción realizada por un agente se conserva, se publica, se procesa y aparece de inmediato en la sesión del empleado. Esto convierte al proyecto en una base clara para demostrar arquitectura distribuida y en un punto de partida extensible para una solución empresarial.
