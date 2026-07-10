namespace VetAuthMaui;

using System.Net.Http.Json;
using System.Text.Json.Serialization;

// главный экран
public partial class MainPage : ContentPage
{
	private HttpClient httpClient = new HttpClient();

	public MainPage()
	{
		InitializeComponent();
		httpClient.Timeout = TimeSpan.FromSeconds(10);
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();

		// если клиент уже вошел
		if (State.IsClientLoggedIn)
		{
			PhoneEntry.Text = State.ClientPhone;
		}
	}

	private async void Login_Click(object sender, EventArgs e)
	{
		var phone = PhoneEntry.Text?.Trim() ?? "";
		var password = PasswordEntry.Text?.Trim() ?? "";

		if (phone == "" || password == "")
		{
			await DisplayAlertAsync("Ошибка", "Введите телефон и пароль.", "ОК");
			return;
		}

		try
		{
			// отправка данных в API
			var response = await httpClient.PostAsJsonAsync(
				$"{Api.BaseUrl}clients/login.php",
				new LoginData(phone, password));

			var result = await response.Content.ReadFromJsonAsync<LoginResult>();

			if (!response.IsSuccessStatusCode || result?.Success != true || result.Client == null)
			{
				await DisplayAlertAsync("Ошибка", result?.Message ?? "Не удалось войти.", "ОК");
				return;
			}

			// сохранение клиента
			State.IsAdminMode = false;
			State.SetClient(result.Client.Id, result.Client.Name, result.Client.Phone);
			PasswordEntry.Text = "";
			await Shell.Current.GoToAsync("HomePage");
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Ошибка", $"Не удалось войти: {ex.Message}", "ОК");
		}
	}

	private async void Register_Click(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("ClientRegister");
	}

	private async void Admin_Click(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("AdminLogin");
	}

	private async void About_Click(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("About");
	}

	// данные входа
	private class LoginData
	{
		[JsonPropertyName("phone")] public string Phone { get; set; }
		[JsonPropertyName("password")] public string Password { get; set; }

		public LoginData(string phone, string password)
		{
			Phone = phone;
			Password = password;
		}
	}
}

















