using Mopups.Services;
using System.Windows.Input;

namespace RoadsideService.Views;

public partial class RequestDetailsBottomSheet
{
    public ICommand CloseCommand { get; }

    public RequestDetailsBottomSheet()
    {
        InitializeComponent();

        // Command to close the popup
        CloseCommand = new Command(() => MopupService.Instance.PopAsync());


        BindingContext = this;

        CloseWhenBackgroundIsClicked = true; // Prevent closing when tapping outside the popup
    }
}