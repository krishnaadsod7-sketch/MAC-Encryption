using MacE.ViewModels;

namespace MacE.Views;

public partial class DevicesPage : ContentPage
{
    public DevicesPage(DevicesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is DevicesViewModel vm)
        {
            vm.LoadDevicesCommand.Execute(null);
        }
    }
}
