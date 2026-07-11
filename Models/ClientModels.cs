namespace VetAuthMaui;

using System.Text.Json.Serialization;

public class ClientResult
{
	[JsonPropertyName("client")]
	public ClientInfo Client { get; set; } = new ClientInfo();

	[JsonPropertyName("requests")]
	public List<Appointment> Appointments { get; set; } = new List<Appointment>();
}

public class ClientInfo
{
	[JsonPropertyName("name")]
	public string Name { get; set; } = "";

	[JsonPropertyName("phone")]
	public string Phone { get; set; } = "";
}
