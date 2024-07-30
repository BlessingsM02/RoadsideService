
namespace RoadsideService.Services
{
    public interface IAuthenticationService
    {
        Task<bool> AuthenticateMobile(string mobile);
        Task<bool> ValidateOTP(string code);
    }
}
