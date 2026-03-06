SET NOCOUNT ON;

CREATE OR ALTER VIEW dbo.vw_ZoneWasteSummary
AS
SELECT
    z.ZoneId,
    z.Name AS ZoneName,
    CAST(p.PickupTime AS DATE) AS ReportDate,
    SUM(p.VolumeCollectedLiters) AS TotalCollectedLiters,
    COUNT(*) AS PickupCount
FROM dbo.PickupEvents p
JOIN dbo.Bins b ON b.BinId = p.BinId
JOIN dbo.Zones z ON z.ZoneId = b.ZoneId
GROUP BY z.ZoneId, z.Name, CAST(p.PickupTime AS DATE);
GO

CREATE OR ALTER VIEW dbo.vw_TruckRouteSummary
AS
SELECT
    r.RouteId,
    r.RouteDate,
    t.TruckId,
    t.RegistrationNo,
    rs.Name AS RouteStatus,
    (SELECT COUNT(*) FROM dbo.RouteStops s WHERE s.RouteId = r.RouteId) AS TotalStops,
    (SELECT COUNT(*) FROM dbo.RouteStops s WHERE s.RouteId = r.RouteId AND s.ActualTime IS NOT NULL) AS CompletedStops
FROM dbo.Routes r
JOIN dbo.Trucks t ON t.TruckId = r.TruckId
JOIN dbo.RouteStatus rs ON rs.RouteStatusId = r.RouteStatusId;
GO
