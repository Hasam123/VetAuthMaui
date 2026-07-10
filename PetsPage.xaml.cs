namespace VetAuthMaui;

using System.Collections.ObjectModel;
using System.Net.Http.Json;

// Страница со списком питомцев клиента.
public partial class PetsPage : ContentPage
{
	private readonly HttpClient httpClient = new HttpClient();
	private readonly ObservableCollection<Pet> pets = new ObservableCollection<Pet>();

	public PetsPage()
	{
		InitializeComponent();
		httpClient.Timeout = TimeSpan.FromSeconds(10);
		PetsCollectionView.ItemsSource = pets;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		if (!State.IsClientLoggedIn)
		{
			await Shell.Current.GoToAsync("//MainPage");
			return;
		}

		await LoadPets();
	}

	private async Task LoadPets()
	{
		try
		{
			StatusLabel.Text = "Загрузка питомцев...";
			var url = $"{Api.BaseUrl}pets/list.php?phone={Uri.EscapeDataString(State.ClientPhone)}";
			var result = await httpClient.GetFromJsonAsync<PetResult>(url);

			pets.Clear();
			foreach (var pet in result?.Pets ?? new List<Pet>())
				pets.Add(pet);

			StatusLabel.Text = pets.Count == 0 ? "Питомцы пока не добавлены" : $"Добавлено питомцев: {pets.Count}";
		}
		catch (Exception ex)
		{
			StatusLabel.Text = "";
			await DisplayAlertAsync("Ошибка", $"Не удалось загрузить питомцев: {ex.Message}", "Понятно");
		}
	}

	private async void AddPet_Click(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("AddPetPage");
	}

	private async void EditPet_Click(object sender, EventArgs e)
	{
		State.SelectedPet = (Pet)((Button)sender).BindingContext;
		await Shell.Current.GoToAsync("EditPetPage");
	}

	private async void DeletePet_Click(object sender, EventArgs e)
	{
		var pet = (Pet)((Button)sender).BindingContext;
		var confirmed = await DisplayAlertAsync("Удалить питомца?", $"Карточка питомца «{pet.Name}» будет удалена.", "Удалить", "Отмена");

		if (!confirmed)
			return;

		try
		{
			var response = await httpClient.PostAsJsonAsync($"{Api.BaseUrl}pets/delete.php", new DeletePetData(pet.Id, State.ClientPhone));
			var result = await response.Content.ReadFromJsonAsync<ApiResult>();

			if (!response.IsSuccessStatusCode || result?.Success != true)
			{
				await DisplayAlertAsync("Ошибка", result?.Message ?? "Не удалось удалить питомца.", "Понятно");
				return;
			}

			await LoadPets();
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Ошибка", $"Не удалось удалить питомца: {ex.Message}", "Понятно");
		}
	}
}

