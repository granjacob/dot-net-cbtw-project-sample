# EP-02 — Gestión de solicitudes

[Volver al índice](README.md)

## Objetivo de la épica

Registrar las necesidades internas y permitir que cada usuario encuentre y
consulte los casos incluidos en su alcance.

## HU-SOL-001 — Crear una solicitud

| Campo | Valor |
|---|---|
| Prioridad | Must |
| Release | MVP funcional |
| Actor | Usuario autenticado |
| Estado | Propuesta |

> Como usuario autenticado, quiero crear una solicitud con la información de mi
> necesidad para que el equipo responsable pueda atenderla.

### Criterios de aceptación

- **CA-01:** El formulario solicita título, descripción, categoría y prioridad.
- **CA-02:** El título, después de eliminar espacios externos, contiene entre 5 y 160 caracteres.
- **CA-03:** La descripción, después de eliminar espacios externos, contiene entre 20 y 4.000 caracteres.
- **CA-04:** La categoría pertenece al catálogo inicial y la prioridad es Baja, Media, Alta o Crítica.
- **CA-05:** Dado un formulario inválido, cuando se intenta crear la solicitud, entonces se muestran errores por campo y no se persiste información.
- **CA-06:** Dada una solicitud válida, cuando se confirma la creación, entonces queda en estado Abierta, sin responsable, con creador y fechas obtenidos por el servidor.
- **CA-07:** El sistema calcula el vencimiento según la prioridad y devuelve un identificador único de la solicitud.
- **CA-08:** La solicitud y el evento de creación quedan confirmados de manera atómica.
- **CA-09:** Después de crearla, el usuario puede abrir su detalle y no se genera un duplicado por pulsaciones repetidas mientras la operación está en curso.

### Reglas relacionadas

- Categorías y prioridades: sección 7 del [índice](README.md).
- Cálculo del vencimiento: HU-SLA-001.
- Confiabilidad del evento: HT-EVT-001.

## HU-SOL-002 — Consultar el listado de solicitudes

| Campo | Valor |
|---|---|
| Prioridad | Must |
| Release | MVP funcional |
| Actor | Usuario autenticado |
| Estado | Propuesta |

> Como usuario autenticado, quiero consultar las solicitudes de mi alcance para
> conocer el trabajo registrado y su situación actual.

### Criterios de aceptación

- **CA-01:** Un empleado visualiza solo sus solicitudes; un agente o administrador visualiza todas.
- **CA-02:** Cada fila muestra como mínimo identificador, título, categoría, prioridad, estado, responsable y última actualización.
- **CA-03:** El resultado se pagina y muestra total de elementos, página actual y total de páginas.
- **CA-04:** El tamaño de página aceptado está entre 1 y 100; el valor inicial de la interfaz es 10.
- **CA-05:** Si no existen resultados, se presenta un estado vacío con una acción útil para crear una solicitud o limpiar filtros.
- **CA-06:** Si la consulta falla, se informa el problema y se permite reintentar sin perder los filtros.
- **CA-07:** El servidor aplica siempre el alcance autorizado, independientemente de los parámetros enviados por el cliente.

## HU-SOL-003 — Buscar, filtrar y ordenar solicitudes

| Campo | Valor |
|---|---|
| Prioridad | Must |
| Release | MVP funcional |
| Actor | Usuario autenticado |
| Estado | Propuesta |

> Como usuario autenticado, quiero buscar, filtrar y ordenar solicitudes para
> localizar rápidamente los casos que necesito atender o revisar.

### Criterios de aceptación

- **CA-01:** La búsqueda encuentra coincidencias autorizadas por título, descripción o identificador visible.
- **CA-02:** Se puede filtrar por estado, prioridad y categoría.
- **CA-03:** La API permite además filtrar por creador y responsable dentro del alcance del rol.
- **CA-04:** Se puede ordenar ascendente o descendentemente por creación, actualización, vencimiento, título, prioridad o estado.
- **CA-05:** Cambiar una búsqueda, filtro u orden reinicia la navegación en la primera página.
- **CA-06:** Limpiar filtros restaura la consulta inicial.
- **CA-07:** Un campo de orden, dirección o valor de filtro no soportado produce un error de validación y nunca una falla interna.
- **CA-08:** La búsqueda escrita rápidamente no genera una petición por cada pulsación; la interfaz aplica una espera breve antes de consultar.

### Dependencias y notas

- El acceso directo “Asignadas a mí” se decide en DEC-007.

## HU-SOL-004 — Consultar el detalle de una solicitud

| Campo | Valor |
|---|---|
| Prioridad | Must |
| Release | MVP funcional |
| Actor | Usuario con acceso a la solicitud |
| Estado | Propuesta |

> Como usuario relacionado con una solicitud, quiero consultar todo su detalle
> para comprender la necesidad, el responsable y su evolución.

### Criterios de aceptación

- **CA-01:** El detalle presenta identificador, título, descripción, categoría, prioridad, estado, creador, responsable, creación, actualización y vencimiento.
- **CA-02:** También presenta los comentarios y el historial de estados que el usuario está autorizado a consultar.
- **CA-03:** Una solicitud inexistente produce un resultado “no encontrada” comprensible.
- **CA-04:** Una solicitud existente pero fuera del alcance del empleado no expone ninguno de sus datos.
- **CA-05:** Las acciones de edición, asignación y cambio de estado se muestran únicamente a agentes y administradores.
- **CA-06:** El usuario puede regresar al listado conservando una experiencia de navegación coherente.
- **CA-07:** Si el detalle cambia mediante un evento autorizado, la vista se actualiza sin recargar la página.

## HU-SOL-005 — Actualizar la información de una solicitud

| Campo | Valor |
|---|---|
| Prioridad | Must |
| Release | MVP funcional |
| Actor | Agente o administrador |
| Estado | Propuesta |

> Como agente, quiero corregir el título, la descripción, la categoría o la
> prioridad de una solicitud para mantener vigente la información de atención.

### Criterios de aceptación

- **CA-01:** Solo un agente o administrador puede ejecutar la actualización.
- **CA-02:** Se aplican las mismas validaciones de título, descripción, categoría y prioridad utilizadas durante la creación.
- **CA-03:** Si cambia la prioridad, el vencimiento se recalcula desde la fecha de creación original con la estrategia de la nueva prioridad.
- **CA-04:** Una solicitud cerrada no puede editarse.
- **CA-05:** Una actualización válida modifica la fecha de última actualización y conserva creador y fecha de creación.
- **CA-06:** La actualización y su evento quedan confirmados de manera atómica.
- **CA-07:** Los usuarios autorizados reciben el cambio sin tener que recargar.
- **CA-08:** Una validación o conflicto no produce cambios parciales.

### Dependencias y notas

- El comportamiento del SLA al cambiar la prioridad debe ratificarse en DEC-005.
- La estrategia ante modificaciones concurrentes se define en DEC-009.

