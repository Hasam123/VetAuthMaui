using Microsoft.UI.Xaml;

namespace VetAuthMaui.WinUI;

// запуск Windows
public partial class App : MauiWinUIApplication
{
	public App()
	{
		this.InitializeComponent();
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}









