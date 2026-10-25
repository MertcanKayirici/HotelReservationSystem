/*
    Hotel Reservation System
    Database bootstrap script

    Purpose:
    - Create HotelDb if it does not exist.
    - Preserve existing database contents.
    - Leave schema creation and updates to EF Core migrations.
*/

IF DB_ID(N'HotelDb') IS NULL
BEGIN
    CREATE DATABASE [HotelDb];
    PRINT 'HotelDb database created successfully.';
END
ELSE
BEGIN
    PRINT 'HotelDb already exists. No changes made.';
END;
GO
