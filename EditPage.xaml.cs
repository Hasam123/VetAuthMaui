namespace VetAuthMaui;

using System.Net.Http.Json;
using System.Text.Json.Serialization;

[QueryProperty("ServiceId", "id")]
[QueryProperty("ServiceTitle", "title")]
[QueryProperty("ServiceDescription", "description")]
[QueryProperty("ServicePrice", "price")]
[QueryProperty("ServiceCategory", "category")]
// редактирование услуги
public partial class EditPage : ContentPage
{
	private HttpClient httpClient = new HttpClient();
	private List<Category> list = new List<Category>()
	{
		new Category("Терапия", "terapia"),
		new Category("Лаборатория", "lab"),
		new Category("Хирургия", "hirurgia"),
		new Category("Вакцинация", "vactinatia"),
		new Category("Стоматология", "stomatologia"),
		new Category("Аллергология", "allergia"),
		new Category("Прочее", "other")
	};

	public string ServiceId { get; set; } = "";
	public string ServiceTitle { get; set; } = "";
	public string ServiceDescription { get; set; } = "";
	public string ServicePrice { get; set; } = "";
	public string ServiceCategory { get; set; } = "";

	public EditPage()
	{
		InitializeComponent();
		httpClient.Timeout = TimeSpan.FromSeconds(10);
		// настройка категорий
		CategoryPicker.ItemsSource = list;
		CategoryPicker.ItemDisplayBinding = new Binding("Name");
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();

		// заполнение формы
		TitleEntry.Text = ServiceTitle;
		DescriptionEditor.Text = ServiceDescription;
		PriceEntry.Text = ServicePrice;
		CategoryPicker.SelectedItem = list.FirstOrDefault(category => category.Value == ServiceCategory);
	}

	private async void Save_Click(object sender, EventArgs e)
	{
		// проверка формы
		var title = TitleEntry.Text?.Trim() ?? "";
		var description = DescriptionEditor.Text?.Trim() ?? "";
		var category = CategoryPicker.SelectedItem as Category;

		if (!int.TryParse(ServiceId, out var id))
		{
			id = 0;
		}

		if (!int.TryParse(PriceEntry.Text, out var price))
		{
			price = 0;
		}

		if (id <= 0 || title == "" || description == "" || price <= 0 || category == null)
		{
			await DisplayAlertAsync("Ошибка", "Заполните название, описание, цену и категорию.", "Понятно");
			return;
		}

		try
		{
			// отправка данных в API
			var response = await httpClient.PostAsJsonAsync(
				$"{Api.BaseUrl}services/update.php",
				new ServiceData(id, title, description, price, category.Value));

			var result = await response.Content.ReadFromJsonAsync<ApiResult>();

			if (!response.IsSuccessStatusCode || result?.Success != true)
			{
				await DisplayAlertAsync("Ошибка", result?.Message ?? "Не удалось обновить услугу.", "Понятно");
				return;
			}

			await DisplayAlertAsync("Готово", result.Message, "Понятно");
			await Shell.Current.GoToAsync("..");
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Ошибка", $"Не удалось обновить услугу: {ex.Message}", "Понятно");
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
		[JsonPropertyName("id")] public int Id { get; set; }
		[JsonPropertyName("title")] public string Title { get; set; }
		[JsonPropertyName("description")] public string Description { get; set; }
		[JsonPropertyName("price")] public int Price { get; set; }
		[JsonPropertyName("category")] public string Category { get; set; }

		public ServiceData(int id, string title, string description, int price, string category)
		{
			Id = id;
			Title = title;
			Description = description;
			Price = price;
			Category = category;
		}
	}

	// ответ API
	private class ApiResult
	{
		[JsonPropertyName("success")]
		public bool Success { get; set; }

		[JsonPropertyName("message")]
		public string Message { get; set; } = "";
	}
}

















