using RoadsideService.ViewModels;
namespace RoadsideService.Views;

public partial class NewPage1 : ContentPage
{
	public NewPage1()
	{
		InitializeComponent();
		BindingContext = new UserViewModel();
    }
}