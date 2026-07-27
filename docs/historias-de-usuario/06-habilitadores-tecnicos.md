# EP-06 — Habilitadores técnicos de plataforma y entrega

[Volver al índice](README.md)

## Objetivo de la épica

Hacer que las historias funcionales puedan operar con integridad, seguridad,
portabilidad, diagnóstico y un proceso de entrega repetible. Estos elementos no
se presentan como valor funcional directo y por eso utilizan el prefijo HT.

## HT-ARC-001 — Mantener límites entre microservicios

| Campo | Valor |
|---|---|
| Prioridad | Must |
| Release | MVP funcional |
| Responsable | Equipo de arquitectura y desarrollo |
| Estado | Propuesta |

> Como equipo de desarrollo, queremos separar solicitudes y notificaciones para
> poder evolucionar y desplegar cada capacidad sin acoplar sus datos internos.

### Criterios de aceptación

- **CA-01:** Request Service es propietario exclusivo de la base de solicitudes.
- **CA-02:** Notification Service es propietario exclusivo de la base de notificaciones y eventos procesados.
- **CA-03:** Ningún microservicio consulta o modifica directamente tablas del otro.
- **CA-04:** La comunicación funcional desde solicitudes hacia notificaciones ocurre mediante contratos de eventos versionables.
- **CA-05:** Cada servicio puede compilarse, probarse, configurarse y desplegarse de forma independiente.
- **CA-06:** La indisponibilidad de Notification Service no impide registrar o gestionar solicitudes.
- **CA-07:** El repositorio conserva las áreas principales frontend, backend y db, con límites comprensibles.

## HT-EVT-001 — Publicar eventos sin perder cambios confirmados

| Campo | Valor |
|---|---|
| Prioridad | Must |
| Release | MVP funcional |
| Responsable | Equipo backend |
| Estado | Propuesta |

> Como responsable del producto, quiero que todo cambio confirmado produzca su
> evento aun si el broker falla temporalmente para no perder notificaciones.

### Criterios de aceptación

- **CA-01:** Crear, editar, asignar, cambiar estado o comentar guarda el cambio y su mensaje Outbox dentro de la misma transacción.
- **CA-02:** Cada evento contiene identificador único, tipo, fecha UTC, solicitud, usuario destinatario, datos relevantes e identificador de correlación.
- **CA-03:** Un proceso independiente publica mensajes pendientes sin bloquear la petición del usuario.
- **CA-04:** Un mensaje se marca como publicado únicamente después de que el broker confirma su recepción.
- **CA-05:** Ante una falla transitoria, el mensaje permanece pendiente y se reintenta sin revertir el cambio funcional ya confirmado.
- **CA-06:** Reiniciar Request Service no elimina eventos pendientes.
- **CA-07:** Los contratos cubren RequestCreated, RequestUpdated, RequestAssigned, RequestStatusChanged y CommentAdded.

## HT-EVT-002 — Consumir eventos de manera idempotente

| Campo | Valor |
|---|---|
| Prioridad | Must |
| Release | MVP funcional |
| Responsable | Equipo backend |
| Estado | Propuesta |

> Como operador de plataforma, quiero que una entrega repetida o fallida no
> duplique notificaciones ni bloquee indefinidamente la cola.

### Criterios de aceptación

- **CA-01:** Notification Service registra el identificador de cada evento procesado con una restricción de unicidad.
- **CA-02:** La notificación y el registro del evento procesado se guardan en una misma transacción.
- **CA-03:** Recibir nuevamente un evento confirmado no crea otra notificación ni otro efecto en tiempo real.
- **CA-04:** Los errores transitorios se reintentan según una política acotada y observable.
- **CA-05:** Después de agotar los reintentos, el mensaje se envía a una dead-letter queue y el consumidor continúa con otros mensajes.
- **CA-06:** Los logs permiten identificar evento, intento y correlación sin registrar credenciales.
- **CA-07:** Existe un procedimiento documentado para inspeccionar y reprocesar mensajes de la dead-letter queue.
- **CA-08:** Reiniciar el consumidor no pierde el registro de idempotencia.

### Dependencias y notas

- La tecnología definitiva del broker se resuelve en DEC-011.

## HT-OPS-001 — Instalar el entorno local con Docker Compose

| Campo | Valor |
|---|---|
| Prioridad | Must |
| Release | MVP funcional |
| Responsable | Equipo de plataforma |
| Estado | Propuesta |

> Como desarrollador o evaluador, quiero levantar toda la solución con Docker
> Compose para probarla en otra máquina sin instalar cada tecnología por separado.

### Criterios de aceptación

- **CA-01:** En una máquina compatible que solo tenga Docker y Compose, ejecutar docker compose up --build inicia la solución.
- **CA-02:** Se levantan frontend, Request Service, Notification Service, SQL Server y el broker local.
- **CA-03:** Los servicios esperan a que sus dependencias estén disponibles y exponen un estado de salud verificable.
- **CA-04:** La aplicación queda accesible mediante puertos documentados y las URLs del frontend no dependen de localhost dentro de los contenedores.
- **CA-05:** Bases y colas que lo requieren conservan datos en volúmenes tras reinicios normales.
- **CA-06:** Puertos, credenciales de demostración y configuraciones pueden reemplazarse mediante variables de entorno.
- **CA-07:** Los valores locales se identifican como demostrativos y no se presentan como secretos válidos de producción.
- **CA-08:** El README documenta inicio, verificación, detención, limpieza voluntaria y solución de problemas comunes.

## HT-OBS-001 — Observar y diagnosticar un flujo completo

| Campo | Valor |
|---|---|
| Prioridad | Must |
| Release | MVP funcional |
| Responsable | Equipo backend y plataforma |
| Estado | Propuesta |

> Como operador de plataforma, quiero seguir una operación desde HTTP hasta el
> evento y la notificación para diagnosticar fallas con rapidez.

### Criterios de aceptación

- **CA-01:** Toda petición acepta un X-Correlation-ID válido o genera uno cuando no se proporciona.
- **CA-02:** El mismo identificador se registra y propaga en la respuesta HTTP, el Outbox, el mensaje y el procesamiento posterior.
- **CA-03:** Los servicios producen logs estructurados con nivel, servicio, operación, correlación y resultado.
- **CA-04:** Los logs nunca incluyen contraseñas, JWT completos, cadenas de conexión ni secretos.
- **CA-05:** Cada API expone liveness para el proceso y readiness para sus dependencias indispensables.
- **CA-06:** Los errores HTTP usan un formato Problem Details consistente y proporcionan una correlación útil para soporte.
- **CA-07:** Una falla de una dependencia se refleja en readiness sin declarar muerto un proceso que todavía puede recuperarse.

## HT-QUA-001 — Automatizar las comprobaciones de calidad

| Campo | Valor |
|---|---|
| Prioridad | Must |
| Release | MVP funcional |
| Responsable | Equipo de desarrollo |
| Estado | Propuesta |

> Como equipo de desarrollo, queremos comprobar cada cambio automáticamente para
> detectar regresiones antes de integrarlo.

### Criterios de aceptación

- **CA-01:** Cada pull request restaura dependencias, compila backend y frontend y ejecuta sus pruebas automatizadas.
- **CA-02:** El backend cubre reglas del dominio, servicios de aplicación, autorización y contratos críticos.
- **CA-03:** Existen pruebas de integración para endpoints y persistencia con dependencias aisladas y reproducibles.
- **CA-04:** El frontend prueba validaciones de formularios, store observado, bus de eventos y estados principales de interfaz.
- **CA-05:** El pipeline construye las tres imágenes utilizadas por Docker Compose.
- **CA-06:** Una compilación, prueba o análisis crítico fallido bloquea la integración y cualquier despliegue.
- **CA-07:** Las pruebas no dependen de datos personales, servicios cloud reales ni orden de ejecución.
- **CA-08:** El resultado del pipeline identifica con claridad la etapa y prueba que falló.

## HT-SEC-001 — Proteger la configuración y el tráfico de producción

| Campo | Valor |
|---|---|
| Prioridad | Must |
| Release | Release nube |
| Responsable | Seguridad y plataforma |
| Estado | Propuesta |

> Como responsable de seguridad, quiero que identidades, secretos y conexiones
> estén protegidos para reducir el riesgo de acceso o exposición no autorizada.

### Criterios de aceptación

- **CA-01:** Producción utiliza un proveedor de identidad aprobado y valida firma, emisor, audiencia, expiración y roles.
- **CA-02:** Claves JWT, credenciales de base de datos y broker se almacenan fuera del repositorio y de las imágenes.
- **CA-03:** Todo tráfico público usa HTTPS y las conexiones privadas sensibles usan cifrado en tránsito cuando el servicio lo soporta.
- **CA-04:** RDS, broker, nodos y servicios internos no son accesibles directamente desde internet.
- **CA-05:** Cada servicio usa credenciales y permisos mínimos para su propia base y sus operaciones de mensajería.
- **CA-06:** CORS permite únicamente orígenes explícitos de la aplicación.
- **CA-07:** Usuarios y contraseñas de demostración no existen en producción.
- **CA-08:** Swagger y OpenAPI permanecen deshabilitados en producción salvo habilitación deliberada y protegida.
- **CA-09:** Dependencias e imágenes se analizan para detectar vulnerabilidades críticas antes de desplegar.

### Dependencias y notas

- Depende de DEC-001 y DEC-013.

## HT-DEP-001 — Desplegar ServiceFlow en AWS

| Campo | Valor |
|---|---|
| Prioridad | Must |
| Release | Release nube |
| Responsable | Equipo de plataforma |
| Estado | Propuesta |

> Como operador de plataforma, quiero aprovisionar y desplegar ServiceFlow de forma
> repetible en AWS para operar una release trazable y recuperable.

### Criterios de aceptación

- **CA-01:** La infraestructura se define como código, se revisa y puede recrearse sin pasos manuales no documentados.
- **CA-02:** El resultado estático de React se publica en un bucket S3 privado y se distribuye mediante CloudFront.
- **CA-03:** Las imágenes versionadas de Request Service y Notification Service se publican en ECR y se ejecutan como workloads independientes en EKS.
- **CA-04:** RDS SQL Server permanece privado y contiene bases lógicas separadas con credenciales distintas para cada microservicio.
- **CA-05:** El broker es privado y la implementación coincide con la decisión DEC-011.
- **CA-06:** CloudFront entrega la SPA y enruta APIs y WebSocket hacia un ALB interno sin almacenar respuestas privadas en caché.
- **CA-07:** Los secretos se obtienen desde AWS Secrets Manager mediante identidades de carga, sin credenciales AWS permanentes en CI.
- **CA-08:** Las migraciones se ejecutan como una etapa o Job controlado antes del rollout.
- **CA-09:** Liveness y readiness gobiernan actualizaciones graduales; una release fallida puede revertirse a una versión anterior identificable.
- **CA-10:** Logs y métricas se centralizan en CloudWatch y existen alarmas para indisponibilidad y DLQ.
- **CA-11:** El pipeline etiqueta artefactos con un identificador inmutable del commit y conserva evidencia de pruebas y despliegue.
- **CA-12:** La estrategia de réplicas de SignalR define afinidad y backplane antes de escalar Notification Service horizontalmente.

### Dependencias y notas

- Requiere DEC-011, DEC-012 y DEC-013.
