-- Adds AvatarUrl column to Users table to store staff profile avatar links
-- Run this script against the SapaFoRestRMS database before applying the updated application binaries.

IF COL_LENGTH('dbo.Users', 'AvatarUrl') IS NULL
BEGIN
    ALTER TABLE dbo.Users
    ADD AvatarUrl NVARCHAR(500) NULL;
END;


