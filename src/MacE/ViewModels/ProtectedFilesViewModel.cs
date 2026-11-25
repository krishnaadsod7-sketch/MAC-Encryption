using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacE.Models;
using MacE.Services.Interfaces;
using System.Collections.ObjectModel;

namespace MacE.ViewModels;

/// <summary>
/// ViewModel for the Protected Files page.
/// Displays .mace files managed by the application.
/// Phase 1: UI structure only. File management comes in Phase 5.
/// </summary>
public partial class ProtectedFilesViewModel : BaseViewModel
{
    public ProtectedFilesViewModel()
    {
        Title = "Protected Files";
    }

    /// <summary>
    /// List of managed .mace files.
    /// </summary>
    public ObservableCollection<ProtectedFileInfo> Files { get; } = new();

    /// <summary>
    /// Load the list of managed files. Called when the page appears.
    /// Phase 1: Returns empty list. File tracking comes later.
    /// </summary>
    [RelayCommand]
    private async Task LoadFilesAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            // Phase 5: Load from local file index
            Files.Clear();
            SetStatus("Protected file management will be available in a later phase.", isError: false);
        }
        catch (Exception ex)
        {
            SetStatus($"Error loading files: {ex.Message}", isError: true);
        }
        finally
        {
            IsBusy = false;
        }

        await Task.CompletedTask;
    }
}
