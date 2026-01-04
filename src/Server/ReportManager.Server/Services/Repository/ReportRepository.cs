using LinqToDB;
using LinqToDB.Data;
using ReportManager.DefinitionModel.Json;
using ReportManager.DefinitionModel.Models;
using ReportManager.DefinitionModel.Models.ReportDefinition;
using ReportManager.DefinitionModel.Models.ReportPreset;
using ReportManager.DefinitionModel.Utils;
using ReportManager.Shared;
using ReportManager.Shared.Dto;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;

namespace ReportManager.Server.Services.Repository
{
	public sealed class ReportRepository
	{
		private readonly string _connectionString;

		public ReportRepository(string connectionString)
		{
			_connectionString = connectionString;
		}

		public (string ViewSchema, string ViewName, string DefinitionJson) GetReportDefinitionByKey(string reportKey)
		{
            using var con = new SqlConnection(_connectionString);
            using var dataConnection = GetDataConnection(con);

            var rd = dataConnection.GetTable<ReportDefinitionDb>()
                .Where(x => x.Key == reportKey && x.IsActive)
                .FirstOrDefault();

            if (rd == null)
            {
                throw new InvalidOperationException("Report not found: " + reportKey);
            }

            return (rd.ViewSchema, rd.ViewName, rd.DefinitionJson);
        }

		public List<PresetInfoDto> GetPresets(string reportKey, Guid userId)
		{
            using var con = new SqlConnection(_connectionString);
            using var dataConnection = GetDataConnection(con);

            var defaultCulture = GetReportDefaultCulture(dataConnection, reportKey);

			var presets = dataConnection.GetTable<ReportViewPresetDb>()
                .Where(x => x.ReportKey == reportKey && (x.OwnerUserId == null || x.OwnerUserId == userId))
                .OrderBy(x => x.OwnerUserId == null ? 0 : 1)
                .ToList();

            var result = new List<PresetInfoDto>(presets.Count);

            foreach (var preset in presets)
            {
                var presetJson = JsonUtil.Deserialize<PresetContentJson>(preset.PresetJson);

                result.Add(new PresetInfoDto
                {
                    PresetId = preset.PresetId,
                    Name = TextsResolver.ResolveText(presetJson.Texts, KnownTextKeys.PresetTitle, CultureInfo.CurrentUICulture.Name, defaultCulture), 
                    IsSystem = preset.OwnerUserId == null,
                    IsDefault = preset.IsDefault
                });
            }

            return result;
        }

        private string GetReportDefaultCulture(DataConnection dataConnection, string reportKey)
        {
            var reportDefinitionJson = dataConnection.GetTable<ReportDefinitionDb>()
                .Where(x => x.Key == reportKey)
                .Select(x => x.DefinitionJson)
                .FirstOrDefault();

            var defaultCulture = Constants.DefaultLanguage;

            if (reportDefinitionJson != null)
            {
                var reportDefinition = JsonUtil.Deserialize<ReportDefinitionJson>(reportDefinitionJson);
                defaultCulture = reportDefinition?.DefaultCulture ?? Constants.DefaultLanguage;
            }

            return defaultCulture;
        }

		public PresetDto GetPreset(Guid presetId, Guid userId)
		{
            using var con = new SqlConnection(_connectionString);
            using var dataConnection = GetDataConnection(con);
            
			var preset = dataConnection.GetTable<ReportViewPresetDb>()
				.Where(x => x.PresetId == presetId && (x.OwnerUserId == null || x.OwnerUserId == userId))
				.FirstOrDefault();

			if (preset == null)
			{
				throw new InvalidOperationException("Preset not found or access denied.");
			}

            var presetJson = JsonUtil.Deserialize<PresetContentJson>(preset.PresetJson);
            var defaultCulture = GetReportDefaultCulture(dataConnection, preset.ReportKey);

            return new PresetDto
            {
                PresetId = preset.PresetId,
                ReportKey = preset.ReportKey,
                Name = TextsResolver.ResolveText(presetJson.Texts, KnownTextKeys.PresetTitle, CultureInfo.CurrentUICulture.Name, defaultCulture),
                IsSystem = preset.OwnerUserId == null,
                IsDefault = preset.IsDefault,
                Content = (PresetContentDto)presetJson
            };
        }

		public Guid SavePreset(PresetDto preset, Guid userId)
		{
			// Only save user preset. Admin presets are created using admin app.
			using (var con = new SqlConnection(_connectionString))
            using (var dataConnection = GetDataConnection(con))
			{
                var defaultCulture = GetReportDefaultCulture(dataConnection, preset.ReportKey);

                // TODO: Maybe load existing preset and merge texts?
                // This title setting is okay, since user presets are not localized anyway.
                var presetJson = (PresetContentJson)preset.Content;
                presetJson.Texts ??= [];
                TextsResolver.SetText(presetJson.Texts, KnownTextKeys.PresetTitle, defaultCulture, preset.Name);

                var json = JsonUtil.Serialize(presetJson);
				
				if (preset.PresetId == Guid.Empty)
				{
                    // Insert
                    var rvp = new ReportViewPresetDb
                    {
                        PresetId = Guid.NewGuid(),
                        ReportKey = preset.ReportKey,
                        OwnerUserId = userId,
                        PresetJson = json,
                        IsDefault = false,
                        CreatedUtc = DateTimeOffset.UtcNow,
                        UpdatedUtc = DateTimeOffset.UtcNow
                    };
					dataConnection.Insert(rvp);
					return rvp.PresetId;
                }
				else
				{
                    // Update
                    dataConnection.GetTable<ReportViewPresetDb>()
						.Where(x => x.PresetId == preset.PresetId && x.OwnerUserId == userId)
						.Set(x => x.PresetJson, json)
						.Set(x => x.UpdatedUtc, DateTimeOffset.UtcNow)
						.Update();
                    return preset.PresetId;
                }
			}
		}

		public void DeletePreset(Guid presetId, Guid userId)
		{
			using (var con = new SqlConnection(_connectionString))
            using (var dataConnection = GetDataConnection(con))
			{
                dataConnection.GetTable<ReportViewPresetDb>()
					.Where(x => x.PresetId == presetId && x.OwnerUserId == userId)
					.Delete();
			}
		}

		public void SetDefaultPreset(Guid presetId, string reportKey, Guid userId)
		{
			using (var con = new SqlConnection(_connectionString))
            using (var dataConnection = GetDataConnection(con))
			{
				var reportViewPresets = dataConnection.GetTable<ReportViewPresetDb>();

				reportViewPresets.Where(x => x.ReportKey == reportKey && x.OwnerUserId == userId)
					.Set(x => x.IsDefault, false)
					.Update();

                reportViewPresets.Where(x => x.ReportKey == reportKey && x.PresetId == presetId && x.OwnerUserId == userId)
                    .Set(x => x.IsDefault, true)
                    .Update();
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
							Key = key == null || key is DBNull ? string.Empty : Convert.ToString(key) ?? string.Empty,
							Text = text == null || text is DBNull ? string.Empty : Convert.ToString(text) ?? string.Empty
                        });
					}
				}
			}
			return result;
		}

		private DataConnection GetDataConnection(SqlConnection connection)
		{
			var dataOptions = new DataOptions()
				.UseSqlServer()
                .UseConnectionString(connection.ConnectionString)
                .UseConnection(connection);

			return new DataConnection(dataOptions);
		}
	}
}
