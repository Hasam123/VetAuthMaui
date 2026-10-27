namespace VetAuthMaui;

using System.Net.Http.Json;
using System.Text.Json.Serialization;

public static class DoctorRecords
{
    public static async Task<List<Appointment>> LoadAsync(HttpClient client)
    {
        var doctorId = State.AdminId;
        if (!State.IsAdminMode || doctorId <= 0)
            throw new InvalidOperationException("Войдите в учётную запись врача заново.");

        var result = await client.GetFromJsonAsync<AppointmentResult>(
            $"{Api.BaseUrl}appointments/list.php?doctor_id={doctorId}");
        if (doctorId != State.AdminId || !State.IsAdminMode)
            throw new InvalidOperationException("Учётная запись врача изменилась.");
        if (result?.Success != true)
            throw new InvalidOperationException(result?.Message ?? "Не удалось загрузить заявки.");

        return (result.Appointments ?? new List<Appointment>())
            .Where(appointment => appointment.DoctorId == doctorId).ToList();
    }

    private class AppointmentResult
    {
        [JsonPropertyName("success")] public bool Success { get; set; }
        [JsonPropertyName("message")] public string Message { get; set; }
        [JsonPropertyName("requests")] public List<Appointment> Appointments { get; set; }
    }
}
