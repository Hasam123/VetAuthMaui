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

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await LoadData();
	}

	private async void Refresh_Click(object sender, EventArgs e)
	{
		await LoadData();
	}

	private async Task LoadData()
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

	private async Task OpenRequests(string status = "")
	{
		if (string.IsNullOrWhiteSpace(status))
			await Shell.Current.GoToAsync("RequestPage");
		else
			await Shell.Current.GoToAsync($"RequestPage?status={Uri.EscapeDataString(status)}");
	}

	private async void AllRequests_Tapped(object sender, TappedEventArgs e)
	{
		await OpenRequests();
	}

	private async void NewRequests_Tapped(object sender, TappedEventArgs e)
	{
		await OpenRequests("new");
	}

	private async void AcceptedRequests_Tapped(object sender, TappedEventArgs e)
	{
		await OpenRequests("accepted");
	}

	private async void DoneRequests_Tapped(object sender, TappedEventArgs e)
	{
		await OpenRequests("done");
	}

	private async void CancelledRequests_Tapped(object sender, TappedEventArgs e)
	{
		await OpenRequests("cancelled");
	}

	private async void ServicesSummary_Tapped(object sender, TappedEventArgs e)
	{
		await Shell.Current.GoToAsync("ServicePage");
	}
private async void Schedule_Click(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("AdminTimePage");
	}

	private async void Services_Click(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("ServicePage");
	}

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























