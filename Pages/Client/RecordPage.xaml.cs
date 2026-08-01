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
	private List<ScheduleDay> days = new List<ScheduleDay>();
	private List<Pet> savedPets = new List<Pet>();
	private Pet selectedPet;
	// Коллекции привязаны к плиткам даты и времени на экране записи.
	private ObservableCollection<DayItem> showDays = new ObservableCollection<DayItem>();
	private ObservableCollection<TimeItem> showTimes = new ObservableCollection<TimeItem>();
	private ScheduleDay selectedDay;
	private TimeItem selectedTime;

	public RecordPage()
	{
		InitializeComponent();
		httpClient.Timeout = TimeSpan.FromSeconds(10);
		DateCollectionView.ItemsSource = showDays;
		TimeCollectionView.ItemsSource = showTimes;
		SavedPetPicker.ItemsSource = savedPets;
		SavedPetPicker.ItemDisplayBinding = new Binding("PickerText");
	}

	// Загружает данные при открытии страницы.
	protected override async void OnAppearing()
	{
		base.OnAppearing();
		FillClient();
		await LoadAppointmentData();
	}

	// Подставляет имя и телефон авторизованного клиента в форму записи.
	private void FillClient()
	{
		if (!State.IsClientLoggedIn)
			return;

		NameEntry.Text = State.ClientName;
		PhoneEntry.Text = State.ClientPhone;
	}

	// Загружает услуги и свободные интервалы времени из API.
	private async Task LoadAppointmentData()
	{
		try
		{
			if (!State.IsClientLoggedIn)
			{
				await Shell.Current.GoToAsync("//MainPage");
				return;
			}

		var servicesResult =
			await httpClient.GetFromJsonAsync<ServiceResult>(
				$"{Api.BaseUrl}services/list.php");

		var scheduleResult =
			await httpClient.GetFromJsonAsync<TimeResult>(
				$"{Api.BaseUrl}schedule/free_slots.php");

		await LoadSavedPets();

			services.Clear();
			services.AddRange(servicesResult?.Services ?? new List<Service>());
			ServicePicker.ItemsSource = services;
			ServicePicker.ItemDisplayBinding = new Binding("Text");

			days.Clear();
			days.AddRange(scheduleResult?.Days ?? new List<ScheduleDay>());

			if (ServicePicker.SelectedIndex < 0 && services.Count > 0)
				ServicePicker.SelectedIndex = 0;

			if (PetTypePicker.SelectedIndex < 0)
				PetTypePicker.SelectedIndex = 0;

			selectedDay = FindFirstAvailableDay();
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
			var url = $"{Api.BaseUrl}pets/list.php?phone={Uri.EscapeDataString(State.ClientPhone)}";
			var result = await httpClient.GetFromJsonAsync<PetResult>(url);

			savedPets.Clear();
			savedPets.Add(new Pet { Id = 0, Name = "Новый питомец" });
			savedPets.AddRange(result?.Pets ?? new List<Pet>());
			SavedPetPicker.ItemsSource = null;
			SavedPetPicker.ItemsSource = savedPets;
			SavedPetsBlock.IsVisible = savedPets.Count > 1;
			SavedPetPicker.SelectedIndex = 0;
		}
		catch
		{
			// Если список временно недоступен, запись вручную остается доступной.
			SavedPetsBlock.IsVisible = false;
		}
	}

	// Подставляет данные выбранного питомца в обычные поля формы.
	private void SavedPet_Changed(object sender, EventArgs e)
	{
		selectedPet = SavedPetPicker.SelectedItem as Pet;

		if (selectedPet == null || selectedPet.Id == 0)
		{
			PetNameEntry.Text = "";
			PetAgeEntry.Text = "";
			PetTypePicker.SelectedItem = null;
			SetPetFieldsEditable(true);
			return;
		}

		PetNameEntry.Text = selectedPet.Name;
		PetAgeEntry.Text = selectedPet.Age;
		SetPetType(selectedPet.Type);
		SetPetFieldsEditable(false);
	}

	// Устанавливает значение или состояние.
	private void SetPetFieldsEditable(bool isEditable)
	{
		PetNameEntry.IsReadOnly = !isEditable;
		PetAgeEntry.IsReadOnly = !isEditable;
		PetTypePicker.IsEnabled = isEditable;
	}

	// Устанавливает значение или состояние.
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

	// Находит нужный элемент в списке.
	private ScheduleDay FindFirstAvailableDay()
	{
		foreach (var day in days)
		{
			foreach (var slot in day.Slots)
			{
				if (slot.IsAvailable)
					return day;
			}
		}

		if (days.Count > 0)
			return days[0];

		return null;
	}

	// Формирует плитки ближайших дней для выбора даты приема.
	private void BuildDays()
	{
		showDays.Clear();

		foreach (var day in days)
			showDays.Add(new DayItem(day, day == selectedDay));
	}

	// Формирует список времени для выбранного дня.
	private void BuildTimes()
	{
		showTimes.Clear();
		selectedTime = null;

		foreach (var slot in selectedDay?.Slots ?? new List<TimeSlot>())
		{
			var item = new TimeItem(slot, false);
			showTimes.Add(item);
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

		if (item == null || !item.Slot.IsAvailable)
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
		var time = selectedTime?.Slot;

		if (name == "" || phone == "" || petName == "" || petType == "" || service == null || time == null)
		{
			await DisplayAlertAsync("Ошибка", "Заполните данные клиента, питомца, услугу и время.", "ОК");
			return;
		}

		try
		{
			var petId = selectedPet?.Id ?? 0;
			var data = new AppointmentData(name, phone, petName, petType, petAge, petId, service.Id, time.Value, comment);
			var response = await httpClient.PostAsJsonAsync($"{Api.BaseUrl}appointments/create.php", data);
			var result = await response.Content.ReadFromJsonAsync<ApiResult>();

			if (!response.IsSuccessStatusCode || result?.Success != true)
			{
				await DisplayAlertAsync("Ошибка", result?.Message ?? "Заявка не отправлена.", "ОК");
				return;
			}

			Clear();
			await DisplayAlertAsync("Заявка отправлена", "Администратор в ближайшее время рассмотрит вашу заявку. Статус заявки - в личном кабинете пользователя", "ОК");
			await LoadAppointmentData();
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Ошибка", $"Не удалось отправить заявку: {ex.Message}", "ОК");
		}
	}

	// Очищает поля формы.
	private void Clear()
	{
		FillClient();
		PetNameEntry.Text = "";
		PetTypePicker.SelectedIndex = 0;
		PetAgeEntry.Text = "";
		SavedPetPicker.SelectedIndex = 0;
		SetPetFieldsEditable(true);
		CommentEditor.Text = "";

		if (services.Count > 0)
			ServicePicker.SelectedIndex = 0;
		else
			ServicePicker.SelectedIndex = -1;

		selectedDay = FindFirstAvailableDay();
		BuildDays();
		BuildTimes();
	}

	private class SelectableItem : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		// Сообщает интерфейсу об изменении цвета или состояния плитки.
		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	// Модель плитки даты: хранит текст и цвета выбранного/обычного состояния.
	private class DayItem : SelectableItem
	{
		public ScheduleDay Day { get; }
		public bool IsSelected { get; set; }
		public string DayName
		{
			get
			{
				return Day.Label;
			}
		}
		public Color BackgroundColor { get; private set; } = Color.FromArgb("#FFFFFF");
		public Color StrokeColor { get; private set; } = Color.FromArgb("#DDEBF3");
		public Color TextColor { get; private set; } = Color.FromArgb("#657084");

		public DayItem(ScheduleDay day, bool isSelected)
		{
			Day = day;
			IsSelected = isSelected;

			RefreshStyle();
		}

		// Обновляет цвета плитки даты после выбора или отмены выбора.
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
		public TimeSlot Slot { get; }
		public bool IsSelected { get; set; }
		public string Label
		{
			get
			{
				return Slot.Label;
			}
		}
		public Color BackgroundColor { get; private set; } = Color.FromArgb("#FFFFFF");
		public Color StrokeColor { get; private set; } = Color.FromArgb("#DDEBF3");
		public Color TextColor { get; private set; } = Color.FromArgb("#17213A");

		public TimeItem(TimeSlot slot, bool isSelected)
		{
			Slot = slot;
			IsSelected = isSelected;
			RefreshStyle();
		}

		// Обновляет цвета плитки времени после выбора или отмены выбора.
		public void RefreshStyle()
		{
			if (!Slot.IsAvailable)
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

	private class AppointmentData
	{
		[JsonPropertyName("name")] public string Name { get; set; }
		[JsonPropertyName("phone")] public string Phone { get; set; }
		[JsonPropertyName("pet_name")] public string PetName { get; set; }
		[JsonPropertyName("pet_type")] public string PetType { get; set; }
		[JsonPropertyName("pet_age")] public string PetAge { get; set; }
		[JsonPropertyName("pet_id")] public int PetId { get; set; }
		[JsonPropertyName("service_id")] public int ServiceId { get; set; }
		[JsonPropertyName("appointment_at")] public string AppointmentAt { get; set; }
		[JsonPropertyName("comment")] public string Comment { get; set; }

		public AppointmentData(string name, string phone, string petName, string petType, string petAge, int petId, int serviceId, string appointmentAt, string comment)
		{
			Name = name;
			Phone = phone;
			PetName = petName;
			PetType = petType;
			PetAge = petAge;
			PetId = petId;
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


	private class Service
	{
		[JsonPropertyName("id")] public int Id { get; set; }
		[JsonPropertyName("title")] public string Title { get; set; } = "";
		[JsonPropertyName("price")] public int Price { get; set; }
		public string Text
		{
			get
			{
				return $"{Title} - {Price} руб.";
			}
		}
	}

	private class TimeResult
	{
		[JsonPropertyName("days")]
		public List<ScheduleDay> Days { get; set; } = new List<ScheduleDay>();
	}

	private class ScheduleDay
	{
		[JsonPropertyName("date")] public string Date { get; set; } = "";
		[JsonPropertyName("label")] public string Label { get; set; } = "";
		[JsonPropertyName("slots")] public List<TimeSlot> Slots { get; set; } = new List<TimeSlot>();
	}

	private class TimeSlot
	{
		[JsonPropertyName("time")] public string Value { get; set; } = "";
		[JsonPropertyName("label")] public string Label { get; set; } = "";
		[JsonPropertyName("is_available")] public bool IsAvailable { get; set; }
	}

}















