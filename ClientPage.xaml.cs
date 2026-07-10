namespace VetAuthMaui;

using System.Collections.ObjectModel;
using System.Net.Http.Json;

// Личный кабинет клиента: карточка клиента, переход к питомцам и список записей.
public partial class ClientPage : ContentPage
{
	private readonly HttpClient httpClient = new HttpClient();
	private readonly ObservableCollection<Appointment> appointments = new ObservableCollection<Appointment>();
	private string phone = "";

	public ClientPage()
	{
		InitializeComponent();
		httpClient.Timeout = TimeSpan.FromSeconds(10);
		RequestsCollectionView.ItemsSource = appointments;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		if (State.IsClientLoggedIn)
			await LoadData(State.ClientPhone);
	}

	private async Task LoadData(string phone)
	{
		try
		{
			this.phone = phone;
			StatusLabel.Text = "Загрузка личного кабинета...";
			ClientCard.IsVisible = false;
			appointments.Clear();

			var url = $"{Api.BaseUrl}clients/profile.php?phone={Uri.EscapeDataString(phone)}";
			var response = await httpClient.GetFromJsonAsync<ClientResult>(url);

			foreach (var appointment in response?.Requests ?? new List<Appointment>())
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

	private async void Pets_Click(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("PetsPage");
	}

	private async void Cancel_Click(object sender, EventArgs e)
	{
		var button = (Button)sender;
		var appointment = (Appointment)button.BindingContext;

		var ok = await DisplayAlertAsync(
			"Отменить запись?",
			$"Запись на {appointment.TimeText} будет отменена.",
			"Отменить",
			"Назад");

		if (!ok)
			return;

		try
		{
			var response = await httpClient.PostAsJsonAsync(
				$"{Api.BaseUrl}appointments/cancel.php",
				new CancelData(appointment.Id, phone));

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
}

