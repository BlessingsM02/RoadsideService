using Firebase.Database;
using Firebase.Database.Query;
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
        private bool _isReadOnly = true;
        private string _editButtonText = "Edit Profile";
        private readonly FirebaseClient _firebaseClient;

        public ProfileViewModel()
        {
            _firebaseClient = new FirebaseClient("https://roadside-service-f65db-default-rtdb.firebaseio.com/");
            LoadUserProfileCommand = new Command(async () => await LoadUserProfileAsync());
            ToggleEditModeCommand = new Command(ToggleEditMode);
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

        public bool IsReadOnly
        {
            get => _isReadOnly;
            set
            {
                _isReadOnly = value;
                OnPropertyChanged();
            }
        }

        public string EditButtonText
        {
            get => _editButtonText;
            set
            {
                _editButtonText = value;
                OnPropertyChanged();
            }
        }

        public Command LoadUserProfileCommand { get; }
        public Command ToggleEditModeCommand { get; }

        private async Task LoadUserProfileAsync()
        {
            var mobileNumber = Preferences.Get("mobile_number", string.Empty);

            if (!string.IsNullOrEmpty(mobileNumber))
            {
                try
                {
                    var users = await _firebaseClient
                        .Child("users")
                        .OnceAsync<Users>();

                    var user = users.FirstOrDefault(u => u.Object.MobileNumber == mobileNumber)?.Object;

                    if (user != null)
                    {
                        FirstName = user.FirstName;
                        LastName = user.LastName;
                        MobileNumber = user.MobileNumber;

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
                            await Application.Current.MainPage.DisplayAlert("Error", "Vehicle not found.", "OK");
                        }
                    }
                    else
                    {
                        await Application.Current.MainPage.DisplayAlert("Error", "User not found.", "OK");
                    }
                }
                catch (Exception ex)
                {
                    // Handle exceptions, such as network errors
                    await Application.Current.MainPage.DisplayAlert("Error", "Unable to load user data. Please check your internet connection.", "OK");
                    return;
                }
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Mobile number not found in preferences.", "OK");
            }
        }

        private void ToggleEditMode()
        {
            if (IsReadOnly)
            {
                IsReadOnly = false;
                EditButtonText = "Save Changes";
            }
            else
            {
                IsReadOnly = true;
                EditButtonText = "Edit Profile";
                SaveProfileAsync();
            }
        }

        private async void SaveProfileAsync()
        {
            try
            {
                var mobileNumber = Preferences.Get("mobile_number", string.Empty);

                if (!string.IsNullOrEmpty(mobileNumber))
                {
                    var user = (await _firebaseClient
                        .Child("users")
                        .OnceAsync<Users>())
                        .FirstOrDefault(u => u.Object.MobileNumber == mobileNumber)?.Object;

                    if (user != null)
                    {
                        user.FirstName = FirstName;
                        user.LastName = LastName;

                        await _firebaseClient
                            .Child("users")
                            .Child(mobileNumber)
                            .PutAsync(user);

                        var vehicle = (await _firebaseClient
                            .Child("vehicles")
                            .OnceAsync<Vehicle>())
                            .FirstOrDefault(v => v.Object.UserId == mobileNumber)?.Object;

                        if (vehicle != null)
                        {
                            vehicle.VehicleDescription = VehicleDescription;
                            vehicle.PlateNumber = PlateNumber;

                            await _firebaseClient
                                .Child("vehicles")
                                .Child(mobileNumber)
                                .PutAsync(vehicle);

                            await Application.Current.MainPage.DisplayAlert("Success", "Profile updated successfully.", "OK");
                        }
                        else
                        {
                            await Application.Current.MainPage.DisplayAlert("Error", "Vehicle not found.", "OK");
                        }
                    }
                    else
                    {
                        await Application.Current.MainPage.DisplayAlert("Error", "User not found.", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to update profile: {ex.Message}", "OK");
            }
        }
    }
}
