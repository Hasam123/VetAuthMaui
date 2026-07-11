namespace VetAuthMaui;

using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

[QueryProperty(nameof(InitialStatus), "status")]
public partial class RequestPage : ContentPage
{
	private readonly HttpClient httpClient = new HttpClient();
	private readonly List<Appointment> allAppointments = new List<Appointment>();
	private readonly ObservableCollection<Appointment> appointments = new ObservableCollection<Appointment>();
	private string initialStatus = "";

	private readonly List<FilterItem> dates = new List<FilterItem>
	{
		new FilterItem("Все даты", "all"),
		new FilterItem("Сегодня", "today"),
		new FilterItem("Завтра", "tomorrow"),
		new FilterItem("Ближайшие 5 дней", "next5"),
		new FilterItem("Без времени", "empty")
	};

	private readonly List<FilterItem> statuses = new List<FilterItem>
	{
		new FilterItem("Все статусы", ""),
		new FilterItem("Новые", "new"),
		new FilterItem("Принятые", "accepted"),
		new FilterItem("Выполненные", "done"),
		new FilterItem("Отмененные", "cancelled")
	};

	public string InitialStatus
	{
		get
		{
			return initialStatus;
		}
		set
		{
			initialStatus = value ?? "";
			ApplyInitialStatus();
		}
	}

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
		RequestsCollectionView.ItemsSource = appointments;
		ApplyInitialStatus();
	}

	// Загружает данные при открытии страницы.
	protected override async void OnAppearing()
	{
		base.OnAppearing();
		ApplyInitialStatus();
		await LoadAppointments();
	}

	// Обрабатывает нажатие кнопки.
	private async void Refresh_Click(object sender, EventArgs e)
	{
		ApplyInitialStatus();
		await LoadAppointments();
	}

	// Обрабатывает изменение выбранного значения.
	private void Filter_Changed(object sender, EventArgs e)
	{
		Filter();
	}

	// Устанавливает статус, переданный при переходе из панели администратора.
	private void ApplyInitialStatus()
	{
		if (StatusFilterPicker == null)
			return;

		var index = statuses.FindIndex(item => item.Value == initialStatus);
		if (index >= 0 && StatusFilterPicker.SelectedIndex != index)
			StatusFilterPicker.SelectedIndex = index;
	}

	// Обрабатывает нажатие кнопки.
	private async void Status_Click(object sender, EventArgs e)
	{
		var button = (Button)sender;
		var appointment = (Appointment)button.BindingContext;

		var selected = await DisplayActionSheetAsync(
			"Изменить статус",
			"Отмена",
			null,
			"Новая",
			"Принята",
			"Выполнена",
			"Отменена");

		var status = "";
		if (selected == "Новая")
			status = "new";
		if (selected == "Принята")
			status = "accepted";
		if (selected == "Выполнена")
			status = "done";
		if (selected == "Отменена")
			status = "cancelled";

		if (status == "")
			return;

		await SaveStatusData(appointment, status);
	}

	// Сохраняет измененные данные.
	private async Task SaveStatusData(Appointment appointment, string status)
	{
		try
		{
			var response = await httpClient.PostAsJsonAsync(
				$"{Api.BaseUrl}appointments/update_status.php",
				new StatusData(appointment.Id, status));

			var result = await response.Content.ReadFromJsonAsync<ApiResult>();

			if (!response.IsSuccessStatusCode || result?.Success != true)
			{
				await DisplayAlertAsync("Ошибка", result?.Message ?? "Статус не изменен.", "ОК");
				return;
			}

			ApplyInitialStatus();
			await LoadAppointments();
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Ошибка", $"Ошибка изменения статуса: {ex.Message}", "ОК");
		}
	}

	// Обрабатывает нажатие кнопки.
	private async void Comment_Click(object sender, EventArgs e)
	{
		var button = (Button)sender;
		var appointment = (Appointment)button.BindingContext;

		var text = await DisplayPromptAsync(
			"Комментарий администратора",
			"Этот комментарий увидит клиент в личном кабинете.",
			"Сохранить",
			"Отмена",
			"Например: приходите за 10 минут до приема",
			initialValue: appointment.AdminComment);

		if (text == null)
			return;

		await SaveCommentData(appointment, text.Trim());
	}

	// Сохраняет измененные данные.
	private async Task SaveCommentData(Appointment appointment, string comment)
	{
		try
		{
			var response = await httpClient.PostAsJsonAsync(
				$"{Api.BaseUrl}appointments/update_admin_comment.php",
				new CommentData(appointment.Id, comment));

			var result = await response.Content.ReadFromJsonAsync<ApiResult>();

			if (!response.IsSuccessStatusCode || result?.Success != true)
			{
				await DisplayAlertAsync("Ошибка", result?.Message ?? "Комментарий не сохранен.", "ОК");
				return;
			}

			ApplyInitialStatus();
			await LoadAppointments();
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Ошибка", $"Ошибка сохранения комментария: {ex.Message}", "ОК");
		}
	}

	// Обрабатывает нажатие кнопки.
	private async void Med_Click(object sender, EventArgs e)
	{
		var button = (Button)sender;
		var appointment = (Appointment)button.BindingContext;

		State.CurrentMedicalRecord = new MedicalRecord
		{
			AppointmentId = appointment.Id,
			ClientName = appointment.Name ?? "",
			PetName = appointment.PetName ?? "",
			AppointmentText = appointment.TimeText ?? "",
			Jaloba = appointment.Jaloba ?? "",
			Diagnoz = appointment.Diagnoz ?? "",
			ObsledResult = appointment.ObsledResult ?? "",
			NazLech = appointment.NazLech ?? "",
			ProcedureDone = appointment.ProcedureDone ?? "",
			TreatmentNotes = appointment.TreatmentNotes ?? ""
		};

		await Shell.Current.GoToAsync("MedicalRecordPage");
	}

	// Обрабатывает нажатие кнопки.
	private async void Delete_Click(object sender, EventArgs e)
	{
		var button = (Button)sender;
		var appointment = (Appointment)button.BindingContext;

		var ok = await DisplayAlertAsync(
			"Удалить заявку?",
			$"Заявка от {appointment.Name} будет удалена.",
			"Удалить",
			"Отмена");

		if (!ok)
			return;

		try
		{
			var response = await httpClient.PostAsJsonAsync(
				$"{Api.BaseUrl}appointments/delete.php",
				new AppointmentIdData(appointment.Id));

			var result = await response.Content.ReadFromJsonAsync<ApiResult>();

			if (!response.IsSuccessStatusCode || result?.Success != true)
			{
				await DisplayAlertAsync("Ошибка", result?.Message ?? "Не удалось удалить заявку.", "ОК");
				return;
			}

			appointments.Remove(appointment);
			ShowCountText();
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Ошибка", $"Ошибка удаления: {ex.Message}", "ОК");
		}
	}

	// Загружает данные для страницы.
	private async Task LoadAppointments()
	{
		try
		{
			StatusLabel.Text = "Загрузка заявок...";

			var response = await httpClient.GetFromJsonAsync<AppointmentResult>($"{Api.BaseUrl}appointments/list.php");
			allAppointments.Clear();

			foreach (var item in response?.Appointments ?? new List<Appointment>())
				allAppointments.Add(item);

			Filter();
		}
		catch (Exception ex)
		{
			StatusLabel.Text = "";
			await DisplayAlertAsync("Ошибка", $"Не удалось загрузить заявки: {ex.Message}", "ОК");
		}
	}

	// Показывает количество заявок после применения фильтра.
	private void ShowCountText()
	{
		if (appointments.Count == 0)
			StatusLabel.Text = "Заявок по фильтру нет.";
		else
			StatusLabel.Text = $"Показано: {appointments.Count} из {allAppointments.Count}";
	}

	// Фильтрует список по выбранным условиям.
	private void Filter()
	{
		var dateFilter = DateFilterPicker.SelectedItem as FilterItem;
		var statusFilter = StatusFilterPicker.SelectedItem as FilterItem;
		var today = DateTime.Today;
		var tomorrow = today.AddDays(1);
		var day2 = today.AddDays(4);

		appointments.Clear();

		foreach (var appointment in allAppointments)
		{
			var statusOk = string.IsNullOrWhiteSpace(statusFilter?.Value) || appointment.Status == statusFilter.Value;
			var date = appointment.AppointmentDate;
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
				appointments.Add(appointment);
		}

		ShowCountText();
	}

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

	private class AppointmentIdData
	{
		[JsonPropertyName("id")]
		public int Id { get; set; }

		public AppointmentIdData(int id)
		{
			Id = id;
		}
	}

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

	private class AppointmentResult
	{
		[JsonPropertyName("requests")]
		public List<Appointment> Appointments { get; set; } = new List<Appointment>();
	}

}
