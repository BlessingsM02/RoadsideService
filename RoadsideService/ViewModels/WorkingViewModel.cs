using Firebase.Database;
using Firebase.Database.Query;
using RoadsideService.Models;
using RoadsideService.Views;
using System.Windows.Input;

namespace RoadsideService.ViewModels
{
    public class WorkingViewModel : BindableObject
    {
        private string _id;
        private string _longitude;
        private string _latitude;
        private bool _isTracking;
        private bool _requestAccepted;
        private readonly FirebaseClient _firebaseClient;
        private CancellationTokenSource _cancellationTokenSource;

        public WorkingViewModel()
        {
            _firebaseClient = new FirebaseClient("https://roadside-service-f65db-default-rtdb.firebaseio.com/");
            ToggleTrackingCommand = new Command(ToggleTracking);
        }

        public string Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged();
            }
        }

        public string Longitude
        {
            get => _longitude;
            set
            {
                _longitude = value;
                OnPropertyChanged();
            }
        }

        public string Latitude
        {
            get => _latitude;
            set
            {
                _latitude = value;
                OnPropertyChanged();
            }
        }

        public bool IsTracking
        {
            get => _isTracking;
            set
            {
                _isTracking = value;
                OnPropertyChanged();
                ToggleTracking();
            }
        }

        public ICommand ToggleTrackingCommand { get; }

        private async Task SendWorkingInfoAsync()
        {
            try
            {
                var mobileNumber = Preferences.Get("mobile_number", string.Empty);
                if (!string.IsNullOrEmpty(mobileNumber))
                {
                    var geolocationRequest = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(10));
                    var location = await Geolocation.GetLocationAsync(geolocationRequest);

                    if (location != null)
                    {
                        Latitude = location.Latitude.ToString();
                        Longitude = location.Longitude.ToString();
                        await SaveLocation(mobileNumber);
                    }
                    else
                    {
                        await Application.Current.MainPage.DisplayAlert("Info", "An error occurred while trying to capture your location.", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exception
            }
        }

        private async Task<bool> SaveLocation(string mobileNumber)
        {
            var users = await _firebaseClient
                .Child("working")
                .OnceAsync<Working>();

            var user = users.FirstOrDefault(u => u.Object.Id == mobileNumber);

            var newTask = new Working
            {
                Id = mobileNumber,
                Latitude = Latitude,
                Longitude = Longitude,
            };

            if (user != null)
            {
                await _firebaseClient
                    .Child("working")
                    .Child(user.Key)
                    .PutAsync(newTask);
            }
            else
            {
                await _firebaseClient
                    .Child("working")
                    .PostAsync(newTask);
            }

            return true;
        }

        private void ToggleTracking()
        {
            if (IsTracking)
            {
                StartTracking();
            }
            else
            {
                StopTracking();
            }
        }

        private async Task CheckRequest()
        {
            try
            {
                var mobileNumber = Preferences.Get("mobile_number", string.Empty);

                var allRequests = await _firebaseClient
                    .Child("ClickedMobileNumbers")
                    .OnceAsync<RequestData>();

                var ownRequest = allRequests.FirstOrDefault(c => c.Object.ServiceProviderId == mobileNumber);

                if (ownRequest != null && ownRequest.Object.Status == "Pending")
                {
                    bool acceptRequest = await Application.Current.MainPage.DisplayAlert(
                        "New Request",
                        $"You have a new request from driver {ownRequest.Object.DriverId} at location ({ownRequest.Object.Latitude}, {ownRequest.Object.Longitude}). Do you want to accept it?",
                        "Yes",
                        "No");

                    if (acceptRequest)
                    {
                        var locationRequest = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
                        var location = await Geolocation.GetLocationAsync(locationRequest);

                        ownRequest.Object.Status = "Accepted";
                        ownRequest.Object.Date = DateTime.Now;
                        ownRequest.Object.ServiceProviderLatitude = location?.Latitude.ToString();
                        ownRequest.Object.ServiceProviderLongitude = location?.Longitude.ToString();

                        await SaveRequestToNewTable(ownRequest.Object);

                        _requestAccepted = true;

                        await _firebaseClient
                            .Child("ClickedMobileNumbers")
                            .Child(ownRequest.Key)
                            .DeleteAsync();

                        await Application.Current.MainPage.DisplayAlert("Request Accepted", "You have accepted the request.", "OK");
                        await Shell.Current.GoToAsync($"//{nameof(RequestDetailsPage)}");
                    }
                    else
                    {
                        ownRequest.Object.Status = "Declined";
                        await SaveRequestToNewTable(ownRequest.Object);

                        await _firebaseClient
                            .Child("ClickedMobileNumbers")
                            .Child(ownRequest.Key)
                            .DeleteAsync();

                        await Application.Current.MainPage.DisplayAlert("Request Declined", "You have declined the request.", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to check request: {ex.Message}", "OK");
            }
        }

        private async Task SaveRequestToNewTable(RequestData requestData)
        {
            try
            {
                await _firebaseClient
                    .Child("requests")
                    .PostAsync(requestData);
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to save request to new table: {ex.Message}", "OK");
            }
        }

        private void StartTracking()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _requestAccepted = false;

            Task.Run(async () =>
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested && !_requestAccepted)
                {
                    await CheckRequest();
                    await SendWorkingInfoAsync();
                    await Task.Delay(TimeSpan.FromSeconds(5), _cancellationTokenSource.Token);
                }
            }, _cancellationTokenSource.Token);
        }

        private async void StopTracking()
        {
            _cancellationTokenSource?.Cancel();

            try
            {
                var mobileNumber = Preferences.Get("mobile_number", string.Empty);
                if (!string.IsNullOrEmpty(mobileNumber))
                {
                    var users = await _firebaseClient
                        .Child("working")
                        .OnceAsync<Working>();

                    var user = users.FirstOrDefault(u => u.Object.Id == mobileNumber);

                    if (user != null)
                    {
                        await _firebaseClient
                            .Child("working")
                            .Child(user.Key)
                            .DeleteAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exception
            }
        }
    }
}
