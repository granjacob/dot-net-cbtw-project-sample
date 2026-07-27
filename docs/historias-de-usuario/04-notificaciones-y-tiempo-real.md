# EP-04 — Notificaciones y tiempo real

[Volver al índice](README.md)

## Objetivo de la épica

Informar cambios relevantes de manera persistente y mantener sincronizadas las
vistas abiertas, incluso cuando distintas personas trabajan al mismo tiempo.

## HU-NOT-001 — Consultar mis notificaciones

| Campo | Valor |
|---|---|
| Prioridad | Must |
| Release | MVP funcional |
| Actor | Usuario autenticado |
| Estado | Propuesta |

> Como usuario autenticado, quiero consultar mis notificaciones para conocer los
> cambios relevantes que ocurrieron en mis solicitudes.

### Criterios de aceptación

- **CA-01:** El centro muestra únicamente notificaciones cuyo propietario coincide con el usuario autenticado.
- **CA-02:** Cada notificación presenta tipo, título, mensaje, fecha, estado de lectura y referencia a la solicitud cuando corresponda.
- **CA-03:** Las notificaciones se ordenan de la más reciente a la más antigua y se consultan de forma paginada.
- **CA-04:** El tamaño de página permitido está entre 1 y 100.
- **CA-05:** Desde una notificación asociada, el usuario puede abrir la solicitud si conserva autorización para verla.
- **CA-06:** El sistema puede registrar avisos para creación, edición, asignación, cambio de estado y comentario.
- **CA-07:** Un usuario no puede obtener una notificación ajena modificando URL, identificador o parámetros.

### Dependencias y notas

- La audiencia persistente inicial es el creador de la solicitud.
- Los destinatarios adicionales se deciden en DEC-006.

## HU-NOT-002 — Identificar notificaciones no leídas

| Campo | Valor |
|---|---|
| Prioridad | Must |
| Release | MVP funcional |
| Actor | Usuario autenticado |
| Estado | Propuesta |

> Como usuario autenticado, quiero ver cuántas notificaciones no he leído y poder
> filtrarlas para atender primero las novedades.

### Criterios de aceptación

- **CA-01:** La navegación muestra un contador con el total de notificaciones no leídas del usuario.
- **CA-02:** El contador se limita visualmente a “99+” sin alterar el total real.
- **CA-03:** El centro permite alternar entre todas y solo las no leídas.
- **CA-04:** Una notificación nueva actualiza la lista y el contador sin recargar.
- **CA-05:** Si la conexión en tiempo real no está disponible, el contador se reconcilia periódicamente con el servidor.
- **CA-06:** El contador nunca incluye notificaciones de otro usuario.

## HU-NOT-003 — Marcar notificaciones como leídas

| Campo | Valor |
|---|---|
| Prioridad | Must |
| Release | MVP funcional |
| Actor | Usuario autenticado |
| Estado | Propuesta |

> Como usuario autenticado, quiero marcar una o todas mis notificaciones como
> leídas para mantener organizado mi centro de novedades.

### Criterios de aceptación

- **CA-01:** El usuario puede marcar individualmente una notificación propia como leída.
- **CA-02:** El usuario puede marcar todas sus notificaciones como leídas mediante una sola acción confirmada.
- **CA-03:** Marcar una notificación ya leída no genera un error ni efectos duplicados.
- **CA-04:** Después de la operación se actualizan el estilo, el filtro y el contador.
- **CA-05:** Un identificador inexistente o ajeno no modifica información de otros usuarios.
- **CA-06:** “Marcar todas” solo afecta al usuario autenticado y devuelve la cantidad modificada.

## HU-RT-001 — Recibir cambios relevantes en tiempo real

| Campo | Valor |
|---|---|
| Prioridad | Must |
| Release | MVP funcional |
| Actor | Usuario autenticado |
| Estado | Propuesta |

> Como usuario autenticado, quiero que las vistas reflejen cambios relevantes sin
> recargar la página para trabajar con información actualizada.

### Criterios de aceptación

- **CA-01:** La aplicación establece una conexión autenticada de tiempo real después de iniciar sesión.
- **CA-02:** Los eventos de creación, edición, asignación, estado y comentario actualizan listado, detalle, historial, dashboard o notificaciones según corresponda.
- **CA-03:** El empleado recibe únicamente eventos de sus solicitudes.
- **CA-04:** Agentes y administradores reciben eventos operativos de las solicitudes incluidas en su alcance global.
- **CA-05:** Recibir un evento invalida o actualiza el estado afectado sin duplicar filas, comentarios ni notificaciones.
- **CA-06:** La interfaz comunica discretamente que recibió una actualización y sigue siendo utilizable.
- **CA-07:** Los grupos de tiempo real se derivan de la identidad y del rol validados; el cliente no puede suscribirse arbitrariamente a otro usuario.
- **CA-08:** La indisponibilidad del canal en tiempo real no impide crear o modificar solicitudes mediante HTTP.

### Dependencias y notas

- Recibir un evento operativo no implica necesariamente crear una notificación persistente para todos sus receptores.
- La audiencia exacta por evento se ratifica en DEC-006.

## HU-RT-002 — Recuperar la sincronización después de una interrupción

| Campo | Valor |
|---|---|
| Prioridad | Must |
| Release | MVP funcional |
| Actor | Usuario autenticado |
| Estado | Propuesta |

> Como usuario autenticado, quiero que la aplicación recupere la conexión y los
> cambios perdidos después de una interrupción para no trabajar con datos obsoletos.

### Criterios de aceptación

- **CA-01:** La interfaz indica los estados Conectando, En tiempo real y Reconectando de forma comprensible.
- **CA-02:** Ante una desconexión transitoria, la aplicación intenta reconectarse automáticamente con esperas progresivas.
- **CA-03:** Después de reconectar, la aplicación vuelve a consultar solicitudes, detalle, historial, notificaciones y contador relevantes.
- **CA-04:** Los eventos repetidos o la reconciliación posterior no generan efectos visuales duplicados.
- **CA-05:** Cada montaje del cliente registra una sola suscripción por evento y la elimina al cerrar sesión o desmontarse.
- **CA-06:** Si no logra reconectarse, las operaciones HTTP continúan disponibles y las vistas ofrecen reintento o actualización.
- **CA-07:** Una sesión expirada durante la reconexión conduce al inicio de sesión en lugar de crear un ciclo infinito.

