using Firebase.Database;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Mopups.Services;

namespace RoadsideService.Views;

public partial class RequestDetailsPage : ContentPage
{
	public RequestDetailsPage()
	{
		InitializeComponent();

	}
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Fetch the user's current location
        var geolocationRequest = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(10));
        var location = await Geolocation.GetLocationAsync(geolocationRequest);

        if (location != null)
        {
            // Center the map on the user's location
            userMap.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromMeters(200)));

            // Add a pin for the user's current location
            var userPin = new Pin
            {
                Label = "Your Location",
                Location = new Location(location.Latitude, location.Longitude),
                Type = PinType.Place
            };
            userMap.Pins.Add(userPin);
        }
        else
        {
            await DisplayAlert("Error", "Unable to get current location.", "OK");
            return;
        }

        // Retrieve the service provider's coordinates from the "requests" table in Firebase
        
    }

    private async void Button_Clicked(object sender, EventArgs e)
    {
        var bottomSheet = new RequestDetailsBottomSheet();

        await MopupService.Instance.PushAsync(bottomSheet);
    }
}