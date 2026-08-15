namespace VetAuthMaui;

using System.Globalization;
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

	// Включает или выключает выбор даты вакцинации по флажку.
	private void VaccinationDate_CheckedChanged(object sender, CheckedChangedEventArgs e)
	{
		VaccinationDatePicker.IsEnabled = e.Value;
	}

	// Сохраняет измененные данные.
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

		if (age.Length > 15)
		{
			await DisplayAlertAsync("Ошибка", "Возраст должен содержать не более 15 символов.", "ОК");
			return;
		}

		if (weight != "")
		{
			weight = weight.Replace(',', '.');
			if (!decimal.TryParse(weight, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var weightValue)
				|| weightValue <= 0 || weightValue > 999.99m)
			{
				await DisplayAlertAsync("Ошибка", "Укажите вес от 0,01 до 999,99 кг.", "ОК");
				return;
			}

			weight = weightValue.ToString("0.##", CultureInfo.InvariantCulture);
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

