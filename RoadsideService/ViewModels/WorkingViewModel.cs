using Firebase.Database;
using Firebase.Database.Query;
using RoadsideService.Models;
using System.Windows.Input;

namespace RoadsideService.ViewModels
{
    public class WorkingViewModel : BindableObject
    {
        private string _id;
        private string _longitude;
        private string _latitude;
        private bool _isTracking;
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
                        // Handle location not available scenario
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
            // Check if a user with the same mobile number already exists
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
                // Update existing record
                await _firebaseClient
                    .Child("working")
                    .Child(user.Key)
                    .PutAsync(newTask);
            }
            else
            {
                // Create new record
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
                CheckRequest();
            }
            else
            {
                StopTracking();
            }
        }

        private async void CheckRequest()
        {
            try
            {
                var mobileNumber = Preferences.Get("mobile_number", string.Empty);

                // Fetch all requests from Firebase
                var allRequest = await _firebaseClient
                    .Child("ClickedMobileNumbers")
                    .OnceAsync<RequestData>();

                // Find the request specific to this service provider
                var ownRequest = allRequest.FirstOrDefault(c => c.Object.ServiceProviderId == mobileNumber);

                if (ownRequest != null && ownRequest.Object.Status == "Pending")
                {
                    // Show a dialog box to the user
                    bool acceptRequest = await Application.Current.MainPage.DisplayAlert(
                        "New Request",
                        $"You have a new request from driver {ownRequest.Object.DriverId} at location ({ownRequest.Object.Latitude}, {ownRequest.Object.Longitude}). Do you want to accept it?",
                        "Yes",
                        "No");

                    if (acceptRequest)
                    {
                        var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
                        var location = await Geolocation.GetLocationAsync(request);

                        // Update the request status to "Accepted"
                        ownRequest.Object.Status = "Accepted";
                        ownRequest.Object.Date = DateTime.Now;
                        ownRequest.Object.ServiceProviderLatitude = location.Latitude.ToString();
                        ownRequest.Object.ServiceProviderLongitude = location.Longitude.ToString();

                        await _firebaseClient
                            .Child("ClickedMobileNumbers")
                            .Child(ownRequest.Key)
                            .PutAsync(ownRequest.Object);

                        // Save the accepted request to the new "requests" table
                        await SaveRequestToNewTable(ownRequest.Object);
                        await _firebaseClient
                            .Child("ClickedMobileNumbers")
                            .Child(ownRequest.Key)
                            .DeleteAsync();

                        await Application.Current.MainPage.DisplayAlert("Request Accepted", "You have accepted the request.", "OK");
                    }
                    else
                    {
                        // Optionally delete or update the request status
                        await _firebaseClient
                            .Child("ClickedMobileNumbers")
                            .Child(ownRequest.Key)
                            .DeleteAsync();

                        // Save the declined request to the new "requests" table with status "Declined"
                        ownRequest.Object.Status = "Declined";
                        await SaveRequestToNewTable(ownRequest.Object);

                        await Application.Current.MainPage.DisplayAlert("Request Declined", "You have declined the request.", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to check request: {ex.Message}", "OK");
            }
        }

        private async Task SaveRequestToNewTable(RequestData requestData)
        {
            try
            {
                // Save the request to the new "requests" table
                await _firebaseClient
                    .Child("requests")
                    .PostAsync(requestData);
            }
            catch (Exception ex)
            {
                // Handle exceptions during saving
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to save request to new table: {ex.Message}", "OK");
            }
        }




        private void StartTracking()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            Task.Run(async () =>
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    //await CheckRequest();
                    await SendWorkingInfoAsync(); // Update location
                    await Task.Delay(TimeSpan.FromSeconds(3), _cancellationTokenSource.Token);
                }
            }, _cancellationTokenSource.Token);
        }


        private async void StopTracking()
        {
            _cancellationTokenSource?.Cancel();

            // Delete the record from the Firebase database
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
