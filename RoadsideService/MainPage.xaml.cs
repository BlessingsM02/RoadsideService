using RoadsideService.Services;
using RoadsideService.Views;

namespace RoadsideService
{
    public partial class MainPage : ContentPage
    {
        private readonly IAuthenticationService _authenticationService;

        public MainPage(IAuthenticationService authenticationService)
        {
            InitializeComponent();

            //check if user is already logged in
            var savedMobileNumber = Preferences.Get("mobile_number", string.Empty);
            if (!string.IsNullOrEmpty(savedMobileNumber))
            {
                // Directly navigate to NewPage1
                Shell.Current.GoToAsync($"//{nameof(HomePage)}");
            }

            _authenticationService = authenticationService;
        }


        private async void Submit_Clicked(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(MobileEntry.Text))
            {
                var isValidMobile = await _authenticationService.AuthenticateMobile(MobileEntry.Text);
                if (isValidMobile)
                {
                    // Show verification UI and disable input controls
                    verificationInfo.IsVisible = true;
                    MobileEntry.IsEnabled = false;
                    btnS.IsEnabled = false;
                }
                else
                {
                    await DisplayAlert("Error", "Invalid Mobile Number", "OK");
                }
            }
            else
            {
                await DisplayAlert("Error", "Please enter a Mobile Number", "OK");
            }
        }

        private async void btnVerify_Clicked(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(codeEntry.Text))
            {
                var isValidCode = await _authenticationService.ValidateOTP(codeEntry.Text);
                if (isValidCode)
                {
                    // Navigate to the next page upon successful validation
                    await Navigation.PushAsync(new Views.NewPage1());
                }
                else
                {
                    await DisplayAlert("Error", "Invalid Verification Code", "OK");
                }
            }
            else
            {
                await DisplayAlert("Error", "Please enter a Verification Code", "OK");
            }
        }
    }

}
