namespace VetAuthMaui;

using System.Net.Http.Json;
using System.Text.Json.Serialization;

// добавление услуги
public partial class AddPage : ContentPage
{
	private HttpClient httpClient = new HttpClient();
	// список категорий
	private List<Category> list = new List<Category>()
	{
		new Category("Прием", "Прием"),
		new Category("Профилактика", "Профилактика"),
		new Category("Уход", "Уход"),
		new Category("Диагностика", "Диагностика"),
		new Category("Стоматология", "Стоматология"),
		new Category("Лечение", "Лечение"),
		new Category("Хирургия", "Хирургия"),
		new Category("Стационар", "Стационар")
	};

	public AddPage()
	{
		InitializeComponent();
		httpClient.Timeout = TimeSpan.FromSeconds(10);
		// настройка категорий
		CategoryPicker.ItemsSource = list;
		CategoryPicker.ItemDisplayBinding = new Binding("Name");
	}

	// Сохраняет измененные данные.
	private async void Save_Click(object sender, EventArgs e)
	{
		// проверка формы
		var title = TitleEntry.Text?.Trim() ?? "";
		var description = DescriptionEditor.Text?.Trim() ?? "";
		var category = CategoryPicker.SelectedItem as Category;

		if (!int.TryParse(PriceEntry.Text, out var price))
		{
			price = 0;
		}

		if (title == "" || description == "" || price <= 0 || category == null)
		{
			await DisplayAlertAsync("Ошибка", "Заполните название, описание, цену и категорию.", "Понятно");
			return;
		}

		try
		{
			// отправка данных в API
			var response = await httpClient.PostAsJsonAsync(
				$"{Api.BaseUrl}services/create.php",
				new ServiceData(title, description, price, category.Value));

			var result = await response.Content.ReadFromJsonAsync<ApiResult>();

			if (!response.IsSuccessStatusCode || result?.Success != true)
			{
				await DisplayAlertAsync("Ошибка", result?.Message ?? "Не удалось добавить услугу.", "Понятно");
				return;
			}

			await DisplayAlertAsync("Готово", result.Message, "Понятно");
			await Shell.Current.GoToAsync("..");
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Ошибка", $"Не удалось добавить услугу: {ex.Message}", "Понятно");
		}
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
	private class ServiceData
	{
		[JsonPropertyName("title")] public string Title { get; set; }
		[JsonPropertyName("description")] public string Description { get; set; }
		[JsonPropertyName("price")] public int Price { get; set; }
		[JsonPropertyName("category")] public string Category { get; set; }

		public ServiceData(string title, string description, int price, string category)
		{
			Title = title;
			Description = description;
			Price = price;
			Category = category;
		}
	}

}



















