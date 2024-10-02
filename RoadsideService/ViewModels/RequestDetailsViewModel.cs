using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.Maui.Controls;
using RoadsideService.Models;
using RoadsideService.Views;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RoadsideService.ViewModels
{
    public class RequestDetailsViewModel : BindableObject
    {
        private readonly FirebaseClient _firebaseClient;
        private readonly FirebaseClient _firebaseClient2;
        private string _serviceProviderId;
        private double _latitude;
        private double _longitude;
        private double _serviceProviderLatitude;
        private double _serviceProviderLongitude;
        private double _amount;
        private string _driverId;
        private string _status;
        private string _ratingId;
        private DateTime _date;
        private string _driverName;
        private string _vehicleDetails;

        public RequestDetailsViewModel()
        {
            OpenDialerCommand = new Command<string>(OpenDialer);
            CompleteRequestCommand = new Command(async () => await CompleteRequestAsync());
            _firebaseClient2 = new FirebaseClient("https://roadside1-1ffd7-default-rtdb.firebaseio.com/");
            _firebaseClient = new FirebaseClient("https://roadside-service-f65db-default-rtdb.firebaseio.com/");
            LoadRequestDetailsCommand = new Command(async () => await LoadRequestDetailsAsync());
        }

        public ICommand OpenDialerCommand { get; }
        public ICommand CompleteRequestCommand { get; }

        private void OpenDialer(string phoneNumber)
        {
            if (!string.IsNullOrWhiteSpace(phoneNumber))
            {
                try
                {
                    PhoneDialer.Open(phoneNumber);
                }
                catch (Exception ex)
                {
                    Application.Current.MainPage.DisplayAlert("Error", "Unable to open the dialer.", "OK");
                }
            }
        }


        private async Task CompleteRequestAsync()
        {
            try
            {
                // Get the mobile number
                var mobileNumber = Preferences.Get("mobile_number", string.Empty);

                // Create a completed request entry in Firebase
                await _firebaseClient
                    .Child("complete")
                    .PostAsync(new
                    {
                        ServiceProviderId = ServiceProviderId,
                        Latitude = Latitude,
                        Longitude = Longitude,
                        ServiceProviderLatitude = ServiceProviderLatitude,
                        ServiceProviderLongitude = ServiceProviderLongitude,
                        Amount = Amount,
                        DriverId = DriverId,
                        Status = "Completed",
                        RatingId = RatingId,
                        Date = DateTime.Now
                    });

                // Optionally update the request's status in the original "requests" table
                var requestToUpdate = (await _firebaseClient
                    .Child("requests")
                    .OnceAsync<RequestData>())
                    .FirstOrDefault(r => r.Object.ServiceProviderId == mobileNumber);

                if (requestToUpdate != null)
                {
                    await _firebaseClient
                        .Child("requests")
                        .Child(requestToUpdate.Key)
                        .PutAsync(new RequestData
                        {
                            ServiceProviderId = ServiceProviderId,
                            Latitude = Latitude,
                            Longitude = Longitude,
                            ServiceProviderLatitude = ServiceProviderLatitude,
                            ServiceProviderLongitude = ServiceProviderLongitude,
                            Amount = Amount,
                            DriverId = DriverId,
                            Status = "Completed",
                            RatingId = RatingId,
                            Date = Date
                        });
                }

                // Send a notification to the other application (pseudo code)
                //await SendNotificationAsync(DriverId, "Your request has been completed!");

                // Show a dialog with the total price (Amount)
                await Application.Current.MainPage.DisplayAlert("Request Completed", $"The total price is: ${Amount}", "OK");
                await Shell.Current.GoToAsync($"//{nameof(HomePage)}");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to complete request: {ex.Message}", "OK");
            }
        }


        private async Task SendNotificationAsync(string recipientId, string message)
        {
            // Logic to send a notification to another user/application
            // For example, you might use Firebase Cloud Messaging (FCM)
            await _firebaseClient
                .Child("notifications")
                .PostAsync(new
                {
                    RecipientId = recipientId,
                    Message = message,
                    Timestamp = DateTime.Now
                });
        }
        public string ServiceProviderId
        {
            get => _serviceProviderId;
            set
            {
                _serviceProviderId = value;
                OnPropertyChanged();
            }
        }

        public double Latitude
        {
            get => _latitude;
            set
            {
                _latitude = value;
                OnPropertyChanged();
            }
        }

        public double Longitude
        {
            get => _longitude;
            set
            {
                _longitude = value;
                OnPropertyChanged();
            }
        }

        public double ServiceProviderLatitude
        {
            get => _serviceProviderLatitude;
            set
            {
                _serviceProviderLatitude = value;
                OnPropertyChanged();
            }
        }

        public double ServiceProviderLongitude
        {
            get => _serviceProviderLongitude;
            set
            {
                _serviceProviderLongitude = value;
                OnPropertyChanged();
            }
        }

        public double Amount
        {
            get => _amount;
            set
            {
                _amount = value;
                OnPropertyChanged();
            }
        }

        public string DriverId
        {
            get => _driverId;
            set
            {
                _driverId = value;
                OnPropertyChanged();
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
            }
        }

        public string RatingId
        {
            get => _ratingId;
            set
            {
                _ratingId = value;
                OnPropertyChanged();
            }
        }

        public DateTime Date
        {
            get => _date;
            set
            {
                _date = value;
                OnPropertyChanged();
            }
        }

        public string DriverName
        {
            get => _driverName;
            set
            {
                _driverName = value;
                OnPropertyChanged();
            }
        }

        public string VehicleDetails
        {
            get => _vehicleDetails;
            set
            {
                _vehicleDetails = value;
                OnPropertyChanged();
            }
        }

        public ICommand LoadRequestDetailsCommand { get; }

        public async Task LoadRequestDetailsAsync()
        {
            try
            {
                var mobileNumber = Preferences.Get("mobile_number", string.Empty);

                // Retrieve request details from the requests table
                var requestDetails = await _firebaseClient
                    .Child("requests")
                    .OnceAsync<RequestData>();

                var requestData = requestDetails.FirstOrDefault(r => r.Object.ServiceProviderId == mobileNumber)?.Object;

                if (requestData != null)
                {
                    ServiceProviderId = requestData.ServiceProviderId;
                    Latitude = requestData.Latitude;
                    Longitude = requestData.Longitude;
                    ServiceProviderLatitude = requestData.ServiceProviderLatitude;
                    ServiceProviderLongitude = requestData.ServiceProviderLongitude;
                    Amount = requestData.Amount;
                    DriverId = requestData.DriverId;
                    Status = requestData.Status;
                    RatingId = requestData.RatingId;
                    Date = requestData.Date;

                    // Retrieve driver name from the users table
                    await LoadDriverNameAsync(requestData.DriverId);

                    // Retrieve vehicle details from the vehicle table
                    await LoadVehicleDetailsAsync(requestData.DriverId);
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "No matching request found.", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to load request details: {ex.Message}", "OK");
            }
        }

        private async Task LoadDriverNameAsync(string driverId)
        {
            try
            {
                var userDetails = await _firebaseClient2
                    .Child("users")
                    .OnceAsync<dynamic>();

                var user = userDetails.FirstOrDefault(u => u.Key == driverId);

                if (user != null)
                {
                    var DriverDetails = $"{user.Object.FirstName} {user.Object.LastName}";
                    DriverName = DriverDetails;
                }
                else
                {
                    DriverName = "????";
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to load driver name: {ex.Message}", "OK");
            }
        }

        private async Task LoadVehicleDetailsAsync(string driverId)
        {
            try
            {
                var vehicleDetails = await _firebaseClient2
                    .Child("vehicles")
                    .OnceAsync<dynamic>();

                var vehicle = vehicleDetails.FirstOrDefault(v => v.Object.UserId == driverId);

                if (vehicle != null)
                {
                    VehicleDetails = $"{vehicle.Object.VehicleDescription} {vehicle.Object.PlateNumber}";
                }
                else
                {
                    VehicleDetails = "No Vehicle Details Found";
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to load vehicle details: {ex.Message}", "OK");
            }
        }
    }
}
