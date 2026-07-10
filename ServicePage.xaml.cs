namespace VetAuthMaui;

using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

// страница услуг
public partial class ServicePage : ContentPage
{
	private HttpClient httpClient = new HttpClient();
	private List<Service> services = new List<Service>();
	private ObservableCollection<Service> showServices = new ObservableCollection<Service>();

	// список категорий
	private List<Category> list = new List<Category>()
	{
		new Category("Все категории", ""),
		new Category("Прием", "Прием"),
		new Category("Профилактика", "Профилактика"),
		new Category("Уход", "Уход"),
		new Category("Диагностика", "Диагностика"),
		new Category("Стоматология", "Стоматология"),
		new Category("Лечение", "Лечение"),
		new Category("Хирургия", "Хирургия"),
		new Category("Стационар", "Стационар")
	};

	public ServicePage()
	{
		InitializeComponent();
		httpClient.Timeout = TimeSpan.FromSeconds(10);

		// настройка списка категорий
		CategoryPicker.ItemsSource = list;
		CategoryPicker.ItemDisplayBinding = new Binding("Name");
		CategoryPicker.SelectedIndex = 0;

		ServicesCollectionView.ItemsSource = showServices;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		// кнопка только для админа
		AddServiceButton.IsVisible = State.IsAdminMode;
		await LoadServices();
	}

	private async void Refresh_Click(object sender, EventArgs e)
	{
		await LoadServices();
	}

	private void Search_Changed(object sender, TextChangedEventArgs e)
	{
		Filter();
	}

	private void Category_Changed(object sender, EventArgs e)
	{
		Filter();
	}

	private async void Add_Click(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("AddPage");
	}

	private async void Edit_Click(object sender, EventArgs e)
	{
		// переход к редактированию
		Button button = (Button)sender;
		Service service = (Service)button.BindingContext;

		var route = $"EditPage?id={service.Id}" +
			$"&title={Uri.EscapeDataString(service.Title)}" +
			$"&description={Uri.EscapeDataString(service.Description)}" +
			$"&price={service.PriceValue}" +
			$"&category={Uri.EscapeDataString(service.Category)}";

		await Shell.Current.GoToAsync(route);
	}

	private async void Delete_Click(object sender, EventArgs e)
	{
		// удаление услуги
		Button button = (Button)sender;
		Service service = (Service)button.BindingContext;

		var ok = await DisplayAlertAsync(
			"Удалить услугу?",
			$"Услуга «{service.Title}» будет удалена из базы.",
			"Удалить",
			"Отмена");

		if (!ok)
			return;

		try
		{
			var response = await httpClient.PostAsJsonAsync(
				$"{Api.BaseUrl}services/delete.php",
				new ServiceData(service.Id));

			var result = await response.Content.ReadFromJsonAsync<ApiResult>();

			if (!response.IsSuccessStatusCode || result?.Success != true)
			{
				await DisplayAlertAsync("Ошибка", result?.Message ?? "Не удалось удалить услугу.", "ОК");
				return;
			}

			services.Remove(service);
			showServices.Remove(service);
			StatusLabel.Text = $"Услуг: {showServices.Count}";
			await DisplayAlertAsync("Готово", "Услуга удалена.", "ОК");
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Ошибка", $"Ошибка удаления: {ex.Message}", "ОК");
		}
	}

	private async Task LoadServices()
	{
		try
		{
			// загрузка услуг
			StatusLabel.Text = "Загрузка услуг...";

			var response = await httpClient.GetFromJsonAsync<ServiceResult>($"{Api.BaseUrl}services/list.php");
			services.Clear();

			foreach (var item in response?.Services ?? new List<Service>())
			{
				services.Add(new Service
				{
					Id = item.Id,
					Title = item.Title,
					Description = item.Description,
					Category = item.Category,
					PriceValue = item.Price,
					PriceText = $"{item.Price} руб.",
					IsAdminMode = State.IsAdminMode
				});
			}

			Filter();
		}
		catch (Exception ex)
		{
			StatusLabel.Text = "";
			await DisplayAlertAsync("Ошибка", $"Не удалось загрузить услуги: {ex.Message}", "ОК");
		}
	}

	private void Filter()
	{
		// фильтр услуг
		var text = SearchEntry.Text?.Trim() ?? "";
		var cat = CategoryPicker.SelectedItem as Category;
		var category = cat?.Value ?? "";

		showServices.Clear();

		foreach (var service in services)
		{
			var categoryOk = category == "" || service.Category == category;
			var searchOk = text == "" ||
				service.Title.ToLower().Contains(text.ToLower()) ||
				service.Description.ToLower().Contains(text.ToLower());

			if (categoryOk && searchOk)
				showServices.Add(service);
		}

		if (showServices.Count == 0)
			StatusLabel.Text = "Услуги не найдены.";
		else
			StatusLabel.Text = $"Услуг: {showServices.Count}";
	}

	// категория
	private class Category
	{
		public string Name { get; set; }
		public string Value { get; set; }

		public Category(string name, string value)
		{
			Name = name;
			Value = value;
		}
	}

	// данные услуги
	// услуга
	private class ServiceData
	{
		[JsonPropertyName("id")]
		public int Id { get; set; }

		public ServiceData(int id)
		{
			Id = id;
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
		[JsonPropertyName("description")] public string Description { get; set; } = "";
		[JsonPropertyName("category")] public string Category { get; set; } = "";
		[JsonPropertyName("price")] public int Price { get; set; }
		public int PriceValue { get; set; }
		public string PriceText { get; set; } = "";
		public bool IsAdminMode { get; set; }
	}
}






























