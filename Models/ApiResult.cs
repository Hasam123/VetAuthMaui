namespace VetAuthMaui;

using System.Text.Json.Serialization;

public class ApiResult
{
	[JsonPropertyName("success")] public bool Success { get; set; }
	[JsonPropertyName("message")] public string Message { get; set; } = "";
}
