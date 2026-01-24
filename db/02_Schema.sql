USE ReportManagerDemo;
GO

-- Core tables
IF OBJECT_ID('dbo.ReportViewPreset','U') IS NOT NULL DROP TABLE [dbo].[ReportViewPreset];
IF OBJECT_ID('dbo.ReportDefinition','U') IS NOT NULL DROP TABLE [dbo].[ReportDefinition];
GO

CREATE TABLE [dbo].[ReportDefinition]
(
    [ReportDefinitionId] INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ReportDefinition PRIMARY KEY,
    [Key] NVARCHAR(100) NOT NULL CONSTRAINT UQ_ReportDefinition_Key UNIQUE,
    [ViewSchema] NVARCHAR(128) NOT NULL CONSTRAINT DF_ReportDefinition_ViewSchema DEFAULT('dbo'),
    [ViewName] NVARCHAR(128) NOT NULL,
    [DefinitionJson] NVARCHAR(MAX) NOT NULL
);
GO

CREATE TABLE [dbo].[ReportViewPreset] (
    [PresetId] UNIQUEIDENTIFIER NOT NULL,
    [ReportDefinitionId] INT NOT NULL,
    [OwnerUserId] UNIQUEIDENTIFIER NULL,
    [PresetJson] NVARCHAR(MAX) NOT NULL,
    [IsDefault] BIT NOT NULL CONSTRAINT [DF_ReportViewPreset_IsDefault] DEFAULT ((0)),
    
    CONSTRAINT [PK_ReportViewPreset] PRIMARY KEY CLUSTERED ([PresetId] ASC),
    CONSTRAINT [FK_ReportViewPreset_ReportDefinition] FOREIGN KEY ([ReportDefinitionId]) 
        REFERENCES [dbo].[ReportDefinition] ([ReportDefinitionId])
);

CREATE NONCLUSTERED INDEX [IX_ReportViewPreset_ReportDefinition_Owner]
    ON [dbo].[ReportViewPreset] ([ReportDefinitionId], [OwnerUserId]);
GO