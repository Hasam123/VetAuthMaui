namespace VetAuthMaui;

using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

// кабинет клиента
public partial class ClientPage : ContentPage
{
	private HttpClient httpClient = new HttpClient();
	private ObservableCollection<Request> requests = new ObservableCollection<Request>();
	private string phone = "";

	public ClientPage()
	{
		InitializeComponent();
		httpClient.Timeout = TimeSpan.FromSeconds(10);
		RequestsCollectionView.ItemsSource = requests;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		// если клиент уже вошел
		if (State.IsClientLoggedIn)
		{
			PhoneEntry.Text = State.ClientPhone;
			await LoadData(State.ClientPhone);
		}
	}

	private async void Load_Click(object sender, EventArgs e)
	{
		// поиск по телефону
		var phone = PhoneEntry.Text?.Trim() ?? "";

		if (phone == "")
		{
			await DisplayAlertAsync("Ошибка", "Введите номер телефона.", "Понятно");
			return;
		}

		await LoadData(phone);
	}

	private async Task LoadData(string phone)
	{
		try
		{
			// загрузка кабинета
			this.phone = phone;
			StatusLabel.Text = "Загрузка личного кабинета...";
			ClientCard.IsVisible = false;
			requests.Clear();

			var url = $"{Api.BaseUrl}appointments/client_profile.php?phone={Uri.EscapeDataString(phone)}";
			var response = await httpClient.GetFromJsonAsync<ClientResult>(url);

			// показ заявок
			foreach (var request in response?.Requests ?? new List<Request>())
			{
				requests.Add(request);
			}

			ClientCard.IsVisible = true;
			ClientNameLabel.Text = $"Имя: {response?.Client.Name}";
			ClientPhoneLabel.Text = $"Телефон: {response?.Client.Phone}";

			if (requests.Count == 0)
			{
				LastStatusLabel.Text = "Заявок пока нет";
				StatusLabel.Text = "Заявок по этому телефону пока нет.";
				return;
			}

			LastStatusLabel.Text = $"Последняя заявка: {requests[0].StatusText}";
			StatusLabel.Text = $"Найдено заявок: {requests.Count}";
		}
		catch (Exception ex)
		{
			StatusLabel.Text = "";
			await DisplayAlertAsync("Ошибка", $"Не удалось загрузить кабинет: {ex.Message}", "Понятно");
		}
	}

	private async void Cancel_Click(object sender, EventArgs e)
	{
		// выбранная запись
		Button button = (Button)sender;
		Request request = (Request)button.BindingContext;

		// подтверждение отмены
		var ok = await DisplayAlertAsync(
			"Отменить запись?",
			$"Запись на {request.TimeText} будет отменена.",
			"Отменить",
			"Назад");

		if (!ok)
			return;

		try
		{
			// отправка отмены
			var response = await httpClient.PostAsJsonAsync(
				$"{Api.BaseUrl}appointments/cancel.php",
				new CancelData(request.Id, phone));

			var result = await response.Content.ReadFromJsonAsync<ApiResult>();

			if (!response.IsSuccessStatusCode || result?.Success != true)
			{
				await DisplayAlertAsync("Ошибка", result?.Message ?? "Не удалось отменить запись.", "Понятно");
				return;
			}

			await DisplayAlertAsync("Готово", result.Message, "ОК");
			Load_Click(this, EventArgs.Empty);
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Ошибка", $"Ошибка отмены записи: {ex.Message}", "Понятно");
		}
	}

	// результат кабинета
	// клиент
	private class ClientResult
	{
		[JsonPropertyName("client")]
		public Client Client { get; set; } = new Client();

		[JsonPropertyName("requests")]
		public List<Request> Requests { get; set; } = new List<Request>();
	}

	// клиент
	private class Client
	{
		[JsonPropertyName("name")]
		public string Name { get; set; } = "";

		[JsonPropertyName("phone")]
		public string Phone { get; set; } = "";
	}

	// данные отмены
	private class CancelData
	{
		[JsonPropertyName("id")]
		public int Id { get; set; }

		[JsonPropertyName("phone")]
		public string Phone { get; set; }

		public CancelData(int id, string phone)
		{
			Id = id;
			Phone = phone;
		}
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
		[JsonPropertyName("id")]
		public int Id { get; set; }

		[JsonPropertyName("comment")]
		public string Comment { get; set; } = "";

		[JsonPropertyName("admin_comment")]
		public string AdminComment { get; set; } = "";

		[JsonPropertyName("pet_name")]
		public string PetName { get; set; } = "";

		[JsonPropertyName("pet_type")]
		public string PetType { get; set; } = "";

		[JsonPropertyName("pet_age")]
		public string PetAge { get; set; } = "";

		[JsonPropertyName("service_title")]
		public string ServiceTitle { get; set; } = "";

		[JsonPropertyName("jaloba")]
		public string Jaloba { get; set; } = "";

		[JsonPropertyName("diagnoz")]
		public string Diagnoz { get; set; } = "";

		[JsonPropertyName("obsled_result")]
		public string ObsledResult { get; set; } = "";

		[JsonPropertyName("naz_lech")]
		public string NazLech { get; set; } = "";

		[JsonPropertyName("procedure_done")]
		public string ProcedureDone { get; set; } = "";

		[JsonPropertyName("treatment_notes")]
		public string TreatmentNotes { get; set; } = "";

		[JsonPropertyName("appointment_at")]
		public string AppointmentAt { get; set; } = "";

		[JsonPropertyName("created")]
		public string Created { get; set; } = "";

		[JsonPropertyName("status")]
		public string Status { get; set; } = "";

		public string StatusText
		{
			get
			{
				if (Status == "new")
					return "Новая";
				if (Status == "accepted")
					return "Принята";
				if (Status == "done")
					return "Выполнена";
				if (Status == "cancelled")
					return "Отменена";

				return Status;
			}
		}

		public string PetInfo
		{
			get
			{
				var age = "";
				if (!string.IsNullOrWhiteSpace(PetAge))
					age = $", {PetAge}";

				if (string.IsNullOrWhiteSpace(PetName))
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

		public bool CanCancel => Status == "new" || Status == "accepted";

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

		private static string FormatDate(string value)
		{
			if (DateTime.TryParse(value, out var date))
				return date.ToString("dd.MM.yyyy, HH:mm");

			return value;
		}
	}
}
































