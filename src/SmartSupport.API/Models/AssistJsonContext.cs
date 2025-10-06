using System.Text.Json.Serialization;

namespace SmartSupport.API.Models;

[JsonSourceGenerationOptions(WriteIndented = false, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(AssistResponse))]
[JsonSerializable(typeof(AssistRequestMetadata))]
public partial class AssistJsonContext : JsonSerializerContext
{
}


