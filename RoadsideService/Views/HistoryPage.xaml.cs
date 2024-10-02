using RoadsideService.ViewModels;

namespace RoadsideService.Views;

public partial class HistoryPage : ContentPage
{
	public HistoryPage()
	{
		InitializeComponent();
        BindingContext = new HistoryViewModel();
    }
}