USE ReportManagerDemo;
GO

-- Core tables
IF OBJECT_ID('dbo.ReportDefinition','U') IS NOT NULL DROP TABLE dbo.ReportDefinition;
IF OBJECT_ID('dbo.ReportViewPreset','U') IS NOT NULL DROP TABLE dbo.ReportViewPreset;
GO

CREATE TABLE dbo.ReportDefinition
(
    ReportDefinitionId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ReportDefinition PRIMARY KEY,
    [Key] NVARCHAR(100) NOT NULL CONSTRAINT UQ_ReportDefinition_Key UNIQUE,
    ViewSchema NVARCHAR(128) NOT NULL CONSTRAINT DF_ReportDefinition_ViewSchema DEFAULT('dbo'),
    ViewName NVARCHAR(128) NOT NULL,
    DefinitionJson NVARCHAR(MAX) NOT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_ReportDefinition_IsActive DEFAULT(1),
    UpdatedUtc DATETIME2(0) NOT NULL CONSTRAINT DF_ReportDefinition_UpdatedUtc DEFAULT (SYSUTCDATETIME())
);
GO

CREATE TABLE dbo.ReportViewPreset
(
    PresetId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ReportViewPreset PRIMARY KEY,
    ReportKey NVARCHAR(100) NOT NULL,
    OwnerUserId UNIQUEIDENTIFIER NULL, -- NULL = system preset
    PresetJson NVARCHAR(MAX) NOT NULL,
    IsDefault BIT NOT NULL CONSTRAINT DF_ReportViewPreset_IsDefault DEFAULT(0),
    CreatedUtc DATETIME2(0) NOT NULL CONSTRAINT DF_ReportViewPreset_CreatedUtc DEFAULT (SYSUTCDATETIME()),
    UpdatedUtc DATETIME2(0) NOT NULL CONSTRAINT DF_ReportViewPreset_UpdatedUtc DEFAULT (SYSUTCDATETIME())
);
GO

CREATE INDEX IX_ReportViewPreset_ReportOwner ON dbo.ReportViewPreset(ReportKey, OwnerUserId);
GO