SET NOCOUNT ON;

------------------------------------------------------------
-- CONFIG (change these if you want)
------------------------------------------------------------
DECLARE @TargetBins INT = 120;         -- total demo bins you want
DECLARE @ReadingsPerNewBin INT = 10;   -- readings inserted for bins that have 0 readings
DECLARE @MakeRoutes BIT = 1;           -- 1 = create demo routes using sp_PlanRouteForZone (if exists)

-- Dhaka-ish bounding box (random distribution)
DECLARE @LatMin DECIMAL(9,6) = 23.700000;
DECLARE @LatMax DECIMAL(9,6) = 23.900000;
DECLARE @LngMin DECIMAL(9,6) = 90.350000;
DECLARE @LngMax DECIMAL(9,6) = 90.500000;

------------------------------------------------------------
-- 1) Ensure lookup rows exist (BinStatus + RouteStatus)
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.BinStatus)
BEGIN
    INSERT INTO dbo.BinStatus (BinStatusId, Name) VALUES
    (1, 'OK'), (2, 'NeedsPickup'), (3, 'Overflowing');
END

IF NOT EXISTS (SELECT 1 FROM dbo.RouteStatus)
BEGIN
    INSERT INTO dbo.RouteStatus (RouteStatusId, Name) VALUES
    (1, 'Planned'), (2, 'InProgress'), (3, 'Completed'), (4, 'Cancelled');
END

PRINT 'Lookups OK';

------------------------------------------------------------
-- 2) Ensure Zones exist
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Zones WHERE Name='Zone A')
    INSERT INTO dbo.Zones(Name, Description) VALUES ('Zone A', 'Demo zone A');

IF NOT EXISTS (SELECT 1 FROM dbo.Zones WHERE Name='Zone B')
    INSERT INTO dbo.Zones(Name, Description) VALUES ('Zone B', 'Demo zone B');

IF NOT EXISTS (SELECT 1 FROM dbo.Zones WHERE Name='Zone C')
    INSERT INTO dbo.Zones(Name, Description) VALUES ('Zone C', 'Demo zone C');

IF NOT EXISTS (SELECT 1 FROM dbo.Zones WHERE Name='Zone D')
    INSERT INTO dbo.Zones(Name, Description) VALUES ('Zone D', 'Demo zone D');

PRINT 'Zones OK';

------------------------------------------------------------
-- 3) Ensure at least 2 trucks exist
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Trucks)
BEGIN
    INSERT INTO dbo.Trucks (RegistrationNo, CapacityKg, Status)
    VALUES ('DHAKA-TRUCK-01', 5000, 'Active'),
           ('DHAKA-TRUCK-02', 4500, 'Active');
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.Trucks WHERE RegistrationNo='DHAKA-TRUCK-01')
        INSERT INTO dbo.Trucks (RegistrationNo, CapacityKg, Status) VALUES ('DHAKA-TRUCK-01', 5000, 'Active');

    IF NOT EXISTS (SELECT 1 FROM dbo.Trucks WHERE RegistrationNo='DHAKA-TRUCK-02')
        INSERT INTO dbo.Trucks (RegistrationNo, CapacityKg, Status) VALUES ('DHAKA-TRUCK-02', 4500, 'Active');
END

PRINT 'Trucks OK';

------------------------------------------------------------
-- 4) Ensure at least 1 driver exists (dbo.Users + dbo.Drivers)
-- NOTE: This is your DBMS Users table (not ASP.NET Identity tables).
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email='driver.demo@smartwaste.local')
BEGIN
    INSERT INTO dbo.Users (FullName, Email, PasswordHash, Status)
    VALUES ('Demo Driver', 'driver.demo@smartwaste.local', NULL, 'Active');
END

DECLARE @DemoUserId INT = (SELECT TOP 1 UserId FROM dbo.Users WHERE Email='driver.demo@smartwaste.local');

IF NOT EXISTS (SELECT 1 FROM dbo.Drivers WHERE UserId=@DemoUserId)
BEGIN
    INSERT INTO dbo.Drivers (UserId, LicenseNo, Phone)
    VALUES (@DemoUserId, 'LIC-DEMO-001', '01700000000');
END

PRINT 'Driver OK';

------------------------------------------------------------
-- 5) Insert demo bins up to @TargetBins
------------------------------------------------------------
DECLARE @ExistingDemoBins INT = (SELECT COUNT(*) FROM dbo.Bins WHERE Location LIKE 'Demo Bin %');
DECLARE @ToInsert INT = @TargetBins - @ExistingDemoBins;

IF @ToInsert > 0
BEGIN
    ;WITH nums AS (
        SELECT TOP (@ToInsert) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
        FROM sys.objects a CROSS JOIN sys.objects b
    ),
    zones AS (
        SELECT ZoneId, Name, ROW_NUMBER() OVER (ORDER BY ZoneId) AS rn
        FROM dbo.Zones
        WHERE Name IN ('Zone A','Zone B','Zone C','Zone D')
    ),
    zcount AS (SELECT COUNT(*) AS cnt FROM zones)
    INSERT INTO dbo.Bins (ZoneId, Location, Latitude, Longitude, CapacityLiters, WasteType, BinStatusId)
    SELECT
        z.ZoneId,
        CONCAT('Demo Bin ', RIGHT(CONCAT('0000', nums.n + @ExistingDemoBins), 4), ' - ', z.Name),
        CAST(@LatMin + (CONVERT(DECIMAL(18,6), ABS(CHECKSUM(NEWID())) % 1000000) / 1000000.0) * (@LatMax - @LatMin) AS DECIMAL(9,6)),
        CAST(@LngMin + (CONVERT(DECIMAL(18,6), ABS(CHECKSUM(NEWID())) % 1000000) / 1000000.0) * (@LngMax - @LngMin) AS DECIMAL(9,6)),
        CASE ABS(CHECKSUM(NEWID())) % 4
            WHEN 0 THEN 80
            WHEN 1 THEN 120
            WHEN 2 THEN 240
            ELSE 360
        END AS CapacityLiters,
        CASE ABS(CHECKSUM(NEWID())) % 4
            WHEN 0 THEN 'General'
            WHEN 1 THEN 'Plastic'
            WHEN 2 THEN 'Organic'
            ELSE 'Paper'
        END AS WasteType,
        CASE
            WHEN ABS(CHECKSUM(NEWID())) % 100 < 65 THEN 1    -- OK
            WHEN ABS(CHECKSUM(NEWID())) % 100 < 90 THEN 2    -- NeedsPickup
            ELSE 3                                           -- Overflowing
        END AS BinStatusId
    FROM nums
    CROSS APPLY (SELECT cnt FROM zcount) c
    JOIN zones z ON z.rn = ((nums.n - 1) % c.cnt) + 1;

    PRINT CONCAT('Inserted demo bins: ', @ToInsert);
END
ELSE
BEGIN
    PRINT 'Demo bins already at/above target. Skipping bin insert.';
END

------------------------------------------------------------
-- 6) Insert readings for bins that have NO readings
------------------------------------------------------------
;WITH bins_no_readings AS (
    SELECT b.BinId
    FROM dbo.Bins b
    WHERE b.Location LIKE 'Demo Bin %'
      AND NOT EXISTS (SELECT 1 FROM dbo.BinReadings r WHERE r.BinId = b.BinId)
),
nums AS (
    SELECT TOP (@ReadingsPerNewBin) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
    FROM sys.objects
)
INSERT INTO dbo.BinReadings (BinId, ReadingTime, FillLevelPercent, Temperature, IsOverflowAlert)
SELECT
    b.BinId,
    DATEADD(HOUR, -1 * (ABS(CHECKSUM(NEWID())) % (24*21)), SYSUTCDATETIME()), -- last 21 days
    x.Fill,
    x.Temp,
    CASE WHEN x.Fill >= 95 THEN 1 ELSE 0 END
FROM bins_no_readings b
CROSS JOIN nums
CROSS APPLY (
    SELECT
        CAST(ABS(CHECKSUM(NEWID())) % 101 AS INT) AS Fill,
        CAST(25 + (ABS(CHECKSUM(NEWID())) % 11) AS INT) AS Temp
) x;

PRINT 'Readings seeded for new demo bins';

------------------------------------------------------------
-- 7) Seed PickupEvents for realistic reports/analytics
-- Inserts pickups only if none exists for that bin already
------------------------------------------------------------
DECLARE @TruckId INT = (SELECT TOP 1 TruckId FROM dbo.Trucks ORDER BY TruckId);
DECLARE @DriverId INT = (SELECT TOP 1 DriverId FROM dbo.Drivers ORDER BY DriverId);

IF @TruckId IS NOT NULL AND @DriverId IS NOT NULL
BEGIN
    ;WITH latest AS (
        SELECT
            b.BinId,
            b.CapacityLiters,
            lr.FillLevelPercent,
            lr.ReadingTime
        FROM dbo.Bins b
        OUTER APPLY (
            SELECT TOP 1 FillLevelPercent, ReadingTime
            FROM dbo.BinReadings r
            WHERE r.BinId = b.BinId
            ORDER BY r.ReadingTime DESC, r.ReadingId DESC
        ) lr
        WHERE b.Location LIKE 'Demo Bin %'
    ),
    candidates AS (
        SELECT TOP 35 *
        FROM latest
        WHERE ISNULL(FillLevelPercent, 0) >= 70
          AND NOT EXISTS (SELECT 1 FROM dbo.PickupEvents p WHERE p.BinId = latest.BinId)
        ORDER BY NEWID()
    )
    INSERT INTO dbo.PickupEvents (BinId, TruckId, DriverId, PickupTime, VolumeCollectedLiters)
    SELECT
        c.BinId,
        @TruckId,
        @DriverId,
        DATEADD(HOUR, -1 * (ABS(CHECKSUM(NEWID())) % (24*10)), SYSUTCDATETIME()), -- last 10 days
        CAST((c.CapacityLiters * (c.FillLevelPercent/100.0)) * (0.55 + (ABS(CHECKSUM(NEWID())) % 36)/100.0) AS DECIMAL(10,2))
    FROM candidates c;

    PRINT 'PickupEvents seeded (for reports/analytics)';
END
ELSE
BEGIN
    PRINT 'PickupEvents skipped: Truck/Driver missing.';
END

------------------------------------------------------------
-- 8) Update BinStatus based on latest reading (realism)
------------------------------------------------------------
;WITH latest AS (
    SELECT b.BinId,
           lr.FillLevelPercent
    FROM dbo.Bins b
    OUTER APPLY (
        SELECT TOP 1 FillLevelPercent
        FROM dbo.BinReadings r
        WHERE r.BinId = b.BinId
        ORDER BY r.ReadingTime DESC, r.ReadingId DESC
    ) lr
    WHERE b.Location LIKE 'Demo Bin %'
)
UPDATE b
SET BinStatusId =
    CASE
        WHEN l.FillLevelPercent >= 95 THEN 3
        WHEN l.FillLevelPercent >= 70 THEN 2
        ELSE 1
    END
FROM dbo.Bins b
JOIN latest l ON l.BinId = b.BinId
WHERE l.FillLevelPercent IS NOT NULL;

PRINT 'Bin statuses refreshed from latest fill';

------------------------------------------------------------
-- 9) Optional: Create demo routes (one per zone) if none recently
------------------------------------------------------------
IF @MakeRoutes = 1 AND OBJECT_ID('dbo.sp_PlanRouteForZone') IS NOT NULL
BEGIN
    DECLARE @Today DATE = CAST(GETUTCDATE() AS DATE);

    IF NOT EXISTS (SELECT 1 FROM dbo.Routes WHERE RouteDate >= DATEADD(DAY, -7, @Today))
    BEGIN
        DECLARE @ZoneId INT;

        DECLARE zone_cursor CURSOR FOR
            SELECT ZoneId FROM dbo.Zones WHERE Name IN ('Zone A','Zone B','Zone C','Zone D');

        OPEN zone_cursor;
        FETCH NEXT FROM zone_cursor INTO @ZoneId;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            EXEC dbo.sp_PlanRouteForZone
                @ZoneId = @ZoneId,
                @RouteDate = @Today,
                @TruckId = @TruckId,
                @DriverId = @DriverId,
                @MinFillPercent = 70;

            FETCH NEXT FROM zone_cursor INTO @ZoneId;
        END

        CLOSE zone_cursor;
        DEALLOCATE zone_cursor;

        PRINT 'Demo routes planned for today (one per demo zone)';
    END
    ELSE
    BEGIN
        PRINT 'Routes exist in last 7 days; skipping route planning';
    END
END
ELSE
BEGIN
    PRINT 'Route planning skipped (disabled or procedure missing)';
END

PRINT 'DONE ✅ 07_SeedDemoData.sql complete';
