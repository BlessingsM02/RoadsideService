using Firebase.Database;
using RoadsideService.Services;
using RoadsideService.Views;
using RoadsideService.Models;

namespace RoadsideService
{
    public partial class MainPage : ContentPage
    {
        private readonly IAuthenticationService _authenticationService;
        private bool _isOTPPhase = false;
        private readonly FirebaseClient _firebaseClient;

        public MainPage(IAuthenticationService authenticationService)
        {
            InitializeComponent();
            _firebaseClient = new FirebaseClient("https://roadside-service-f65db-default-rtdb.firebaseio.com/");
            _authenticationService = authenticationService;
            CheckIfUserHasRequest();
            CheckIfUserIsLoggedIn();
        }

        //check if user is already logged in
        private void CheckIfUserIsLoggedIn()
        {
            var savedMobileNumber = Preferences.Get("mobile_number", string.Empty);
            if (!string.IsNullOrEmpty(savedMobileNumber))
            {
                Shell.Current.GoToAsync($"//{nameof(HomePage)}");
            }
        }

        //check if user has an active request
        private async Task CheckIfUserHasRequest()
        {
            var savedMobileNumber = Preferences.Get("mobile_number", string.Empty);
            var requests = await _firebaseClient
                        .Child("request")
                        .OnceAsync<dynamic>();

            foreach (var request in requests)
            {

                if (request.Object.ServiceProviderId == savedMobileNumber && request.Object.Status == "Accepted")
                {
                    await Shell.Current.GoToAsync($"//{nameof(RequestDetailsPage)}");
                }
            }
        }

        private async void Submit_Clicked(object sender, EventArgs e)
        {
            if (_isOTPPhase)
            {
                await VerifyOTP();
            }
            else
            {
                await SubmitMobileNumber();
            }
        }

        private async Task SubmitMobileNumber()
        {
            try
            {
                if (IsValidMobileNumber())
                {
                    IsBusy = true; // Show loading indicator
                    var isValidMobile = await _authenticationService.AuthenticateMobile("+26" + MobileEntry.Text);
                    if (isValidMobile)
                    {
                        TransitionToOTPPhase();
                    }
                    else
                    {
                        await DisplayAlert("Error", "Failed to send OTP. Please try again.", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false; // Hide loading indicator
            }
        }


        private async Task VerifyOTP()
        {
            if (IsValidOTP())
            {
                var isValidCode = await _authenticationService.ValidateOTP(codeEntry.Text);
                if (isValidCode)
                {
                    await Navigation.PushAsync(new NewPage1());
                }
                else
                {
                    await DisplayAlert("Error", "Invalid Verification Code", "OK");
                }
            }
        }

        private void TransitionToOTPPhase()
        {
            _isOTPPhase = true;
            MobileEntry.IsEnabled = false; // Disable mobile number input
            codeEntry.IsEnabled = true;    // Enable OTP input
            btnSubmit.Text = "Verify";     // Change button text to "Verify"
        }

        private bool IsValidMobileNumber()
        {
            if (string.IsNullOrWhiteSpace(MobileEntry.Text))
            {
                DisplayAlert("Error", "Please enter a Mobile Number", "OK");
                return false;
            }
            return true;
        }

        private bool IsValidOTP()
        {
            if (string.IsNullOrWhiteSpace(codeEntry.Text))
            {
                DisplayAlert("Error", "Please enter a Verification Code", "OK");
                return false;
            }
            return true;
        }
    }
}