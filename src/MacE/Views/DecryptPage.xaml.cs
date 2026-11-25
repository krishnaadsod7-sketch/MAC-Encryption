using MacE.ViewModels;

namespace MacE.Views;

public partial class DecryptPage : ContentPage
{
    public DecryptPage(DecryptViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
