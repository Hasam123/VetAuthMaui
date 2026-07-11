namespace VetAuthMaui;

using System.Net.Http.Json;

// Форма изменения питомца.
public partial class EditPetPage : ContentPage
{
	private readonly HttpClient httpClient = new HttpClient();
	private Pet pet;

	public EditPetPage()
	{
		InitializeComponent();
		httpClient.Timeout = TimeSpan.FromSeconds(10);
	}

	// Загружает данные при открытии страницы.
	protected override async void OnAppearing()
	{
		base.OnAppearing();

		pet = State.SelectedPet;
		if (pet == null)
		{
			await DisplayAlertAsync("Ошибка", "Питомец не выбран.", "Понятно");
			await Shell.Current.GoToAsync("..");
			return;
		}

		NameEntry.Text = pet.Name;
		SetPetType(pet.Type);
		AgeEntry.Text = pet.Age;
		WeightEntry.Text = pet.Weight;
		if (DateTime.TryParse(pet.LastVaccinationDate, out var date))
		{
			HasVaccinationDateCheckBox.IsChecked = true;
			VaccinationDatePicker.Date = date;
		}
		else
		{
			HasVaccinationDateCheckBox.IsChecked = false;
		}
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

		try
		{
			var data = new UpdatePetData(pet.Id, State.ClientPhone, name, type, age, weight, vaccinationDate);
			var response = await httpClient.PostAsJsonAsync($"{Api.BaseUrl}pets/update.php", data);
			var result = await response.Content.ReadFromJsonAsync<ApiResult>();

			if (!response.IsSuccessStatusCode || result?.Success != true)
			{
				await DisplayAlertAsync("Ошибка", result?.Message ?? "Не удалось изменить питомца.", "Понятно");
				return;
			}

			await DisplayAlertAsync("Готово", "Данные питомца изменены.", "ОК");
			State.SelectedPet = null;
			await Shell.Current.GoToAsync("..");
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Ошибка", $"Не удалось изменить питомца: {ex.Message}", "Понятно");
		}
	}

	// Устанавливает значение или состояние.
	private void SetPetType(string type)
	{
		if (string.IsNullOrWhiteSpace(type))
		{
			TypePicker.SelectedIndex = 0;
			return;
		}

		for (var index = 0; index < TypePicker.Items.Count; index++)
		{
			if (string.Equals(TypePicker.Items[index], type, StringComparison.OrdinalIgnoreCase))
			{
				TypePicker.SelectedIndex = index;
				return;
			}
		}

		TypePicker.Items.Add(type);
		TypePicker.SelectedIndex = TypePicker.Items.Count - 1;
	}
}

