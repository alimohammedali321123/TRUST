using BLL.Interfaces;
using BLL.IServices;
using DAL.AccountVerificationCode;
using DAL.DbContext;
using DAL.DTOs.Authentication;
using DAL.DTOs.GenerateRefreshToken;
using DAL.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BLL.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IConfiguration config;
        private readonly TrustDB db;
        private readonly EmailService emailservice;

        public AuthService(
            UserManager<ApplicationUser> _userManager,
            IConfiguration _config,
            TrustDB _db,
            EmailService _emailservice)
        {
            userManager = _userManager;
            config = _config;
            db = _db;
            emailservice = _emailservice;
        }

        public async Task<ServiceResult> Registeration(Registeration reg)
        {
            var newuser = new ApplicationUser
            {
                FullName=reg.FullName,
                Email = reg.Email,
                UserName = reg.Email.Split('@')[0],
                IsDeleted = false,
                GoogleLogin=false
            };
            var existemailuser =await userManager.Users.FirstOrDefaultAsync(u=>u.Email==reg.Email);
            if (existemailuser != null)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "هذا الايميل مسجل مسبقا من فضلك ادحل ايميل اخر"
                };
            }
            var result = await userManager.CreateAsync(newuser, reg.Password);

            if (!result.Succeeded)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = GetErrors(result)
                };
            }
            var addrole= await userManager.AddToRoleAsync(existemailuser, "Guest");
            if (!addrole.Succeeded)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = GetErrors(addrole)
                };
            }
            await SendVerificationCode(
                reg.Email,
                "تأكيد التسجيل",
                "شكرا لتسجيلك معنا فى المنصة ونتطلع دائما لخدمتكم ");

            return new ServiceResult
            {
                Success = true,
                Message = "تم تسجيل الحساب بنجاح وفى انتظار تأكيد الايميل."
            };
        }

        public async Task<ServiceResult> ResendEmailCode(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "يجب إدخال البريد الإلكتروني."
                };
            }

            var existuser = await userManager.FindByEmailAsync(email);

            if (existuser == null || existuser.IsDeleted)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "المستخدم غير موجود."
                };
            }

            if (existuser.EmailConfirmed)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "تم تأكيد البريد الإلكتروني بالفعل."
                };
            }

            var sent = await SendVerificationCode(
                email,
                "تأكيد التسجيل",
                "رمز التحقق الخاص بك هو: ");

            if (!sent)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "من فضلك انتظر دقيقة قبل طلب كود جديد."
                };
            }

            return new ServiceResult
            {
                Success = true,
                Message = "تم إرسال رمز التحقق إلى البريد الإلكتروني."
            };
        }

        public async Task<ServiceResult> Verify(Verify verify)
        {
            var existuser = await userManager.FindByEmailAsync(verify.Email);

            if (existuser == null || existuser.IsDeleted)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "المستخدم غير موجود."
                };
            }

            if (existuser.EmailConfirmed)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "تم تأكيد البريد الإلكتروني بالفعل."
                };
            }

            var (verificationcode, error) = await GetValidCode(verify);

            if (verificationcode == null)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = error
                };
            }

            existuser.EmailConfirmed = true;

            await userManager.UpdateAsync(existuser);

            db.Set<AccountVerification>().Remove(verificationcode);

            await db.SaveChangesAsync();

            return new ServiceResult
            {
                Success = true,
                Message = "تم تأكيد التسجيل بنجاح."
            };
        }
        public async Task<ServiceResult> LogIn(LogIn logIn)
        {
            var existuser = await userManager.FindByEmailAsync(logIn.Email);

            if (existuser == null ||
                !await userManager.CheckPasswordAsync(existuser, logIn.Password))
            {
                return new ServiceResult
                {
                    Success = false,
                    ErrorCode= "Invalid_Credentials",
                    Message = "البريد الإلكتروني أو كلمة المرور غير صالحة."
                };
            }
            if (existuser.IsDeleted)
            {
                return new ServiceResult
                {
                    Success = false,
                    ErrorCode="DeletedAccount",
                    Message = "هذا الحساب محذوف تواصل مع الدعم للاسترجاع"
                };
            }

            if (!existuser.EmailConfirmed)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "يجب تأكيد الايميل اولا",
                };
            }

            return await GenerateTokens(existuser);
        }

        public async Task<ServiceResult> GoogleLogin(GoogleLogin dto)
        {
            if (string.IsNullOrEmpty(dto.IdToken))
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "التوكن مطلوب."
                };
            }

            Google.Apis.Auth.GoogleJsonWebSignature.Payload payload;
            try
            {
                var settings = new Google.Apis.Auth.GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { config["Authentication:Google:ClientId"]! }
                };

                payload = await Google.Apis.Auth.GoogleJsonWebSignature.ValidateAsync(dto.IdToken, settings);
            }
            catch (Google.Apis.Auth.InvalidJwtException)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "توكن جوجل غير صالح."
                };
            }

            if (!payload.EmailVerified)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "الإيميل غير مؤكد من جوجل."
                };
            }

            var existuser = await userManager.FindByEmailAsync(payload.Email);

            if (existuser != null && existuser.IsDeleted)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "هذا الحساب محذوف."
                };
            }

            if (existuser == null)
            {
                existuser = new ApplicationUser
                {
                    Email = payload.Email,
                    UserName = payload.Email,
                    EmailConfirmed = true,
                    IsDeleted=false,
                    GoogleLogin=true
                    
                };

                var result = await userManager.CreateAsync(existuser);

                if (!result.Succeeded)
                {
                    return new ServiceResult
                    {
                        Success = false,
                        Message = GetErrors(result)
                    };
                }
            }

            var logins = await userManager.GetLoginsAsync(existuser);

            if (!logins.Any(l => l.LoginProvider == "Google"))
            {
                var addLogin = await userManager.AddLoginAsync(
                    existuser,
                    new UserLoginInfo("Google", payload.Subject, "Google"));

                if (!addLogin.Succeeded)
                {
                    return new ServiceResult
                    {
                        Success = false,
                        Message = GetErrors(addLogin)
                    };
                }
            }

            return await GenerateTokens(existuser);
        }

        public async Task<ServiceResult> ForgetPassword(string email)
        {
            var existuser = await userManager.FindByEmailAsync(email);

            if (existuser == null || existuser.IsDeleted)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "المستخدم غير موجود."
                };
            }

            if (!existuser.EmailConfirmed)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "الحساب غير نشط لعدم تأكيد الايميل من فضلك أكد الايميل ثم جاول مجددا"
                };
            }

            var sent = await SendVerificationCode(
                email,
                "استعادة كلمة المرور",
                "رمز التحقق الخاص بك هو: ");

            if (!sent)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "من فضلك انتظر دقيقة قبل طلب كود جديد."
                };
            }

            return new ServiceResult
            {
                Success = true,
                Message = "تم إرسال رمز التحقق إلى البريد الإلكتروني."
            };
        }

        public async Task<ServiceResult> VerifyForgetPassword(Verify verify)
        {
            var existuser = await userManager.FindByEmailAsync(verify.Email);

            if (existuser == null || existuser.IsDeleted)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "المستخدم غير موجود."
                };
            }

            var (verificationcode, error) = await GetValidCode(verify);

            if (verificationcode == null)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = error
                };
            }

            verificationcode.IsVerified = true;

            await db.SaveChangesAsync();

            return new ServiceResult
            {
                Success = true,
                Message = "تم تأكيد الكود بنجاح."
            };
        }

        public async Task<ServiceResult> ResetForgottenPassword(ForgetPassword rp)
        {
            var existuser = await userManager.FindByEmailAsync(rp.Email);

            if (existuser == null || existuser.IsDeleted)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "المستخدم غير موجود."
                };
            }

            if (!existuser.EmailConfirmed)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "الحساب غير نشط لعدم تأكيد الايميل من فضلك أكد الايميل ثم جاول مجددا"
                };
            }

            if (rp.Password != rp.ConfirmPassword)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "كلمة المرور غير متطابقة."
                };
            }

            var verificationcode = await db.Set<AccountVerification>()
                .FirstOrDefaultAsync(x => x.Email == rp.Email);

            if (verificationcode == null || !verificationcode.IsVerified)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "يجب تأكيد الكود أولا."
                };
            }

            var token = await userManager.GeneratePasswordResetTokenAsync(existuser);

            var result = await userManager.ResetPasswordAsync(existuser, token, rp.Password);

            if (!result.Succeeded)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = GetErrors(result)
                };
            }

            db.Set<AccountVerification>().Remove(verificationcode);

            await db.SaveChangesAsync();

            return new ServiceResult
            {
                Success = true,
                Message = "تم تغيير كلمة المرور بنجاح."
            };
        }
        public async Task<ServiceResult> RequestChangePassword(string userid)
        {
            var existuser = await userManager.FindByIdAsync(userid);

            if (existuser == null || existuser.IsDeleted)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "المستخدم غير موجود."
                };
            }

            var sent = await SendVerificationCode(
                existuser.Email,
                "تغيير كلمة المرور",
                "رمز التحقق الخاص بك هو: ");

            if (!sent)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "من فضلك انتظر دقيقة قبل طلب كود جديد."
                };
            }

            return new ServiceResult
            {
                Success = true,
                Message = "تم إرسال رمز التحقق."
            };
        }

        public async Task<ServiceResult> VerifyChangePassword(Verify verify)
        {
            var (verificationcode, error) = await GetValidCode(verify);

            if (verificationcode == null)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = error
                };
            }

            verificationcode.IsVerified = true;

            await db.SaveChangesAsync();

            return new ServiceResult
            {
                Success = true,
                Message = "تم تأكيد الكود."
            };
        }

        public async Task<ServiceResult> ChangePassword(ResetPassword cp, string userid)
        {
            var existuser = await userManager.FindByIdAsync(userid);

            if (existuser == null || existuser.IsDeleted)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "المستخدم غير موجود."
                };
            }

            var verificationcode = await db.Set<AccountVerification>()
                .FirstOrDefaultAsync(x => x.Email == existuser.Email);

            if (verificationcode == null || !verificationcode.IsVerified)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "يجب تأكيد الكود أولا."
                };
            }

            var result = await userManager.ChangePasswordAsync(existuser, cp.CurrentPassword, cp.Password);

            if (!result.Succeeded)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = GetErrors(result)
                };
            }

            db.Set<AccountVerification>().Remove(verificationcode);

            await db.SaveChangesAsync();

            return new ServiceResult
            {
                Success = true,
                Message = "تم تغيير كلمة المرور بنجاح."
            };
        }
        public async Task<ServiceResult> RefreshToken(Token rt)
        {
            if (string.IsNullOrEmpty(rt.RefreshToken))
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "هذا الحقل فارغ من فضلك ادخل التوكن ثم اعد المحاولة مرة اخرى."
                };
            }

            var checkreftoken = await db.Set<RefreshToken>()
                .FirstOrDefaultAsync(reft => reft.Token == rt.RefreshToken && !reft.IsRevoked);

            if (checkreftoken == null || checkreftoken.ExpiredDate <= DateTime.UtcNow)
            {
                if (checkreftoken != null)
                {
                    checkreftoken.IsRevoked = true;

                    await db.SaveChangesAsync();
                }

                return new ServiceResult
                {
                    Success = false,
                    Message = "توكن غير موجود او منتهى الصلاحية."
                };
            }

            var checkuser = await userManager.FindByIdAsync(checkreftoken.UserId);

            if (checkuser == null || checkuser.IsDeleted)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "المستخدم غير موجود."
                };
            }

            checkreftoken.IsRevoked = true;

            await db.SaveChangesAsync();

            return await GenerateTokens(checkuser);
        }

        private async Task<ServiceResult> GenerateTokens(ApplicationUser existuser)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, existuser.Id.ToString()),
                new Claim(ClaimTypes.Email, existuser.Email!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var roles = await userManager.GetRolesAsync(existuser);

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Authentication:JWT:key"]!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: claims,
                issuer: config["Authentication:JWT:issuer"],
                audience: config["Authentication:JWT:audience"],
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds);

            var expiredtokens = await db.Set<RefreshToken>()
                .Where(rt => rt.UserId == existuser.Id && rt.ExpiredDate <= DateTime.UtcNow)
                .ToListAsync();

            if (expiredtokens.Any())
            {
                db.Set<RefreshToken>().RemoveRange(expiredtokens);
            }

            var generaterefreshtoken = new GenerateRefreshToken();

            var newrefreshtoken = new RefreshToken
            {
                Token = generaterefreshtoken.RefreshToken(),
                UserId = existuser.Id,
                ExpiredDate = DateTime.UtcNow.AddDays(10),
                IsRevoked = false
            };

            db.Set<RefreshToken>().Add(newrefreshtoken);

            await db.SaveChangesAsync();

            return new ServiceResult
            {
                Success = true,
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Expire = token.ValidTo,
                RefreshToken = newrefreshtoken.Token
            };
        }

        private async Task<bool> SendVerificationCode(string email, string subject, string text)
        {
            var oldCode = await db.Set<AccountVerification>()
                .FirstOrDefaultAsync(x => x.Email == email);

            if (oldCode != null &&
                oldCode.CreatedAt.AddMinutes(1) > DateTime.UtcNow)
            {
                return false;
            }

            if (oldCode != null)
            {
                db.Set<AccountVerification>().Remove(oldCode);
            }

            var generate = new VerificationCode();

            var code = generate.GenerateverificationCode();

            db.Set<AccountVerification>().Add(new AccountVerification
            {
                Email = email,
                Code = code,
                CreatedAt = DateTime.UtcNow,
                IsVerified = false
            });

            await db.SaveChangesAsync();

            await emailservice.SendEmailAsync(email, subject, $"{text}{code}");

            return true;
        }

        private async Task<(AccountVerification Verification, string Error)> GetValidCode(Verify verify)
        {
            var verificationcode = await db.Set<AccountVerification>()
                .FirstOrDefaultAsync(x => x.Email == verify.Email);

            if (verificationcode == null)
            {
                return (null, "لا يوجد كود تحقق.");
            }

            if (verificationcode.CreatedAt.AddMinutes(10) < DateTime.UtcNow)
            {
                db.Set<AccountVerification>().Remove(verificationcode);

                await db.SaveChangesAsync();

                return (null, "انتهت صلاحية الكود.");
            }

            if (verificationcode.Code != verify.Code)
            {
                return (null, "الكود غير صحيح.");
            }

            return (verificationcode, null);
        }

        private static void DeletePhoto(string folder, string photo)
        {
            if (string.IsNullOrEmpty(photo))
                return;

            var path = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "Images",
                folder,
                photo);

            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }

        private static string GetErrors(IdentityResult result)
        {
            return string.Join(" | ", result.Errors.Select(e => e.Description));
        }
    }
}