using Firebase.Database;
using Firebase.Database.Query;
using RoadsideService.Models;
using RoadsideService.Views;
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
        private double _price;
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
            _firebaseClient = new FirebaseClient("https://roadside-service-f65db-default-rtdb.firebaseio.com/");
            _firebaseClient2 = new FirebaseClient("https://roadside1-1ffd7-default-rtdb.firebaseio.com/");
            LoadRequestDetailsCommand = new Command(async () => await LoadRequestDetailsAsync());
        }

        public ICommand OpenDialerCommand { get; }
        public ICommand CompleteRequestCommand { get; }

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
                        Amount = Price,
                        DriverId = DriverId,
                        Status = "Completed",
                        RatingId = RatingId,
                        Date = Date
                    });

                // Optionally update the request's status in the original "requests" table
                var requestToUpdate = (await _firebaseClient
                    .Child("request")
                    .OnceAsync<RequestData>())
                    .FirstOrDefault(r => r.Object.ServiceProviderId == mobileNumber);

                if (requestToUpdate != null)
                {
                    await _firebaseClient
                        .Child("request")
                        .Child(requestToUpdate.Key)
                        .PutAsync(new RequestData
                        {
                            ServiceProviderId = ServiceProviderId,
                            Latitude = Latitude,
                            Longitude = Longitude,
                            ServiceProviderLatitude = ServiceProviderLatitude,
                            ServiceProviderLongitude = ServiceProviderLongitude,
                            Price = Price,
                            DriverId = DriverId,
                            Status = "Completed",
                            RatingId = RatingId,
                            Date = Date
                        });
                }

                // Send a notification to the other application (pseudo code)
                //await SendNotificationAsync(DriverId, "Your request has been completed!");

                // Show a dialog with the total price (Amount)
                await Application.Current.MainPage.DisplayAlert("Request Completed", $"The total price is: K{Price}", "OK");
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

        public double Price
        {
            get => _price;
            set
            {
                _price = value;
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
                    .Child("request")
                    .OnceAsync<RequestData>();
                //retrieve user data
                var specificUser = await _firebaseClient2
                    .Child("users")
                    .OnceAsync<Users>();


                var requestData = requestDetails.FirstOrDefault(r => r.Object.ServiceProviderId == mobileNumber)?.Object;
                //var currentUser = specificUser.FirstOrDefault(c => c.Object.MobileNumber == requestData.DriverId)?.Object; //Specific user
                if (requestData != null)
                {
                    ServiceProviderId = requestData.ServiceProviderId;
                    Latitude = requestData.Latitude;
                    Longitude = requestData.Longitude;
                    ServiceProviderLatitude = requestData.ServiceProviderLatitude;
                    ServiceProviderLongitude = requestData.ServiceProviderLongitude;
                    Price = requestData.Price;
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
                    .OnceAsync<Users>();

                var user = userDetails.FirstOrDefault(u => u.Object.MobileNumber == DriverId);

                if (user != null)
                {
                    var driverDetails = $"{user.Object.FullName}";
                    DriverName = driverDetails;
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

        private async Task LoadVehicleDetailsAsync(string serviceProviderId)
        {
            try
            {
                // Query the users table to get the service provider details by ServiceProviderId
                var userDetails = await _firebaseClient2
                    .Child("users")
                    .OnceAsync<Users>();

                var user = userDetails.FirstOrDefault(u => u.Object.MobileNumber == serviceProviderId);

                if (user != null)
                {
                    // Once we have the user, query the vehicle details using their UserId
                    var vehicleDetails = await _firebaseClient2
                        .Child("vehicles")
                        .OnceAsync<Vehicle>();

                    var vehicle = vehicleDetails.FirstOrDefault(v => v.Object.UserId == user.Object.UserId.ToString());

                    if (vehicle != null)
                    {
                        VehicleDetails = $"{vehicle.Object.VehicleDescription} {vehicle.Object.PlateNumber}";
                    }
                    else
                    {
                        VehicleDetails = "No Vehicle Details Found";
                    }
                }
                else
                {
                    VehicleDetails = "User not found";
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to load vehicle details: {ex.Message}", "OK");
            }
        }

    }
}
