namespace VetAuthMaui;

using System.Text.Json.Serialization;

public class Doctor
{
	[JsonPropertyName("id")] public int Id { get; set; }
	[JsonPropertyName("name")] public string Name { get; set; } = "";
}
public class DoctorResult
{
	[JsonPropertyName("success")] public bool Success { get; set; }
	[JsonPropertyName("message")] public string Message { get; set; }
	[JsonPropertyName("doctors")] public List<Doctor> Doctors { get; set; } = new();
}
