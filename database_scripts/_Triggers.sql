SET NOCOUNT ON;

------------------------------------------------------------
-- 1) After insert BinReadings -> update bin status
------------------------------------------------------------
IF OBJECT_ID('dbo.TR_BinReadings_AfterInsert_UpdateBinStatus','TR') IS NOT NULL
    DROP TRIGGER dbo.TR_BinReadings_AfterInsert_UpdateBinStatus;
GO

CREATE TRIGGER dbo.TR_BinReadings_AfterInsert_UpdateBinStatus
ON dbo.BinReadings
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE b
    SET BinStatusId =
        CASE
            WHEN i.IsOverflowAlert = 1 OR i.FillLevelPercent >= 95 THEN 3
            WHEN i.FillLevelPercent >= 70 THEN 2
            ELSE 1
        END
    FROM dbo.Bins b
    JOIN inserted i ON i.BinId = b.BinId;
END
GO

------------------------------------------------------------
-- 2) After insert PickupEvents -> reset bin status to OK
------------------------------------------------------------
IF OBJECT_ID('dbo.TR_PickupEvents_AfterInsert_ResetBin','TR') IS NOT NULL
    DROP TRIGGER dbo.TR_PickupEvents_AfterInsert_ResetBin;
GO

CREATE TRIGGER dbo.TR_PickupEvents_AfterInsert_ResetBin
ON dbo.PickupEvents
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE b
    SET BinStatusId = 1
    FROM dbo.Bins b
    JOIN inserted i ON i.BinId = b.BinId;
END
GO

------------------------------------------------------------
-- 3) Audit Routes updates -> write to AuditLogs
------------------------------------------------------------
IF OBJECT_ID('dbo.TR_Routes_AfterUpdate_Audit','TR') IS NOT NULL
    DROP TRIGGER dbo.TR_Routes_AfterUpdate_Audit;
GO

CREATE TRIGGER dbo.TR_Routes_AfterUpdate_Audit
ON dbo.Routes
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.AuditLogs(TableName, RecordId, OperationType, OldValue, NewValue)
    SELECT
        'Routes' AS TableName,
        CAST(i.RouteId AS NVARCHAR(60)) AS RecordId,
        'UPDATE' AS OperationType,
        (SELECT d.RouteId, d.TruckId, d.DriverId, d.RouteDate, d.RouteStatusId, d.StartedAt, d.CompletedAt
         FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) AS OldValue,
        (SELECT i.RouteId, i.TruckId, i.DriverId, i.RouteDate, i.RouteStatusId, i.StartedAt, i.CompletedAt
         FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) AS NewValue
    FROM inserted i
    JOIN deleted d ON d.RouteId = i.RouteId;
END
GO
