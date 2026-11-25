namespace MacE;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		// Apply saved theme preference (System Default, Light, or Dark)
		var themePref = Preferences.Default.Get("MacE_UserAppTheme", "System");
		UserAppTheme = themePref switch
		{
			"Light" => AppTheme.Light,
			"Dark" => AppTheme.Dark,
			_ => AppTheme.Unspecified
		};
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}
