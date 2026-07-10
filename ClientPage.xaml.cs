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
	private ObservableCollection<Pet> pets = new ObservableCollection<Pet>();
	private string phone = "";

	public ClientPage()
	{
		InitializeComponent();
		httpClient.Timeout = TimeSpan.FromSeconds(10);
		RequestsCollectionView.ItemsSource = requests;
		PetsCollectionView.ItemsSource = pets;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		// если клиент уже вошел
		if (State.IsClientLoggedIn)
		{
			await LoadData(State.ClientPhone);
		}
	}

	private async Task LoadData(string phone)
	{
		try
		{
			// загрузка кабинета
			this.phone = phone;
			StatusLabel.Text = "Загрузка личного кабинета...";
			ClientCard.IsVisible = false;
			PetsCard.IsVisible = false;
			requests.Clear();
			pets.Clear();

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

			await LoadPets(phone);

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

	// Загружает питомцев владельца в отдельный блок личного кабинета.
	private async Task LoadPets(string phone)
	{
		var url = $"{Api.BaseUrl}appointments/pets_list.php?phone={Uri.EscapeDataString(phone)}";
		var result = await httpClient.GetFromJsonAsync<PetResult>(url);

		pets.Clear();
		foreach (var pet in result?.Pets ?? new List<Pet>())
		{
			pets.Add(pet);
		}

		PetsCard.IsVisible = true;
		PetsStatusLabel.Text = pets.Count == 0 ? "Питомцы пока не добавлены" : $"Добавлено питомцев: {pets.Count}";
	}

	private async void AddPet_Click(object sender, EventArgs e)
	{
		var currentPhone = phone;

		if (currentPhone == "")
		{
			await DisplayAlertAsync("Ошибка", "Войдите в личный кабинет клиента.", "Понятно");
			return;
		}

		var name = await DisplayPromptAsync("Питомец", "Кличка питомца", "Далее", "Отмена", "Например: Барни");
		if (name == null)
			return;

		name = name.Trim();
		if (name == "")
		{
			await DisplayAlertAsync("Ошибка", "Введите кличку питомца.", "Понятно");
			return;
		}

		var type = await DisplayPromptAsync("Питомец", "Вид животного", "Далее", "Отмена", "Например: собака");
		if (type == null)
			return;

		type = type.Trim();
		if (type == "")
		{
			await DisplayAlertAsync("Ошибка", "Введите вид животного.", "Понятно");
			return;
		}

		var age = await DisplayPromptAsync("Питомец", "Возраст", "Далее", "Отмена", "Например: 3 года");
		if (age == null)
			return;

		var weight = await DisplayPromptAsync("Питомец", "Вес", "Далее", "Отмена", "Например: 12 кг");
		if (weight == null)
			return;

		var vaccination = await DisplayPromptAsync("Питомец", "Дата последней прививки", "Далее", "Отмена", "Например: 12.03.2026");
		if (vaccination == null)
			return;

		var vaccinationDate = NormalizeDate(vaccination.Trim());
		if (vaccination.Trim() != "" && vaccinationDate == "")
		{
			await DisplayAlertAsync("Ошибка", "Введите дату прививки в формате 12.03.2026 или 2026-03-12.", "Понятно");
			return;
		}

		var photo = await PickPetPhoto();
		await SavePet(currentPhone, name, type, age.Trim(), weight.Trim(), vaccinationDate, photo);
	}

	// Изменяет данные выбранного питомца и сохраняет их в его карточке.
	private async void EditPet_Click(object sender, EventArgs e)
	{
		var pet = (Pet)((Button)sender).BindingContext;
		var name = await DisplayPromptAsync("Изменить питомца", "Кличка питомца", "Далее", "Отмена", initialValue: pet.Name);
		if (name == null)
			return;

		name = name.Trim();
		if (name == "")
		{
			await DisplayAlertAsync("Ошибка", "Введите кличку питомца.", "Понятно");
			return;
		}

		var type = await DisplayPromptAsync("Изменить питомца", "Вид животного", "Далее", "Отмена", initialValue: pet.Type);
		if (type == null)
			return;

		type = type.Trim();
		if (type == "")
		{
			await DisplayAlertAsync("Ошибка", "Введите вид животного.", "Понятно");
			return;
		}

		var age = await DisplayPromptAsync("Изменить питомца", "Возраст", "Далее", "Отмена", initialValue: pet.Age);
		if (age == null)
			return;

		var weight = await DisplayPromptAsync("Изменить питомца", "Вес", "Далее", "Отмена", initialValue: pet.Weight);
		if (weight == null)
			return;

		var vaccination = await DisplayPromptAsync("Изменить питомца", "Дата последней прививки", "Далее", "Отмена", "Например: 12.03.2026", initialValue: FormatEditableDate(pet.LastVaccinationDate));
		if (vaccination == null)
			return;

		var vaccinationDate = NormalizeDate(vaccination.Trim());
		if (vaccination.Trim() != "" && vaccinationDate == "")
		{
			await DisplayAlertAsync("Ошибка", "Введите дату прививки в формате 12.03.2026 или 2026-03-12.", "Понятно");
			return;
		}

		var photo = pet.Photo;
		var photoChoice = await DisplayActionSheetAsync("Фото питомца", "Оставить как есть", null, "Заменить фото", "Удалить фото");
		if (photoChoice == "Заменить фото")
			photo = await PickPetPhoto();
		else if (photoChoice == "Удалить фото")
			photo = "";

		await UpdatePet(pet.Id, name, type, age.Trim(), weight.Trim(), vaccinationDate, photo);
	}

	// Удаляет карточку питомца только после подтверждения пользователя.
	private async void DeletePet_Click(object sender, EventArgs e)
	{
		var pet = (Pet)((Button)sender).BindingContext;
		var confirmed = await DisplayAlertAsync("Удалить питомца?", $"Карточка питомца «{pet.Name}» будет удалена.", "Удалить", "Отмена");
		if (!confirmed)
			return;

		try
		{
			var response = await httpClient.PostAsJsonAsync($"{Api.BaseUrl}appointments/pets_delete.php", new DeletePetData(pet.Id, phone));
			var result = await response.Content.ReadFromJsonAsync<ApiResult>();

			if (!response.IsSuccessStatusCode || result?.Success != true)
			{
				await DisplayAlertAsync("Ошибка", result?.Message ?? "Не удалось удалить питомца.", "Понятно");
				return;
			}

			await LoadPets(phone);
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Ошибка", $"Не удалось удалить питомца: {ex.Message}", "Понятно");
		}
	}

	// Сохраняет питомца через API и обновляет список без перезахода в кабинет.
	private async Task SavePet(string phone, string name, string type, string age, string weight, string vaccinationDate, string photo)
	{
		try
		{
			var data = new AddPetData(phone, name, type, age, weight, vaccinationDate, photo);
			var response = await httpClient.PostAsJsonAsync($"{Api.BaseUrl}appointments/pets_create.php", data);
			var result = await response.Content.ReadFromJsonAsync<ApiResult>();

			if (!response.IsSuccessStatusCode || result?.Success != true)
			{
				await DisplayAlertAsync("Ошибка", result?.Message ?? "Питомец не добавлен.", "Понятно");
				return;
			}

			await DisplayAlertAsync("Готово", "Питомец добавлен.", "ОК");
			await LoadPets(phone);
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Ошибка", $"Не удалось добавить питомца: {ex.Message}", "Понятно");
		}
	}

	private async Task UpdatePet(int id, string name, string type, string age, string weight, string vaccinationDate, string photo)
	{
		try
		{
			var data = new UpdatePetData(id, phone, name, type, age, weight, vaccinationDate, photo);
			var response = await httpClient.PostAsJsonAsync($"{Api.BaseUrl}appointments/pets_update.php", data);
			var result = await response.Content.ReadFromJsonAsync<ApiResult>();

			if (!response.IsSuccessStatusCode || result?.Success != true)
			{
				await DisplayAlertAsync("Ошибка", result?.Message ?? "Не удалось изменить питомца.", "Понятно");
				return;
			}

			await DisplayAlertAsync("Готово", "Данные питомца изменены.", "ОК");
			await LoadPets(phone);
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Ошибка", $"Не удалось изменить питомца: {ex.Message}", "Понятно");
		}
	}

	// Фото необязательное: пользователь может оставить карточку только с текстовыми данными.
	private async Task<string> PickPetPhoto()
	{
		var choice = await DisplayActionSheetAsync("Фото питомца", "Без фото", null, "Выбрать фото");
		if (choice != "Выбрать фото")
			return "";

		try
		{
			var files = await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions
			{
				Title = "Выберите фото питомца"
			});
			var file = files?.FirstOrDefault();

			if (file == null)
				return "";

			using var stream = await file.OpenReadAsync();
			using var memory = new MemoryStream();
			await stream.CopyToAsync(memory);
			return Convert.ToBase64String(memory.ToArray());
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Фото не выбрано", $"Не удалось добавить фото: {ex.Message}", "Понятно");
			return "";
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
			await LoadData(phone);
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Ошибка", $"Ошибка отмены записи: {ex.Message}", "Понятно");
		}
	}

	private static string NormalizeDate(string value)
	{
		if (value == "")
			return "";

		var formats = new[] { "dd.MM.yyyy", "yyyy-MM-dd" };
		if (DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
			return date.ToString("yyyy-MM-dd");

		if (DateTime.TryParse(value, out date))
			return date.ToString("yyyy-MM-dd");

		return "";
	}

	private static string FormatEditableDate(string value)
	{
		return DateTime.TryParse(value, out var date) ? date.ToString("dd.MM.yyyy") : value;
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

	private class PetResult
	{
		[JsonPropertyName("pets")]
		public List<Pet> Pets { get; set; } = new List<Pet>();
	}

	// питомец клиента
	private class Pet
	{
		[JsonPropertyName("id")]
		public int Id { get; set; }

		[JsonPropertyName("name")]
		public string Name { get; set; } = "";

		[JsonPropertyName("type")]
		public string Type { get; set; } = "";

		[JsonPropertyName("age")]
		public string Age { get; set; } = "";

		[JsonPropertyName("weight")]
		public string Weight { get; set; } = "";

		[JsonPropertyName("last_vaccination_date")]
		public string LastVaccinationDate { get; set; } = "";

		[JsonPropertyName("photo")]
		public string Photo { get; set; } = "";

		public bool HasPhoto => !string.IsNullOrWhiteSpace(Photo);
		public bool HasNoPhoto => !HasPhoto;
		public string TypeText => string.IsNullOrWhiteSpace(Type) ? "Вид не указан" : Type;

		public string AgeWeightText
		{
			get
			{
				var parts = new List<string>();

				if (!string.IsNullOrWhiteSpace(Age))
					parts.Add($"Возраст: {Age}");
				if (!string.IsNullOrWhiteSpace(Weight))
					parts.Add($"Вес: {Weight}");

				return parts.Count == 0 ? "Возраст и вес не указаны" : string.Join(" · ", parts);
			}
		}

		public string VaccinationText
		{
			get
			{
				if (string.IsNullOrWhiteSpace(LastVaccinationDate))
					return "Прививка не указана";

				if (DateTime.TryParse(LastVaccinationDate, out var date))
					return $"Последняя прививка: {date:dd.MM.yyyy}";

				return $"Последняя прививка: {LastVaccinationDate}";
			}
		}

		public ImageSource PhotoImage
		{
			get
			{
				if (string.IsNullOrWhiteSpace(Photo))
					return null;

				try
				{
					var bytes = Convert.FromBase64String(Photo);
					return ImageSource.FromStream(() => new MemoryStream(bytes));
				}
				catch
				{
					return null;
				}
			}
		}
	}

	// данные добавления питомца
	private class AddPetData
	{
		[JsonPropertyName("phone")] public string Phone { get; set; }
		[JsonPropertyName("name")] public string Name { get; set; }
		[JsonPropertyName("type")] public string Type { get; set; }
		[JsonPropertyName("age")] public string Age { get; set; }
		[JsonPropertyName("weight")] public string Weight { get; set; }
		[JsonPropertyName("last_vaccination_date")] public string LastVaccinationDate { get; set; }
		[JsonPropertyName("photo")] public string Photo { get; set; }

		public AddPetData(string phone, string name, string type, string age, string weight, string lastVaccinationDate, string photo)
		{
			Phone = phone;
			Name = name;
			Type = type;
			Age = age;
			Weight = weight;
			LastVaccinationDate = lastVaccinationDate;
			Photo = photo;
		}
	}

	private class UpdatePetData : AddPetData
	{
		[JsonPropertyName("id")] public int Id { get; set; }

		public UpdatePetData(int id, string phone, string name, string type, string age, string weight, string lastVaccinationDate, string photo)
			: base(phone, name, type, age, weight, lastVaccinationDate, photo)
		{
			Id = id;
		}
	}

	private class DeletePetData
	{
		[JsonPropertyName("id")] public int Id { get; set; }
		[JsonPropertyName("phone")] public string Phone { get; set; }

		public DeletePetData(int id, string phone)
		{
			Id = id;
			Phone = phone;
		}
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
					return "#4AA3D8";
				if (Status == "accepted")
					return "#FF8A5B";
				if (Status == "done")
					return "#30B878";
				if (Status == "cancelled")
					return "#D9534F";

				return "#657084";
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

