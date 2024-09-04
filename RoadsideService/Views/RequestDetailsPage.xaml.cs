using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Mopups.Services;
using RoadsideService.Models;
using RoadsideService.ViewModels;


namespace RoadsideService.Views;
public partial class RequestDetailsPage : ContentPage
{
    private readonly FirebaseClient _firebaseClient;
    private CancellationTokenSource _cancellationTokenSource;
    private Location _userLocation;
    private Location _serviceProviderLocation;

    public RequestDetailsPage()
    {
        InitializeComponent();
        _firebaseClient = new FirebaseClient("https://roadside-service-f65db-default-rtdb.firebaseio.com/");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _cancellationTokenSource = new CancellationTokenSource();

        // Start updating location every 5 seconds
        StartLocationUpdates();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _cancellationTokenSource?.Cancel(); // Stop location updates when page disappears
    }

    private async void StartLocationUpdates()
    {
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            await UpdateLocationAsync();
            await Task.Delay(TimeSpan.FromSeconds(5), _cancellationTokenSource.Token);
        }
    }

    private async Task UpdateLocationAsync()
    {
        try
        {
            // Fetch the user's current location
            var geolocationRequest = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(10));
            var location = await Geolocation.GetLocationAsync(geolocationRequest);

            if (location != null)
            {
                _userLocation = location;

                //Update user's location on the map
                //userMap.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromMeters(200)));
                //Update the request table with the current location

                var mobileNumber = Preferences.Get("mobile_number", string.Empty);
                var serviceRequest = await _firebaseClient
                    .Child("requests")
                    .OnceAsync<RequestData>();

                var currentServiceRequest = serviceRequest.FirstOrDefault(u => u.Object.ServiceProviderId == mobileNumber);

                if (currentServiceRequest != null)
                {
                    currentServiceRequest.Object.ServiceProviderLatitude = location.Latitude;
                    currentServiceRequest.Object.ServiceProviderLongitude = location.Longitude;

                    await _firebaseClient
                        .Child("requests")
                        .Child(currentServiceRequest.Key)
                        .PutAsync(currentServiceRequest.Object);

                    // Update or add the user's current location pin on the map
                    var userPin = new Pin
                    {
                        Label = "Your Location",
                        Location = new Location(location.Latitude, location.Longitude),
                        Type = PinType.Place
                    };
                    userMap.Pins.Clear();
                    userMap.Pins.Add(userPin);

                    // Get the service provider's location and add a pin
                    var latitude = currentServiceRequest.Object.Latitude;
                    var longitude = currentServiceRequest.Object.Longitude;

                    
                        _serviceProviderLocation = new Location(latitude, longitude);
                        var servicePin = new Pin
                        {
                            Label = "Driver Location",
                            Location = _serviceProviderLocation,
                            Type = PinType.Place
                        };
                        userMap.Pins.Add(servicePin);

                        // Draw the route between the two locations
                        DrawRoute(_userLocation, _serviceProviderLocation);
                    
                }
            }
            else
            {
                await DisplayAlert("Error", "Unable to get current location.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Something went wrong: {ex.Message}", "OK");
        }
    }

    private void DrawRoute(Location startLocation, Location endLocation)
    {
        var routeLine = new Polyline
        {
            StrokeColor = Colors.Blue,
            StrokeWidth = 3
        };

        // Assuming we use straight-line points for the route (a real implementation would require route data)
        routeLine.Geopath.Add(startLocation);
        routeLine.Geopath.Add(endLocation);

        userMap.MapElements.Clear();
        userMap.MapElements.Add(routeLine);
    }

    private async void Button_Clicked(object sender, EventArgs e)
    {
        var bottomSheet = new RequestDetailsBottomSheet();
        await MopupService.Instance.PushAsync(bottomSheet);
    }
}

