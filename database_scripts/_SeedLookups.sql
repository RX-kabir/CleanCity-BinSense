SET NOCOUNT ON;

-- Bin Status
IF NOT EXISTS (SELECT 1 FROM dbo.BinStatus)
BEGIN
    INSERT INTO dbo.BinStatus (BinStatusId, Name) VALUES
    (1, 'OK'),
    (2, 'NeedsPickup'),
    (3, 'Overflowing');
END

-- Route Status
IF NOT EXISTS (SELECT 1 FROM dbo.RouteStatus)
BEGIN
    INSERT INTO dbo.RouteStatus (RouteStatusId, Name) VALUES
    (1, 'Planned'),
    (2, 'InProgress'),
    (3, 'Completed'),
    (4, 'Cancelled');
END
GO
