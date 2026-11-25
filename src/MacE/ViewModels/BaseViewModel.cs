using CommunityToolkit.Mvvm.ComponentModel;

namespace MacE.ViewModels;

/// <summary>
/// Base class for all ViewModels in MAC-E.
/// Provides common properties used across pages.
/// 
/// Uses CommunityToolkit.Mvvm's ObservableObject, which provides:
/// - INotifyPropertyChanged implementation
/// - [ObservableProperty] source generator for automatic property notification
/// - [RelayCommand] source generator for ICommand implementations
/// </summary>
public partial class BaseViewModel : ObservableObject
{
    /// <summary>
    /// Indicates whether the ViewModel is currently performing an async operation.
    /// Bind to this in XAML to show/hide loading indicators.
    /// </summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>
    /// Title displayed in the page header or Shell tab.
    /// </summary>
    [ObservableProperty]
    private string _title = string.Empty;

    /// <summary>
    /// A status or error message to display to the user.
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>
    /// Whether the status message indicates an error (for styling purposes).
    /// </summary>
    [ObservableProperty]
    private bool _isError;

    /// <summary>
    /// Set a status message for the user.
    /// </summary>
    protected void SetStatus(string message, bool isError = false)
    {
        StatusMessage = message;
        IsError = isError;
    }

    /// <summary>
    /// Clear the status message.
    /// </summary>
    protected void ClearStatus()
    {
        StatusMessage = string.Empty;
        IsError = false;
    }
}
