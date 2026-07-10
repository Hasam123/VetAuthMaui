namespace VetAuthMaui;

using System.Net.Http.Json;

// Форма добавления питомца.
public partial class AddPetPage : ContentPage
{
	private readonly HttpClient httpClient = new HttpClient();

	public AddPetPage()
	{
		InitializeComponent();
		httpClient.Timeout = TimeSpan.FromSeconds(10);
		TypePicker.SelectedIndex = 0;
	}

	private void VaccinationDate_CheckedChanged(object sender, CheckedChangedEventArgs e)
	{
		VaccinationDatePicker.IsEnabled = e.Value;
	}

	private async void Save_Click(object sender, EventArgs e)
	{
		var name = NameEntry.Text?.Trim() ?? "";
		var type = TypePicker.SelectedItem?.ToString() ?? "";
		var age = AgeEntry.Text?.Trim() ?? "";
		var weight = WeightEntry.Text?.Trim() ?? "";
		var vaccinationDate = "";
		if (HasVaccinationDateCheckBox.IsChecked)
			vaccinationDate = VaccinationDatePicker.Date?.ToString("yyyy-MM-dd") ?? "";

		if (name == "" || type == "")
		{
			await DisplayAlertAsync("Ошибка", "Заполните кличку и вид питомца.", "ОК");
			return;
		}

		try
		{
			var data = new AddPetData(State.ClientPhone, name, type, age, weight, vaccinationDate);
			var response = await httpClient.PostAsJsonAsync($"{Api.BaseUrl}pets/create.php", data);
			var result = await response.Content.ReadFromJsonAsync<ApiResult>();

			if (!response.IsSuccessStatusCode || result?.Success != true)
			{
				await DisplayAlertAsync("Ошибка", result?.Message ?? "Питомец не добавлен.", "Понятно");
				return;
			}

			await DisplayAlertAsync("Готово", "Питомец добавлен.", "ОК");
			await Shell.Current.GoToAsync("..");
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Ошибка", $"Не удалось добавить питомца: {ex.Message}", "Понятно");
		}
	}
}

