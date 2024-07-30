using Firebase.Auth;
using Firebase;
using Android.App;

namespace RoadsideService.Services
{
    internal class AuthenticationService : IAuthenticationService
    {

        public Task<bool> AuthenticateMobile(string mobile)
        {
            throw new NotImplementedException();
        }



        public async Task<bool> ValidateOTP(string code)
        {
            throw new NotImplementedException();
        }
    }
}
