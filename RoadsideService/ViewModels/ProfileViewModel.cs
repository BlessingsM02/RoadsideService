
using Firebase.Database;
using RoadsideService.Models;

namespace RoadsideService.ViewModels
{
    internal class ProfileViewModel : BindableObject
    {
        private string _firstName;
        private string _lastName;
        private string _vehicleDescription;
        private string _plateNumber;
        private string _mobileNumber;
        private FirebaseClient _firebaseClient;

        public ProfileViewModel()
        {
            _firebaseClient = new FirebaseClient("https://roadside-service-f65db-default-rtdb.firebaseio.com/");
            LoadUserProfileCommand = new Command(async () => await LoadUserProfileAsync());
            LoadUserProfileCommand.Execute(null);
        }

        public string FirstName
        {
            get => _firstName;
            set
            {
                _firstName = value;
                OnPropertyChanged();
            }
        }

        public string LastName
        {
            get => _lastName;
            set
            {
                _lastName = value;
                OnPropertyChanged();
            }
        }

        public string VehicleDescription
        {
            get => _vehicleDescription;
            set
            {
                _vehicleDescription = value;
                OnPropertyChanged();
            }
        }

        public string PlateNumber
        {
            get => _plateNumber;
            set
            {
                _plateNumber = value;
                OnPropertyChanged();
            }
        }

        public string MobileNumber
        {
            get => _mobileNumber;
            set
            {
                _mobileNumber = value;
                OnPropertyChanged();
            }
        }

        public Command LoadUserProfileCommand { get; }

        private async Task LoadUserProfileAsync()
        {
            // Retrieve the mobile number from preferences
            var mobileNumber = Preferences.Get("mobile_number", string.Empty);

            if (!string.IsNullOrEmpty(mobileNumber))
            {
                // Retrieve user details
                var users = await _firebaseClient
                    .Child("users")
                    .OnceAsync<Users>();

                var user = users.FirstOrDefault(u => u.Object.MobileNumber == mobileNumber)?.Object;

                if (user != null)
                {
                    FirstName = user.FirstName;
                    LastName = user.LastName;
                    MobileNumber = user.MobileNumber;

                    // Retrieve vehicle details using the user ID
                    var vehicles = await _firebaseClient
                        .Child("vehicles")
                        .OnceAsync<Vehicle>();

                    var vehicle = vehicles.FirstOrDefault(v => v.Object.UserId == mobileNumber)?.Object;

                    if (vehicle != null)
                    {
                        VehicleDescription = vehicle.VehicleDescription;
                        PlateNumber = vehicle.PlateNumber;
                    }
                    else
                    {
                        // Handle the case where the vehicle is not found
                        await Application.Current.MainPage.DisplayAlert("Error", "Vehicle not found.", "OK");
                    }
                }
                else
                {
                    // Handle the case where the user is not found
                    await Application.Current.MainPage.DisplayAlert("Error", "User not found.", "OK");
                }
            }
            else
            {
                // Handle the case where the mobile number is not found in preferences
                await Application.Current.MainPage.DisplayAlert("Error", "Mobile number not found in preferences.", "OK");
            }
        }

    }

}