# EP-01 — Autenticación y autorización

[Volver al índice](README.md)

## Objetivo de la épica

Permitir el acceso autenticado a ServiceFlow y garantizar que cada persona pueda
ver y ejecutar únicamente las acciones correspondientes a su rol y a su alcance
de datos.

## HU-AUT-001 — Iniciar sesión

| Campo | Valor |
|---|---|
| Prioridad | Must |
| Release | MVP funcional |
| Actor | Usuario registrado |
| Estado | Propuesta |

> Como usuario registrado, quiero iniciar sesión con mi correo y contraseña para
> acceder a las funciones que corresponden a mi rol.

### Criterios de aceptación

- **CA-01:** Dado un usuario activo con credenciales válidas, cuando inicia sesión, entonces el sistema crea una sesión con su identificador, nombre, rol y fecha de expiración.
- **CA-02:** Dadas credenciales inválidas, cuando se intenta iniciar sesión, entonces no se crea una sesión y se muestra un mensaje comprensible que no revela si falló el correo o la contraseña.
- **CA-03:** Dado un formulario incompleto o con un correo inválido, cuando se intenta enviarlo, entonces se señalan los campos que deben corregirse.
- **CA-04:** Dado un usuario autenticado, cuando consume una API protegida o abre la conexión en tiempo real, entonces su credencial se transmite mediante el mecanismo autorizado.
- **CA-05:** La identidad y el rol utilizados por el backend provienen del token validado y nunca de campos modificables del request.

### Dependencias y notas

- El proveedor inicial puede emitir JWT local para desarrollo.
- El proveedor de producción se define en DEC-001.

## HU-AUT-002 — Mantener y cerrar la sesión

| Campo | Valor |
|---|---|
| Prioridad | Must |
| Release | MVP funcional |
| Actor | Usuario autenticado |
| Estado | Propuesta |

> Como usuario autenticado, quiero conservar mi sesión mientras sea válida y
> poder cerrarla para impedir que otra persona use mi cuenta.

### Criterios de aceptación

- **CA-01:** Dada una sesión válida, cuando el usuario recarga la aplicación, entonces puede continuar sin volver a autenticarse.
- **CA-02:** Dado que el usuario selecciona “Cerrar sesión”, cuando finaliza la acción, entonces se elimina la credencial local, se cierra la conexión en tiempo real y se descartan los datos privados almacenados en memoria o caché.
- **CA-03:** Dada una sesión inexistente o expirada, cuando el usuario intenta abrir una ruta protegida, entonces se le dirige al inicio de sesión.
- **CA-04:** Dado que una API rechaza una credencial expirada, cuando la aplicación recibe la respuesta, entonces finaliza la sesión y evita ciclos de reintento.
- **CA-05:** Después de cerrar sesión, utilizar el botón “Atrás” del navegador no vuelve a mostrar información privada utilizable.

## HU-AUT-003 — Autorizar acciones según el rol

| Campo | Valor |
|---|---|
| Prioridad | Must |
| Release | MVP funcional |
| Actor | Administrador |
| Estado | Propuesta |

> Como administrador, quiero que las acciones estén restringidas por rol para
> proteger la operación frente a cambios no autorizados.

### Criterios de aceptación

- **CA-01:** Los permisos efectivos coinciden con la matriz definida en el índice del backlog.
- **CA-02:** Dado un empleado, cuando intenta editar, asignar o cambiar el estado de una solicitud, entonces el backend rechaza la operación aunque se invoque la API sin usar la interfaz.
- **CA-03:** Dado un agente o administrador, cuando gestiona una solicitud existente, entonces puede editarla, asignarla y ejecutar una transición válida.
- **CA-04:** La interfaz oculta o deshabilita las acciones no permitidas, pero el backend continúa siendo la fuente de autoridad.
- **CA-05:** Dada una operación autenticada pero no autorizada, cuando el backend la rechaza, entonces devuelve un error consistente y no persiste cambios ni eventos.
- **CA-06:** Los tres roles pueden crear solicitudes durante el MVP, de acuerdo con la línea base propuesta.

### Dependencias y notas

- La diferencia funcional futura entre agente y administrador se define en DEC-002.
- Si el negocio decide que solo el empleado puede crear solicitudes, deben ajustarse la matriz y HU-SOL-001.

## HU-AUT-004 — Aislar la información por usuario

| Campo | Valor |
|---|---|
| Prioridad | Must |
| Release | MVP funcional |
| Actor | Empleado |
| Estado | Propuesta |

> Como empleado, quiero acceder únicamente a mis solicitudes y notificaciones
> para que la información de otras personas permanezca privada.

### Criterios de aceptación

- **CA-01:** Dado un empleado autenticado, cuando lista solicitudes, entonces recibe únicamente aquellas cuyo creador coincide con su identidad validada.
- **CA-02:** Dado un empleado, cuando intenta abrir, comentar o consultar el historial de una solicitud ajena, entonces se rechaza el acceso sin devolver su contenido.
- **CA-03:** Cambiar filtros, parámetros de URL o payloads no permite que un empleado amplíe su alcance.
- **CA-04:** Dado un agente o administrador, cuando consulta solicitudes, entonces obtiene el alcance global permitido para operación.
- **CA-05:** Cada usuario puede consultar y marcar únicamente sus propias notificaciones.
- **CA-06:** Los eventos en tiempo real se entregan solo a los usuarios o grupos autorizados para conocer la solicitud.
- **CA-07:** Los logs de acceso denegado permiten diagnóstico mediante un identificador de correlación y no incluyen credenciales.

