# Base de datos

ServiceFlow aplica el patrón **database per service** sobre una sola instancia local de SQL Server:

- `ServiceFlowRequests`: propiedad exclusiva de Request Service.
- `ServiceFlowNotifications`: propiedad exclusiva de Notification Service.

Los servicios usan `EnsureCreated` con reintentos al arrancar, por lo que `docker compose up --build` no necesita ejecutar scripts previamente. [`01-create-databases.sql`](./01-create-databases.sql) queda disponible para aprovisionamiento manual o como referencia operativa.

Los datos se conservan en el volumen Docker `serviceflow-sqlserver-data`. Ningún microservicio consulta tablas pertenecientes al otro.
