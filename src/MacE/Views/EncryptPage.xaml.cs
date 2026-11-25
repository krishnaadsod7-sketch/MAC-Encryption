using MacE.ViewModels;

namespace MacE.Views;

public partial class EncryptPage : ContentPage
{
    public EncryptPage(EncryptViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is EncryptViewModel vm)
        {
            vm.LoadDevicesCommand.Execute(null);
        }
    }
}
