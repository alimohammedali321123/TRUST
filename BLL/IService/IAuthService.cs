using BLL.Services;
using DAL.DTOs.Authentication;
using DAL.DTOs.GenerateRefreshToken;

namespace BLL.IServices
{
    public interface IAuthService
    {
        Task<ServiceResult> Registeration(Registeration reg);

        Task<ServiceResult> Verify(Verify verify);

        Task<ServiceResult> LogIn(LogIn logIn);

        Task<ServiceResult> GoogleLogin(GoogleLogin dto);

        Task<ServiceResult> ForgetPassword(string email);

        Task<ServiceResult> VerifyForgetPassword(Verify verify);

        Task<ServiceResult> ResetForgottenPassword(ForgetPassword rp);

        Task<ServiceResult> RequestChangePassword(string userid);

        Task<ServiceResult> VerifyChangePassword(Verify verify);

        Task<ServiceResult> ChangePassword(ResetPassword cp, string userid);
        Task<ServiceResult> RefreshToken(Token rt);
    }
}