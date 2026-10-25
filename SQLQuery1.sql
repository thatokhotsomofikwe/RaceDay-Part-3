IF COL_LENGTH('dbo.Events', 'ImageUrl') IS NULL
BEGIN
    ALTER TABLE dbo.Events
    ADD ImageUrl NVARCHAR(MAX) NULL;
END;