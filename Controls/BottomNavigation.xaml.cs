namespace VetAuthMaui;

public partial class BottomNavigation : ContentView
{
	public BottomNavigation()
	{
		InitializeComponent();
	}

	// Открывает главную вкладку клиента.
	private async void Home_Tapped(object sender, TappedEventArgs e)
	{
		await Shell.Current.GoToAsync("//HomePage");
	}

	// Открывает вкладку с услугами клиники.
	private async void Services_Tapped(object sender, TappedEventArgs e)
	{
		State.IsAdminMode = false;
		await Shell.Current.GoToAsync("//ServicePage");
	}

	// Открывает вкладку для записи на прием.
	private async void Record_Tapped(object sender, TappedEventArgs e)
	{
		await Shell.Current.GoToAsync("//RecordPage");
	}

	// Открывает личный кабинет текущего клиента.
	private async void Profile_Tapped(object sender, TappedEventArgs e)
	{
		await Shell.Current.GoToAsync("//ClientPage");
	}
}
