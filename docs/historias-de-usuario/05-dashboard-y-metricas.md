# EP-05 — Dashboard y métricas

[Volver al índice](README.md)

## Objetivo de la épica

Convertir los datos autorizados de solicitudes en un resumen útil para tomar
decisiones operativas sin reemplazar el listado detallado.

## HU-DAS-001 — Consultar el resumen operativo

| Campo | Valor |
|---|---|
| Prioridad | Must |
| Release | MVP funcional |
| Actor | Usuario autenticado |
| Estado | Propuesta |

> Como usuario autenticado, quiero ver un resumen de las solicitudes de mi alcance
> para comprender rápidamente la situación actual.

### Criterios de aceptación

- **CA-01:** Un empleado ve métricas calculadas únicamente con sus solicitudes; agentes y administradores ven el alcance global.
- **CA-02:** El dashboard muestra como mínimo total activo —Abiertas, Pendientes y En progreso—, solicitudes críticas activas y distribución de todas las solicitudes por estado.
- **CA-03:** Se presenta una lista de solicitudes recientes con acceso a su detalle.
- **CA-04:** Cada cifra se calcula sobre todo el conjunto autorizado y no únicamente sobre la primera página de resultados.
- **CA-05:** La suma de la distribución por estados coincide con el total de solicitudes del alcance autorizado; el total activo se calcula por separado.
- **CA-06:** Un estado sin solicitudes se representa con cero y no produce errores en la gráfica.
- **CA-07:** Los cambios recibidos en tiempo real actualizan o reconcilian las métricas sin recargar.
- **CA-08:** El dashboard informa estados de carga, vacío y error, y permite reintentar.

### Dependencias y notas

- Las fórmulas exactas de “activo”, “abierto” y “crítico” deben ratificarse en DEC-010.

## HU-DAS-002 — Consultar mi carga como agente

| Campo | Valor |
|---|---|
| Prioridad | Should |
| Release | MVP funcional, si hay capacidad |
| Actor | Agente |
| Estado | Propuesta |

> Como agente, quiero ver mis solicitudes asignadas y las que aún no tienen
> responsable para organizar mi jornada de trabajo.

### Criterios de aceptación

- **CA-01:** El resumen diferencia solicitudes asignadas al agente autenticado y solicitudes sin asignar.
- **CA-02:** El agente puede abrir desde el indicador el listado con el filtro equivalente aplicado.
- **CA-03:** Se priorizan visualmente los casos críticos, vencidos o con vencimiento más cercano.
- **CA-04:** Las solicitudes Cerradas no se incluyen en la carga activa.
- **CA-05:** Una reasignación actualiza los conteos del agente anterior y del nuevo responsable.
- **CA-06:** Los conteos provienen del servidor o de una agregación completa y no de una página parcial.

### Dependencias y notas

- Requiere una definición de “Asignadas a mí” y una identidad de responsable resuelta en DEC-003 y DEC-007.

## HU-DAS-003 — Consultar cumplimiento de SLA

| Campo | Valor |
|---|---|
| Prioridad | Could |
| Release | Release nube |
| Actor | Administrador |
| Estado | Propuesta |

> Como administrador, quiero consultar indicadores de cumplimiento de SLA para
> identificar tendencias y oportunidades de mejora del servicio.

### Criterios de aceptación

- **CA-01:** El administrador puede seleccionar un periodo y una zona horaria de reporte.
- **CA-02:** El dashboard muestra cantidad y porcentaje de solicitudes resueltas dentro y fuera del SLA.
- **CA-03:** La fórmula distingue casos activos vencidos de casos terminados fuera de plazo.
- **CA-04:** Los resultados pueden segmentarse al menos por prioridad, categoría y estado.
- **CA-05:** Cada indicador permite navegar al conjunto de solicitudes que lo compone.
- **CA-06:** Los cálculos usan el conjunto completo del periodo y producen el mismo resultado al repetirse con los mismos datos.
- **CA-07:** Cuando no hay datos suficientes se muestra “Sin datos” y no un porcentaje engañoso.

### Dependencias y notas

- Requiere reglas definitivas de SLA en DEC-005 y fórmulas de métricas en DEC-010.
