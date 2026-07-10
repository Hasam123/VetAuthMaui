namespace VetAuthMaui;

using System.Net.Http.Json;
using System.Text.Json.Serialization;

// медицинская карта
public partial class MedicalRecordPage : ContentPage
{
	private HttpClient httpClient = new HttpClient();
	private bool initialized;

	public MedicalRecordPage()
	{
		InitializeComponent();
		httpClient.Timeout = TimeSpan.FromSeconds(10);
	}

	private int requestId;

	protected override void OnAppearing()
	{
		base.OnAppearing();

		// чтобы не заполнять второй раз
		if (initialized)
			return;

		initialized = true;
		var draft = State.CurrentMedicalRecord;

		// если заявка не выбрана
		if (draft == null)
		{
			PatientLabel.Text = "Заявка не выбрана";
			AppointmentLabel.Text = "";
			return;
		}

		// заполнение формы
		requestId = draft.RequestId;
		PatientLabel.Text = $"{draft.ClientName} - {draft.PetName}";
		AppointmentLabel.Text = draft.AppointmentText;
		JalobaEditor.Text = draft.Jaloba;
		DiagnozEditor.Text = draft.Diagnoz;
		ObsledResultEditor.Text = draft.ObsledResult;
		NazLechEditor.Text = draft.NazLech;
		ProcedureDoneEditor.Text = draft.ProcedureDone;
		TreatmentNotesEditor.Text = draft.TreatmentNotes;
	}

	private async void Save_Click(object sender, EventArgs e)
	{
		// проверка заявки
		if (requestId <= 0)
		{
			await DisplayAlertAsync("Ошибка", "Не найдена заявка для сохранения.", "ОК");
			return;
		}

		try
		{
			// отправка медкарты
			var response = await httpClient.PostAsJsonAsync(
				$"{Api.BaseUrl}appointments/update_medical_record.php",
				new RecordData(
					requestId,
					JalobaEditor.Text?.Trim() ?? "",
					DiagnozEditor.Text?.Trim() ?? "",
					ObsledResultEditor.Text?.Trim() ?? "",
					NazLechEditor.Text?.Trim() ?? "",
					ProcedureDoneEditor.Text?.Trim() ?? "",
					TreatmentNotesEditor.Text?.Trim() ?? ""));

			var result = await response.Content.ReadFromJsonAsync<ApiResult>();

			if (!response.IsSuccessStatusCode || result?.Success != true)
			{
				await DisplayAlertAsync("Ошибка", result?.Message ?? "Медицинская запись не сохранена.", "ОК");
				return;
			}

			await DisplayAlertAsync("Готово", result.Message, "ОК");
			await Shell.Current.GoToAsync("..");
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Ошибка", $"Ошибка сохранения медицинской записи: {ex.Message}", "ОК");
		}
	}

	// данные медкарты
	private class RecordData
	{
		[JsonPropertyName("id")] public int Id { get; set; }
		[JsonPropertyName("jaloba")] public string Jaloba { get; set; }
		[JsonPropertyName("diagnoz")] public string Diagnoz { get; set; }
		[JsonPropertyName("obsled_result")] public string ObsledResult { get; set; }
		[JsonPropertyName("naz_lech")] public string NazLech { get; set; }
		[JsonPropertyName("procedure_done")] public string ProcedureDone { get; set; }
		[JsonPropertyName("treatment_notes")] public string TreatmentNotes { get; set; }

		public RecordData(int id, string jaloba, string diagnoz, string obsledResult, string nazLech, string procedureDone, string treatmentNotes)
		{
			Id = id;
			Jaloba = jaloba;
			Diagnoz = diagnoz;
			ObsledResult = obsledResult;
			NazLech = nazLech;
			ProcedureDone = procedureDone;
			TreatmentNotes = treatmentNotes;
		}
	}

	// ответ API
}




















