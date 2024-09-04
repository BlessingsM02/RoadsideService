using Mopups.Services;
using RoadsideService.ViewModels;
using System.Windows.Input;

namespace RoadsideService.Views;

public partial class RequestDetailsBottomSheet
{
    public ICommand CloseCommand { get; }
    private readonly RequestDetailsViewModel _viewModel;
    public RequestDetailsBottomSheet()
    {
        InitializeComponent();
        _viewModel = new RequestDetailsViewModel();
        BindingContext = _viewModel;
       // CloseCommand = new Command(() => MopupService.Instance.PopAsync());
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadRequestDetailsCommand.Execute(null);
    }
}