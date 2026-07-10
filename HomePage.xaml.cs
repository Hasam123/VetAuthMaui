namespace VetAuthMaui;

// меню клиента
public partial class HomePage : ContentPage
{
	public HomePage()
	{
		InitializeComponent();
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		ClientNameLabel.Text = State.IsClientLoggedIn
			? $"Здравствуйте, {State.ClientName}"
			: "Все нужное для записи и контроля заявок";
	}

	private async void Profile_Click(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("ClientPage");
	}

	private async void Services_Click(object sender, EventArgs e)
	{
		State.IsAdminMode = false;
		await Shell.Current.GoToAsync("ServicePage");
	}

	private async void Appointment_Click(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("RecordPage");
	}

	private async void Contacts_Click(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("ContactPage");
	}

	private async void Logout_Click(object sender, EventArgs e)
	{
		State.LogoutClient();
		await Shell.Current.GoToAsync("//MainPage");
	}
}














