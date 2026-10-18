using CarnetDepenses.ViewModels;

namespace CarnetDepenses;

public partial class MainPage : ContentPage
{
    private readonly DepensesViewModel _vm;

    public MainPage(DepensesViewModel vm)   // injecte
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.ChargerAsync();    // recharge
    }
}
