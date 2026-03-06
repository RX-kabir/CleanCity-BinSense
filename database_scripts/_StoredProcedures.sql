SET NOCOUNT ON;

------------------------------------------------------------
-- A) Plan Route for a Zone
-- Creates Routes + RouteStops, returns RouteId
------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_PlanRouteForZone
    @ZoneId INT,
    @RouteDate DATE,
    @TruckId INT,
    @DriverId INT,
    @MinFillPercent INT = 70
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @RouteId INT;

    BEGIN TRAN;

    INSERT INTO dbo.Routes(TruckId, DriverId, RouteDate, RouteStatusId)
    VALUES (@TruckId, @DriverId, @RouteDate, 1); -- Planned

    SET @RouteId = SCOPE_IDENTITY();

    ;WITH candidates AS
    (
        SELECT
            b.BinId,
            b.Location,
            lr.FillLevelPercent,
            b.BinStatusId
        FROM dbo.Bins b
        OUTER APPLY (
            SELECT TOP 1 FillLevelPercent
            FROM dbo.BinReadings r
            WHERE r.BinId = b.BinId
            ORDER BY r.ReadingTime DESC, r.ReadingId DESC
        ) lr
        WHERE b.ZoneId = @ZoneId
          AND (
                ISNULL(lr.FillLevelPercent, 0) >= @MinFillPercent
                OR b.BinStatusId IN (2,3)
              )
    )
    INSERT INTO dbo.RouteStops(RouteId, BinId, StopOrder, PlannedTime)
    SELECT
        @RouteId,
        c.BinId,
        ROW_NUMBER() OVER(ORDER BY c.BinId) AS StopOrder,
        CAST(@RouteDate AS DATETIME2(3)) AS PlannedTime
    FROM candidates c;

    COMMIT;

    SELECT @RouteId AS RouteId;
END
GO

------------------------------------------------------------
-- B) Start Route (Planned -> InProgress)
------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_StartRoute
    @RouteId INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Routes
    SET RouteStatusId = 2,
        StartedAt = COALESCE(StartedAt, SYSUTCDATETIME())
    WHERE RouteId = @RouteId;
END
GO

------------------------------------------------------------
-- C) Complete Route (InProgress -> Completed)
------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_CompleteRoute
    @RouteId INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Routes
    SET RouteStatusId = 3,
        CompletedAt = COALESCE(CompletedAt, SYSUTCDATETIME())
    WHERE RouteId = @RouteId;
END
GO

------------------------------------------------------------
-- D) Route map data (for Leaflet)
------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_GetRouteMapData
    @RouteId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        s.RouteStopId,
        s.RouteId,
        s.StopOrder,
        s.PlannedTime,
        s.ActualTime,
        s.CollectedVolumeLiters,
        b.BinId,
        b.Location,
        b.Latitude,
        b.Longitude,
        b.BinStatusId,
        bs.Name AS StatusName,
        b.CapacityLiters,
        b.WasteType
    FROM dbo.RouteStops s
    JOIN dbo.Bins b ON b.BinId = s.BinId
    JOIN dbo.BinStatus bs ON bs.BinStatusId = b.BinStatusId
    WHERE s.RouteId = @RouteId
    ORDER BY s.StopOrder;
END
GO

------------------------------------------------------------
-- E) Public nearby bins (radius in KM)
------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_Public_GetBinsNear
    @Latitude  DECIMAL(9,6),
    @Longitude DECIMAL(9,6),
    @RadiusKm  FLOAT = 2.0,
    @MaxResults INT = 200
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @p geography = geography::Point(@Latitude, @Longitude, 4326);

    SELECT TOP (@MaxResults)
        b.BinId,
        b.Location,
        b.Latitude,
        b.Longitude,
        b.WasteType,
        b.CapacityLiters,
        b.BinStatusId,
        bs.Name AS StatusName,
        COALESCE(lr.FillLevelPercent, 0) AS LatestFillLevelPercent,
        lr.ReadingTime AS LastReadingTime,
        CAST(@p.STDistance(geography::Point(b.Latitude, b.Longitude, 4326)) / 1000.0 AS DECIMAL(10,2)) AS DistanceKm
    FROM dbo.Bins b
    JOIN dbo.BinStatus bs ON bs.BinStatusId = b.BinStatusId
    OUTER APPLY (
        SELECT TOP 1 FillLevelPercent, ReadingTime
        FROM dbo.BinReadings
        WHERE BinId = b.BinId
        ORDER BY ReadingTime DESC, ReadingId DESC
    ) lr
    WHERE @p.STDistance(geography::Point(b.Latitude, b.Longitude, 4326)) <= (@RadiusKm * 1000.0)
    ORDER BY DistanceKm ASC;
END
GO

------------------------------------------------------------
-- F) Admin analytics snapshot (KPIs + trend + top lists)
------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_Admin_AnalyticsSnapshot
    @FromDate DATE,
    @ToDate   DATE
AS
BEGIN
    SET NOCOUNT ON;

    -- 1) KPI Cards
    SELECT
        (SELECT COUNT(*) 
         FROM dbo.PickupEvents 
         WHERE PickupTime >= @FromDate AND PickupTime < DATEADD(day,1,@ToDate)) AS TotalPickups,

        (SELECT ISNULL(SUM(VolumeCollectedLiters),0)
         FROM dbo.PickupEvents
         WHERE PickupTime >= @FromDate AND PickupTime < DATEADD(day,1,@ToDate)) AS TotalCollectedLiters,

        (SELECT COUNT(*) 
         FROM dbo.BinReadings
         WHERE IsOverflowAlert = 1 AND ReadingTime >= @FromDate AND ReadingTime < DATEADD(day,1,@ToDate)) AS OverflowAlerts,

        (SELECT COUNT(*)
         FROM dbo.Routes
         WHERE RouteDate >= @FromDate AND RouteDate <= @ToDate) AS TotalRoutes;

    -- 2) Trend
    SELECT
        CAST(PickupTime AS DATE) AS [Day],
        COUNT(*) AS PickupCount,
        ISNULL(SUM(VolumeCollectedLiters),0) AS CollectedLiters
    FROM dbo.PickupEvents
    WHERE PickupTime >= @FromDate AND PickupTime < DATEADD(day,1,@ToDate)
    GROUP BY CAST(PickupTime AS DATE)
    ORDER BY [Day];

    -- 3) Top zones
    SELECT TOP 10
        z.Name AS ZoneName,
        ISNULL(SUM(p.VolumeCollectedLiters),0) AS TotalCollectedLiters,
        COUNT(*) AS PickupCount
    FROM dbo.PickupEvents p
    JOIN dbo.Bins b ON b.BinId = p.BinId
    JOIN dbo.Zones z ON z.ZoneId = b.ZoneId
    WHERE p.PickupTime >= @FromDate AND p.PickupTime < DATEADD(day,1,@ToDate)
    GROUP BY z.Name
    ORDER BY TotalCollectedLiters DESC;

    -- 4) Top overflowing bins
    SELECT TOP 10
        b.BinId,
        b.Location,
        z.Name AS ZoneName,
        COUNT(*) AS OverflowCount
    FROM dbo.BinReadings r
    JOIN dbo.Bins b ON b.BinId = r.BinId
    JOIN dbo.Zones z ON z.ZoneId = b.ZoneId
    WHERE r.IsOverflowAlert = 1
      AND r.ReadingTime >= @FromDate AND r.ReadingTime < DATEADD(day,1,@ToDate)
    GROUP BY b.BinId, b.Location, z.Name
    ORDER BY OverflowCount DESC;

    -- 5) Truck utilization
    SELECT
        t.RegistrationNo,
        COUNT(DISTINCT r.RouteId) AS Routes,
        COUNT(s.RouteStopId) AS TotalStops,
        SUM(CASE WHEN s.ActualTime IS NOT NULL THEN 1 ELSE 0 END) AS CompletedStops
    FROM dbo.Routes r
    JOIN dbo.Trucks t ON t.TruckId = r.TruckId
    LEFT JOIN dbo.RouteStops s ON s.RouteId = r.RouteId
    WHERE r.RouteDate >= @FromDate AND r.RouteDate <= @ToDate
    GROUP BY t.RegistrationNo
    ORDER BY Routes DESC, TotalStops DESC;
END
GO
