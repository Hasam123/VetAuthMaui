namespace VetAuthMaui;

using System.Text.Json.Serialization;

public class PreviousVisits
{
    [JsonPropertyName("success")] public bool Success { get; set; }
    [JsonPropertyName("visits")] public List<PrimaryVisit> Visits { get; set; } = new();
}

public class PrimaryVisit : Appointment
{
    [JsonPropertyName("primary_at")] public string PrimaryAt { get; set; } = "";
    [JsonPropertyName("last_visit_id")] public int LastVisitId { get; set; }
    public string DisplayText => Id == 0 ? "Первичный приём / новое заболевание"
        : $"Повторный по приёму №{Id} · {(DateTime.TryParse(PrimaryAt, out var date) ? date.ToString("dd.MM.yyyy") : PrimaryAt)}";
}
