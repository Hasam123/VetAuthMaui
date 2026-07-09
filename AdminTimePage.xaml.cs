namespace VetAuthMaui;

using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

// расписание
public partial class AdminTimePage : ContentPage
{
	private HttpClient httpClient = new HttpClient();
	private List<Day> days = new List<Day>();
	private List<Request> requests = new List<Request>();
	private ObservableCollection<Slot> slots = new ObservableCollection<Slot>();

	public AdminTimePage()
	{
		InitializeComponent();
		httpClient.Timeout = TimeSpan.FromSeconds(10);
		ScheduleCollectionView.ItemsSource = slots;
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

	private void Date_Changed(object sender, EventArgs e)
	{
		ShowDay();
	}

	private async Task LoadData()
	{
		try
		{
			StatusLabel.Text = "Загрузка расписания...";

			// запрос времени и заявок
			var task2 = httpClient.GetFromJsonAsync<TimeResult>($"{Api.BaseUrl}schedule/free_slots.php");
			var task3 = httpClient.GetFromJsonAsync<RequestResult>($"{Api.BaseUrl}schedule/get_zapis_admin.php");

			var res2 = await task2;
			var res3 = await task3;

			days.Clear();
			days.AddRange(res2?.Days ?? new List<Day>());

			requests.Clear();
			requests.AddRange((res3?.Requests ?? new List<Request>())
				.Where(request => !string.IsNullOrWhiteSpace(request.AppointmentAt)));

			DatePicker.ItemsSource = days;
			DatePicker.ItemDisplayBinding = new Binding("Label");

			if (DatePicker.SelectedIndex < 0 && days.Count > 0)
				DatePicker.SelectedIndex = 0;

			ShowDay();
		}
		catch (Exception ex)
		{
			StatusLabel.Text = "";
			await DisplayAlertAsync("Ошибка", $"Не удалось загрузить расписание: {ex.Message}", "ОК");
		}
	}

	private void ShowDay()
	{
		var day = DatePicker.SelectedItem as Day;
		slots.Clear();

		if (day == null)
		{
			StatusLabel.Text = "Нет дней для расписания.";
			return;
		}

		// показ слотов
		foreach (var slot in day.Slots)
		{
			var request = requests.FirstOrDefault(item => item.AppointmentAt == slot.Value);
			slots.Add(new Slot(slot, request));
		}

		// подсчет мест
		var busy = slots.Count(slot => slot.IsBusy);
		var free = slots.Count(slot => !slot.IsBusy && !slot.IsPast);
		StatusLabel.Text = $"Свободно: {free}, занято: {busy}";
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

	// результат заявок
	// заявка
	private class RequestResult
	{
		[JsonPropertyName("requests")]
		public List<Request> Requests { get; set; } = new List<Request>();
	}

	// заявка
	private class Request
	{
		[JsonPropertyName("id")] public int Id { get; set; }
		[JsonPropertyName("name")] public string Name { get; set; } = "";
		[JsonPropertyName("phone")] public string Phone { get; set; } = "";
		[JsonPropertyName("pet_name")] public string PetName { get; set; } = "";
		[JsonPropertyName("pet_type")] public string PetType { get; set; } = "";
		[JsonPropertyName("service_title")] public string ServiceTitle { get; set; } = "";
		[JsonPropertyName("appointment_at")] public string AppointmentAt { get; set; } = "";
		[JsonPropertyName("comment")] public string Comment { get; set; } = "";
		[JsonPropertyName("status")] public string Status { get; set; } = "";
	}

	// ячейка времени
	private class Slot
	{
		private Request request;

		public Slot(Time slot, Request request)
		{
			this.request = request;
			TimeLabel = slot.Label;
			IsBusy = request != null;
			IsPast = !slot.IsAvailable && request == null;
			RequestId = request?.Id ?? 0;
		}

		public string TimeLabel { get; }
		public int RequestId { get; }
		public bool IsBusy { get; }
		public bool IsPast { get; }

		public LayoutOptions ContentHorizontalOptions => IsBusy ? LayoutOptions.Start : LayoutOptions.Center;
		public TextAlignment TextAlignment => IsBusy ? TextAlignment.Start : TextAlignment.Center;
		public string StateText
		{
			get
			{
				if (IsBusy)
					return "Занято";
				if (IsPast)
					return "Недоступно";

				return "Свободно";
			}
		}

		public string CardColor
		{
			get
			{
				if (!IsBusy && !IsPast)
					return "#FFFFFF";
				if (IsPast)
					return "#F3F6FA";
				if (request.Status == "accepted")
					return "#FFF4EC";
				if (request.Status == "done")
					return "#EFFAF4";
				if (request.Status == "cancelled")
					return "#FFF0F0";

				return "#F1F8FD";
			}
		}

		public string BadgeTextColor
		{
			get
			{
				if (!IsBusy && !IsPast)
					return "#12A7A7";

				return "#FFFFFF";
			}
		}
		public string BadgeColor
		{
			get
			{
				if (!IsBusy && !IsPast)
					return "#FFFFFF";
				if (IsPast)
					return "#9AA8B8";
				if (request.Status == "accepted")
					return "#FF8A5B";
				if (request.Status == "done")
					return "#30B878";
				if (request.Status == "cancelled")
					return "#D9534F";

				return "#4AA3D8";
			}
		}

		public string BorderColor
		{
			get
			{
				if (!IsBusy && !IsPast)
					return "#12A7A7";
				if (IsPast)
					return "#DDEBF3";
				if (request.Status == "accepted")
					return "#FF8A5B";
				if (request.Status == "done")
					return "#30B878";
				if (request.Status == "cancelled")
					return "#D9534F";

				return "#4AA3D8";
			}
		}

		public string TextColor
		{
			get
			{
				if (!IsBusy && !IsPast)
					return "#12A7A7";
				if (IsPast)
					return "#657084";
				if (request.Status == "accepted")
					return "#FF8A5B";
				if (request.Status == "done")
					return "#30B878";
				if (request.Status == "cancelled")
					return "#D9534F";

				return "#4AA3D8";
			}
		}

		public string ClientInfo
		{
			get
			{
				if (request == null)
					return "";

				return $"{request.Name}, {request.Phone}";
			}
		}

		public string PetInfo
		{
			get
			{
				if (request == null)
					return "";
				if (string.IsNullOrWhiteSpace(request.PetName))
					return "Питомец не указан";

				return $"Питомец: {request.PetName}, {request.PetType}";
			}
		}

		public string ServiceInfo
		{
			get
			{
				if (request == null)
					return "";
				if (string.IsNullOrWhiteSpace(request.ServiceTitle))
					return "Услуга не выбрана";

				return $"Услуга: {request.ServiceTitle}";
			}
		}

		public string StatusText
		{
			get
			{
				if (request == null)
					return "";

				return $"Статус: {GetStatus(request.Status)}";
			}
		}

		private static string GetStatus(string status)
		{
			if (status == "new")
				return "новая";
			if (status == "accepted")
				return "принята";
			if (status == "done")
				return "выполнена";
			if (status == "cancelled")
				return "отменена";

			return status;
		}
	}
}




























