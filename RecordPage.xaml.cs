namespace VetAuthMaui;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

// запись на прием
public partial class RecordPage : ContentPage
{
	private HttpClient httpClient = new HttpClient();
	private List<Service> services = new List<Service>();
	private List<Day> days = new List<Day>();
	private List<SavedPet> savedPets = new List<SavedPet>();
	// Коллекции привязаны к плиткам даты и времени на экране записи.
	private ObservableCollection<DayItem> showDays = new ObservableCollection<DayItem>();
	private ObservableCollection<TimeItem> showTimes = new ObservableCollection<TimeItem>();
	private Day selectedDay;
	private TimeItem selectedTime;

	public RecordPage()
	{
		InitializeComponent();
		httpClient.Timeout = TimeSpan.FromSeconds(10);
		DateCollectionView.ItemsSource = showDays;
		TimeCollectionView.ItemsSource = showTimes;
		SavedPetPicker.ItemsSource = savedPets;
		SavedPetPicker.ItemDisplayBinding = new Binding("Text");
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		FillClient();
		await LoadData();
	}

	private void FillClient()
	{
		if (!State.IsClientLoggedIn)
			return;

		NameEntry.Text = State.ClientName;
		PhoneEntry.Text = State.ClientPhone;
	}

	// Загружает услуги и свободные интервалы времени из API.
	private async Task LoadData()
	{
		try
		{
			if (!State.IsClientLoggedIn)
			{
				await Shell.Current.GoToAsync("ClientLogin");
				return;
			}

		var task1 = httpClient.GetFromJsonAsync<ServiceResult>($"{Api.BaseUrl}services/list.php");
		var task2 = httpClient.GetFromJsonAsync<TimeResult>($"{Api.BaseUrl}schedule/free_slots.php");
		var task3 = LoadSavedPets();
		var res = await task1;
		var res2 = await task2;
		await task3;

			services.Clear();
			services.AddRange(res?.Services ?? new List<Service>());
			ServicePicker.ItemsSource = services;
			ServicePicker.ItemDisplayBinding = new Binding("Text");

			days.Clear();
			days.AddRange(res2?.Days ?? new List<Day>());

			if (ServicePicker.SelectedIndex < 0 && services.Count > 0)
				ServicePicker.SelectedIndex = 0;

			if (PetTypePicker.SelectedIndex < 0)
				PetTypePicker.SelectedIndex = 0;

			selectedDay = days.FirstOrDefault(day => day.Slots.Any(slot => slot.IsAvailable)) ?? days.FirstOrDefault();
			BuildDays();
			BuildTimes();
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Ошибка", $"Не удалось загрузить услуги и расписание: {ex.Message}", "ОК");
		}
	}

	// Загружает питомцев из личного кабинета текущего клиента.
	private async Task LoadSavedPets()
	{
		if (!State.IsClientLoggedIn || string.IsNullOrWhiteSpace(State.ClientPhone))
			return;

		try
		{
			var url = $"{Api.BaseUrl}appointments/pets_list.php?phone={Uri.EscapeDataString(State.ClientPhone)}";
			var result = await httpClient.GetFromJsonAsync<SavedPetResult>(url);

			savedPets.Clear();
			savedPets.AddRange(result?.Pets ?? new List<SavedPet>());
			SavedPetPicker.ItemsSource = null;
			SavedPetPicker.ItemsSource = savedPets;
			SavedPetsBorder.IsVisible = savedPets.Count > 0;
			SavedPetPicker.SelectedIndex = -1;
		}
		catch
		{
			// Если список временно недоступен, запись вручную остается доступной.
			SavedPetsBorder.IsVisible = false;
		}
	}

	// Подставляет данные выбранного питомца в обычные поля формы.
	private void SavedPet_Changed(object sender, EventArgs e)
	{
		if (SavedPetPicker.SelectedItem is not SavedPet pet)
			return;

		PetNameEntry.Text = pet.Name;
		PetAgeEntry.Text = pet.Age;
		SetPetType(pet.Type);
	}

	private void SetPetType(string type)
	{
		if (string.IsNullOrWhiteSpace(type))
			return;

		for (var index = 0; index < PetTypePicker.Items.Count; index++)
		{
			if (string.Equals(PetTypePicker.Items[index], type, StringComparison.OrdinalIgnoreCase))
			{
				PetTypePicker.SelectedIndex = index;
				return;
			}
		}

		PetTypePicker.Items.Add(type);
		PetTypePicker.SelectedIndex = PetTypePicker.Items.Count - 1;
	}

	// Формирует плитки ближайших дней для выбора даты приема.
	private void BuildDays()
	{
		showDays.Clear();

		foreach (var day in days)
			showDays.Add(new DayItem(day, day == selectedDay));
	}

	// Формирует список времени для выбранного дня и автоматически выбирает первый свободный слот.
	private void BuildTimes()
	{
		showTimes.Clear();
		selectedTime = null;

		foreach (var slot in selectedDay?.Slots ?? new List<Time>())
		{
			var item = new TimeItem(slot, false);
			showTimes.Add(item);

			if (selectedTime == null && slot.IsAvailable)
			{
				selectedTime = item;
				item.IsSelected = true;
				item.RefreshStyle();
			}
		}
	}

	// Обрабатывает нажатие на дату и обновляет доступное время.
	private void Date_Tapped(object sender, TappedEventArgs e)
	{
		var view = (BindableObject)sender;
		var item = view.BindingContext as DayItem;

		if (item == null)
			return;

		selectedDay = item.Day;

		foreach (var day in showDays)
		{
			day.IsSelected = day.Day == selectedDay;
			day.RefreshStyle();
		}

		BuildTimes();
	}

	// Обрабатывает выбор времени; занятые интервалы выбрать нельзя.
	private void Time_Tapped(object sender, TappedEventArgs e)
	{
		var view = (BindableObject)sender;
		var item = view.BindingContext as TimeItem;

		if (item == null || !item.Time.IsAvailable)
			return;

		foreach (var time in showTimes)
		{
			time.IsSelected = false;
			time.RefreshStyle();
		}

		item.IsSelected = true;
		item.RefreshStyle();
		selectedTime = item;
	}

	// Проверяет форму и отправляет заявку на прием в базу через API.
	private async void Send_Click(object sender, EventArgs e)
	{
		var name = NameEntry.Text?.Trim() ?? "";
		var phone = PhoneEntry.Text?.Trim() ?? "";
		var petName = PetNameEntry.Text?.Trim() ?? "";
		var petType = PetTypePicker.SelectedItem?.ToString() ?? "";
		var petAge = PetAgeEntry.Text?.Trim() ?? "";
		var comment = CommentEditor.Text?.Trim() ?? "";
		var service = ServicePicker.SelectedItem as Service;
		var time = selectedTime?.Time;

		if (name == "" || phone == "" || petName == "" || petType == "" || service == null || time == null || comment == "")
		{
			await DisplayAlertAsync("Ошибка", "Заполните имя, телефон, питомца, услугу, дату, время и комментарий.", "ОК");
			return;
		}

		try
		{
			var data = new FeedbackData(name, phone, petName, petType, petAge, service.Id, time.Value, comment);
			var response = await httpClient.PostAsJsonAsync($"{Api.BaseUrl}appointments/create.php", data);
			var result = await response.Content.ReadFromJsonAsync<ApiResult>();

			if (!response.IsSuccessStatusCode || result?.Success != true)
			{
				await DisplayAlertAsync("Ошибка", result?.Message ?? "Заявка не отправлена.", "ОК");
				return;
			}

			Clear();
			await DisplayAlertAsync("Заявка отправлена", "Администратор проверит запись и изменит статус.", "ОК");
			await LoadData();
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Ошибка", $"Не удалось отправить заявку: {ex.Message}", "ОК");
		}
	}

	private void Clear()
	{
		FillClient();
		PetNameEntry.Text = "";
		PetTypePicker.SelectedIndex = 0;
		PetAgeEntry.Text = "";
		SavedPetPicker.SelectedIndex = -1;
		CommentEditor.Text = "";

		if (services.Count > 0)
			ServicePicker.SelectedIndex = 0;
		else
			ServicePicker.SelectedIndex = -1;

		selectedDay = days.FirstOrDefault(day => day.Slots.Any(slot => slot.IsAvailable)) ?? days.FirstOrDefault();
		BuildDays();
		BuildTimes();
	}

	private class SelectableItem : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	// Модель плитки даты: хранит текст и цвета выбранного/обычного состояния.
	private class DayItem : SelectableItem
	{
		public Day Day { get; }
		public bool IsSelected { get; set; }
		public string DayName { get; }
		public string DayNumber { get; }
		public Color BackgroundColor { get; private set; } = Color.FromArgb("#FFFFFF");
		public Color StrokeColor { get; private set; } = Color.FromArgb("#DDEBF3");
		public Color TextColor { get; private set; } = Color.FromArgb("#657084");

		public DayItem(Day day, bool isSelected)
		{
			Day = day;
			IsSelected = isSelected;

			var label = day.Label ?? "";
			var parts = label.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			DayName = parts.Length > 0 ? parts[0] : label;
			DayNumber = parts.Length > 1 ? parts[1] : "";

			RefreshStyle();
		}

		public void RefreshStyle()
		{
			if (IsSelected)
			{
				BackgroundColor = Color.FromArgb("#12A7A7");
				StrokeColor = Color.FromArgb("#12A7A7");
				TextColor = Color.FromArgb("#FFFFFF");
			}
			else
			{
				BackgroundColor = Color.FromArgb("#FFFFFF");
				StrokeColor = Color.FromArgb("#DDEBF3");
				TextColor = Color.FromArgb("#657084");
			}

			OnPropertyChanged(nameof(BackgroundColor));
			OnPropertyChanged(nameof(StrokeColor));
			OnPropertyChanged(nameof(TextColor));
			OnPropertyChanged(nameof(IsSelected));
		}
	}

	// Модель плитки времени: отвечает за цвета свободного, занятого и выбранного слота.
	private class TimeItem : SelectableItem
	{
		public Time Time { get; }
		public bool IsSelected { get; set; }
		public string Label => Time.Label;
		public Color BackgroundColor { get; private set; } = Color.FromArgb("#FFFFFF");
		public Color StrokeColor { get; private set; } = Color.FromArgb("#DDEBF3");
		public Color TextColor { get; private set; } = Color.FromArgb("#17213A");

		public TimeItem(Time time, bool isSelected)
		{
			Time = time;
			IsSelected = isSelected;
			RefreshStyle();
		}

		public void RefreshStyle()
		{
			if (!Time.IsAvailable)
			{
				BackgroundColor = Color.FromArgb("#EEF3F7");
				StrokeColor = Color.FromArgb("#EEF3F7");
				TextColor = Color.FromArgb("#9AA8B8");
			}
			else if (IsSelected)
			{
				BackgroundColor = Color.FromArgb("#12A7A7");
				StrokeColor = Color.FromArgb("#12A7A7");
				TextColor = Color.FromArgb("#FFFFFF");
			}
			else
			{
				BackgroundColor = Color.FromArgb("#FFFFFF");
				StrokeColor = Color.FromArgb("#DDEBF3");
				TextColor = Color.FromArgb("#17213A");
			}

			OnPropertyChanged(nameof(BackgroundColor));
			OnPropertyChanged(nameof(StrokeColor));
			OnPropertyChanged(nameof(TextColor));
			OnPropertyChanged(nameof(IsSelected));
		}
	}

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

	private class ServiceResult
	{
		[JsonPropertyName("services")]
		public List<Service> Services { get; set; } = new List<Service>();
	}

	private class SavedPetResult
	{
		[JsonPropertyName("pets")] public List<SavedPet> Pets { get; set; } = new List<SavedPet>();
	}

	private class SavedPet
	{
		[JsonPropertyName("id")] public int Id { get; set; }
		[JsonPropertyName("name")] public string Name { get; set; } = "";
		[JsonPropertyName("type")] public string Type { get; set; } = "";
		[JsonPropertyName("age")] public string Age { get; set; } = "";
		public string Text => string.IsNullOrWhiteSpace(Type) ? Name : $"{Name} - {Type}";
	}

	private class Service
	{
		[JsonPropertyName("id")] public int Id { get; set; }
		[JsonPropertyName("title")] public string Title { get; set; } = "";
		[JsonPropertyName("price")] public int Price { get; set; }
		public string Text => $"{Title} - {Price} руб.";
	}

	private class TimeResult
	{
		[JsonPropertyName("days")]
		public List<Day> Days { get; set; } = new List<Day>();
	}

	private class Day
	{
		[JsonPropertyName("date")] public string Date { get; set; } = "";
		[JsonPropertyName("label")] public string Label { get; set; } = "";
		[JsonPropertyName("slots")] public List<Time> Slots { get; set; } = new List<Time>();
	}

	private class Time
	{
		[JsonPropertyName("time")] public string Value { get; set; } = "";
		[JsonPropertyName("label")] public string Label { get; set; } = "";
		[JsonPropertyName("is_available")] public bool IsAvailable { get; set; }
	}

	private class ApiResult
	{
		[JsonPropertyName("success")] public bool Success { get; set; }
		[JsonPropertyName("message")] public string Message { get; set; } = "";
	}
}










