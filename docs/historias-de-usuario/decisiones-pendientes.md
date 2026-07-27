# Decisiones pendientes de producto y arquitectura

[Volver al índice](README.md)

## Propósito

Estas decisiones evitan esconder ambigüedades dentro de los criterios de
aceptación. Una historia afectada no cumple la Definition of Ready hasta que su
decisión bloqueante haya sido ratificada.

Las líneas base permiten estimar el backlog; no sustituyen la aprobación del
Product Owner o del responsable de arquitectura.

## Resumen

| ID | Decisión | Responsable sugerido | Línea base para estimar |
|---|---|---|---|
| DEC-001 | Proveedor de identidad | Producto + Seguridad | JWT local en desarrollo y Cognito/OIDC en producción. |
| DEC-002 | Diferencia entre Agente y Administrador | Producto | Mismos permisos operativos; Administrador añade supervisión. |
| DEC-003 | Responsable elegible | Producto + Seguridad | Identificador de usuario en MVP; solo agentes activos en producción. |
| DEC-004 | Comentarios después del cierre | Producto | Permitidos, inmutables y solo para usuarios autorizados. |
| DEC-005 | Semántica completa del SLA | Producto | Tiempo calendario y vencimientos 7 d, 3 d, 24 h y 4 h. |
| DEC-006 | Destinatarios de eventos y notificaciones | Producto + Arquitectura | Persistencia para creador; tiempo real para creador y operación. |
| DEC-007 | Cola “Asignadas a mí” | Producto | Filtro visible para agente y vínculo desde el dashboard. |
| DEC-008 | Alcance de auditoría | Producto + Cumplimiento | Historial funcional de estados en MVP. |
| DEC-009 | Edición concurrente | Arquitectura | Concurrencia optimista y respuesta explícita de conflicto. |
| DEC-010 | Fórmulas del dashboard | Producto | Agregados de servidor sobre el conjunto completo autorizado. |
| DEC-011 | Broker local y de AWS | Arquitectura | RabbitMQ local; decidir Amazon MQ o adaptador SQS en AWS. |
| DEC-012 | Objetivos no funcionales | Producto + SRE | Definir SLO, rendimiento, volumen, RTO, RPO y retención. |
| DEC-013 | Alcance de la release AWS | Producto + Plataforma | Frontend en S3/CloudFront y APIs en EKS. |

## DEC-001 — Proveedor de identidad

**Preguntas:** ¿se mantendrá un emisor JWT propio, se integrará Amazon Cognito o
se utilizará otro proveedor OIDC? ¿Cómo se resuelven alta, baja, recuperación,
MFA y revocación?

**Impacta:** HU-AUT-001, HU-AUT-002, HT-SEC-001 y HT-DEP-001.

**Recomendación:** conservar usuarios simulados solo para desarrollo y pruebas.
Usar un proveedor administrado compatible con OIDC en producción y no guardar
contraseñas dentro de ServiceFlow.

## DEC-002 — Diferencia entre Agente y Administrador

**Preguntas:** ¿el administrador también atiende solicitudes? ¿Puede configurar
catálogos, SLA, roles o reportes? ¿Los tres roles pueden crear solicitudes?

**Impacta:** HU-AUT-003, HU-SOL-001 y las historias de dashboard.

**Línea base:** los tres roles pueden crear; Agente y Administrador comparten
permisos operativos. Las funciones exclusivas de configuración quedan fuera del
MVP hasta que negocio las defina.

## DEC-003 — Responsable elegible

**Preguntas:** ¿se escribe un correo o se selecciona desde un directorio? ¿Solo un
agente activo puede ser responsable? ¿Se permite desasignar y autoasignarse?

**Impacta:** HU-FLU-001, HU-DAS-002 y las notificaciones de asignación.

**Recomendación:** seleccionar un usuario activo con rol Agente a partir del
proveedor de identidad; permitir autoasignación, reasignación y desasignación con
trazabilidad.

## DEC-004 — Comentarios en solicitudes cerradas

**Preguntas:** ¿el cierre bloquea toda interacción o se permite información
posterior? ¿Existe una ventana de tiempo?

**Impacta:** HU-COM-001.

**Línea base:** permitir comentarios inmutables después del cierre para agregar
contexto, sin reabrir ni alterar los datos de la solicitud.

## DEC-005 — Semántica del SLA

Se deben ratificar:

- duración por prioridad;
- tiempo calendario o calendario laboral;
- zona horaria y festivos;
- pausa o continuidad durante Pendiente;
- efecto de cambiar prioridad;
- efecto de resolver, cerrar o reabrir;
- umbral de “próximo a vencer”;
- diferencia entre primera respuesta y resolución;
- reglas de cumplimiento histórico y escalamiento.

**Impacta:** HU-SLA-001, HU-SLA-002 y HU-DAS-003.

**Línea base:** cálculo desde la creación en tiempo calendario; al cambiar la
prioridad se recalcula desde la creación; Resuelta y Cerrada detienen el
indicador activo.

## DEC-006 — Destinatarios de eventos y notificaciones

**Preguntas:** para cada evento, ¿reciben aviso el creador, el responsable, el
actor, todos los agentes o los administradores? ¿Quién recibe una notificación
persistente y quién solo una actualización operativa en vivo?

**Impacta:** toda la épica EP-04.

**Línea base:** el creador recibe notificación persistente. El creador y los
grupos Agente/Administrador reciben actualizaciones en vivo de acuerdo con su
alcance. Una asignación futura debería notificar también al nuevo responsable.

### Matriz inicial por evento y canal

| Evento | Notificación persistente | SignalR al creador | SignalR a Agente/Administrador |
|---|---|:---:|:---:|
| RequestCreated | Creador | Sí | Sí |
| RequestUpdated | Creador | Sí | Sí |
| RequestAssigned | Creador; nuevo responsable por confirmar | Sí | Sí |
| RequestStatusChanged | Creador | Sí | Sí |
| CommentAdded | Creador | Sí | Sí |
| NotificationCreated | No crea otro registro; representa la notificación ya persistida | Sí | No |

El actor de un cambio no recibe automáticamente una notificación persistente si
no coincide con un destinatario definido. Los grupos operativos reciben el evento
de dominio para sincronizar sus vistas, pero eso no agrega elementos a su centro
personal de notificaciones.

## DEC-007 — Experiencia “Asignadas a mí”

**Preguntas:** ¿es un filtro del listado, una bandeja independiente o ambas?
¿Debe incluir Pendientes y excluir Resueltas?

**Impacta:** HU-SOL-003 y HU-DAS-002.

**Recomendación:** ofrecer un filtro visible reutilizable desde el dashboard y
excluir únicamente Cerradas de la carga activa.

## DEC-008 — Alcance y retención de auditoría

**Preguntas:** ¿el historial debe registrar solo estados o también edición,
prioridad, asignación y comentarios? ¿Cuánto tiempo se conserva y quién lo
consulta?

**Impacta:** HU-AUD-001, HT-OBS-001 y obligaciones de cumplimiento.

**Línea base:** historial funcional de creación y cambios de estado; los eventos
técnicos no sustituyen una auditoría empresarial si esta es requerida.

## DEC-009 — Edición concurrente

**Preguntas:** ¿qué ocurre si dos agentes editan el mismo caso? ¿se rechaza el
segundo cambio, se fusiona o gana el último?

**Impacta:** HU-SOL-005, HU-FLU-001 y HU-FLU-002.

**Recomendación:** exponer una versión o ETag, aplicar concurrencia optimista y
devolver un conflicto que permita recargar antes de reintentar.

## DEC-010 — Fórmulas del dashboard

Se deben definir los estados incluidos en “activo” y “abierto”, la definición de
“crítico”, periodos, zona horaria, solicitudes reabiertas y redondeo de
porcentajes.

**Impacta:** toda la épica EP-05.

**Recomendación:** calcular agregados en el backend sobre el conjunto completo
autorizado; no derivarlos de una página limitada del listado.

## DEC-011 — Broker local y de AWS

La propuesta original menciona Amazon SQS, mientras el desarrollo local puede
usar RabbitMQ. SQS no implementa AMQP y requiere un adaptador distinto.

**Opciones:**

1. RabbitMQ local y Amazon MQ for RabbitMQ en AWS: menor cambio.
2. RabbitMQ local y Amazon SQS en AWS: mayor trabajo, menor operación de broker.
3. LocalStack/SQS local y SQS en AWS: mayor simetría, nuevo entorno de desarrollo.

**Impacta:** HT-EVT-001, HT-EVT-002 y HT-DEP-001.

## DEC-012 — Objetivos no funcionales

Antes de dimensionar producción se deben acordar:

- disponibilidad mensual;
- latencia de APIs y tiempo máximo de actualización en vivo;
- usuarios concurrentes y solicitudes por día;
- tamaño y retención de datos, eventos, notificaciones y logs;
- RTO y RPO;
- ventanas de mantenimiento;
- accesibilidad objetivo;
- navegadores y dispositivos soportados.

**Impacta:** arquitectura, pruebas, costos y criterios operativos de todas las
releases.

## DEC-013 — Alcance de la release AWS

**Preguntas:** ¿AWS forma parte del MVP de evaluación o de una release posterior?
¿Se exige infraestructura como código, dominio real, alta disponibilidad y
rollback automatizado?

**Línea base:** frontend estático en S3 privado distribuido por CloudFront; dos
APIs en EKS; bases lógicas separadas en RDS SQL Server; broker privado; secretos
externos; observabilidad en CloudWatch.

**Impacta:** HT-SEC-001 y HT-DEP-001.
