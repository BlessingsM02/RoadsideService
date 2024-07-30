using RoadsideService.ViewModels;

namespace RoadsideService.Views;

public partial class ProfilePage : ContentPage
{
	public ProfilePage()
	{
		InitializeComponent();
        BindingContext = new ProfileViewModel();

    }
    private async void OnLogoutButtonClicked(object sender, EventArgs e)
    {
        // Clear the preferences
        Preferences.Clear();

        // Navigate back to the login page
        await Shell.Current.GoToAsync($"//{nameof(MainPage)}");
    }
}