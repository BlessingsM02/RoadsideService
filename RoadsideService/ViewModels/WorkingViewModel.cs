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
            }
            else
            {
                StopTracking();
            }
        }

        private void StartTracking()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            Task.Run(async () =>
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    await SendWorkingInfoAsync();
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
