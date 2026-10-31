using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Core.View;
using NativeView = Android.Views.View;

namespace VetAuthMaui;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
// запуск Android
public class MainActivity : MauiAppCompatActivity
{
	private NativeView navigationBarBackground;

	protected override void OnCreate(Bundle savedInstanceState)
	{
		base.OnCreate(savedInstanceState);
		ConfigureNavigationBar();
	}

	protected override void OnResume()
	{
		base.OnResume();
		ConfigureNavigationBar();
	}

	private void ConfigureNavigationBar()
	{
		if (Window?.DecorView is not FrameLayout decor)
			return;

		var color = Android.Graphics.Color.ParseColor("#17213A");
		WindowCompat.GetInsetsController(Window, decor).AppearanceLightNavigationBars = false;

		// Older Android versions draw this background outside the app's content.
		if (!OperatingSystem.IsAndroidVersionAtLeast(35))
		{
			Window.SetNavigationBarColor(color);
			return;
		}

		if (navigationBarBackground == null)
		{
			// Android 15+ can draw content behind a transparent navigation bar.
			// Paint only the system inset, without changing MAUI's content padding.
			navigationBarBackground = new NativeView(this)
			{
				Clickable = false,
				Focusable = false,
				ImportantForAccessibility = ImportantForAccessibility.No
			};
			navigationBarBackground.SetBackgroundColor(color);
			decor.AddView(navigationBarBackground,
				new FrameLayout.LayoutParams(0, 0, GravityFlags.Bottom));
			ViewCompat.SetOnApplyWindowInsetsListener(navigationBarBackground,
				new NavigationBarInsetsListener());
		}

		navigationBarBackground.BringToFront();
		ViewCompat.RequestApplyInsets(navigationBarBackground);
	}

	private sealed class NavigationBarInsetsListener : Java.Lang.Object, IOnApplyWindowInsetsListener
	{
		public WindowInsetsCompat OnApplyWindowInsets(NativeView view, WindowInsetsCompat insets)
		{
			var bars = insets.GetInsets(WindowInsetsCompat.Type.NavigationBars());
			var layout = (FrameLayout.LayoutParams)view.LayoutParameters;
			layout.Width = bars.Bottom > 0 ? ViewGroup.LayoutParams.MatchParent : Math.Max(bars.Left, bars.Right);
			layout.Height = bars.Bottom > 0 ? bars.Bottom : ViewGroup.LayoutParams.MatchParent;
			layout.Gravity = bars.Bottom > 0 ? GravityFlags.Bottom
				: bars.Right > 0 ? GravityFlags.Right : GravityFlags.Left;
			view.LayoutParameters = layout;
			// The keyboard supplies its own navigation-bar background while open.
			view.Visibility = insets.IsVisible(WindowInsetsCompat.Type.Ime())
				? ViewStates.Invisible : ViewStates.Visible;
			return insets;
		}
	}
}







