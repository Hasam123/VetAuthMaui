namespace VetAuthMaui;

using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

// расписание
public partial class AdminTimePage : ContentPage
{
	private HttpClient httpClient = new HttpClient();
	private List<ScheduleDay> days = new List<ScheduleDay>();
	private List<Appointment> appointments = new List<Appointment>();
	// Коллекция слотов отображается в расписании администратора.
	private ObservableCollection<Slot> slots = new ObservableCollection<Slot>();

	public AdminTimePage()
	{
		InitializeComponent();
		httpClient.Timeout = TimeSpan.FromSeconds(10);
		ScheduleCollectionView.ItemsSource = slots;
	}

	// Загружает данные при открытии страницы.
	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await LoadSchedule();
	}

	// Обрабатывает нажатие кнопки.
	private async void Refresh_Click(object sender, EventArgs e)
	{
		await LoadSchedule();
	}

	// Обрабатывает изменение выбранного значения.
	private void Date_Changed(object sender, EventArgs e)
	{
		ShowDay();
	}

	// Загружает интервалы времени и заявки, чтобы собрать расписание администратора.
	private async Task LoadSchedule()
	{
		try
		{
			StatusLabel.Text = "Загрузка расписания...";

			var scheduleResult =
				await httpClient.GetFromJsonAsync<TimeResult>(
					$"{Api.BaseUrl}schedule/free_slots.php");

			var appointmentsResult =
				await httpClient.GetFromJsonAsync<AppointmentResult>(
					$"{Api.BaseUrl}schedule/get_zapis_admin.php");

			days.Clear();
			days.AddRange(scheduleResult?.Days ?? new List<ScheduleDay>());

			appointments.Clear();
			appointments.AddRange((appointmentsResult?.Appointments ?? new List<Appointment>())
				.Where(appointment => !string.IsNullOrWhiteSpace(appointment.AppointmentAt)));

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

	// Показывает выбранный день и сопоставляет каждый временной слот с заявкой.
	private void ShowDay()
	{
		var day = DatePicker.SelectedItem as ScheduleDay;
		slots.Clear();

		if (day == null)
		{
			StatusLabel.Text = "Нет дней для расписания.";
			return;
		}

		// показ слотов
		foreach (var slot in day.Slots)
		{
			var appointment = appointments.FirstOrDefault(item => item.AppointmentAt == slot.Value);
			slots.Add(new Slot(slot, appointment));
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
		public List<ScheduleDay> Days { get; set; } = new List<ScheduleDay>();
	}

	// день
	private class ScheduleDay
	{
		[JsonPropertyName("date")] public string Date { get; set; } = "";
		[JsonPropertyName("label")] public string Label { get; set; } = "";
		[JsonPropertyName("slots")] public List<TimeSlot> Slots { get; set; } = new List<TimeSlot>();
	}

	// время
	private class TimeSlot
	{
		[JsonPropertyName("time")] public string Value { get; set; } = "";
		[JsonPropertyName("label")] public string Label { get; set; } = "";
		[JsonPropertyName("is_available")] public bool IsAvailable { get; set; }
	}

	// результат заявок
	// заявка
	private class AppointmentResult
	{
		[JsonPropertyName("requests")]
		public List<Appointment> Appointments { get; set; } = new List<Appointment>();
	}

	// ячейка времени
	private class Slot
	{
		private Appointment appointment;

		public Slot(TimeSlot slot, Appointment appointment)
		{
			this.appointment = appointment;
			TimeLabel = slot.Label;
			IsBusy = appointment != null;
			IsPast = !slot.IsAvailable && appointment == null;
			RequestId = appointment?.Id ?? 0;
		}

		public string TimeLabel { get; }
		public int RequestId { get; }
		public bool IsBusy { get; }
		public bool IsPast { get; }

		public LayoutOptions ContentHorizontalOptions
		{
			get
			{
				if (IsBusy)
					return LayoutOptions.Start;

				return LayoutOptions.Center;
			}
		}

		public TextAlignment TextAlignment
		{
			get
			{
				if (IsBusy)
					return TextAlignment.Start;

				return TextAlignment.Center;
			}
		}
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

		// Цвет карточки помогает быстро отличить свободные, занятые и отмененные записи.
		public string CardColor
		{
			get
			{
				if (!IsBusy && !IsPast)
					return "#FFFFFF";
				if (IsPast)
					return "#F3F6FA";
				if (appointment.Status == "accepted")
					return "#FFF4EC";
				if (appointment.Status == "done")
					return "#EFFAF4";
				if (appointment.Status == "cancelled")
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
		// Цвет бейджа времени зависит от состояния слота или статуса заявки.
		public string BadgeColor
		{
			get
			{
				if (!IsBusy && !IsPast)
					return "#FFFFFF";
				if (IsPast)
					return "#9AA8B8";
				if (appointment.Status == "accepted")
					return "#FF8A5B";
				if (appointment.Status == "done")
					return "#30B878";
				if (appointment.Status == "cancelled")
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
				if (appointment.Status == "accepted")
					return "#FF8A5B";
				if (appointment.Status == "done")
					return "#30B878";
				if (appointment.Status == "cancelled")
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
				if (appointment.Status == "accepted")
					return "#FF8A5B";
				if (appointment.Status == "done")
					return "#30B878";
				if (appointment.Status == "cancelled")
					return "#D9534F";

				return "#4AA3D8";
			}
		}

		// Данные клиента выводятся только у занятого слота.
		public string ClientInfo
		{
			get
			{
				if (appointment == null)
					return "";

				return $"{appointment.Name}, {appointment.Phone}";
			}
		}

		public string PetInfo
		{
			get
			{
				if (appointment == null)
					return "";
				if (string.IsNullOrWhiteSpace(appointment.PetName))
					return "Питомец не указан";

				return $"Питомец: {appointment.PetName}, {appointment.PetType}";
			}
		}

		public string ServiceInfo
		{
			get
			{
				if (appointment == null)
					return "";
				if (string.IsNullOrWhiteSpace(appointment.ServiceTitle))
					return "Услуга не выбрана";

				return $"Услуга: {appointment.ServiceTitle}";
			}
		}

		public string StatusText
		{
			get
			{
				if (appointment == null)
					return "";

				return $"Статус: {GetStatus(appointment.Status)}";
			}
		}

		// Возвращает данные для отображения.
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





































