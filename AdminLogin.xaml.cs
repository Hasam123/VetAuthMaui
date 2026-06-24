namespace VetAuthMaui;

using System.Net.Http.Json;
using System.Text.Json.Serialization;

// вход админа
public partial class AdminLogin : ContentPage
{
	private HttpClient httpClient = new HttpClient();

	public AdminLogin()
	{
		InitializeComponent();
		httpClient.Timeout = TimeSpan.FromSeconds(10);
	}

	private void ShowPassword_Changed(object sender, CheckedChangedEventArgs e)
	{
		PasswordEntry.IsPassword = !e.Value;
	}

	private async void Login_Click(object sender, EventArgs e)
	{
		// проверка формы
		var login = LoginEntry.Text?.Trim() ?? "";
		var password = PasswordEntry.Text ?? "";

		if (login == "")
		{
			await DisplayAlertAsync("Ошибка", "Введите логин.", "ОК");
			return;
		}

		if (password == "")
		{
			await DisplayAlertAsync("Ошибка", "Введите пароль.", "ОК");
			return;
		}

		try
		{
			MessageLabel.Text = "Проверяем данные...";

			// отправка данных в API
			var data = new LoginData(login, password);
			var response = await httpClient.PostAsJsonAsync($"{Api.BaseUrl}admin/login.php", data);
			var result = await response.Content.ReadFromJsonAsync<LoginResult>();

			if (response.IsSuccessStatusCode && result?.Success == true)
			{
				MessageLabel.Text = result.Message;
				await Shell.Current.GoToAsync("AdminPage");
				return;
			}

			await DisplayAlertAsync("Ошибка входа", result?.Message ?? "Неверный логин или пароль.", "ОК");
			MessageLabel.Text = "";
		}
		catch (Exception ex)
		{
			MessageLabel.Text = "";
			await DisplayAlertAsync("Ошибка", $"Не удалось войти: {ex.Message}", "ОК");
		}
	}

	// данные входа
	private class LoginData
	{
		[JsonPropertyName("login")]
		public string Login { get; set; }

		[JsonPropertyName("password")]
		public string Password { get; set; }

		public LoginData(string login, string password)
		{
			Login = login;
			Password = password;
		}
	}

	// ответ входа
	private class LoginResult
	{
		[JsonPropertyName("success")] public bool Success { get; set; }
		[JsonPropertyName("message")] public string Message { get; set; } = "";
	}
}













