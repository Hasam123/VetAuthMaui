namespace VetAuthMaui;

using System.Collections.ObjectModel;
using System.Net.Http.Json;

// Личный кабинет клиента: карточка клиента, переход к питомцам и список записей.
public partial class ClientPage : ContentPage
{
	private readonly HttpClient httpClient = new HttpClient();
	private readonly ObservableCollection<Appointment> appointments = new ObservableCollection<Appointment>();

	public ClientPage()
	{
		InitializeComponent();
		httpClient.Timeout = TimeSpan.FromSeconds(10);
		RequestsCollectionView.ItemsSource = appointments;
	}

	// Загружает данные при открытии страницы.
	protected override async void OnAppearing()
	{
		base.OnAppearing();

		if (State.IsClientLoggedIn)
			await LoadClientCabinet();
	}

	// Загружает данные для страницы.
	private async Task LoadClientCabinet()
	{
		try
		{
			if (string.IsNullOrWhiteSpace(State.ClientPhone))
				return;

			StatusLabel.Text = "Загрузка личного кабинета...";
			ClientCard.IsVisible = false;
			appointments.Clear();

			var url = $"{Api.BaseUrl}clients/profile.php?phone={Uri.EscapeDataString(State.ClientPhone)}";
			var response = await httpClient.GetFromJsonAsync<ClientResult>(url);

			foreach (var appointment in response?.Appointments ?? new List<Appointment>())
				appointments.Add(appointment);

			ClientCard.IsVisible = true;
			ClientNameLabel.Text = $"Имя: {response?.Client.Name}";
			ClientPhoneLabel.Text = $"Телефон: {response?.Client.Phone}";

			if (appointments.Count == 0)
			{
				LastStatusLabel.Text = "Заявок пока нет";
				StatusLabel.Text = "Заявок по этому телефону пока нет.";
				return;
			}

			LastStatusLabel.Text = $"Последняя заявка: {appointments[0].StatusText}";
			StatusLabel.Text = $"Найдено заявок: {appointments.Count}";
		}
		catch (Exception ex)
		{
			StatusLabel.Text = "";
			await DisplayAlertAsync("Ошибка", $"Не удалось загрузить кабинет: {ex.Message}", "Понятно");
		}
	}

	// Обрабатывает нажатие кнопки.
	private async void Pets_Click(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("PetsPage");
	}

	// Обрабатывает нажатие кнопки.
	private async void Cancel_Click(object sender, EventArgs e)
	{
		var button = (Button)sender;
		var appointment = (Appointment)button.BindingContext;

		var ok = await DisplayAlertAsync(
			"Отменить запись?",
			$"{appointment.TimeText} будет отменена.",
			"Отменить",
			"Назад");

		if (!ok)
			return;

		try
		{
			var response = await httpClient.PostAsJsonAsync(
				$"{Api.BaseUrl}appointments/cancel.php",
				new CancelData(appointment.Id, State.ClientPhone));

			var result = await response.Content.ReadFromJsonAsync<ApiResult>();

			if (!response.IsSuccessStatusCode || result?.Success != true)
			{
				await DisplayAlertAsync("Ошибка", result?.Message ?? "Не удалось отменить запись.", "Понятно");
				return;
			}

			await DisplayAlertAsync("Готово", result.Message, "ОК");
			await LoadClientCabinet();
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Ошибка", $"Ошибка отмены записи: {ex.Message}", "Понятно");
		}
	}
}

