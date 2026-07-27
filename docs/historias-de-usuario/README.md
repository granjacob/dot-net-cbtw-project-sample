# Backlog inicial de historias de usuario

## 1. Propósito

Este backlog define el alcance inicial de **ServiceFlow**, una plataforma para
registrar, atender y seguir solicitudes internas de una empresa. Está escrito como
si el desarrollo aún no hubiera comenzado y sirve como base para refinamiento,
estimación, planificación de releases y pruebas de aceptación.

Las historias funcionales expresan valor para una persona. Los requisitos de
arquitectura, seguridad, operación y entrega se documentan aparte como
**habilitadores técnicos**.

## 2. Visión del producto

Permitir que un empleado solicite ayuda y conozca el avance de su caso, mientras
los equipos responsables organizan, asignan y atienden el trabajo con trazabilidad,
SLA, notificaciones y actualización en tiempo real.

## 3. Actores

| Actor | Necesidad principal |
|---|---|
| Empleado (`Employee`) | Crear solicitudes y hacer seguimiento únicamente a sus propios casos. |
| Agente (`Agent`) | Consultar la operación completa, asumir o asignar casos y gestionar su ciclo de vida. |
| Administrador (`Administrator`) | Supervisar la operación completa y ejecutar las mismas acciones operativas de un agente. |
| Operador de plataforma | Instalar, desplegar, observar y recuperar la solución. |
| Equipo de desarrollo | Entregar cambios comprobables y mantener los servicios de forma independiente. |

El proveedor de identidad, el broker de mensajes y los microservicios son
participantes del sistema, pero no se consideran personas usuarias.

## 4. Matriz de permisos propuesta

Esta matriz es la línea base para los criterios de aceptación y debe ser ratificada
por el Product Owner antes del desarrollo.

| Capacidad | Empleado | Agente | Administrador |
|---|:---:|:---:|:---:|
| Iniciar y cerrar sesión | Sí | Sí | Sí |
| Crear una solicitud | Sí | Sí | Sí |
| Listar o abrir solicitudes propias | Sí | Sí | Sí |
| Listar o abrir cualquier solicitud | No | Sí | Sí |
| Editar datos, prioridad o categoría | No | Sí | Sí |
| Asignar o retirar un responsable | No | Sí | Sí |
| Cambiar el estado | No | Sí | Sí |
| Comentar una solicitud visible | Sí | Sí | Sí |
| Consultar el historial de una solicitud visible | Sí | Sí | Sí |
| Gestionar sus propias notificaciones | Sí | Sí | Sí |
| Consultar métricas de su alcance | Propias | Globales | Globales |

## 5. Priorización y releases

Se utiliza MoSCoW:

- **Must:** indispensable para que el incremento cumpla su objetivo.
- **Should:** importante, pero el producto conserva valor si se entrega después.
- **Could:** mejora deseable que puede posponerse.
- **Won't now:** fuera del alcance acordado para estas releases.

Los documentos distinguen dos hitos:

| Hito | Resultado esperado |
|---|---|
| MVP funcional | Flujo completo ejecutable localmente con autenticación, solicitudes, notificaciones y tiempo real. |
| Release nube | Operación segura y observable en AWS, con entrega automatizada. |

No se asignan puntos de historia en este documento. La estimación corresponde al
equipo después de refinar dependencias y decisiones pendientes.

## 6. Índice de épicas

| Épica | Documento | Resultado |
|---|---|---|
| EP-01 Identidad y acceso | [01-autenticacion-y-autorizacion.md](01-autenticacion-y-autorizacion.md) | Sesiones seguras, roles y aislamiento de información. |
| EP-02 Gestión de solicitudes | [02-gestion-de-solicitudes.md](02-gestion-de-solicitudes.md) | Creación, consulta, búsqueda y edición controlada. |
| EP-03 Flujo, colaboración y SLA | [03-flujo-colaboracion-y-sla.md](03-flujo-colaboracion-y-sla.md) | Asignación, estados, comentarios, auditoría y vencimientos. |
| EP-04 Notificaciones y tiempo real | [04-notificaciones-y-tiempo-real.md](04-notificaciones-y-tiempo-real.md) | Avisos persistentes y vistas sincronizadas sin recargar. |
| EP-05 Dashboard | [05-dashboard-y-metricas.md](05-dashboard-y-metricas.md) | Resumen operativo según el alcance del usuario. |
| EP-06 Plataforma y entrega | [06-habilitadores-tecnicos.md](06-habilitadores-tecnicos.md) | Arquitectura confiable, Docker, observabilidad, calidad y AWS. |
| Decisiones | [decisiones-pendientes.md](decisiones-pendientes.md) | Preguntas que el negocio o la arquitectura deben resolver. |

El backlog contiene **23 historias funcionales** y **8 habilitadores técnicos**.

## 7. Reglas de negocio iniciales

### Categorías

- Soporte técnico (`TechnicalSupport`).
- Mantenimiento (`Maintenance`).
- Acceso a sistemas (`SystemAccess`).
- Compras y suministros (`Purchasing`).
- Incidente operativo (`OperationalIncident`).

### Prioridades y SLA propuesto

| Prioridad | Vencimiento desde la creación |
|---|---:|
| Baja (`Low`) | 7 días calendario |
| Media (`Medium`) | 3 días calendario |
| Alta (`High`) | 24 horas calendario |
| Crítica (`Critical`) | 4 horas calendario |

Estos valores permiten estimar y probar el MVP. Horarios hábiles, festivos,
pausas y escalamiento requieren una decisión de negocio.

### Estados

`Open`, `InProgress`, `Pending`, `Resolved` y `Closed`. La matriz detallada de
transiciones está en la historia `HU-FLU-002`.

## 8. Alcance excluido inicialmente

- Envío real de correo electrónico.
- Aplicación móvil nativa.
- Archivos adjuntos pesados.
- Multitenancy.
- Administración completa de usuarios y directorios desde ServiceFlow.
- Eliminación de solicitudes, comentarios o historial auditado.
- Automatizaciones avanzadas de escalamiento y calendarios laborales.

## 9. Definition of Ready

Una historia puede entrar a un sprint cuando:

- tiene valor, actor y criterios de aceptación comprensibles;
- sus reglas de negocio y dependencias están identificadas;
- las decisiones que bloquean su implementación están resueltas;
- el equipo puede estimarla y probarla de manera independiente;
- los diseños o contratos necesarios están disponibles.

## 10. Definition of Done

Una historia se considera terminada cuando:

- todos sus criterios de aceptación están demostrados;
- se respetan autorización, aislamiento de datos y validaciones del servidor;
- las pruebas automatizadas relevantes pasan;
- no quedan errores conocidos de severidad alta o crítica;
- logs y errores no exponen credenciales ni datos sensibles;
- la documentación y el contrato API afectados están actualizados;
- el incremento puede ejecutarse mediante el mecanismo de despliegue de su release.

## 11. Convención de los criterios

Los criterios se escriben de manera verificable y, cuando describen un escenario,
utilizan la forma **Dado / Cuando / Entonces**. Cada identificador es estable y
puede utilizarse en casos de prueba, commits o herramientas como Jira:

- `HU-*`: historia con valor funcional.
- `HT-*`: habilitador técnico.
- `CA-*`: criterio de aceptación dentro de una historia.
