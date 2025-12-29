USE ReportManagerDemo
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
(3, N'OSVČ', 1),
(4, N'Nezisková organizace', 1),
(5, N'Veřejný sektor', 1);
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
(N'S-100', N'ACME s.r.o.', 2, 250.00, '2025-01-01'),
(N'S-101', N'Novák', 1, 387.00, '2025-01-04'),
(N'S-102', N'Jana Malá', 1, 524.00, '2025-01-07'),
(N'S-103', N'Petr Kříž', 3, 661.00, '2025-01-10'),
(N'S-104', N'Grafika Kolín', 3, 798.00, '2025-01-13'),
(N'S-105', N'Globex', 2, 935.00, '2025-01-16'),
(N'S-106', N'Alfa Steel a.s.', 2, 1072.00, '2025-01-19'),
(N'S-107', N'Naděje pro děti, z.s.', 4, 1209.00, '2025-01-22'),
(N'S-108', N'Město Beroun', 5, 1346.00, '2025-01-25'),
(N'S-109', N'ZŠ Komenského', 5, 1483.00, '2025-01-28'),
(N'S-110', N'ACME s.r.o.', 2, 1620.00, '2025-01-31'),
(N'S-111', N'Novák', 1, 1757.00, '2025-02-03'),
(N'S-112', N'Jana Malá', 1, 1894.00, '2025-02-06'),
(N'S-113', N'Petr Kříž', 3, 2031.00, '2025-02-09'),
(N'S-114', N'Grafika Kolín', 3, 2168.00, '2025-02-12'),
(N'S-115', N'Globex', 2, 2305.00, '2025-02-15'),
(N'S-116', N'Alfa Steel a.s.', 2, 2442.00, '2025-02-18'),
(N'S-117', N'Naděje pro děti, z.s.', 4, 2579.00, '2025-02-21'),
(N'S-118', N'Město Beroun', 5, 2716.00, '2025-02-24'),
(N'S-119', N'ZŠ Komenského', 5, 2853.00, '2025-02-27'),
(N'S-120', N'ACME s.r.o.', 2, 2990.00, '2025-03-02'),
(N'S-121', N'Novák', 1, 3127.00, '2025-03-05'),
(N'S-122', N'Jana Malá', 1, 3264.00, '2025-03-08'),
(N'S-123', N'Petr Kříž', 3, 3401.00, '2025-03-11'),
(N'S-124', N'Grafika Kolín', 3, 3538.00, '2025-03-14'),
(N'S-125', N'Globex', 2, 3675.00, '2025-03-17'),
(N'S-126', N'Alfa Steel a.s.', 2, 3812.00, '2025-03-20'),
(N'S-127', N'Naděje pro děti, z.s.', 4, 3949.00, '2025-03-23'),
(N'S-128', N'Město Beroun', 5, 4086.00, '2025-03-26'),
(N'S-129', N'ZŠ Komenského', 5, 4223.00, '2025-03-29'),
(N'S-130', N'ACME s.r.o.', 2, 4360.00, '2025-04-01'),
(N'S-131', N'Novák', 1, 4497.00, '2025-04-04'),
(N'S-132', N'Jana Malá', 1, 4634.00, '2025-04-07'),
(N'S-133', N'Petr Kříž', 3, 4771.00, '2025-04-10'),
(N'S-134', N'Grafika Kolín', 3, 4908.00, '2025-04-13'),
(N'S-135', N'Globex', 2, 5045.00, '2025-04-16'),
(N'S-136', N'Alfa Steel a.s.', 2, 5182.00, '2025-04-19'),
(N'S-137', N'Naděje pro děti, z.s.', 4, 5319.00, '2025-04-22'),
(N'S-138', N'Město Beroun', 5, 5456.00, '2025-04-25'),
(N'S-139', N'ZŠ Komenského', 5, 5593.00, '2025-04-28'),
(N'S-140', N'ACME s.r.o.', 2, 5730.00, '2025-05-01'),
(N'S-141', N'Novák', 1, 5867.00, '2025-05-04'),
(N'S-142', N'Jana Malá', 1, 6004.00, '2025-05-07'),
(N'S-143', N'Petr Kříž', 3, 6141.00, '2025-05-10'),
(N'S-144', N'Grafika Kolín', 3, 6278.00, '2025-05-13'),
(N'S-145', N'Globex', 2, 6415.00, '2025-05-16'),
(N'S-146', N'Alfa Steel a.s.', 2, 6552.00, '2025-05-19'),
(N'S-147', N'Naděje pro děti, z.s.', 4, 6689.00, '2025-05-22'),
(N'S-148', N'Město Beroun', 5, 6826.00, '2025-05-25'),
(N'S-149', N'ZŠ Komenského', 5, 6963.00, '2025-05-28'),
(N'S-150', N'ACME s.r.o.', 2, 7100.00, '2025-05-31'),
(N'S-151', N'Novák', 1, 7237.00, '2025-06-03'),
(N'S-152', N'Jana Malá', 1, 7374.00, '2025-06-06'),
(N'S-153', N'Petr Kříž', 3, 7511.00, '2025-06-09'),
(N'S-154', N'Grafika Kolín', 3, 7648.00, '2025-06-12'),
(N'S-155', N'Globex', 2, 7785.00, '2025-06-15'),
(N'S-156', N'Alfa Steel a.s.', 2, 7922.00, '2025-06-18'),
(N'S-157', N'Naděje pro děti, z.s.', 4, 8059.00, '2025-06-21'),
(N'S-158', N'Město Beroun', 5, 8196.00, '2025-06-24'),
(N'S-159', N'ZŠ Komenského', 5, 8333.00, '2025-06-27'),
(N'S-160', N'ACME s.r.o.', 2, 8470.00, '2025-06-30'),
(N'S-161', N'Novák', 1, 8607.00, '2025-07-03'),
(N'S-162', N'Jana Malá', 1, 8744.00, '2025-07-06'),
(N'S-163', N'Petr Kříž', 3, 8881.00, '2025-07-09'),
(N'S-164', N'Grafika Kolín', 3, 9018.00, '2025-07-12'),
(N'S-165', N'Globex', 2, 9155.00, '2025-07-15'),
(N'S-166', N'Alfa Steel a.s.', 2, 9292.00, '2025-07-18'),
(N'S-167', N'Naděje pro děti, z.s.', 4, 9429.00, '2025-07-21'),
(N'S-168', N'Město Beroun', 5, 9566.00, '2025-07-24'),
(N'S-169', N'ZŠ Komenského', 5, 9703.00, '2025-07-27'),
(N'S-170', N'ACME s.r.o.', 2, 9840.00, '2025-07-30'),
(N'S-171', N'Novák', 1, 9977.00, '2025-08-02'),
(N'S-172', N'Jana Malá', 1, 10114.00, '2025-08-05'),
(N'S-173', N'Petr Kříž', 3, 10251.00, '2025-08-08'),
(N'S-174', N'Grafika Kolín', 3, 10388.00, '2025-08-11'),
(N'S-175', N'Globex', 2, 10525.00, '2025-08-14'),
(N'S-176', N'Alfa Steel a.s.', 2, 10662.00, '2025-08-17'),
(N'S-177', N'Naděje pro děti, z.s.', 4, 10799.00, '2025-08-20'),
(N'S-178', N'Město Beroun', 5, 10936.00, '2025-08-23'),
(N'S-179', N'ZŠ Komenského', 5, 11073.00, '2025-08-26'),
(N'S-180', N'ACME s.r.o.', 2, 11210.00, '2025-08-29'),
(N'S-181', N'Novák', 1, 11347.00, '2025-09-01'),
(N'S-182', N'Jana Malá', 1, 11484.00, '2025-09-04'),
(N'S-183', N'Petr Kříž', 3, 11621.00, '2025-09-07'),
(N'S-184', N'Grafika Kolín', 3, 11758.00, '2025-09-10'),
(N'S-185', N'Globex', 2, 11895.00, '2025-09-13'),
(N'S-186', N'Alfa Steel a.s.', 2, 12032.00, '2025-09-16'),
(N'S-187', N'Naděje pro děti, z.s.', 4, 12169.00, '2025-09-19'),
(N'S-188', N'Město Beroun', 5, 12306.00, '2025-09-22'),
(N'S-189', N'ZŠ Komenského', 5, 12443.00, '2025-09-25'),
(N'S-190', N'ACME s.r.o.', 2, 12580.00, '2025-09-28'),
(N'S-191', N'Novák', 1, 12717.00, '2025-10-01'),
(N'S-192', N'Jana Malá', 1, 12854.00, '2025-10-04'),
(N'S-193', N'Petr Kříž', 3, 12991.00, '2025-10-07'),
(N'S-194', N'Grafika Kolín', 3, 13128.00, '2025-10-10'),
(N'S-195', N'Globex', 2, 13265.00, '2025-10-13'),
(N'S-196', N'Alfa Steel a.s.', 2, 13402.00, '2025-10-16'),
(N'S-197', N'Naděje pro děti, z.s.', 4, 13539.00, '2025-10-19'),
(N'S-198', N'Město Beroun', 5, 13676.00, '2025-10-22'),
(N'S-199', N'ZŠ Komenského', 5, 13813.00, '2025-10-25');
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
