namespace VetAuthMaui;

using System.Net.Http.Json;
using System.Text.Json.Serialization;

// страница админа
public partial class AdminPage : ContentPage
{
	private HttpClient httpClient = new HttpClient();

	public AdminPage()
	{
		InitializeComponent();
		httpClient.Timeout = TimeSpan.FromSeconds(10);
	}

	// Загружает данные при открытии страницы.
	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await LoadStatistics();
	}

	// Обрабатывает нажатие кнопки.
	private async void Refresh_Click(object sender, EventArgs e)
	{
		await LoadStatistics();
	}

	// Загружает данные для страницы.
	private async Task LoadStatistics()
	{
		try
		{
			// загрузка статистики
			StatsStatusLabel.Text = "Загрузка статистики...";
		

			// запрос статистики
		var response = await httpClient.GetFromJsonAsync<StatisticResult>($"{Api.BaseUrl}admin/stats.php");
			var statistic = response?.Statistic ?? new Statistic();

			// вывод статистики
		TotalRequestsLabel.Text = statistic.RequestsTotal.ToString();
			NewRequestsLabel.Text = statistic.RequestsNew.ToString();
			AcceptedRequestsLabel.Text = statistic.RequestsAccepted.ToString();
			DoneRequestsLabel.Text = statistic.RequestsDone.ToString();
			CancelledRequestsLabel.Text = statistic.RequestsCancelled.ToString();
			ServicesTotalLabel.Text = statistic.ServicesTotal.ToString();

			StatsStatusLabel.Text = "Статистика обновлена";
		}
		catch (Exception ex)
		{
			StatsStatusLabel.Text = $"Ошибка загрузки статистики: {ex.Message}";
		}
	}

	// Открывает список заявок и передает статус для начального фильтра.
	private async Task OpenRequests(string status = "")
	{
		if (string.IsNullOrWhiteSpace(status))
			await Shell.Current.GoToAsync("RequestPage");
		else
			await Shell.Current.GoToAsync($"RequestPage?status={Uri.EscapeDataString(status)}");
	}

	// Открывает список всех заявок без фильтра по статусу.
	private async void AllRequests_Tapped(object sender, TappedEventArgs e)
	{
		await OpenRequests();
	}

	// Открывает список только новых заявок.
	private async void NewRequests_Tapped(object sender, TappedEventArgs e)
	{
		await OpenRequests("new");
	}

	// Открывает список заявок, которые администратор уже принял.
	private async void AcceptedRequests_Tapped(object sender, TappedEventArgs e)
	{
		await OpenRequests("accepted");
	}

	// Открывает список завершенных заявок.
	private async void DoneRequests_Tapped(object sender, TappedEventArgs e)
	{
		await OpenRequests("done");
	}

	// Открывает список заявок, отмененных клиентами.
	private async void CancelledRequests_Tapped(object sender, TappedEventArgs e)
	{
		await OpenRequests("cancelled");
	}

	// Открывает страницу услуг при нажатии на карточку статистики.
	private async void ServicesSummary_Tapped(object sender, TappedEventArgs e)
	{
		await Shell.Current.GoToAsync("AdminServicePage");
	}
// Обрабатывает нажатие кнопки.
private async void Schedule_Click(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("AdminTimePage");
	}

	// Обрабатывает нажатие кнопки.
	private async void Services_Click(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("AdminServicePage");
	}

	// Обрабатывает нажатие кнопки.
	private async void Logout_Click(object sender, EventArgs e)
	{
		State.IsAdminMode = false;
		await Shell.Current.GoToAsync("//MainPage");
	}

	// результат статистики
	// статистика
	private class StatisticResult
	{
		[JsonPropertyName("stats")]
		public Statistic Statistic { get; set; } = new Statistic();
	}

	// статистика
	private class Statistic
	{
		[JsonPropertyName("requests_total")] public int RequestsTotal { get; set; }
		[JsonPropertyName("requests_new")] public int RequestsNew { get; set; }
		[JsonPropertyName("requests_accepted")] public int RequestsAccepted { get; set; }
		[JsonPropertyName("requests_done")] public int RequestsDone { get; set; }
		[JsonPropertyName("requests_cancelled")] public int RequestsCancelled { get; set; }
		[JsonPropertyName("services_total")] public int ServicesTotal { get; set; }
	}
}
























