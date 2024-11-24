using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Mopups.Services;
using Plugin.LocalNotification;
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
        try
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                // Update location and check request status
                await UpdateLocationAsync();

                // Wait 5 seconds between each update
                await Task.Delay(TimeSpan.FromSeconds(5), _cancellationTokenSource.Token);
            }
        }
        catch (TaskCanceledException)
        {
            // Handle task cancellation (e.g., when the page is closed or request is complete)
        }
        catch (Exception ex)
        {
            // Handle any other errors
            await DisplayAlert("Error", $"Something went wrong: {ex.Message}", "OK");
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

                // Fetch mobile number from preferences
                var mobileNumber = Preferences.Get("mobile_number", string.Empty);

                // Fetch the current request associated with the service provider
                var serviceRequest = await _firebaseClient
                    .Child("request")
                    .OnceAsync<RequestData>();

                var currentServiceRequest = serviceRequest.FirstOrDefault(u => u.Object.ServiceProviderId == mobileNumber);

                if (currentServiceRequest != null)
                {
                    // Check if the request is completed or canceled
                    if (currentServiceRequest.Object.Status == "Completed" || currentServiceRequest.Object.Status == "Canceled")
                    {
                        await DisplayAlert("Request Status", "This request has already been completed.", "OK");
                        _cancellationTokenSource?.Cancel(); // Stop further location updates
                        //await MopupService.Instance.PopAsync();
                        await Shell.Current.GoToAsync($"//{nameof(HomePage)}"); 
                        return; // Exit early
                    }

                    // Update service provider's location in Firebase
                    currentServiceRequest.Object.ServiceProviderLatitude = location.Latitude;
                    currentServiceRequest.Object.ServiceProviderLongitude = location.Longitude;

                    await _firebaseClient
                        .Child("request")
                        .Child(currentServiceRequest.Key)
                        .PutAsync(currentServiceRequest.Object);

                    // Get the service provider's location and update the map with a pin
                    var latitude = currentServiceRequest.Object.Latitude;
                    var longitude = currentServiceRequest.Object.Longitude;

                    _serviceProviderLocation = new Location(latitude, longitude);
                    var servicePin = new Pin
                    {
                        Label = "Driver Location",
                        Location = _serviceProviderLocation,
                        Type = PinType.Place
                    };

                    // Clear existing pins and add the updated pin
                    userMap.Pins.Clear();
                    userMap.Pins.Add(servicePin);

                    // Optionally, draw the route between the two locations
                    //DrawRoute(_userLocation, _serviceProviderLocation);
                }
                else
                {
                    await ShowNotification("Alert", "Service has been cancled");
                    await DisplayAlert("Alert", "Request has been Canceled.", "OK");
                    _cancellationTokenSource?.Cancel();
                    await Shell.Current.GoToAsync($"//{nameof(HomePage)}");
                    await MopupService.Instance.PopAsync();
                    return;// Stop further updates if the request is not found
                }
            }
            else
            {
                await DisplayAlert("Error", "Unable to get current location.", "OK");
            }
        }
        catch (Exception ex)
        {
            //await DisplayAlert("Error", "Something went wrong", "OK");
        }
    }
    private async Task ShowNotification(string title, string description)
    {
        var tes = new NotificationRequest
        {
            NotificationId = 113,
            Title = title,
            Description = description,
            BadgeNumber = 42,

        };
        await LocalNotificationCenter.Current.Show(tes);
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
