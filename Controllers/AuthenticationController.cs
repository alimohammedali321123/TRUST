using BLL.IServices;
using DAL.DTOs.Authentication;
using DAL.DTOs.GenerateRefreshToken;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace Teaching_Platform.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthService service;

        public AuthenticationController(IAuthService _service)
        {
            service = _service;
        }


        [HttpPost("Registeration")]
        [EnableRateLimiting("limit")]
        public async Task<IActionResult> Registeration(Registeration reg)
        {
            var result = await service.Registeration(reg);


            if (!result.Success)
            {
                return BadRequest(result.Message);
            }


            return Ok(result.Message);
        }
        [HttpPost("Verify")]
        [EnableRateLimiting("limit")]
        public async Task<IActionResult> Verify(Verify verify)
        {
            var result = await service.Verify(verify);


            if (!result.Success)
            {
                return BadRequest(result.Message);
            }


            return Ok(result.Message);
        }
        [HttpPost("LogIn")]
        [EnableRateLimiting("limit")]
        public async Task<IActionResult> LogIn(LogIn logIn)
        {
            var result = await service.LogIn(logIn);
            if (!result.Success)
            {
                if(result.ErrorCode== "DeletedAccount")
                {
                    return StatusCode(410, result.Message);
                }
                return BadRequest(result.Message);
            }
            return Ok(new
            {
                token = result.Token,
                expire = result.Expire,
                refreshtoken = result.RefreshToken
            });
        }
        [HttpPost("GoogleLogin")]
        [EnableRateLimiting("limit")]
        public async Task<IActionResult> GoogleLogin(GoogleLogin dto)
        {
            var result = await service.GoogleLogin(dto);


            if (!result.Success)
            {
                return BadRequest(result.Message);
            }


            return Ok(new
            {
                token = result.Token,
                expire = result.Expire,
                refreshtoken = result.RefreshToken
            });
        }
        [HttpPost("ForgetPassword")]
        [EnableRateLimiting("limit")]
        public async Task<IActionResult> ForgetPassword(string email)
        {
            var result = await service.ForgetPassword(email);


            if (!result.Success)
            {
                return BadRequest(result.Message);
            }
            return Ok(result.Message);
        }



        [HttpPost("VerifyForgetPassword")]
        [EnableRateLimiting("limit")]
        public async Task<IActionResult> VerifyForgetPassword(Verify verify)
        {
            var result = await service.VerifyForgetPassword(verify);


            if (!result.Success)
            {
                return BadRequest(result.Message);
            }


            return Ok(result.Message);
        }



        [HttpPost("ResetForgottenPassword")]
        [EnableRateLimiting("limit")]
        public async Task<IActionResult> ResetForgottenPassword(ForgetPassword rp)
        {
            var result = await service.ResetForgottenPassword(rp);


            if (!result.Success)
            {
                return BadRequest(result.Message);
            }


            return Ok(result.Message);
        }



        [HttpPost("RequestChangePassword")]
        [EnableRateLimiting("limit")]
        public async Task<IActionResult> RequestChangePassword()
        {
            var userid = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userid == null)
            {
                return Unauthorized("يجب تسجيل الدخول اولا");
            }


            var result = await service.RequestChangePassword(userid);


            if (!result.Success)
            {
                return BadRequest(result.Message);
            }


            return Ok(result.Message);
        }



        [HttpPost("VerifyChangePassword")]
        [EnableRateLimiting("limit")]
        public async Task<IActionResult> VerifyChangePassword(Verify verify)
        {
            var result = await service.VerifyChangePassword(verify);


            if (!result.Success)
            {
                return BadRequest(result.Message);
            }


            return Ok(result.Message);
        }



        [HttpPost("ChangePassword")]
        [EnableRateLimiting("limit")]
        public async Task<IActionResult> ChangePassword(ResetPassword cp)
        {
            var userid = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userid == null)
            {
                return Unauthorized("يجب تسجيل الدخول اولا");
            }


            var result = await service.ChangePassword(cp, userid);


            if (!result.Success)
            {
                return BadRequest(result.Message);
            }


            return Ok(result.Message);
        }
        [HttpPost("RefreshToken")]
        public async Task<IActionResult> RefreshToken(Token rt)
        {
            var result = await service.RefreshToken(rt);


            if (!result.Success)
            {
                return BadRequest(result.Message);
            }


            return Ok(new
            {
                token = result.Token,
                expired = result.Expire,
                refreshtoken = result.RefreshToken
            });
        }
    }
}