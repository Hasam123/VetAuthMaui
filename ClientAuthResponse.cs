namespace VetAuthMaui;

using System.Text.Json.Serialization;

// ответ входа
public class LoginResult
{
	[JsonPropertyName("success")] public bool Success { get; set; }
	[JsonPropertyName("message")] public string Message { get; set; } = "";
	[JsonPropertyName("client")] public Client Client { get; set; } = new Client();
}

// клиент
public class Client
{
	[JsonPropertyName("id")] public int Id { get; set; }
	[JsonPropertyName("name")] public string Name { get; set; } = "";
	[JsonPropertyName("phone")] public string Phone { get; set; } = "";
}













