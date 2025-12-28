using ReportManager.ApiContracts.Dto;
using ReportManager.DefinitionModel.Json;
using System.Data;
using System.Data.SqlClient;

namespace ReportManager.Server.Repository
{
	public sealed class ReportRepository
	{
		private readonly string _connectionString;

		public ReportRepository(string connectionString)
		{
			_connectionString = connectionString;
		}

		public (int Version, string ViewSchema, string ViewName, string DefinitionJson) GetReportDefinitionByKey(string reportKey)
		{
			using (var con = new SqlConnection(_connectionString))
			using (var cmd = con.CreateCommand())
			{
				cmd.CommandText = @"
SELECT TOP (1) Version, ViewSchema, ViewName, DefinitionJson
FROM dbo.ReportDefinition
WHERE [Key] = @key AND IsActive = 1;
";
				cmd.Parameters.AddWithValue("@key", reportKey);
				con.Open();
				using (var rdr = cmd.ExecuteReader())
				{
					if (!rdr.Read())
						throw new InvalidOperationException("Report not found: " + reportKey);

					int version = rdr.GetInt32(0);
					string schema = rdr.GetString(1);
					string view = rdr.GetString(2);
					string json = rdr.GetString(3);
					return (version, schema, view, json);
				}
			}
		}

		public DataTable ExecuteDataTable(string sql, List<SqlParameter> parameters)
		{
			using (var con = new SqlConnection(_connectionString))
			using (var cmd = con.CreateCommand())
			{
				cmd.CommandText = sql;
				cmd.Parameters.AddRange(parameters.ToArray());
				using (var da = new SqlDataAdapter(cmd))
				{
					var dt = new DataTable();
					da.Fill(dt);
					return dt;
				}
			}
		}

		public int ExecuteScalarInt(string sql, List<SqlParameter> parameters)
		{
			using (var con = new SqlConnection(_connectionString))
			using (var cmd = con.CreateCommand())
			{
				cmd.CommandText = sql;
				cmd.Parameters.AddRange(parameters.ToArray());
				con.Open();
				object o = cmd.ExecuteScalar();
				return o == null || o is DBNull ? 0 : Convert.ToInt32(o);
			}
		}

		public List<PresetInfoDto> GetPresets(string reportKey, Guid userId)
		{
			var result = new List<PresetInfoDto>();
			using (var con = new SqlConnection(_connectionString))
			using (var cmd = con.CreateCommand())
			{
				cmd.CommandText = @"
SELECT PresetId, [Name], OwnerUserId, IsDefault
FROM dbo.ReportViewPreset
WHERE ReportKey = @rk AND (OwnerUserId IS NULL OR OwnerUserId = @uid)
ORDER BY CASE WHEN OwnerUserId IS NULL THEN 0 ELSE 1 END, [Name];
";
				cmd.Parameters.AddWithValue("@rk", reportKey);
				cmd.Parameters.AddWithValue("@uid", userId);
				con.Open();
				using (var rdr = cmd.ExecuteReader())
				{
					while (rdr.Read())
					{
						var owner = rdr.IsDBNull(2) ? (Guid?)null : rdr.GetGuid(2);
						result.Add(new PresetInfoDto
						{
							PresetId = rdr.GetGuid(0),
							Name = rdr.GetString(1),
							IsSystem = owner == null,
							IsDefault = rdr.GetBoolean(3)
						});
					}
				}
			}
			return result;
		}

		public PresetDto GetPreset(Guid presetId, Guid userId)
		{
			using (var con = new SqlConnection(_connectionString))
			using (var cmd = con.CreateCommand())
			{
				cmd.CommandText = @"
SELECT PresetId, ReportKey, [Name], OwnerUserId, PresetJson, IsDefault
FROM dbo.ReportViewPreset
WHERE PresetId = @id AND (OwnerUserId IS NULL OR OwnerUserId = @uid);
";
				cmd.Parameters.AddWithValue("@id", presetId);
				cmd.Parameters.AddWithValue("@uid", userId);
				con.Open();
				using (var rdr = cmd.ExecuteReader())
				{
					if (!rdr.Read()) throw new InvalidOperationException("Preset not found or access denied.");

					var owner = rdr.IsDBNull(3) ? (Guid?)null : rdr.GetGuid(3);
					var presetJson = rdr.GetString(4);

					var content = JsonUtil.Deserialize<PresetContentDto>(presetJson) ?? new PresetContentDto();

					return new PresetDto
					{
						PresetId = rdr.GetGuid(0),
						ReportKey = rdr.GetString(1),
						Name = rdr.GetString(2),
						IsSystem = owner == null,
						IsDefault = rdr.GetBoolean(5),
						Content = content
					};
				}
			}
		}

		public Guid SavePreset(PresetDto preset, Guid userId)
		{
			// system presets not supported in demo (OwnerUserId NULL) - assume user preset
			using (var con = new SqlConnection(_connectionString))
			using (var cmd = con.CreateCommand())
			{
				cmd.CommandText = @"
IF EXISTS (SELECT 1 FROM dbo.ReportViewPreset WHERE PresetId = @id)
BEGIN
	UPDATE dbo.ReportViewPreset
	SET [Name]=@name, PresetJson=@json, UpdatedUtc=SYSUTCDATETIME()
	WHERE PresetId=@id AND OwnerUserId=@uid;
END
ELSE
BEGIN
	INSERT INTO dbo.ReportViewPreset (PresetId, ReportKey, [Name], OwnerUserId, PresetJson, IsDefault, CreatedUtc, UpdatedUtc)
	VALUES (@id, @rk, @name, @uid, @json, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
END
SELECT @id;
";
				var id = preset.PresetId == Guid.Empty ? Guid.NewGuid() : preset.PresetId;
				var json = JsonUtil.Serialize(preset.Content ?? new PresetContentDto());

				cmd.Parameters.AddWithValue("@id", id);
				cmd.Parameters.AddWithValue("@rk", preset.ReportKey);
				cmd.Parameters.AddWithValue("@name", preset.Name);
				cmd.Parameters.AddWithValue("@uid", userId);
				cmd.Parameters.AddWithValue("@json", json);
				con.Open();
				return (Guid)cmd.ExecuteScalar();
			}
		}

		public void DeletePreset(Guid presetId, Guid userId)
		{
			using (var con = new SqlConnection(_connectionString))
			using (var cmd = con.CreateCommand())
			{
				cmd.CommandText = @"DELETE FROM dbo.ReportViewPreset WHERE PresetId=@id AND OwnerUserId=@uid;";
				cmd.Parameters.AddWithValue("@id", presetId);
				cmd.Parameters.AddWithValue("@uid", userId);
				con.Open();
				cmd.ExecuteNonQuery();
			}
		}

		public void SetDefaultPreset(Guid presetId, string reportKey, Guid userId)
		{
			using (var con = new SqlConnection(_connectionString))
			using (var cmd = con.CreateCommand())
			{
				cmd.CommandText = @"
UPDATE dbo.ReportViewPreset SET IsDefault=0 WHERE ReportKey=@rk AND OwnerUserId=@uid;
UPDATE dbo.ReportViewPreset SET IsDefault=1 WHERE PresetId=@id AND ReportKey=@rk AND OwnerUserId=@uid;
";
				cmd.Parameters.AddWithValue("@rk", reportKey);
				cmd.Parameters.AddWithValue("@uid", userId);
				cmd.Parameters.AddWithValue("@id", presetId);
				con.Open();
				cmd.ExecuteNonQuery();
			}
		}

		public List<LookupItemDto> ExecuteLookup(string sql, string keyCol, string textCol)
		{
			var result = new List<LookupItemDto>();
			using (var con = new SqlConnection(_connectionString))
			using (var cmd = con.CreateCommand())
			{
				cmd.CommandText = sql;
				con.Open();
				using (var rdr = cmd.ExecuteReader())
				{
					int keyOrdinal = rdr.GetOrdinal(keyCol);
					int textOrdinal = rdr.GetOrdinal(textCol);
					while (rdr.Read())
					{
						var key = rdr.GetValue(keyOrdinal);
						var text = rdr.GetValue(textOrdinal);
						result.Add(new LookupItemDto
						{
							Key = key == null || key is DBNull ? "" : Convert.ToString(key),
							Text = text == null || text is DBNull ? "" : Convert.ToString(text)
						});
					}
				}
			}
			return result;
		}
	}
}
