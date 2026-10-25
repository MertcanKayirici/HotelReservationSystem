/*
    Hotel Reservation System
    Development seed data

    IMPORTANT:
    - Run EF Core migrations before executing this script.
    - Use only in development or test environments.
    - Room names are used to avoid inserting the same seed rooms again.
*/

SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

INSERT INTO dbo.Rooms
    (Name, PricePerNight, Capacity, IsAvailable)
SELECT
    seed.Name,
    seed.PricePerNight,
    seed.Capacity,
    seed.IsAvailable
FROM
(
    VALUES
        (N'Standard Room 101', CAST(1500.00 AS DECIMAL(18,2)), 2, CAST(1 AS BIT)),
        (N'Deluxe Room 201',   CAST(2500.00 AS DECIMAL(18,2)), 2, CAST(1 AS BIT)),
        (N'Family Room 301',   CAST(3500.00 AS DECIMAL(18,2)), 4, CAST(1 AS BIT))
) AS seed(Name, PricePerNight, Capacity, IsAvailable)
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.Rooms AS existing
    WHERE existing.Name = seed.Name
);

COMMIT TRANSACTION;
GO
