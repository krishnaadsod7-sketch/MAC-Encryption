using Microsoft.Extensions.Logging;
using MacE.Services;
using MacE.Services.Interfaces;
using MacE.ViewModels;
using MacE.Views;

namespace MacE;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // ── Platform-specific service registrations ──
        // Each platform provides its own implementation of IKeyStorageProvider
        // and IDeviceInfoProvider. These are registered via conditional compilation
        // because .NET MAUI's single-project structure compiles all platforms
        // from the same csproj.

#if WINDOWS
        builder.Services.AddSingleton<IKeyStorageProvider, Platforms.Windows.WindowsDpapiKeyStorage>();
        builder.Services.AddSingleton<IDeviceInfoProvider, Platforms.Windows.WindowsDeviceInfoProvider>();
#elif ANDROID
        builder.Services.AddSingleton<IKeyStorageProvider, Platforms.Android.AndroidKeystoreKeyStorage>();
        builder.Services.AddSingleton<IDeviceInfoProvider, Platforms.Android.AndroidDeviceInfoProvider>();
#endif

        // ── Shared service registrations ──
        // These services contain platform-independent business logic.
        // They depend on the platform-specific interfaces above via DI.
        builder.Services.AddSingleton<IDeviceService, DeviceService>();
        builder.Services.AddSingleton<ICryptoService, CryptoService>();
        builder.Services.AddSingleton<IMaceFileService, MaceFileService>();
        builder.Services.AddSingleton<IWorkspaceService, WorkspaceService>();

        // ── ViewModel registrations ──
        // Registered as Transient so each page gets a fresh ViewModel instance.
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<EncryptViewModel>();
        builder.Services.AddTransient<DecryptViewModel>();
        builder.Services.AddTransient<DevicesViewModel>();
        builder.Services.AddTransient<ProtectedFilesViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();

        // ── Page registrations ──
        // Pages receive their ViewModels via constructor injection.
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<EncryptPage>();
        builder.Services.AddTransient<DecryptPage>();
        builder.Services.AddTransient<DevicesPage>();
        builder.Services.AddTransient<ProtectedFilesPage>();
        builder.Services.AddTransient<SettingsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
