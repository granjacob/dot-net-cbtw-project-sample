-- Script opcional para aprovisionamiento manual.
-- Docker Compose no depende de este archivo: cada microservicio crea y administra
-- exclusivamente su propia base mediante Entity Framework Core.

IF DB_ID(N'ServiceFlowRequests') IS NULL
BEGIN
    CREATE DATABASE [ServiceFlowRequests];
END;
GO

IF DB_ID(N'ServiceFlowNotifications') IS NULL
BEGIN
    CREATE DATABASE [ServiceFlowNotifications];
END;
GO
