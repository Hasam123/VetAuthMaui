namespace VetAuthMaui;

using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Collections.ObjectModel;

// медицинская карта
public partial class MedicalRecordPage : ContentPage
{
	private HttpClient httpClient = new HttpClient();
	private int loadedAppointmentId;
	private int loadedDoctorId;
	private bool saving;
    private bool primaryLoaded;
    private bool servicesLoaded;
    private bool completed;
    private readonly ObservableCollection<PerformedService> performedServices = new();
    private List<CatalogService> catalog = new();

	public MedicalRecordPage()
	{
		InitializeComponent();
		BindableLayout.SetItemsSource(ServiceRows, performedServices);
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
        MedicalForm.IsEnabled = false;
        servicesLoaded = false;
        performedServices.Clear();
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
            var visit = await httpClient.GetFromJsonAsync<VisitServicesResult>($"{Api.BaseUrl}appointments/services.php?id={appointmentId}&doctor_id={loadedDoctorId}");
            if (visit?.Success != true) throw new InvalidOperationException(visit?.Message ?? "Не удалось загрузить услуги приёма");
            completed = visit.Status == "done";
            ServiceSelection.IsVisible = !completed;
            SaveButton.IsVisible = !completed && visit.Status != "cancelled";
            if (completed)
            {
                foreach (var service in visit.Services) performedServices.Add(service);
                CompletedMessageLabel.Text = performedServices.Count > 0
                    ? "Приём завершён. Медкарта и оказанные услуги доступны только для просмотра."
                    : "Приём завершён. Для старой записи отдельный список оказанных услуг отсутствует.";
                CompletedMessageLabel.IsVisible = true;
            }
            else
            {
                var result = await httpClient.GetFromJsonAsync<CatalogResult>($"{Api.BaseUrl}services/list.php");
                if (result?.Success != true) throw new InvalidOperationException("Не удалось загрузить каталог услуг");
                catalog = result.Services ?? new();
                ServicePicker.ItemsSource = catalog;
                var main = catalog.FirstOrDefault(s => s.Id == visit.PrimaryServiceId);
                if (main != null) performedServices.Add(ToPerformed(main, 1));
                ServicePicker.SelectedItem = main;
                CompletedMessageLabel.IsVisible = false;
            }
            if (State.CurrentMedicalRecord?.AppointmentId != appointmentId || State.AdminId != loadedDoctorId)
                throw new InvalidOperationException("Выбранный приём или учётная запись изменились. Откройте медкарту заново.");
            servicesLoaded = true;
            if (visit.Status == "cancelled")
            {
                CompletedMessageLabel.Text = "Запись отменена. Завершить приём нельзя.";
                CompletedMessageLabel.IsVisible = true;
            }
            UpdateServiceTotal();
            MedicalForm.IsEnabled = !completed && visit.Status != "cancelled";
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
        if (completed || !servicesLoaded || performedServices.Count == 0)
        {
            await DisplayAlertAsync("Ошибка", "Дождитесь загрузки и добавьте оказанные услуги.", "ОК");
            return;
        }
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

        var confirmed = await DisplayAlertAsync("Завершить приём?",
            $"Услуг: {performedServices.Sum(s => s.Quantity)}. Предварительная стоимость: {performedServices.Sum(s => (long)s.TotalPrice):N0} руб. После завершения изменить данные нельзя.",
            "Завершить", "Назад");
        if (!confirmed || saving || State.AdminId != loadedDoctorId) return;
		try
		{
			saving = true;
			MedicalForm.IsEnabled = false;
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
                    ((PrimaryVisit)PrimaryPicker.SelectedItem).Id == 0 ? null : ((PrimaryVisit)PrimaryPicker.SelectedItem).Id)
                    {
                        DoctorId = loadedDoctorId,
                        Services = performedServices.Select(s => new ServiceLine { ServiceId = s.ServiceId, Quantity = s.Quantity }).ToList()
                    });

			var result = await response.Content.ReadFromJsonAsync<ApiResult>();

			if (!response.IsSuccessStatusCode || result?.Success != true)
			{
				await DisplayAlertAsync("Ошибка", result?.Message ?? "Медицинская запись не сохранена.", "ОК");
				return;
			}

			await DisplayAlertAsync("Готово", result.Message, "ОК");
			State.CurrentMedicalRecord = null;
			await Shell.Current.GoToAsync("..");
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Ошибка", $"Ошибка сохранения медицинской записи: {ex.Message}", "ОК");
		}
		finally { saving = false; MedicalForm.IsEnabled = !completed; }

	}

	// данные медкарты
	private class RecordData
	{
		[JsonPropertyName("doctor_id")] public int DoctorId { get; set; }
		[JsonPropertyName("services")] public List<ServiceLine> Services { get; set; } = new();
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

    private static PerformedService ToPerformed(CatalogService service, int quantity) => new()
    {
        ServiceId = service.Id, Name = service.Title, Category = service.Category,
        Quantity = quantity, TotalPrice = checked(service.Price * quantity)
    };

    private void UpdateServiceTotal() => ServicesTotalLabel.Text = $"Итого: {performedServices.Sum(s => (long)s.TotalPrice):N0} руб.";

    private async void AddService_Click(object sender, EventArgs e)
    {
        if (completed || saving || ServicePicker.SelectedItem is not CatalogService service) return;
        if (!int.TryParse(QuantityEntry.Text, out var quantity) || quantity < 1 || quantity > 1000)
        { await DisplayAlertAsync("Ошибка", "Укажите количество от 1 до 1000", "ОК"); return; }
        var old = performedServices.FirstOrDefault(s => s.ServiceId == service.Id);
        var combined = quantity + (old?.Quantity ?? 0);
        if (combined > 1000 || (old == null && performedServices.Count >= 50)
            || performedServices.Sum(s => (long)s.TotalPrice) + (long)service.Price * quantity > int.MaxValue)
        { await DisplayAlertAsync("Ошибка", "Превышено допустимое количество или стоимость услуг", "ОК"); return; }
        if (old != null) performedServices.Remove(old);
        performedServices.Add(ToPerformed(service, combined));
        QuantityEntry.Text = "1"; UpdateServiceTotal();
    }

    private void RemoveService_Click(object sender, EventArgs e)
    {
        if (completed || saving) return;
        if ((sender as Button)?.BindingContext is PerformedService row) performedServices.Remove(row);
        UpdateServiceTotal();
    }

    private async void ServiceQuantity_Click(object sender, EventArgs e)
    {
        if (completed || saving || (sender as Button)?.BindingContext is not PerformedService row) return;
        var input = await DisplayPromptAsync("Количество", row.Name, "Сохранить", "Отмена",
            initialValue: row.Quantity.ToString(), keyboard: Keyboard.Numeric);
        if (input == null || completed || saving || !performedServices.Contains(row)) return;
        var service = catalog.FirstOrDefault(s => s.Id == row.ServiceId);
        if (service == null || !int.TryParse(input, out var quantity) || quantity < 1 || quantity > 1000
            || performedServices.Sum(s => (long)s.TotalPrice) - row.TotalPrice + (long)service.Price * quantity > int.MaxValue)
        { await DisplayAlertAsync("Ошибка", "Укажите допустимое количество от 1 до 1000", "ОК"); return; }
        var index = performedServices.IndexOf(row);
        performedServices[index] = ToPerformed(service, quantity); UpdateServiceTotal();
    }

    private class ServiceLine
    {
        [JsonPropertyName("service_id")] public int ServiceId { get; set; }
        [JsonPropertyName("kol")] public int Quantity { get; set; }
    }
    private class CatalogResult
    {
        [JsonPropertyName("success")] public bool Success { get; set; }
        [JsonPropertyName("services")] public List<CatalogService> Services { get; set; } = new();
    }
    private class CatalogService
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; } = "";
        [JsonPropertyName("category")] public string Category { get; set; } = "";
        [JsonPropertyName("price")] public int Price { get; set; }
        public string DisplayText => $"{Title} - {Price:N0} руб.";
    }
}




















