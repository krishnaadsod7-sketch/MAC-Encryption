using MacE.ViewModels;

namespace MacE.Views;

public partial class ProtectedFilesPage : ContentPage
{
    public ProtectedFilesPage(ProtectedFilesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ProtectedFilesViewModel vm)
        {
            vm.LoadFilesCommand.Execute(null);
        }
    }
}
