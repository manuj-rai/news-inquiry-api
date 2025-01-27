using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using MyApi.Data.Models;
using MyApi.Data.Repositories;

namespace MyApi.Controllers
{
    public class AuthController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly INewsRepository _newsRepository;

        public AuthController(IConfiguration configuration, INewsRepository newsRepository)
        {
            _configuration = configuration;
            _newsRepository = newsRepository;
        }

        [HttpPost("generateOTP")]
        public async Task<IActionResult> GenerateOTP([FromBody] GenerateOtpRequest request)
        {
            if (string.IsNullOrEmpty(request?.Email))
            {
                return BadRequest(new ApiResponse<string>(Enums.ResponseStatus.BadRequest, "Email is required."));
            }

            try
            {
                // Call the repository method to generate OTP
                var otp = await _newsRepository.GenerateOTPAsync(request.Email);

                if (string.IsNullOrEmpty(otp))
                {
                    return StatusCode(500, new ApiResponse<string>(Enums.ResponseStatus.GenericError, "Failed to generate OTP."));
                }

                // Assuming OTP is sent via email (you can implement the sending logic here)
                return Ok(new { OTP = otp, message = "OTP sent successfully to your email!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error generating OTP", error = ex.Message });
            }
        }

        [HttpPost("validateOTP")]
        public async Task<IActionResult> ValidateOTP([FromBody] OTPValidationRequest request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.OTP))
            {
                return BadRequest(new ApiResponse<string>(Enums.ResponseStatus.BadRequest, "Email and OTP are required."));
            }

            try
            {
                // Call the repository method to validate OTP
                var isValid = await _newsRepository.ValidateOTPAsync(request.Email, request.OTP);

                if (isValid)
                {
                    return Ok(new ApiResponse<string>(Enums.ResponseStatus.Success, "OTP validated successfully."));
                }
                else
                {
                    return BadRequest(new ApiResponse<string>(Enums.ResponseStatus.GenericError, "Invalid OTP."));
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error validating OTP", error = ex.Message });
            }
        }

        [HttpPost("resetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] Data.Models.ResetPasswordRequest request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.NewPassword))
            {
                return BadRequest("Email and New Password are required.");
            }

            // Call the ResetPassword method in the service
            var result = await _newsRepository.ResetPassword(request.Email, request.NewPassword);

            if (result == "Password reset failed")
            {
                return StatusCode(500, "An error occurred while resetting the password.");
            }

            return Ok(new { message = "Password reset successfully" });
        }
    }
}