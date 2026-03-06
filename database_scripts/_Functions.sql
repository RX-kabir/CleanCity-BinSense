SET NOCOUNT ON;

-- 1) Avg fill level of a bin over last N days
CREATE OR ALTER FUNCTION dbo.fn_BinAverageFillLevel
(
    @BinId INT,
    @Days INT
)
RETURNS DECIMAL(10,2)
AS
BEGIN
    DECLARE @avg DECIMAL(10,2);

    SELECT @avg = AVG(CAST(FillLevelPercent AS DECIMAL(10,2)))
    FROM dbo.BinReadings
    WHERE BinId = @BinId
      AND ReadingTime >= DATEADD(DAY, -@Days, SYSUTCDATETIME());

    RETURN ISNULL(@avg, 0);
END
GO

-- 2) Total collected liters in a zone for a specific day
CREATE OR ALTER FUNCTION dbo.fn_ZoneDailyCollectedVolume
(
    @ZoneId INT,
    @Day DATE
)
RETURNS DECIMAL(12,2)
AS
BEGIN
    DECLARE @sum DECIMAL(12,2);

    SELECT @sum = SUM(p.VolumeCollectedLiters)
    FROM dbo.PickupEvents p
    JOIN dbo.Bins b ON b.BinId = p.BinId
    WHERE b.ZoneId = @ZoneId
      AND CAST(p.PickupTime AS DATE) = @Day;

    RETURN ISNULL(@sum, 0);
END
GO

-- 3) Truck utilization (CompletedStops / TotalStops) in date range
CREATE OR ALTER FUNCTION dbo.fn_TruckUtilizationRate
(
    @TruckId INT,
    @FromDate DATE,
    @ToDate DATE
)
RETURNS DECIMAL(10,4)
AS
BEGIN
    DECLARE @total INT = 0, @done INT = 0;

    SELECT @total = COUNT(*)
    FROM dbo.RouteStops s
    JOIN dbo.Routes r ON r.RouteId = s.RouteId
    WHERE r.TruckId = @TruckId
      AND r.RouteDate >= @FromDate AND r.RouteDate <= @ToDate;

    SELECT @done = COUNT(*)
    FROM dbo.RouteStops s
    JOIN dbo.Routes r ON r.RouteId = s.RouteId
    WHERE r.TruckId = @TruckId
      AND r.RouteDate >= @FromDate AND r.RouteDate <= @ToDate
      AND s.ActualTime IS NOT NULL;

    RETURN CASE WHEN @total = 0 THEN 0 ELSE CAST(@done AS DECIMAL(10,4)) / @total END;
END
GO
