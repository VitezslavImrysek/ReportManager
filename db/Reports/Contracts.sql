/* REPORT: Contracts */
/* GENERATED: 2026-01-24T16:04:53Z */
/* DO NOT EDIT BY HAND */

BEGIN TRY
BEGIN TRAN;

DECLARE @ReportKey nvarchar(100) = N'Contracts';
DECLARE @ViewSchema nvarchar(128) = N'dbo';
DECLARE @ViewName   nvarchar(128) = N'v_ContractsReport';
DECLARE @ReportDefinitionId int;

/* === ReportDefinitionJson BEGIN === */
DECLARE @DefinitionJson nvarchar(max) = N'{
  "version": 1,
  "defaultCulture": "cs",
  "texts": {
    "cs": {
      "col.castka": "Částka",
      "col.cislo_smlouvy": "Číslo smlouvy",
      "col.datum_od": "Datum od",
      "col.id_smlouva": "ID smlouvy",
      "col.typ_zakaznika": "Typ zákazníka",
      "col.typ_zakaznika_text": "Typ zákazníka",
      "col.zakaznik": "Zákazník",
      "report.title": "Smlouvy"
    },
    "en": {
      "col.castka": "Amount",
      "col.cislo_smlouvy": "Contract number",
      "col.datum_od": "Start date",
      "col.id_smlouva": "Contract ID",
      "col.typ_zakaznika": "Customer type",
      "col.typ_zakaznika_text": "Customer type",
      "col.zakaznik": "Customer",
      "report.title": "Contracts"
    }
  },
  "columns": [
    {
      "key": "id_smlouva",
      "type": "integer",
      "flags": "alwaysSelect, hidden, primaryKey, sortable",
      "filter": {
        "flags": "none"
      },
      "sort": {
        "flags": "hidden"
      }
    },
    {
      "key": "cislo_smlouvy",
      "type": "string",
      "flags": "filterable, sortable",
      "filter": {
        "flags": "none"
      },
      "sort": {
        "flags": "none"
      }
    },
    {
      "key": "zakaznik",
      "type": "string",
      "flags": "filterable, sortable",
      "filter": {
        "flags": "none"
      },
      "sort": {
        "flags": "none"
      }
    },
    {
      "key": "typ_zakaznika",
      "type": "integer",
      "flags": "hidden, filterable",
      "filter": {
        "lookup": {
          "mode": "sql",
          "sql": {
            "commandText": "SELECT Id, Nazev FROM dbo.CustomerType_Loc WHERE IsActive=1 ORDER BY Nazev",
            "keyColumn": "Id",
            "textColumn": "Nazev"
          },
          "items": []
        },
        "flags": "none"
      }
    },
    {
      "key": "typ_zakaznika_text",
      "type": "string",
      "flags": "sortable",
      "sort": {
        "flags": "none"
      }
    },
    {
      "key": "castka",
      "type": "decimal",
      "flags": "filterable, sortable",
      "filter": {
        "flags": "none"
      },
      "sort": {
        "flags": "none"
      }
    },
    {
      "key": "datum_od",
      "type": "date",
      "flags": "filterable, sortable",
      "filter": {
        "flags": "none"
      },
      "sort": {
        "flags": "none"
      }
    }
  ],
  "defaultSort": [
    {
      "columnKey": "cislo_smlouvy",
      "direction": "asc"
    }
  ]
}';
/* === ReportDefinitionJson END === */

-- Upsert ReportDefinition
MERGE dbo.ReportDefinition AS t
USING (SELECT @ReportKey AS [Key]) AS s
ON t.[Key] = s.[Key]
WHEN MATCHED THEN
  UPDATE SET
	t.ViewSchema = @ViewSchema,
	t.ViewName = @ViewName,
	t.DefinitionJson = @DefinitionJson
WHEN NOT MATCHED THEN
  INSERT ([Key],ViewSchema,ViewName,DefinitionJson)
  VALUES (@ReportKey,@ViewSchema,@ViewName,@DefinitionJson);

SELECT @ReportDefinitionId = ReportDefinitionId FROM dbo.ReportDefinition WHERE [Key] = @ReportKey;

-- System presets (OwnerUserId IS NULL)
/* === SystemPresets BEGIN === */

-- preset: Contracts_7b62d22d6d2d4a1ea6d2d1ccf0fcb81a
DECLARE @PresetKey_1 nvarchar(100) = N'Contracts_7b62d22d6d2d4a1ea6d2d1ccf0fcb81a';
DECLARE @PresetId_1 uniqueidentifier = '1619bed5-aadc-5813-a1f0-5cf6ba692a7b';
DECLARE @IsDefault_1 bit = 1;

DECLARE @PresetJson_1 nvarchar(max) = N'{
  "grid": {
    "hiddenColumns": [
      "cislo_smlouvy"
    ],
    "order": []
  },
  "query": {
    "filters": [],
    "sorting": [
      {
        "columnKey": "cislo_smlouvy",
        "direction": "asc"
      }
    ],
    "selectedColumns": []
  },
  "texts": {
    "cs": {
      "preset.title": "Výchozí"
    },
    "en": {
      "preset.title": "Default"
    }
  }
}';

MERGE dbo.ReportViewPreset AS pv
USING (SELECT @PresetId_1 AS PresetId) AS s
ON pv.PresetId = s.PresetId
WHEN MATCHED THEN
  UPDATE SET
	pv.ReportDefinitionId = @ReportDefinitionId,
	pv.OwnerUserId = NULL,
	pv.PresetJson = @PresetJson_1,
	pv.IsDefault = @IsDefault_1
WHEN NOT MATCHED THEN
  INSERT (PresetId, ReportDefinitionId, OwnerUserId, PresetJson, IsDefault)
  VALUES (@PresetId_1, @ReportDefinitionId, NULL, @PresetJson_1, @IsDefault_1);

-- enforce single default (system)
UPDATE dbo.ReportViewPreset
SET IsDefault = CASE WHEN PresetId = '1619bed5-aadc-5813-a1f0-5cf6ba692a7b' THEN 1 ELSE 0 END
WHERE ReportDefinitionId = @ReportDefinitionId AND OwnerUserId IS NULL;

/* === SystemPresets END === */

COMMIT;
END TRY
BEGIN CATCH
  IF @@TRANCOUNT > 0 ROLLBACK;
  THROW;
END CATCH
GO
