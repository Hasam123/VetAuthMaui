namespace VetAuthMaui;

using System.Net.Http.Json;
using System.Text.Json.Serialization;

// медицинская карта
public partial class MedicalRecordPage : ContentPage
{
	private HttpClient httpClient = new HttpClient();
	private int loadedAppointmentId;
	private int loadedDoctorId;
	private bool saving;

	public MedicalRecordPage()
	{
		InitializeComponent();
		httpClient.Timeout = TimeSpan.FromSeconds(10);
	}

	private int appointmentId;

	// Загружает данные при открытии страницы.
	protected override void OnAppearing()
	{
		base.OnAppearing();

		// чтобы не заполнять второй раз
		var draft = State.CurrentMedicalRecord;
		if (draft != null && draft.AppointmentId == loadedAppointmentId && loadedDoctorId == State.AdminId)
			return;
		appointmentId = 0;
		loadedAppointmentId = 0;

		// если заявка не выбрана
		if (draft == null)
		{
			PatientLabel.Text = "Заявка не выбрана";
			AppointmentLabel.Text = "";
			return;
		}

		// заполнение формы
		appointmentId = draft.AppointmentId;
		loadedAppointmentId = appointmentId;
		loadedDoctorId = State.AdminId;
		PatientLabel.Text = $"{draft.ClientName} - {draft.PetName}";
		AppointmentLabel.Text = draft.AppointmentText;
		JalobaEditor.Text = draft.Jaloba;
		DiagnozEditor.Text = draft.Diagnoz;
		NazLechEditor.Text = draft.NazLech;
		ProcedureDoneEditor.Text = draft.ProcedureDone;
		TreatmentNotesEditor.Text = draft.TreatmentNotes;
	}

	// Сохраняет измененные данные.
	private async void Save_Click(object sender, EventArgs e)
	{
		if (saving) return;
		if (!State.IsAdminMode || State.AdminId <= 0 || State.AdminId != loadedDoctorId)
		{
			await DisplayAlertAsync("Ошибка", "Войдите в учётную запись врача заново.", "ОК");
			return;
		}
		if (new[] { JalobaEditor.Text, DiagnozEditor.Text,
			NazLechEditor.Text, ProcedureDoneEditor.Text, TreatmentNotesEditor.Text }
			.All(string.IsNullOrWhiteSpace))
		{
			await DisplayAlertAsync("Ошибка", "Заполните медицинскую запись.", "ОК");
			return;
		}
		// проверка заявки
		if (appointmentId <= 0)
		{
			await DisplayAlertAsync("Ошибка", "Не найдена заявка для сохранения.", "ОК");
			return;
		}

		try
		{
			saving = true;
			// отправка медкарты
			var response = await httpClient.PostAsJsonAsync(
				$"{Api.BaseUrl}appointments/update_medical_record.php",
				new RecordData(
					appointmentId,
					JalobaEditor.Text?.Trim() ?? "",
					DiagnozEditor.Text?.Trim() ?? "",
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
		finally { saving = false; }

	}

	// данные медкарты
	private class RecordData
	{
		[JsonPropertyName("id")] public int Id { get; set; }
		[JsonPropertyName("jaloba")] public string Jaloba { get; set; }
		[JsonPropertyName("diagnoz")] public string Diagnoz { get; set; }
		[JsonPropertyName("naz_lech")] public string NazLech { get; set; }
		[JsonPropertyName("procedure_done")] public string ProcedureDone { get; set; }
		[JsonPropertyName("treatment_notes")] public string TreatmentNotes { get; set; }

		public RecordData(int id, string jaloba, string diagnoz, string nazLech, string procedureDone, string treatmentNotes)
		{
			Id = id;
			Jaloba = jaloba;
			Diagnoz = diagnoz;
			NazLech = nazLech;
			ProcedureDone = procedureDone;
			TreatmentNotes = treatmentNotes;
		}
	}

	// ответ API
}




















