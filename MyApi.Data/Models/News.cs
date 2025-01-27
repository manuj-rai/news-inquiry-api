namespace MyApi.Data.Models
{
    // Header model
    // Enum for Response Status
    public class Enums
    {
        public enum ResponseStatus
        {
            Success = 100,
            NoDataFound = 108,
            GenericError = 105,
            Unauthorized = 401,
            BadRequest = 400,
            NotFound = 404
        }
    }

    // Response Header
    public class ResponseHeader
    {
        public int StatusCode { get; set; }
        public string Status { get; set; }
        public string Desc { get; set; }

        public ResponseHeader(Enums.ResponseStatus statusCode, string desc = null)
        {
            StatusCode = (int)statusCode;
            Status = statusCode.ToString();
            Desc = desc;
        }
    }

    // Response Wrapper
    public class ApiResponse<T>
    {
        public ResponseHeader Header { get; set; }
        public T Data { get; set; }

        public ApiResponse(T data, Enums.ResponseStatus statusCode, string desc)
        {
            Header = new ResponseHeader(statusCode, desc);
            Data = data;
        }

        // Overload constructor for success responses
        public ApiResponse(T data) : this(data, Enums.ResponseStatus.Success, "Operation completed successfully")
        {
        }

        // Overload constructor for empty data or specific messages
        public ApiResponse(Enums.ResponseStatus statusCode, string desc) : this(default, statusCode, desc)
        {
        }
    }


    public class ActiveNewsResponse
    {
        public IEnumerable<News> NewsContent { get; set; }
        public int TotalCount { get; set; }
    }

    public class NewsRequest
    {
        public string Title { get; set; }
        public IFormFile BigImage { get; set; }
        public IFormFile SmallImage { get; set; }
        public string ShortDesc { get; set; }
        public string NewsContent { get; set; }
        public DateTime PostingDate { get; set; }
        public string CopyWriteText { get; set; }
        public string TagName { get; set; }
        public int AuthorID { get; set; }
        public string CreatedBy { get; set; }
    }

    public class TagSuggestion
    {
        public string TagName { get; set; }
    }

    public class CodeLookup
    {
        public int CodeId { get; set; }
        public string CodeName { get; set; }
    }

    public class InsertInquiry
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Company { get; set; }
        public string State { get; set; }
        public string Gender { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public string Comments { get; set; }

        public string CreatedBy { get; set; }

    }

    public class UpdateInquiryRequest
    {
        public int InquiryID { get; set; }
        public string Action { get; set; }  // 'approve', 'unapprove', 'delete'
    }

    public class News
    {
        public int NewsID { get; set; }
        public string Title { get; set; }
        public string BigImage { get; set; }
        public string SmallImage { get; set; }
        public string ShortDesc { get; set; }
        public string NewsContent { get; set; }
        public DateTime PostingDate { get; set; }
        public string CopywriteText { get; set; }
        public int AuthorID { get; set; }
        public int TagID { get; set; }
        public string TagNames { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }
        public bool IsActive { get; set; }
    }

    public class GetTopNews
    {
        public int NewsID { get; set; }
        public string Title { get; set; }
        public string BigImage { get; set; }
        public string ShortDesc { get; set; }
        public string NewsContent { get; set; }
        public DateTime PostingDate { get; set; }
        public string CopywriteText { get; set; }
        public string TagNames { get; set; }
    }

    public class Tag
    {
        public int TagID { get; set; }
        public string TagName { get; set; }

    }

    public class Inquiry
    {
        public int ID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Gender { get; set; }
        public string Country { get; set; }
        public string Status { get; set; }

    }

    public class User
    {
        public int UserID { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public int Role { get; set; }
        public bool IsActive { get; set; }
    }

    public class UserDetails
    {
        public int UserID { get; set; }
        public int Role { get; set; }
        public string Name { get; set; }
        public string UserName { get; set; }
        public string EmailID { get; set; }
        public string PhoneNumber { get; set; }
        public string Password { get; set; }
        public string ProfilePicture { get; set; }
        public bool IsAdmin { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class UpdateUser
    {
        public int UserID { get; set; }
        public string UserName { get; set; }
        public string Name { get; set; }
        public string EmailID { get; set; }
        public string PhoneNumber { get; set; }
        public string Password { get; set; }  
        public IFormFile ProfilePicture { get; set; }   
    }

    public class UpdateIsAdminDto
    {
        public bool IsAdmin { get; set; }
    }

    public class UserRegistration
    {
        public string Name { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public int DepartmentID { get; set; }
        public string PhoneNumber { get; set; }
        public IFormFile ProfilePicture { get; set; }
    }

    public class RecentUsers
    {
        public int UserID { get; set; }
        public string Name { get; set; }
        public string ProfilePicture { get; set; }
        public DateTime CreatedDate { get; set; }
        public int TotalUsers { get; set; }
    }

    public class Country
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class GenerateOtpRequest
    {
        public string Email { get; set; }
    }

    public class OTPDetails
    {
        public int OTPId { get; set; }
        public string Email { get; set; }
        public string OTP { get; set; }
        public DateTime ExpiryTime { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class OTPValidationRequest
    {
        public string Email { get; set; }
        public string OTP { get; set; }
    }

    public class ResetPasswordRequest
    {
        public string Email { get; set; }
        public string NewPassword { get; set; }
    }

}

public class LoginResponse
{
    public bool IsAuthenticated { get; set; }
    public string Token { get; set; }
    public int Role { get; set; }
    public string UserName { get; set; }
}