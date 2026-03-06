SET NOCOUNT ON;

-- Bins: zone filter
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Bins_ZoneId' AND object_id=OBJECT_ID('dbo.Bins'))
    CREATE INDEX IX_Bins_ZoneId ON dbo.Bins(ZoneId);

-- Bins: status filter
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Bins_BinStatusId' AND object_id=OBJECT_ID('dbo.Bins'))
    CREATE INDEX IX_Bins_BinStatusId ON dbo.Bins(BinStatusId);

-- Readings: latest reading per bin
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_BinReadings_BinId_ReadingTime' AND object_id=OBJECT_ID('dbo.BinReadings'))
    CREATE INDEX IX_BinReadings_BinId_ReadingTime ON dbo.BinReadings(BinId, ReadingTime DESC);

-- Routes: date filtering
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Routes_RouteDate' AND object_id=OBJECT_ID('dbo.Routes'))
    CREATE INDEX IX_Routes_RouteDate ON dbo.Routes(RouteDate);

-- RouteStops: route + order
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_RouteStops_RouteId_StopOrder' AND object_id=OBJECT_ID('dbo.RouteStops'))
    CREATE INDEX IX_RouteStops_RouteId_StopOrder ON dbo.RouteStops(RouteId, StopOrder);

-- PickupEvents: time filtering
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_PickupEvents_PickupTime' AND object_id=OBJECT_ID('dbo.PickupEvents'))
    CREATE INDEX IX_PickupEvents_PickupTime ON dbo.PickupEvents(PickupTime);
GO
