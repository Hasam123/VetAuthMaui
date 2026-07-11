using Microsoft.Extensions.DependencyInjection;

namespace VetAuthMaui;

// запуск приложения
public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	// Создает главное окно приложения.
	protected override Window CreateWindow(IActivationState activationState)
	{
		return new Window(new AppShell());
	}
}













