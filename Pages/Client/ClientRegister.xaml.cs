namespace VetAuthMaui;

using System.Net.Http.Json;
using System.Text.Json.Serialization;

// регистрация клиента
public partial class ClientRegister : ContentPage
{
	private HttpClient httpClient = new HttpClient();

	public ClientRegister()
	{
		InitializeComponent();
		httpClient.Timeout = TimeSpan.FromSeconds(10);
	}

	// Обрабатывает нажатие кнопки.
	private async void Register_Click(object sender, EventArgs e)
	{
		var name = NameEntry.Text?.Trim() ?? "";
		var phone = PhoneEntry.Text?.Trim() ?? "";
		var password = PasswordEntry.Text?.Trim() ?? "";

		if (name == "" || phone == "" || password == "")
		{
			await DisplayAlertAsync("Ошибка", "Заполните имя, телефон и пароль.", "ОК");
			return;
		}

		try
		{
			// отправка данных в API
			var response = await httpClient.PostAsJsonAsync(
				$"{Api.BaseUrl}clients/register.php",
				new RegisterData(name, phone, password));

			var result = await response.Content.ReadFromJsonAsync<LoginResult>();

			if (!response.IsSuccessStatusCode || result?.Success != true || result.Client == null)
			{
				await DisplayAlertAsync("Ошибка", result?.Message ?? "Регистрация не выполнена.", "ОК");
				return;
			}

			// сохранение клиента
			State.SetClient(result.Client.Id, result.Client.Name, result.Client.Phone);
			await DisplayAlertAsync("Готово", result.Message, "ОК");
			await Shell.Current.GoToAsync("//HomePage");
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Ошибка", $"Не удалось зарегистрироваться: {ex.Message}", "ОК");
		}
	}

	// данные регистрации
	private class RegisterData
	{
		[JsonPropertyName("name")] public string Name { get; set; }
		[JsonPropertyName("phone")] public string Phone { get; set; }
		[JsonPropertyName("password")] public string Password { get; set; }

		public RegisterData(string name, string phone, string password)
		{
			Name = name;
			Phone = phone;
			Password = password;
		}
	}
}
















