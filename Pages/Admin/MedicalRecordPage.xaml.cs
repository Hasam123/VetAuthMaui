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
    private bool primaryLoaded;

	public MedicalRecordPage()
	{
		InitializeComponent();
		httpClient.Timeout = TimeSpan.FromSeconds(10);
	}

	private int appointmentId;

	// Загружает данные при открытии страницы.
	protected override async void OnAppearing()
	{
		base.OnAppearing();

		// чтобы не заполнять второй раз
		var draft = State.CurrentMedicalRecord;
		if (draft != null && draft.AppointmentId == loadedAppointmentId && loadedDoctorId == State.AdminId)
			return;
		appointmentId = 0;
		loadedAppointmentId = 0;
        PreviousRecordCard.IsVisible = false;
        PreviousVisitLabel.Text = "";
        PreviousRecordLabel.Text = "";

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
		AppointmentLabel.Text = draft.AppointmentText + (string.IsNullOrWhiteSpace(draft.Address) ? "" : $"\nАдрес: {draft.Address}") + (string.IsNullOrWhiteSpace(draft.PetPol) ? "" : $"\nПол: {draft.PetPol}");
		JalobaEditor.Text = draft.Jaloba;
		DiagnozEditor.Text = draft.Diagnoz;
		NazLechEditor.Text = draft.NazLech;
		ProcedureDoneEditor.Text = draft.ProcedureDone;
		TreatmentNotesEditor.Text = draft.TreatmentNotes;
        PredDiagEditor.Text = draft.PredDiag;
        KlinPriznEditor.Text = draft.KlinPrizn;
        IshodEntry.Text = draft.Ishod;
        DiseaseDatePicker.MaximumDate = DateTime.Today;
        HasDiseaseDate.IsChecked = DateTime.TryParse(draft.DataZabol, out var illnessDate);
        DiseaseDatePicker.Date = HasDiseaseDate.IsChecked ? illnessDate : DateTime.Today;
        primaryLoaded = false;
        try {
            var response = await httpClient.GetFromJsonAsync<PreviousVisits>($"{Api.BaseUrl}appointments/previous.php?id={appointmentId}");
            if (response?.Success != true) throw new InvalidOperationException("Не удалось загрузить первичные приёмы");
            var visits = new List<PrimaryVisit> { new PrimaryVisit() };
            visits.AddRange(response.Visits ?? new List<PrimaryVisit>());
            PrimaryPicker.ItemsSource = visits;
            PrimaryPicker.SelectedItem = visits.FirstOrDefault(v => v.Id == (draft.PervPriemId ?? 0));
            primaryLoaded = true;
        } catch (Exception ex) {
            loadedAppointmentId = 0;
            await DisplayAlertAsync("Ошибка", ex.Message, "ОК");
        }
	}

    private void Primary_Changed(object sender, EventArgs e)
    {
        var visit = PrimaryPicker.SelectedItem as PrimaryVisit;
        PreviousRecordCard.IsVisible = visit?.Id > 0;
        PreviousVisitLabel.Text = visit?.Id > 0 ? $"Приём №{visit.LastVisitId}\n{visit.TimeText}\n{visit.DoctorInfo}\n{visit.ServiceInfo}" : "";
        PreviousRecordLabel.Text = visit?.Id > 0 ? visit.MedText : "";
    }

	// Сохраняет измененные данные.
	private async void Save_Click(object sender, EventArgs e)
	{
		if (saving) return;
        if (!primaryLoaded || PrimaryPicker.SelectedItem is not PrimaryVisit) { await DisplayAlertAsync("Ошибка", "Дождитесь загрузки и выберите тип приёма", "ОК"); return; }
		if (!State.IsAdminMode || State.AdminId <= 0 || State.AdminId != loadedDoctorId)
		{
			await DisplayAlertAsync("Ошибка", "Войдите в учётную запись врача заново.", "ОК");
			return;
		}
		if (string.IsNullOrWhiteSpace(JalobaEditor.Text))
		{
			await DisplayAlertAsync("Ошибка", "Заполните жалобы / причину обращения.", "ОК");
			return;
		}
        if (string.IsNullOrWhiteSpace(PredDiagEditor.Text) && string.IsNullOrWhiteSpace(DiagnozEditor.Text))
        {
            await DisplayAlertAsync("Ошибка", "Заполните предварительный или заключительный диагноз.", "ОК");
            return;
        }
        if (string.IsNullOrWhiteSpace(NazLechEditor.Text) && string.IsNullOrWhiteSpace(ProcedureDoneEditor.Text))
        {
            await DisplayAlertAsync("Ошибка", "Укажите назначенное лечение или выполненные процедуры.", "ОК");
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
					TreatmentNotesEditor.Text?.Trim() ?? "",
                    HasDiseaseDate.IsChecked ? DiseaseDatePicker.Date?.ToString("yyyy-MM-dd") ?? "" : "",
                    PredDiagEditor.Text?.Trim() ?? "", KlinPriznEditor.Text?.Trim() ?? "", IshodEntry.Text?.Trim() ?? "",
                    ((PrimaryVisit)PrimaryPicker.SelectedItem).Id == 0 ? null : ((PrimaryVisit)PrimaryPicker.SelectedItem).Id));

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
		        [JsonPropertyName("data_zabol")] public string DataZabol { get; set; }
        [JsonPropertyName("pred_diag")] public string PredDiag { get; set; }
        [JsonPropertyName("klin_prizn")] public string KlinPrizn { get; set; }
        [JsonPropertyName("ishod")] public string Ishod { get; set; }
        [JsonPropertyName("perv_priem_id")] public int? PervPriemId { get; set; }
        [JsonPropertyName("id")] public int Id { get; set; }
		[JsonPropertyName("jaloba")] public string Jaloba { get; set; }
		[JsonPropertyName("diagnoz")] public string Diagnoz { get; set; }
		[JsonPropertyName("naz_lech")] public string NazLech { get; set; }
		[JsonPropertyName("procedure_done")] public string ProcedureDone { get; set; }
		[JsonPropertyName("treatment_notes")] public string TreatmentNotes { get; set; }

		public RecordData(int id, string jaloba, string diagnoz, string nazLech, string procedureDone, string treatmentNotes, string dataZabol, string predDiag, string klinPrizn, string ishod, int? primary)
		{
			Id = id;
			Jaloba = jaloba;
			Diagnoz = diagnoz;
			NazLech = nazLech;
			ProcedureDone = procedureDone;
			TreatmentNotes = treatmentNotes;
            DataZabol = dataZabol; PredDiag = predDiag; KlinPrizn = klinPrizn; Ishod = ishod; PervPriemId = primary;
		}
	}

	private void DiseaseDate_Changed(object sender, CheckedChangedEventArgs e) => DiseaseDatePicker.IsEnabled = e.Value;
}




















