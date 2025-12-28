using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace ReportManager.DefinitionModel.Json;

public static class JsonUtil
{
	public static readonly JsonSerializerSettings Settings = new()
	{
		Formatting = Formatting.Indented,
		NullValueHandling = NullValueHandling.Ignore,
		ContractResolver = new DefaultContractResolver
		{
			NamingStrategy = new CamelCaseNamingStrategy()
		},
		Converters =
		{
			new StringEnumConverter()
		}
	};

	public static string Serialize<T>(T value) => JsonConvert.SerializeObject(value, Settings);
	public static T? Deserialize<T>(string json) => JsonConvert.DeserializeObject<T>(json, Settings);
}
