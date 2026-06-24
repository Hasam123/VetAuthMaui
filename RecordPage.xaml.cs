namespace VetAuthMaui;

using System.Net.Http.Json;
using System.Text.Json.Serialization;

// запись на прием
public partial class RecordPage : ContentPage
{
	private HttpClient httpClient = new HttpClient();
	private List<Service> services = new List<Service>();
	private List<Day> days = new List<Day>();

	public RecordPage()
	{
		InitializeComponent();
		httpClient.Timeout = TimeSpan.FromSeconds(10);
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		// заполнение клиента
		FillClient();
		await LoadData();
	}

	private void FillClient()
	{
		// проверка входа
		if (!State.IsClientLoggedIn)
			return;

		NameEntry.Text = State.ClientName;
		PhoneEntry.Text = State.ClientPhone;
	}

	private async Task LoadData()
	{
		try
		{
			// проверка входа
		if (!State.IsClientLoggedIn)
			{
				await Shell.Current.GoToAsync("ClientLogin");
				return;
			}

			// загрузка услуг и времени
			var task1 = httpClient.GetFromJsonAsync<ServiceResult>($"{Api.BaseUrl}services/list.php");
			var task2 = httpClient.GetFromJsonAsync<TimeResult>($"{Api.BaseUrl}schedule/free_slots.php");
			var res = await task1;
			var res2 = await task2;

			services.Clear();
			services.AddRange(res?.Services ?? new List<Service>());
			ServicePicker.ItemsSource = services;
			ServicePicker.ItemDisplayBinding = new Binding("Text");

			days.Clear();
			days.AddRange(res2?.Days ?? new List<Day>());
			DatePicker.ItemsSource = days;
			DatePicker.ItemDisplayBinding = new Binding("Label");

			// выбор первого значения
			if (ServicePicker.SelectedIndex < 0 && services.Count > 0)
				ServicePicker.SelectedIndex = 0;

			if (PetTypePicker.SelectedIndex < 0)
				PetTypePicker.SelectedIndex = 0;

			if (DatePicker.SelectedIndex < 0 && days.Count > 0)
			{
				var id = days.FindIndex(day => day.Slots.Any(slot => slot.IsAvailable));
								if (id >= 0)
					DatePicker.SelectedIndex = id;
				else
					DatePicker.SelectedIndex = 0;
			}

			UpdateTimePicker();
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Ошибка", $"Не удалось загрузить услуги и расписание: {ex.Message}", "ОК");
		}
	}

	private void Date_Changed(object sender, EventArgs e)
	{
		UpdateTimePicker();
	}

	private void UpdateTimePicker()
	{
		// список свободного времени
		var day = DatePicker.SelectedItem as Day;
		var list2 = day?.Slots
			.Where(slot => slot.IsAvailable)
			.ToList() ?? new List<Time>();

		TimePicker.ItemsSource = list2;
		TimePicker.ItemDisplayBinding = new Binding("Label");
				if (list2.Count > 0)
			TimePicker.SelectedIndex = 0;
		else
			TimePicker.SelectedIndex = -1;
	}

	private async void Send_Click(object sender, EventArgs e)
	{
		// данные из формы
		var name = NameEntry.Text?.Trim() ?? "";
		var phone = PhoneEntry.Text?.Trim() ?? "";
		var petName = PetNameEntry.Text?.Trim() ?? "";
		var petType = PetTypePicker.SelectedItem?.ToString() ?? "";
		var petAge = PetAgeEntry.Text?.Trim() ?? "";
		var comment = CommentEditor.Text?.Trim() ?? "";
		var service = ServicePicker.SelectedItem as Service;
		var time = TimePicker.SelectedItem as Time;

		if (name == "" || phone == "" || petName == "" || petType == "" || service == null || time == null || comment == "")
		{
			await DisplayAlertAsync("Ошибка", "Заполните имя, телефон, питомца, услугу, дату, время и комментарий.", "ОК");
			return;
		}

		try
		{
			// отправка заявки
			var data = new FeedbackData(name, phone, petName, petType, petAge, service.Id, time.Value, comment);
			var response = await httpClient.PostAsJsonAsync($"{Api.BaseUrl}appointments/create.php", data);
			var result = await response.Content.ReadFromJsonAsync<ApiResult>();

			if (!response.IsSuccessStatusCode || result?.Success != true)
			{
				await DisplayAlertAsync("Ошибка", result?.Message ?? "Заявка не отправлена.", "ОК");
				return;
			}

			Clear();
			await DisplayAlertAsync("Готово", result.Message, "ОК");
			await LoadData();
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Ошибка", $"Не удалось отправить заявку: {ex.Message}", "ОК");
		}
	}

	private void Clear()
	{
		// заполнение клиента
		FillClient();
		PetNameEntry.Text = "";
		PetTypePicker.SelectedIndex = 0;
		PetAgeEntry.Text = "";
				if (services.Count > 0)
			ServicePicker.SelectedIndex = 0;
		else
			ServicePicker.SelectedIndex = -1;
				if (days.Count > 0)
			DatePicker.SelectedIndex = 0;
		else
			DatePicker.SelectedIndex = -1;
		UpdateTimePicker();
		CommentEditor.Text = "";
	}

	// данные записи
	private class FeedbackData
	{
		[JsonPropertyName("name")] public string Name { get; set; }
		[JsonPropertyName("phone")] public string Phone { get; set; }
		[JsonPropertyName("pet_name")] public string PetName { get; set; }
		[JsonPropertyName("pet_type")] public string PetType { get; set; }
		[JsonPropertyName("pet_age")] public string PetAge { get; set; }
		[JsonPropertyName("service_id")] public int ServiceId { get; set; }
		[JsonPropertyName("appointment_at")] public string AppointmentAt { get; set; }
		[JsonPropertyName("comment")] public string Comment { get; set; }

		public FeedbackData(string name, string phone, string petName, string petType, string petAge, int serviceId, string appointmentAt, string comment)
		{
			Name = name;
			Phone = phone;
			PetName = petName;
			PetType = petType;
			PetAge = petAge;
			ServiceId = serviceId;
			AppointmentAt = appointmentAt;
			Comment = comment;
		}
	}

	// результат услуг
	// услуга
	private class ServiceResult
	{
		[JsonPropertyName("services")]
		public List<Service> Services { get; set; } = new List<Service>();
	}

	// услуга
	private class Service
	{
		[JsonPropertyName("id")] public int Id { get; set; }
		[JsonPropertyName("title")] public string Title { get; set; } = "";
		[JsonPropertyName("price")] public int Price { get; set; }

		public string Text => $"{Title} - {Price} руб.";
	}

	// результат времени
	// время
	private class TimeResult
	{
		[JsonPropertyName("days")]
		public List<Day> Days { get; set; } = new List<Day>();
	}

	// день
	private class Day
	{
		[JsonPropertyName("date")] public string Date { get; set; } = "";
		[JsonPropertyName("label")] public string Label { get; set; } = "";
		[JsonPropertyName("slots")] public List<Time> Slots { get; set; } = new List<Time>();
	}

	// время
	private class Time
	{
		[JsonPropertyName("time")] public string Value { get; set; } = "";
		[JsonPropertyName("label")] public string Label { get; set; } = "";
		[JsonPropertyName("is_available")] public bool IsAvailable { get; set; }
	}

	// ответ API
	private class ApiResult
	{
		[JsonPropertyName("success")] public bool Success { get; set; }
		[JsonPropertyName("message")] public string Message { get; set; } = "";
	}
}





























