SET NOCOUNT ON;

-- Lookups
IF OBJECT_ID('dbo.BinStatus','U') IS NULL
BEGIN
    CREATE TABLE dbo.BinStatus(
        BinStatusId TINYINT NOT NULL CONSTRAINT PK_BinStatus PRIMARY KEY,
        Name NVARCHAR(50) NOT NULL CONSTRAINT UQ_BinStatus_Name UNIQUE
    );
END
GO

IF OBJECT_ID('dbo.RouteStatus','U') IS NULL
BEGIN
    CREATE TABLE dbo.RouteStatus(
        RouteStatusId TINYINT NOT NULL CONSTRAINT PK_RouteStatus PRIMARY KEY,
        Name NVARCHAR(50) NOT NULL CONSTRAINT UQ_RouteStatus_Name UNIQUE
    );
END
GO

-- Zones
IF OBJECT_ID('dbo.Zones','U') IS NULL
BEGIN
    CREATE TABLE dbo.Zones(
        ZoneId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Zones PRIMARY KEY,
        Name NVARCHAR(120) NOT NULL CONSTRAINT UQ_Zones_Name UNIQUE,
        Description NVARCHAR(400) NULL
    );
END
GO

-- Bins
IF OBJECT_ID('dbo.Bins','U') IS NULL
BEGIN
    CREATE TABLE dbo.Bins(
        BinId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Bins PRIMARY KEY,
        ZoneId INT NOT NULL,
        Location NVARCHAR(200) NOT NULL,
        Latitude DECIMAL(9,6) NOT NULL,
        Longitude DECIMAL(9,6) NOT NULL,
        CapacityLiters INT NOT NULL,
        WasteType NVARCHAR(50) NOT NULL,
        BinStatusId TINYINT NOT NULL,
        CONSTRAINT FK_Bins_Zones FOREIGN KEY (ZoneId) REFERENCES dbo.Zones(ZoneId),
        CONSTRAINT FK_Bins_BinStatus FOREIGN KEY (BinStatusId) REFERENCES dbo.BinStatus(BinStatusId)
    );
END
GO

-- Readings
IF OBJECT_ID('dbo.BinReadings','U') IS NULL
BEGIN
    CREATE TABLE dbo.BinReadings(
        ReadingId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BinReadings PRIMARY KEY,
        BinId INT NOT NULL,
        ReadingTime DATETIME2(3) NOT NULL CONSTRAINT DF_BinReadings_ReadingTime DEFAULT SYSUTCDATETIME(),
        FillLevelPercent INT NOT NULL, -- 0..100
        Temperature INT NULL,
        IsOverflowAlert BIT NOT NULL CONSTRAINT DF_BinReadings_IsOverflow DEFAULT (0),
        CONSTRAINT FK_BinReadings_Bins FOREIGN KEY (BinId) REFERENCES dbo.Bins(BinId)
    );
END
GO

-- Trucks
IF OBJECT_ID('dbo.Trucks','U') IS NULL
BEGIN
    CREATE TABLE dbo.Trucks(
        TruckId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Trucks PRIMARY KEY,
        RegistrationNo NVARCHAR(60) NOT NULL CONSTRAINT UQ_Trucks_Reg UNIQUE,
        CapacityKg INT NOT NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_Trucks_Status DEFAULT ('Active')
    );
END
GO

-- Users (DBMS table; Identity is separate)
IF OBJECT_ID('dbo.Users','U') IS NULL
BEGIN
    CREATE TABLE dbo.Users(
        UserId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
        FullName NVARCHAR(120) NOT NULL,
        Email NVARCHAR(256) NOT NULL CONSTRAINT UQ_Users_Email UNIQUE,
        PasswordHash NVARCHAR(400) NULL,
        CreatedAt DATETIME2(3) NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME(),
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_Users_Status DEFAULT('Active')
    );
END
GO

-- Drivers
IF OBJECT_ID('dbo.Drivers','U') IS NULL
BEGIN
    CREATE TABLE dbo.Drivers(
        DriverId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Drivers PRIMARY KEY,
        UserId INT NOT NULL,
        LicenseNo NVARCHAR(60) NOT NULL,
        Phone NVARCHAR(30) NULL,
        CONSTRAINT FK_Drivers_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId)
    );
END
GO

-- Routes
IF OBJECT_ID('dbo.Routes','U') IS NULL
BEGIN
    CREATE TABLE dbo.Routes(
        RouteId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Routes PRIMARY KEY,
        TruckId INT NOT NULL,
        DriverId INT NOT NULL,
        RouteDate DATE NOT NULL,
        RouteStatusId TINYINT NOT NULL CONSTRAINT DF_Routes_Status DEFAULT (1), -- Planned
        StartedAt DATETIME2(3) NULL,
        CompletedAt DATETIME2(3) NULL,
        CONSTRAINT FK_Routes_Trucks FOREIGN KEY (TruckId) REFERENCES dbo.Trucks(TruckId),
        CONSTRAINT FK_Routes_Drivers FOREIGN KEY (DriverId) REFERENCES dbo.Drivers(DriverId),
        CONSTRAINT FK_Routes_RouteStatus FOREIGN KEY (RouteStatusId) REFERENCES dbo.RouteStatus(RouteStatusId)
    );
END
GO

-- RouteStops
IF OBJECT_ID('dbo.RouteStops','U') IS NULL
BEGIN
    CREATE TABLE dbo.RouteStops(
        RouteStopId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RouteStops PRIMARY KEY,
        RouteId INT NOT NULL,
        BinId INT NOT NULL,
        StopOrder INT NOT NULL,
        PlannedTime DATETIME2(3) NULL,
        ActualTime DATETIME2(3) NULL,
        CollectedVolumeLiters DECIMAL(10,2) NULL,
        CONSTRAINT FK_RouteStops_Routes FOREIGN KEY (RouteId) REFERENCES dbo.Routes(RouteId),
        CONSTRAINT FK_RouteStops_Bins FOREIGN KEY (BinId) REFERENCES dbo.Bins(BinId),
        CONSTRAINT UQ_RouteStops_Route_StopOrder UNIQUE(RouteId, StopOrder)
    );
END
GO

-- PickupEvents
IF OBJECT_ID('dbo.PickupEvents','U') IS NULL
BEGIN
    CREATE TABLE dbo.PickupEvents(
        PickupId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PickupEvents PRIMARY KEY,
        BinId INT NOT NULL,
        TruckId INT NOT NULL,
        DriverId INT NOT NULL,
        PickupTime DATETIME2(3) NOT NULL CONSTRAINT DF_PickupEvents_Time DEFAULT SYSUTCDATETIME(),
        VolumeCollectedLiters DECIMAL(10,2) NOT NULL,
        CONSTRAINT FK_PickupEvents_Bins FOREIGN KEY (BinId) REFERENCES dbo.Bins(BinId),
        CONSTRAINT FK_PickupEvents_Trucks FOREIGN KEY (TruckId) REFERENCES dbo.Trucks(TruckId),
        CONSTRAINT FK_PickupEvents_Drivers FOREIGN KEY (DriverId) REFERENCES dbo.Drivers(DriverId)
    );
END
GO

-- MaintenanceRecords
IF OBJECT_ID('dbo.MaintenanceRecords','U') IS NULL
BEGIN
    CREATE TABLE dbo.MaintenanceRecords(
        MaintenanceId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Maintenance PRIMARY KEY,
        TruckId INT NOT NULL,
        StartDate DATE NOT NULL,
        EndDate DATE NULL,
        Description NVARCHAR(400) NULL,
        Cost DECIMAL(12,2) NULL,
        CONSTRAINT FK_Maintenance_Trucks FOREIGN KEY (TruckId) REFERENCES dbo.Trucks(TruckId)
    );
END
GO

-- AuditLogs
IF OBJECT_ID('dbo.AuditLogs','U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLogs(
        AuditId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AuditLogs PRIMARY KEY,
        TableName NVARCHAR(120) NOT NULL,
        RecordId NVARCHAR(60) NOT NULL,
        OperationType NVARCHAR(20) NOT NULL, -- INSERT/UPDATE/DELETE
        ChangedAt DATETIME2(3) NOT NULL CONSTRAINT DF_AuditLogs_ChangedAt DEFAULT SYSUTCDATETIME(),
        ChangedByUserId INT NULL,
        OldValue NVARCHAR(MAX) NULL,
        NewValue NVARCHAR(MAX) NULL
    );
END
GO
