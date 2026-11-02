namespace VetAuthMaui;

using System.Text.Json.Serialization;

public class PerformedService
{
    [JsonPropertyName("service_id")] public int ServiceId { get; set; }
    [JsonPropertyName("service_name")] public string Name { get; set; } = "";
    [JsonPropertyName("service_category")] public string Category { get; set; } = "";
    [JsonPropertyName("kol")] public int Quantity { get; set; }
    [JsonPropertyName("total_price")] public int TotalPrice { get; set; }
    public string DisplayText => $"{Name} × {Quantity} - {TotalPrice:N0} руб.";
}

public class VisitServicesResult
{
    [JsonPropertyName("success")] public bool Success { get; set; }
    [JsonPropertyName("message")] public string Message { get; set; } = "";
    [JsonPropertyName("status")] public string Status { get; set; } = "";
    [JsonPropertyName("primary_service_id")] public int PrimaryServiceId { get; set; }
    [JsonPropertyName("performed_services")] public List<PerformedService> Services { get; set; } = new();
}
