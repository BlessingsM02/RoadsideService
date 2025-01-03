﻿using Firebase.Database;
using Firebase.Database.Query;
using RoadsideService.Models;
using RoadsideService.Views;
using System.Windows.Input;
using System.Threading;
using Microsoft.Maui.Devices.Sensors;
using Plugin.LocalNotification;

namespace RoadsideService.ViewModels
{
    public class WorkingViewModel : BindableObject
    {
        private string _id;
        private string _longitude;
        private string _latitude;
        private bool _isTracking;
        private string _toggleTrackingButtonText = "Start Working";
        private Color _trackingButtonColor = Colors.Green;
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
            }
        }

        public string ToggleTrackingButtonText
        {
            get => _toggleTrackingButtonText;
            set
            {
                _toggleTrackingButtonText = value;
                OnPropertyChanged();
            }
        }

        public Color TrackingButtonColor
        {
            get => _trackingButtonColor;
            set
            {
                _trackingButtonColor = value;
                OnPropertyChanged();
            }
        }

        public ICommand ToggleTrackingCommand { get; }

        private async void ToggleTracking()
        {
            if (IsTracking)
            {
                StopTracking();
                ToggleTrackingButtonText = "Start Working";
                TrackingButtonColor = Colors.Green;
            }
            else
            {
                bool hasActiveRequests = await HasActiveRequests();
                if (hasActiveRequests)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Active Request",
                        "You already have an active request. Complete it before starting work.",
                        "OK");
                    await Shell.Current.GoToAsync($"//{nameof(RequestDetailsPage)}");
                    
                }

                StartTracking();
                ToggleTrackingButtonText = "Stop Working";
                TrackingButtonColor = Colors.Red;
            }
        }

        private async Task<bool> HasActiveRequests()
        {
            try
            {
                var mobileNumber = Preferences.Get("mobile_number", string.Empty);
                if (string.IsNullOrEmpty(mobileNumber))
                {
                    return false; // No mobile number means no active requests for this user.
                }

                var allRequests = await _firebaseClient.Child("request").OnceAsync<RequestData>();
                return allRequests.Any(request =>
                    request.Object.ServiceProviderId == mobileNumber &&
                     request.Object.Status == "Accepted");
            }
            catch (Exception)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Something went wrong", "OK");
                return false;
            }
        }


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
            catch (FeatureNotEnabledException)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Geolocation is not enabled. Please enable it.", "OK");
            }
            catch (PermissionException)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Geolocation permission is denied.", "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Failed to send working info.", "OK");
            }
        }

        private async Task<bool> SaveLocation(string mobileNumber)
        {
            int retryCount = 0;
            const int maxRetries = 3;

            while (retryCount < maxRetries)
            {
                try
                {
                    var users = await _firebaseClient.Child("working").OnceAsync<Working>();
                    var user = users.FirstOrDefault(u => u.Object.Id == mobileNumber);

                    var newTask = new Working
                    {
                        Id = mobileNumber,
                        Latitude = Latitude,
                        Longitude = Longitude,
                    };

                    if (user != null)
                    {
                        await _firebaseClient.Child("working").Child(user.Key).PutAsync(newTask);
                    }
                    else
                    {
                        await _firebaseClient.Child("working").PostAsync(newTask);
                    }
                    return true;
                }
                catch (FirebaseException)
                {
                    retryCount++;
                    if (retryCount == maxRetries)
                    {
                        await Application.Current.MainPage.DisplayAlert("Error", "Something went wrong with the location.", "OK");
                        return false;
                    }
                    await Task.Delay(2000);
                }
                catch (Exception)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "An unexpected error occurred while saving location.", "OK");
                    return false;
                }
            }
            return false;
        }

        private void StartTracking()
        {
            if (IsTracking) return;

            _isTracking = true;
            CheckRequest();
            _cancellationTokenSource = new CancellationTokenSource();
            Task.Run(async () =>
            {
                try
                {
                    while (!_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        await SendWorkingInfoAsync();
                        await Task.Delay(TimeSpan.FromSeconds(5), _cancellationTokenSource.Token);
                    }
                }
                catch (TaskCanceledException)
                {
                    // Task was canceled, safely exit
                }
                catch (Exception)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Failed to track location.", "OK");
                }
            }, _cancellationTokenSource.Token);
        }

        private void StopTracking()
        {
            if (!_isTracking) return;

            _isTracking = false;
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            DeleteLocation();
        }

        private async void DeleteLocation()
        {
            try
            {
                var mobileNumber = Preferences.Get("mobile_number", string.Empty);
                if (!string.IsNullOrEmpty(mobileNumber))
                {
                    var users = await _firebaseClient.Child("working").OnceAsync<Working>();
                    var user = users.FirstOrDefault(u => u.Object.Id == mobileNumber);
                    if (user != null)
                    {
                        await _firebaseClient.Child("working").Child(user.Key).DeleteAsync();
                    }
                }
            }
            catch (Exception)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Failed to delete location data.", "OK");
            }
        }
        private async Task ShowNotification(string title, string description)
        {
            var tes = new NotificationRequest
            {
                NotificationId = 114,
                Title = title,
                Description = description,
                BadgeNumber = 42,

            };
            await LocalNotificationCenter.Current.Show(tes);
        }
        private async void CheckRequest()
        {
            try
            {
                var mobileNumber = Preferences.Get("mobile_number", string.Empty);
                var allRequest = await _firebaseClient.Child("request").OnceAsync<RequestData>();
                var ownRequest = allRequest.FirstOrDefault(c => c.Object.ServiceProviderId == mobileNumber);

                if (ownRequest != null && ownRequest.Object.Status == "Pending")
                {
                    await ShowNotification("New Request", $"You have a new request from {ownRequest.Object.DriverId}");
                    bool acceptRequest = await Application.Current.MainPage.DisplayAlert(
                        "New Request",
                        $"You have a new request from driver {ownRequest.Object.DriverId}. Do you want to accept it?",
                        "Yes",
                        "No");

                    if (acceptRequest)
                    {
                        var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
                        var location = await Geolocation.GetLocationAsync(request);

                        ownRequest.Object.Status = "Accepted";
                        ownRequest.Object.Date = DateTime.Now;
                        ownRequest.Object.Price = ownRequest.Object.Price;
                        ownRequest.Object.ServiceProviderLatitude = location.Latitude;
                        ownRequest.Object.ServiceProviderLongitude = location.Longitude;

                        await _firebaseClient.Child("request").Child(ownRequest.Key).PutAsync(ownRequest.Object);

                        //await SaveRequestToNewTable(ownRequest.Object);
                        //await _firebaseClient.Child("request").Child(ownRequest.Key).DeleteAsync();

                        StopTracking();
                        await Application.Current.MainPage.DisplayAlert("Request Accepted", "You have accepted the request.", "OK");
                        await Shell.Current.GoToAsync($"//{nameof(RequestDetailsPage)}");
                    }
                    if(!acceptRequest)
                    {
                      
                        ownRequest.Object.Status = "Declined";
                        await _firebaseClient.Child("request").Child(ownRequest.Key).PutAsync(ownRequest.Object);
                        await Application.Current.MainPage.DisplayAlert("Request Declined", "You have declined the request.", "OK");
                    }
                }
            }
            catch (Exception)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Failed to check requests.", "OK");
            }
        }

        private async Task SaveRequestToNewTable(RequestData requestData)
        {
            try
            {
                await _firebaseClient.Child("request").PostAsync(requestData);
            }
            catch (Exception)
            {
                await Application.Current.MainPage.DisplayAlert("Info", "Something went wrong while saving the request.", "OK");
            }
        }
    }
}