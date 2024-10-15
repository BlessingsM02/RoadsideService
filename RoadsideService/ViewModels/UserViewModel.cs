using System.Windows.Input;
using Firebase.Database;
using Firebase.Database.Query;
using RoadsideService.Views;
using RoadsideService.Models;

namespace RoadsideService.ViewModels
{
    internal class UserViewModel : BindableObject
    {
        private string _fullName;
        private string _contact;
        private string _vehicleDescription;
        private string _plateNumber;
        private FirebaseClient _firebaseClient;

        public UserViewModel()
        {
            _firebaseClient = new FirebaseClient("https://roadside-service-f65db-default-rtdb.firebaseio.com/");
            SubmitCommand = new Command(async () => await SubmitAsync());
            CheckUserExistsAsync();
        }

        public string FullName
        {
            get => _fullName;
            set
            {
                _fullName = value;
                OnPropertyChanged();
            }
        }

        public string Contact
        {
            get => _contact;
            set
            {
                _contact = value;
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

        public ICommand SubmitCommand { get; }

        private async Task CheckUserExistsAsync()
        {
            var mobileNumber = Preferences.Get("mobile_number", string.Empty);

            if (!string.IsNullOrEmpty(mobileNumber))
            {
                var users = await _firebaseClient
                    .Child("users")
                    .OnceAsync<Users>();

                var user = users.FirstOrDefault(u => u.Object.MobileNumber == mobileNumber);

                if (user != null)
                {
                    await Application.Current.MainPage.DisplayAlert("Info", "A user with this contact already exists.", "OK");
                    await Shell.Current.GoToAsync($"//{nameof(ProfilePage)}");
                }
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Contact not found in preferences.", "OK");
            }
        }

        private async Task SubmitAsync()
        {
            if (string.IsNullOrEmpty(FullName) ||
                string.IsNullOrEmpty(VehicleDescription) || string.IsNullOrEmpty(PlateNumber))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "All fields are required.", "OK");
                return;
            }

            try
            {
                var mobileNumber = Preferences.Get("mobile_number", string.Empty);

                if (!string.IsNullOrEmpty(mobileNumber))
                {
                    // Get the UserId after saving the user
                    int userId = await SaveUser();
                    if (userId > 0) // Ensure a valid UserId was returned
                    {
                        await SaveVehicle(userId); // Pass the UserId to SaveVehicle
                        await Application.Current.MainPage.DisplayAlert("Success", "Information saved successfully.", "OK");
                        await Shell.Current.GoToAsync($"//{nameof(HomePage)}");
                    }
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "There was a problem with your contact number.", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }

        private async Task<int> SaveUser()
        {
            var users = await _firebaseClient
                .Child("users")
                .OnceAsync<Users>();

            var user = users.FirstOrDefault(u => u.Object.MobileNumber == Contact);

            if (user != null)
            {
                await Application.Current.MainPage.DisplayAlert("Alert", "A user with this contact already exists.", "OK");
                await Shell.Current.GoToAsync($"//{nameof(ProfilePage)}");
                return -1; // Return -1 to indicate failure
            }

            // Fetch the last UserId and increment it
            int newUserId = await GetNextUserId();
            var mobileNumber = Preferences.Get("mobile_number", string.Empty);
            var newUser = new Users
            {
                UserId = newUserId,
                FullName = FullName,
                MobileNumber = mobileNumber, // Updated field for contact
                //PlateNumber = PlateNumber // Include PlateNumber in user record
            };

            await _firebaseClient
                .Child("users")
                .PostAsync(newUser);

            return newUserId; // Return the new UserId
        }

        private async Task SaveVehicle(int userId)
        {
            var vehicle = new Vehicle
            {
                UserId = userId.ToString(), // Now using the UserId for reference
                VehicleDescription = VehicleDescription,
                PlateNumber = PlateNumber
            };

            await _firebaseClient
                .Child("vehicles")
                .PostAsync(vehicle);
        }

        private async Task<int> GetNextUserId()
        {
            var users = await _firebaseClient
                .Child("users")
                .OrderBy("UserId")
                .OnceAsync<Users>();

            if (users.Any())
            {
                var lastUser = users.OrderByDescending(u => u.Object.UserId).FirstOrDefault();
                return lastUser.Object.UserId + 1;
            }

            return 1; // If no users exist, start from 1
        }


    }
}