namespace VetAuthMaui;

using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

// список заявок
public partial class RequestPage : ContentPage
{
	private HttpClient httpClient = new HttpClient();
	private List<Request> allRequests = new List<Request>();
	private ObservableCollection<Request> requests = new ObservableCollection<Request>();
	private List<FilterItem> dates = new List<FilterItem>()
	{
		new FilterItem("Все даты", "all"),
		new FilterItem("Сегодня", "today"),
		new FilterItem("Завтра", "tomorrow"),
		new FilterItem("Ближайшие 5 дней", "next5"),
		new FilterItem("Без времени", "empty")
	};
	private List<FilterItem> statuses = new List<FilterItem>()
	{
		new FilterItem("Все статусы", ""),
		new FilterItem("Новые", "new"),
		new FilterItem("Принятые", "accepted"),
		new FilterItem("Выполненные", "done"),
		new FilterItem("Отмененные", "cancelled")
	};

	public RequestPage()
	{
		InitializeComponent();
		httpClient.Timeout = TimeSpan.FromSeconds(10);
		DateFilterPicker.ItemsSource = dates;
		DateFilterPicker.ItemDisplayBinding = new Binding("Name");
		DateFilterPicker.SelectedIndex = 0;
		StatusFilterPicker.ItemsSource = statuses;
		StatusFilterPicker.ItemDisplayBinding = new Binding("Name");
		StatusFilterPicker.SelectedIndex = 0;
		RequestsCollectionView.ItemsSource = requests;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await LoadData();
	}

	private async void Refresh_Click(object sender, EventArgs e)
	{
		await LoadData();
	}

	private void Filter_Changed(object sender, EventArgs e)
	{
		Filter();
	}

	private async void Status_Click(object sender, EventArgs e)
	{
		// выбранная заявка
		Button button = (Button)sender;
		Request request = (Request)button.BindingContext;

		// выбор нового статуса
		var selected = await DisplayActionSheetAsync(
			"Изменить статус",
			"Отмена",
			null,
			"Новая",
			"Принята",
			"Выполнена",
			"Отменена");

		var text = "";
		if (selected == "Новая")
			text = "new";
		if (selected == "Принята")
			text = "accepted";
		if (selected == "Выполнена")
			text = "done";
		if (selected == "Отменена")
			text = "cancelled";

		if (text == "")
			return;

		await SaveStatusData(request, text);
	}

	private async Task SaveStatusData(Request request, string text)
	{
		try
		{
			// отправка статуса
			var response = await httpClient.PostAsJsonAsync(
				$"{Api.BaseUrl}appointments/update_status.php",
				new StatusData(request.Id, text));

			var result = await response.Content.ReadFromJsonAsync<ApiResult>();

			if (!response.IsSuccessStatusCode || result?.Success != true)
			{
				await DisplayAlertAsync("Ошибка", result?.Message ?? "Статус не изменен.", "ОК");
				return;
			}

			await LoadData();
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Ошибка", $"Ошибка изменения статуса: {ex.Message}", "ОК");
		}
	}

	private async void Comment_Click(object sender, EventArgs e)
	{
		Button button = (Button)sender;
		Request request = (Request)button.BindingContext;

		var text = await DisplayPromptAsync(
			"Комментарий администратора",
			"Этот комментарий увидит клиент в личном кабинете.",
			"Сохранить",
			"Отмена",
			"Например: приходите за 10 минут до приема",
			initialValue: request.AdminComment);

		if (text == null)
			return;

		await SaveCommentData(request, text.Trim());
	}

	private async Task SaveCommentData(Request request, string comment)
	{
		try
		{
			var response = await httpClient.PostAsJsonAsync(
				$"{Api.BaseUrl}appointments/update_admin_comment.php",
				new CommentData(request.Id, comment));

			var result = await response.Content.ReadFromJsonAsync<ApiResult>();

			if (!response.IsSuccessStatusCode || result?.Success != true)
			{
				await DisplayAlertAsync("Ошибка", result?.Message ?? "Комментарий не сохранен.", "ОК");
				return;
			}

			await LoadData();
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Ошибка", $"Ошибка сохранения комментария: {ex.Message}", "ОК");
		}
	}

	private async void Med_Click(object sender, EventArgs e)
	{
		Button button = (Button)sender;
		Request request = (Request)button.BindingContext;

		// подготовка медкарты
		State.CurrentMedicalRecord = new Record
		{
			RequestId = request.Id,
			ClientName = request.Name ?? "",
			PetName = request.PetName ?? "",
			AppointmentText = request.TimeText ?? "",
			Jaloba = request.Jaloba ?? "",
			Diagnoz = request.Diagnoz ?? "",
			ObsledResult = request.ObsledResult ?? "",
			NazLech = request.NazLech ?? "",
			ProcedureDone = request.ProcedureDone ?? "",
			TreatmentNotes = request.TreatmentNotes ?? ""
		};

		await Shell.Current.GoToAsync("MedicalRecordPage");
	}

	private async void Delete_Click(object sender, EventArgs e)
	{
		// удаление заявки
		Button button = (Button)sender;
		Request request = (Request)button.BindingContext;

		var ok = await DisplayAlertAsync(
			"Удалить заявку?",
			$"Заявка от {request.Name} будет удалена.",
			"Удалить",
			"Отмена");

		if (!ok)
			return;

		try
		{
			var response = await httpClient.PostAsJsonAsync(
				$"{Api.BaseUrl}appointments/delete.php",
				new RequestData(request.Id));

			var result = await response.Content.ReadFromJsonAsync<ApiResult>();

			if (!response.IsSuccessStatusCode || result?.Success != true)
			{
				await DisplayAlertAsync("Ошибка", result?.Message ?? "Не удалось удалить заявку.", "ОК");
				return;
			}

			requests.Remove(request);
			ShowCountText();
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Ошибка", $"Ошибка удаления: {ex.Message}", "ОК");
		}
	}

	private async Task LoadData()
	{
		try
		{
			// загрузка заявок
			StatusLabel.Text = "Загрузка заявок...";

			var response = await httpClient.GetFromJsonAsync<RequestResult>($"{Api.BaseUrl}appointments/list.php");
			allRequests.Clear();

			foreach (var item in response?.Requests ?? new List<Request>())
				allRequests.Add(item);

			Filter();
		}
		catch (Exception ex)
		{
			StatusLabel.Text = "";
			await DisplayAlertAsync("Ошибка", $"Не удалось загрузить заявки: {ex.Message}", "ОК");
		}
	}

	private void ShowCountText()
	{
		if (requests.Count == 0)
			StatusLabel.Text = "Заявок по фильтру нет.";
		else
			StatusLabel.Text = $"Показано: {requests.Count} из {allRequests.Count}";
	}

	private void Filter()
	{
		// фильтр заявок
		var dateFilter = DateFilterPicker.SelectedItem as FilterItem;
		var statusFilter = StatusFilterPicker.SelectedItem as FilterItem;
		var today = DateTime.Today;
		var tomorrow = today.AddDays(1);
		var day2 = today.AddDays(4);

		requests.Clear();

		foreach (var request in allRequests)
		{
			var statusOk = string.IsNullOrWhiteSpace(statusFilter?.Value) || request.Status == statusFilter.Value;
			var date = request.AppointmentDate;
			var dateOk = true;
			var filter = dateFilter?.Value ?? "all";

			if (filter == "today")
				dateOk = date.Date == today;
			if (filter == "tomorrow")
				dateOk = date.Date == tomorrow;
			if (filter == "next5")
				dateOk = date != DateTime.MinValue && date.Date >= today && date.Date <= day2;
			if (filter == "empty")
				dateOk = date == DateTime.MinValue;

			if (statusOk && dateOk)
				requests.Add(request);
		}

		ShowCountText();
	}

	// пункт фильтра
	private class FilterItem
	{
		public string Name { get; set; }
		public string Value { get; set; }

		public FilterItem(string name, string value)
		{
			Name = name;
			Value = value;
		}
	}

	// данные заявки
	// заявка
	private class RequestData
	{
		[JsonPropertyName("id")]
		public int Id { get; set; }

		public RequestData(int id)
		{
			Id = id;
		}
	}

	// данные статуса
	private class StatusData
	{
		[JsonPropertyName("id")]
		public int Id { get; set; }

		[JsonPropertyName("status")]
		public string Status { get; set; }

		public StatusData(int id, string status)
		{
			Id = id;
			Status = status;
		}
	}

	// данные комментария
	private class CommentData
	{
		[JsonPropertyName("id")]
		public int Id { get; set; }

		[JsonPropertyName("admin_comment")]
		public string AdminComment { get; set; }

		public CommentData(int id, string adminComment)
		{
			Id = id;
			AdminComment = adminComment;
		}
	}
	// результат заявок
	// заявка
	private class RequestResult
	{
		[JsonPropertyName("requests")]
		public List<Request> Requests { get; set; } = new List<Request>();
	}

	// ответ API
	private class ApiResult
	{
		[JsonPropertyName("success")] public bool Success { get; set; }
		[JsonPropertyName("message")] public string Message { get; set; } = "";
	}

	// заявка
	private class Request
	{
		[JsonPropertyName("id")] public int Id { get; set; }
		[JsonPropertyName("name")] public string Name { get; set; } = "";
		[JsonPropertyName("phone")] public string Phone { get; set; } = "";
		[JsonPropertyName("comment")] public string Comment { get; set; } = "";
		[JsonPropertyName("admin_comment")] public string AdminComment { get; set; } = "";
		[JsonPropertyName("created")] public string Created { get; set; } = "";
		[JsonPropertyName("pet_name")] public string PetName { get; set; } = "";
		[JsonPropertyName("pet_type")] public string PetType { get; set; } = "";
		[JsonPropertyName("pet_age")] public string PetAge { get; set; } = "";
		[JsonPropertyName("service_title")] public string ServiceTitle { get; set; } = "";
		[JsonPropertyName("appointment_at")] public string AppointmentAt { get; set; } = "";
		[JsonPropertyName("jaloba")] public string Jaloba { get; set; } = "";
		[JsonPropertyName("diagnoz")] public string Diagnoz { get; set; } = "";
		[JsonPropertyName("obsled_result")] public string ObsledResult { get; set; } = "";
		[JsonPropertyName("naz_lech")] public string NazLech { get; set; } = "";
		[JsonPropertyName("procedure_done")] public string ProcedureDone { get; set; } = "";
		[JsonPropertyName("treatment_notes")] public string TreatmentNotes { get; set; } = "";

		[JsonPropertyName("status")] public string Status { get; set; } = "";

		public string PetInfo
		{
			get
			{
				var age = "";
				if (PetAge != "")
					age = $", {PetAge}";

				if (PetName == "")
					return "Питомец не указан";

				return $"Питомец: {PetName}, {PetType}{age}";
			}
		}

		public string ServiceInfo
		{
			get
			{
				if (string.IsNullOrWhiteSpace(ServiceTitle))
					return "Услуга не выбрана";

				return $"Услуга: {ServiceTitle}";
			}
		}

		public string TimeText
		{
			get
			{
				if (string.IsNullOrWhiteSpace(AppointmentAt))
					return "Время не выбрано";

				return $"Запись: {FormatDate(AppointmentAt)}";
			}
		}

		public string AdminText
		{
			get
			{
				if (string.IsNullOrWhiteSpace(AdminComment))
					return "Комментарий администратора: нет";

				return $"Комментарий администратора: {AdminComment}";
			}
		}

		public string MedText
		{
			get
			{
				if (string.IsNullOrWhiteSpace(Diagnoz) && string.IsNullOrWhiteSpace(NazLech))
					return "Медицинская запись: нет";

				var parts = new List<string>();

				if (!string.IsNullOrWhiteSpace(Jaloba))
					parts.Add($"Жалоба: {Jaloba}");
				if (!string.IsNullOrWhiteSpace(Diagnoz))
					parts.Add($"Диагноз: {Diagnoz}");
				if (!string.IsNullOrWhiteSpace(ObsledResult))
					parts.Add($"Результат: {ObsledResult}");
				if (!string.IsNullOrWhiteSpace(NazLech))
					parts.Add($"Лечение: {NazLech}");
				if (!string.IsNullOrWhiteSpace(ProcedureDone))
					parts.Add($"Сделано: {ProcedureDone}");
				if (!string.IsNullOrWhiteSpace(TreatmentNotes))
					parts.Add($"Заметки: {TreatmentNotes}");

				return string.Join("\n", parts);
			}
		}

		public string CreatedText
		{
			get
			{
				if (string.IsNullOrWhiteSpace(Created))
					return "";

				return $"Создана: {FormatDate(Created)}";
			}
		}

		public DateTime AppointmentDate => GetDate(AppointmentAt);

		public string StatusText
		{
			get
			{
				if (Status == "new")
					return "Статус: новая";
				if (Status == "accepted")
					return "Статус: принята";
				if (Status == "done")
					return "Статус: выполнена";
				if (Status == "cancelled")
					return "Статус: отменена";

				return $"Статус: {Status}";
			}
		}

		public string Color
		{
			get
			{
				if (Status == "new")
					return "#009EDB";
				if (Status == "accepted")
					return "#F39C12";
				if (Status == "done")
					return "#27AE60";
				if (Status == "cancelled")
					return "#8E8E8E";

				return "#646464";
			}
		}

		private static DateTime GetDate(string value)
		{
			if (DateTime.TryParse(value, out var date))
				return date;

			return DateTime.MinValue;
		}

		private static string FormatDate(string value)
		{
			var date = GetDate(value);
						if (date == DateTime.MinValue)
				return value;

			return date.ToString("dd.MM.yyyy, HH:mm");
		}
	}
}



































