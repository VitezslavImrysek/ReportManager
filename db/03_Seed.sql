USE ReportManagerDemo;
GO

-- Demo data: Contracts + lookup
IF OBJECT_ID('dbo.CustomerType_Loc','U') IS NOT NULL DROP TABLE dbo.CustomerType_Loc;
IF OBJECT_ID('dbo.Contracts','U') IS NOT NULL DROP TABLE dbo.Contracts;
GO

CREATE TABLE dbo.CustomerType_Loc
(
    Id INT NOT NULL CONSTRAINT PK_CustomerType_Loc PRIMARY KEY,
    Nazev NVARCHAR(200) NOT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_CustomerType_Loc_IsActive DEFAULT(1)
);
GO

INSERT INTO dbo.CustomerType_Loc (Id, Nazev, IsActive) VALUES
(1, N'Domácnost', 1),
(2, N'Firma', 1),
(3, N'VIP', 1);
GO

CREATE TABLE dbo.Contracts
(
    ContractId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Contracts PRIMARY KEY,
    ContractNumber NVARCHAR(50) NOT NULL,
    CustomerName NVARCHAR(200) NOT NULL,
    CustomerTypeId INT NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    StartDate DATE NOT NULL
);
GO

INSERT INTO dbo.Contracts (ContractNumber, CustomerName, CustomerTypeId, Amount, StartDate) VALUES
(N'S-100', N'ACME s.r.o.', 2, 1200.50, '2025-01-01'),
(N'S-101', N'Novák', 1, 300.00, '2025-02-15'),
(N'S-200', N'Globex', 2, 9999.99, '2025-03-10'),
(N'S-201', N'VIP Client', 3, 15000.00, '2025-04-05');
GO

-- View for report
IF OBJECT_ID('dbo.v_ContractsReport','V') IS NOT NULL DROP VIEW dbo.v_ContractsReport;
GO
CREATE VIEW dbo.v_ContractsReport
AS
SELECT
    c.ContractId       AS id_smlouva,
    c.ContractNumber   AS cislo_smlouvy,
    c.CustomerName     AS zakaznik,
    c.CustomerTypeId   AS typ_zakaznika,
    ct.Nazev           AS typ_zakaznika_text,
    c.Amount           AS castka,
    c.StartDate        AS datum_od
FROM dbo.Contracts c
JOIN dbo.CustomerType_Loc ct ON ct.Id = c.CustomerTypeId;
GO
