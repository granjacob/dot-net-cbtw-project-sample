# EP-03 — Flujo, colaboración y SLA

[Volver al índice](README.md)

## Objetivo de la épica

Coordinar la atención de una solicitud mediante responsables, estados,
comentarios, trazabilidad y una fecha objetivo calculada por prioridad.

## HU-FLU-001 — Asignar un responsable

| Campo | Valor |
|---|---|
| Prioridad | Must |
| Release | MVP funcional |
| Actor | Agente o administrador |
| Estado | Propuesta |

> Como agente, quiero asignar, reasignar o retirar el responsable de una solicitud
> para que la propiedad operativa del caso sea clara.

### Criterios de aceptación

- **CA-01:** Solo un agente o administrador puede modificar la asignación.
- **CA-02:** Dada una solicitud abierta, pendiente, en progreso o resuelta, cuando se elige un responsable válido, entonces el detalle y el listado muestran la nueva asignación.
- **CA-03:** Reasignar conserva la solicitud y reemplaza únicamente al responsable actual.
- **CA-04:** Retirar el responsable deja la solicitud explícitamente “Sin asignar”.
- **CA-05:** Una solicitud cerrada no puede asignarse, reasignarse ni desasignarse.
- **CA-06:** Una asignación válida actualiza la fecha de modificación y genera un evento con responsable anterior, nuevo responsable y actor.
- **CA-07:** El cambio de asignación y su evento se guardan de manera atómica.
- **CA-08:** Los usuarios autorizados ven el cambio sin recargar la página.

### Dependencias y notas

- La fuente de agentes elegibles y la validación del responsable se resuelven en DEC-003.
- Para el MVP puede utilizarse un identificador de usuario de máximo 256 caracteres mientras no exista directorio.

## HU-FLU-002 — Cambiar el estado de una solicitud

| Campo | Valor |
|---|---|
| Prioridad | Must |
| Release | MVP funcional |
| Actor | Agente o administrador |
| Estado | Propuesta |

> Como agente, quiero cambiar el estado de una solicitud para comunicar su avance
> y mantener un flujo de atención consistente.

### Criterios de aceptación

- **CA-01:** Toda solicitud se crea en estado Abierta.
- **CA-02:** Solo un agente o administrador puede cambiar el estado.
- **CA-03:** La interfaz ofrece únicamente los destinos permitidos desde el estado actual.
- **CA-04:** El backend rechaza transiciones no permitidas o al mismo estado, aunque se invoquen fuera de la interfaz.
- **CA-05:** Una transición válida actualiza estado y fecha de modificación, agrega el historial y genera el evento correspondiente en una misma transacción.
- **CA-06:** La solicitud Cerrada es terminal: no permite nuevos cambios de estado, edición ni asignación.
- **CA-07:** Ante una transición inválida no se modifica la solicitud, el historial ni el Outbox.

### Matriz de transiciones propuesta

| Estado actual | Estados permitidos |
|---|---|
| Abierta (Open) | Pendiente, En progreso, Cerrada |
| Pendiente (Pending) | Abierta, En progreso, Cerrada |
| En progreso (InProgress) | Pendiente, Resuelta, Cerrada |
| Resuelta (Resolved) | En progreso, Cerrada |
| Cerrada (Closed) | Ninguno |

## HU-COM-001 — Agregar y consultar comentarios

| Campo | Valor |
|---|---|
| Prioridad | Must |
| Release | MVP funcional |
| Actor | Usuario con acceso a la solicitud |
| Estado | Propuesta |

> Como usuario relacionado con una solicitud, quiero agregar comentarios para
> compartir contexto y dejar constancia de la conversación.

### Criterios de aceptación

- **CA-01:** Un empleado comenta únicamente sus solicitudes; agentes y administradores pueden comentar cualquier solicitud visible para ellos.
- **CA-02:** El comentario es obligatorio, se normalizan sus espacios externos y su contenido no supera 2.000 caracteres.
- **CA-03:** El autor y la fecha se obtienen en el servidor a partir de la sesión y del reloj confiable.
- **CA-04:** Un comentario guardado aparece con autor, contenido y fecha en orden cronológico.
- **CA-05:** Los comentarios son inmutables durante el MVP: no se editan ni eliminan.
- **CA-06:** El comentario y su evento se guardan de manera atómica.
- **CA-07:** Los demás usuarios autorizados ven el comentario sin recargar.
- **CA-08:** Como línea base del MVP, una solicitud cerrada admite comentarios adicionales, aunque sus datos, asignación y estado estén bloqueados.

### Dependencias y notas

- El negocio debe ratificar el uso de comentarios en solicitudes cerradas en DEC-004.

## HU-AUD-001 — Consultar el historial de estados

| Campo | Valor |
|---|---|
| Prioridad | Must |
| Release | MVP funcional |
| Actor | Usuario con acceso a la solicitud |
| Estado | Propuesta |

> Como usuario relacionado con una solicitud, quiero consultar su historial de
> estados para saber cómo evolucionó y quién realizó cada cambio.

### Criterios de aceptación

- **CA-01:** La creación se presenta como “Creada como Abierta”, con actor y fecha.
- **CA-02:** Cada transición posterior muestra estado anterior, estado nuevo, usuario que la ejecutó y fecha.
- **CA-03:** El historial se presenta del evento más reciente al más antiguo.
- **CA-04:** Solo puede consultarlo una persona autorizada para ver la solicitud.
- **CA-05:** Los registros históricos no pueden modificarse ni eliminarse mediante las APIs funcionales.
- **CA-06:** Una transición fallida no deja registros de historial.

### Dependencias y notas

- El MVP audita estados. La ampliación a edición, asignación y comentarios se decide en DEC-008.

## HU-SLA-001 — Calcular y mostrar el vencimiento

| Campo | Valor |
|---|---|
| Prioridad | Must |
| Release | MVP funcional |
| Actor | Agente |
| Estado | Propuesta |

> Como agente, quiero conocer la fecha límite de cada solicitud para priorizar el
> trabajo según el compromiso de servicio.

### Criterios de aceptación

- **CA-01:** Al crear una solicitud se calcula una fecha de vencimiento usando la prioridad y los valores definidos en el índice.
- **CA-02:** El cálculo inicial usa tiempo calendario desde la creación y se conserva en UTC; la interfaz lo presenta en la zona horaria del usuario.
- **CA-03:** Al cambiar la prioridad se recalcula el vencimiento desde la fecha de creación original.
- **CA-04:** El detalle muestra fecha límite y tiempo restante o vencido.
- **CA-05:** Una solicitud se considera vencida cuando supera la fecha límite sin estar Resuelta ni Cerrada.
- **CA-06:** El listado puede ordenarse por vencimiento para atender primero los casos más urgentes.
- **CA-07:** Un valor de prioridad no soportado se rechaza y no se asigna un vencimiento arbitrario.

### Dependencias y notas

- Calendario laboral, pausa en estado Pendiente, reapertura y cambio de prioridad se ratifican en DEC-005.

## HU-SLA-002 — Identificar SLA próximos a vencer o incumplidos

| Campo | Valor |
|---|---|
| Prioridad | Should |
| Release | MVP funcional, si hay capacidad |
| Actor | Agente o administrador |
| Estado | Propuesta |

> Como agente, quiero distinguir las solicitudes próximas a vencer o vencidas para
> actuar antes de incumplir el SLA.

### Criterios de aceptación

- **CA-01:** La interfaz diferencia visualmente casos en tiempo, próximos a vencer y vencidos sin depender únicamente del color.
- **CA-02:** El umbral de “próximo a vencer” es una regla de negocio documentada y se aplica de forma consistente.
- **CA-03:** La clasificación utiliza una hora confiable y no la hora manipulable del navegador.
- **CA-04:** Resolver o cerrar una solicitud detiene la indicación activa de vencimiento, sin borrar la fecha límite histórica.
- **CA-05:** El usuario puede localizar los casos vencidos o próximos a vencer dentro de su alcance.

### Dependencias y notas

- Requiere que DEC-005 defina el umbral y la medición de cumplimiento.

