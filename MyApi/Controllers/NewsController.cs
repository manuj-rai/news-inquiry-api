using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MyApi;
using MyApi.Data.Models;
using MyApi.Data.Repositories;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

public class NewsController : ControllerBase
{
    private readonly ILogger<NewsController> _logger;
    private readonly INewsRepository _newsRepository;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;
    private readonly string _profilePictureDirectory = @"E:\Project\InquiryManagementSystem\InquiryManagementSystem\InquiryManagementSystem\ProfilePicture\";

    public NewsController(ILogger<NewsController> logger, INewsRepository newsRepository, IWebHostEnvironment env, IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _newsRepository = newsRepository ?? throw new ArgumentNullException(nameof(newsRepository));
        _env = env;
        _configuration = configuration;
    }




    [HttpGet("GetActiveNews")]
    [ServiceFilter(typeof(AllowSpecificOriginFilter))]
    public async Task<IActionResult> GetActiveNews([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var result = await _newsRepository.GetActiveNewsAsync(pageIndex, pageSize);
            if (result?.Data?.NewsContent == null || !result.Data.NewsContent.Any())
            {
                var noDataResponse = new ApiResponse<object>(Enums.ResponseStatus.NoDataFound, "No active news available.");
                return Ok(noDataResponse);
            }
            var successResponse = new ApiResponse<object>(result.Data, Enums.ResponseStatus.Success, "Active news fetched successfully.");
            return Ok(successResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = new ApiResponse<string>(Enums.ResponseStatus.GenericError, ex.Message);
            return StatusCode(500, errorResponse);
        }
    }




    [HttpGet("GetTopNews")]
    [ServiceFilter(typeof(AllowSpecificOriginFilter))]
    public async Task<IActionResult> GetTopNews([FromQuery] int take = 5, [FromQuery] int skip = 0)
    {
        try
        {
            var topNews = (await _newsRepository.GetTopNewsAsync(take, skip)).ToList();
            var response = new ApiResponse<List<GetTopNews>>(topNews,
                topNews.Any() ? Enums.ResponseStatus.Success : Enums.ResponseStatus.NoDataFound,
                topNews.Any() ? "Top news fetched successfully" : "No top news available");
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = new ApiResponse<string>(Enums.ResponseStatus.GenericError, ex.Message);
            return StatusCode(500, errorResponse);
        }
    }




    [HttpGet("Categories")]
    [ServiceFilter(typeof(AllowSpecificOriginFilter))]
    public async Task<IActionResult> GetTags()
    {
        try
        {
            var tags = (await _newsRepository.GetAllTagsAsync()).ToList();
            var response = new ApiResponse<List<Tag>>(tags,
                tags.Any() ? Enums.ResponseStatus.Success : Enums.ResponseStatus.NoDataFound,
                tags.Any() ? "Tags fetched successfully" : "No tags available");
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = new ApiResponse<string>(Enums.ResponseStatus.GenericError, ex.Message);
            return StatusCode(500, errorResponse);
        }
    }




    [HttpGet("News-by-Categories")]
    [ServiceFilter(typeof(AllowSpecificOriginFilter))]
    public async Task<IActionResult> GetNewsByTag([FromQuery] string tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName))
        {
            var errorResponse = new ApiResponse<string>(Enums.ResponseStatus.BadRequest, "Tag name is required.");
            return BadRequest(errorResponse);
        }
        try
        {
            var news = await _newsRepository.GetNewsByTagAsync(tagName);
            if (news == null || !news.Any())
            {
                var noDataResponse = new ApiResponse<IEnumerable<News>>(Enums.ResponseStatus.NoDataFound, "No news found for the given tag.");
                return Ok(noDataResponse);
            }
            var response = new ApiResponse<IEnumerable<News>>(news, Enums.ResponseStatus.Success, "News fetched successfully.");
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = new ApiResponse<string>(Enums.ResponseStatus.GenericError, $"Failed to fetch news for tag {tagName}: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }





    [HttpGet("GetPaginatedInquiries")]
    [ServiceFilter(typeof(AllowSpecificOriginFilter))]
    public async Task<IActionResult> GetPaginatedInquiries(int pageNumber = 1,int pageSize = 10,string gender = null,string country = null,string status = null,string sortDirection = "ASC")
    {
        if (pageNumber <= 0 || pageSize <= 0)
        {
            var errorResponse = new ApiResponse<string>(Enums.ResponseStatus.BadRequest, "Page number and page size must be greater than zero.");
            return BadRequest(errorResponse);
        }
        if (sortDirection != "ASC" && sortDirection != "DESC")
        {
            var errorResponse = new ApiResponse<string>(Enums.ResponseStatus.BadRequest, "Sort direction must be either 'ASC' or 'DESC'.");
            return BadRequest(errorResponse);
        }
        try
        {
            var (inquiries, totalCount) = await _newsRepository.GetInquiriesAsync(pageNumber, pageSize, gender, country, status, sortDirection);
            if (inquiries == null || !inquiries.Any())
            {
                var noDataResponse = new ApiResponse<object>(Enums.ResponseStatus.NoDataFound, "No inquiries found.");
                return Ok(noDataResponse);
            }
            var successResponse = new ApiResponse<object>(
                new
                {
                    Data = inquiries,  
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                },
                Enums.ResponseStatus.Success,
                "Inquiries fetched successfully."
            );
            return Ok(successResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = new ApiResponse<string>(Enums.ResponseStatus.GenericError, $"Failed to fetch inquiries: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }





    [HttpPost("UpdateInquiryStatus")]
    public async Task<IActionResult> UpdateInquiryStatus([FromBody] UpdateInquiryRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.Action))
        {
            var errorResponse = new ApiResponse<string>(Enums.ResponseStatus.BadRequest, "Invalid request. Action is required.");
            return BadRequest(errorResponse);
        }
        try
        {
            await _newsRepository.UpdateInquiryStatusAsync(request.InquiryID, request.Action);
            var successResponse = new ApiResponse<string>(Enums.ResponseStatus.Success, "Action performed successfully.");
            return Ok(successResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = new ApiResponse<string>(Enums.ResponseStatus.GenericError, $"Failed to update inquiry status: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }




    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] User loginRequest)
    {
        if (loginRequest == null || string.IsNullOrEmpty(loginRequest.UserName) || string.IsNullOrEmpty(loginRequest.Password))
        {
            var errorResponse = new ApiResponse<string>(
                Enums.ResponseStatus.BadRequest,
                "Invalid request. Username or password is missing."
            );
            return BadRequest(errorResponse);
        }

        // Validate user and get user details (including isAdmin)
        var user = await _newsRepository.ValidateUserAsync(loginRequest.UserName, loginRequest.Password);

        if (user != null && user.IsActive)
        {
            // Generate JWT token
            var token = GenerateJwtToken(user.UserName, user.Role);

            var successResponse = new ApiResponse<LoginResponse>(
                new LoginResponse
                {
                    IsAuthenticated = true,
                    Token = token,
                    Role = user.Role,
                    UserName = user.UserName
                },
                Enums.ResponseStatus.Success,
                "Login successful."
            );

            return Ok(successResponse);
        }

        // If validation fails
        var unauthorizedResponse = new ApiResponse<string>(
            Enums.ResponseStatus.Unauthorized,
            "Invalid username or password."
        );
        return Unauthorized(unauthorizedResponse);
    }



    private string GenerateJwtToken(string userName, Int32 role)
    {
        var claims = new[]
        {
        new Claim(ClaimTypes.Name, userName),
        new Claim("Role", role.ToString())
    };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            _configuration["Jwt:Issuer"],
            _configuration["Jwt:Audience"],
            claims,
            expires: DateTime.Now.AddHours(2),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }




    [HttpGet("GetUserDetails")]
    [ServiceFilter(typeof(AllowSpecificOriginFilter))]
    public async Task<IActionResult> GetUserDetails(string userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            var errorResponse = new ApiResponse<string>(Enums.ResponseStatus.BadRequest, "Username is required.");
            return BadRequest(errorResponse);
        }
        var userDetails = await _newsRepository.GetUserDetailsAsync(userName);
        if (userDetails == null)
        {
            var notFoundResponse = new ApiResponse<string>(Enums.ResponseStatus.NotFound, "User not found.");
            return NotFound(notFoundResponse);
        }
        var successResponse = new ApiResponse<UserDetails>(userDetails, Enums.ResponseStatus.Success, "User details retrieved successfully.");
        return Ok(successResponse);
    }





    [HttpPost("InsertInquiry")]
    public async Task<IActionResult> CreateInquiry([FromBody] InsertInquiry inquiry)
    {
        if (inquiry == null)
        {
            var errorResponse = new ApiResponse<string>(Enums.ResponseStatus.BadRequest, "Inquiry data is required.");
            return BadRequest(errorResponse);
        }
        try
        {
            await _newsRepository.AddInquiryAsync(inquiry);
            var successResponse = new ApiResponse<string>(Enums.ResponseStatus.Success, "Inquiry created successfully.");
            return Ok(successResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = new ApiResponse<string>(Enums.ResponseStatus.GenericError, $"Error: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }




    [HttpPost("register")]
    public async Task<IActionResult> Register([FromForm] UserRegistration user)
    {
        try
        {
            if (user.ProfilePicture == null || user.ProfilePicture.Length == 0)
            {
                var errorResponse = new ApiResponse<string>(Enums.ResponseStatus.BadRequest, "Profile picture is required.");
                return BadRequest(errorResponse);
            }
            string fileExtension = Path.GetExtension(user.ProfilePicture.FileName).ToLower();
            if (fileExtension != ".jpg" && fileExtension != ".png" && fileExtension != ".jpeg")
            {
                var errorResponse = new ApiResponse<string>(Enums.ResponseStatus.BadRequest, "Only .jpg, .png, and .jpeg formats are allowed.");
                return BadRequest(errorResponse);
            }
            string baseDirectory = @"E:\Project\InquiryManagementSystem\InquiryManagementSystem\InquiryManagementSystem\ProfilePicture";
            string uniqueFolderName = user.UserName; 
            string userFolder = Path.Combine(baseDirectory, uniqueFolderName);
            if (!Directory.Exists(userFolder))
            {
                Directory.CreateDirectory(userFolder);
            }
            string fileName = Path.GetFileName(user.ProfilePicture.FileName); 
            string filePath = Path.Combine(userFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await user.ProfilePicture.CopyToAsync(stream);
            }
            string relativePath = $"~/ProfilePicture/{uniqueFolderName}/{fileName}";
            var result = await _newsRepository.RegisterUserAsync(user, relativePath);
            if (result)
            {
                var successResponse = new ApiResponse<object>(new { Message = "User registered successfully.", ProfilePicturePath = relativePath });
                return Ok(successResponse);
            }
            var failureResponse = new ApiResponse<string>(Enums.ResponseStatus.GenericError, "An error occurred while registering the user.");
            return StatusCode(500, failureResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = new ApiResponse<string>(Enums.ResponseStatus.GenericError, $"Internal Server Error: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }




    [HttpPost("UpdateUserDetails")]
    public async Task<IActionResult> UpdateUserDetails([FromForm] UpdateUser user)
    {
        try
        {
            string relativePath = null;
            if (user.ProfilePicture != null && user.ProfilePicture.Length > 0)
            {
                Console.WriteLine("Starting file upload...");
                string fileExtension = Path.GetExtension(user.ProfilePicture.FileName).ToLower();
                if (fileExtension != ".jpg" && fileExtension != ".png" && fileExtension != ".jpeg")
                {
                    var errorResponse = new ApiResponse<string>(Enums.ResponseStatus.BadRequest, "Only .jpg, .png, and .jpeg formats are allowed.");
                    return BadRequest(errorResponse);
                }
                string baseDirectory = @"E:\Project\InquiryManagementSystem\InquiryManagementSystem\InquiryManagementSystem\ProfilePicture\";
                string uniqueFolderName = user.UserName; 
                string userFolder = Path.Combine(baseDirectory, uniqueFolderName);
                if (!Directory.Exists(userFolder))
                {
                    Directory.CreateDirectory(userFolder);
                }
                var existingFiles = Directory.GetFiles(userFolder);
                foreach (var existingFile in existingFiles)
                {
                    System.IO.File.Delete(existingFile);  
                }
                string fileName = Path.GetFileName(user.ProfilePicture.FileName);
                string filePath = Path.Combine(userFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await user.ProfilePicture.CopyToAsync(stream);
                    Console.WriteLine($"File {fileName} uploaded successfully at {filePath}");
                }
                relativePath = $"~/ProfilePicture/{uniqueFolderName}/{fileName}";
            }
            else
            {
                var existingUser = await _newsRepository.GetUserDetailsAsync(user.UserName); 
                if (existingUser != null)
                {
                    relativePath = existingUser.ProfilePicture; 
                }
            }
            var result = await _newsRepository.UpdateUserDetailsAsync(user, relativePath);
            if (result)
            {
                var successResponse = new ApiResponse<object>(
                     new { header = "Success", data = "User details updated successfully." },
                     Enums.ResponseStatus.Success,
                     "User details updated successfully."
                 );
                return Ok(successResponse);
            }
            var failureResponse = new ApiResponse<string>(
                Enums.ResponseStatus.GenericError,
                "Failed to update user details."
            );
            return StatusCode(500, failureResponse);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating user details: {ex.Message}");
            var errorResponse = new ApiResponse<string>(
                Enums.ResponseStatus.GenericError,
                $"An error occurred while updating user details: {ex.Message}"
            );
            return StatusCode(500, errorResponse);
        }
    }




    [HttpGet("recent-users")]
    [ServiceFilter(typeof(AllowSpecificOriginFilter))]
    public async Task<IActionResult> GetTop5RecentUsers()
    {
        try
        {
            var users = await _newsRepository.GetTop5RecentUsersAsync();

            if (users == null || !users.Any())
            {
                var noDataResponse = new ApiResponse<object>(Enums.ResponseStatus.NoDataFound,"No recent users found.");
                return Ok(noDataResponse);
            }
            var successResponse = new ApiResponse<IEnumerable<RecentUsers>>(users, Enums.ResponseStatus.Success, "Recent users fetched successfully.");
            return Ok(successResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = new ApiResponse<string>(Enums.ResponseStatus.GenericError,$"Internal server error: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }




    [HttpPost("add-news")]
    public async Task<IActionResult> AddNews([FromForm] NewsRequest newsRequest)
    {
        try
        {
            if (string.IsNullOrEmpty(newsRequest.Title))
                return BadRequest(new ApiResponse<string>(Enums.ResponseStatus.BadRequest, "Title is required"));
            if (string.IsNullOrEmpty(newsRequest.ShortDesc))
                return BadRequest(new ApiResponse<string>(Enums.ResponseStatus.BadRequest, "Short Description is required"));
            string rootPath = @"E:\Project\NewsPortal\NewsPortal\NewsImages";  
            int newsID = await _newsRepository.AddNewsAsync(newsRequest, null, null);  

            if (newsID > 0)
            {
                string folderPath = Path.Combine(rootPath, newsID.ToString());
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath); 
                }
                string bigImagePath = null;
                string smallImagePath = null;
                if (newsRequest.BigImage != null)
                {
                    var bigImageFileName = $"{Guid.NewGuid()}_{newsRequest.BigImage.FileName}";
                    var fullBigImagePath = Path.Combine(folderPath, bigImageFileName);
                    using (var stream = new FileStream(fullBigImagePath, FileMode.Create))
                    {
                        await newsRequest.BigImage.CopyToAsync(stream);
                    }
                    bigImagePath = Path.Combine("~/NewsImages", newsID.ToString(), bigImageFileName);
                }
                if (newsRequest.SmallImage != null)
                {
                    var smallImageFileName = $"{Guid.NewGuid()}_{newsRequest.SmallImage.FileName}";
                    var fullSmallImagePath = Path.Combine(folderPath, smallImageFileName);
                    using (var stream = new FileStream(fullSmallImagePath, FileMode.Create))
                    {
                        await newsRequest.SmallImage.CopyToAsync(stream);
                    }
                    smallImagePath = Path.Combine("~/NewsImages", newsID.ToString(), smallImageFileName);
                }
                await _newsRepository.UpdateNewsImagesAsync(newsID, bigImagePath, smallImagePath);
                return Ok(new
                {
                    Header = new
                    {
                        StatusCode = 200,
                        Status = "Success",
                        Description = "News added successfully"
                    },
                    Data = new
                    {
                        NewsID = newsID,
                        Message = "News created successfully",
                        BigImagePath = bigImagePath,
                        SmallImagePath = smallImagePath
                    }
                });
            }
            else
            {
                return StatusCode(500, new ApiResponse<string>(Enums.ResponseStatus.GenericError, "Failed to add news"));
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<string>(Enums.ResponseStatus.GenericError, ex.Message));
        }
    }



    [HttpGet("suggestions")]
    [ServiceFilter(typeof(AllowSpecificOriginFilter))]
    public async Task<IActionResult> GetTagSuggestions([FromQuery] string query)
    {
        if (string.IsNullOrEmpty(query) || query.Length < 1)
        {
            var badRequestResponse = new ApiResponse<string>(Enums.ResponseStatus.BadRequest,"Query must be at least 1 character long.");
            return BadRequest(badRequestResponse);
        }
        try
        {
            var tags = await _newsRepository.GetTagSuggestionsAsync(query);
            if (tags == null || !tags.Any())
            {
                var noDataResponse = new ApiResponse<object>(Enums.ResponseStatus.NoDataFound,"No tag suggestions found.");
                return Ok(noDataResponse);
            }
            var successResponse = new ApiResponse<IEnumerable<TagSuggestion>>(tags,Enums.ResponseStatus.Success,"Tag suggestions fetched successfully.");
            return Ok(successResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = new ApiResponse<string>(Enums.ResponseStatus.GenericError,$"Internal server error: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }




    [HttpGet("GetGenderOptions")]
    [ServiceFilter(typeof(AllowSpecificOriginFilter))]
    public async Task<IActionResult> GetGenderOptions()
    {
        try
        {
            var genders = await _newsRepository.GetGenderOptionsAsync();
            if (genders == null || !genders.Any())
            {
                return NotFound(new ApiResponse<object>(Enums.ResponseStatus.NoDataFound, "No gender options found."));
            }
            return Ok(new ApiResponse<IEnumerable<CodeLookup>>(genders, Enums.ResponseStatus.Success, "Gender options retrieved successfully."));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<object>(Enums.ResponseStatus.GenericError, ex.Message));
        }
    }




    [HttpGet("GetCountryList")]
    [ServiceFilter(typeof(AllowSpecificOriginFilter))]
    public async Task<IActionResult> GetCountryList()
    {
        try
        {
            IEnumerable<Country> countries = await _newsRepository.GetCountryListAsync();
            if (countries == null || !countries.Any())
            {
                var noDataResponse = new ApiResponse<IEnumerable<Country>>(Enums.ResponseStatus.NoDataFound, "No countries found.");
                return NotFound(noDataResponse);
            }
            var apiResponse = new ApiResponse<IEnumerable<Country>>(countries, Enums.ResponseStatus.Success, "Country list fetched successfully.");
            return Ok(apiResponse);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<object>(Enums.ResponseStatus.GenericError, ex.Message));
        }
    }




    [HttpGet("GetPaginatedUsers")]
    public async Task<IActionResult> GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var response = new ApiResponse<object>(null, Enums.ResponseStatus.Success, ""); // Declare the response variable

        if (pageNumber <= 0 || pageSize <= 0)
        {
            response = new ApiResponse<object>(null, Enums.ResponseStatus.BadRequest, "Page number and page size must be greater than 0.");
            return BadRequest(response);
        }

        var users = await _newsRepository.GetUsersWithPaginationAsync(pageNumber, pageSize);

        if (users == null || !users.Any())
        {
            response = new ApiResponse<object>(null, Enums.ResponseStatus.NoDataFound, "No users found.");
            return NotFound(response);
        }

        response = new ApiResponse<object>(users, Enums.ResponseStatus.Success, "Users retrieved successfully.");
        return Ok(response);
    }



    [HttpPut("{id}/isAdmin")]
    public async Task<IActionResult> UpdateIsAdmin(int id, [FromBody] UpdateIsAdminDto updateIsAdminDto)
    {
        var response = new ApiResponse<object>(null, Enums.ResponseStatus.Success, ""); // Declare the response variable

        if (id <= 0)
        {
            response = new ApiResponse<object>(null, Enums.ResponseStatus.BadRequest, "Invalid User ID.");
            return BadRequest(response);
        }

        if (updateIsAdminDto == null)
        {
            response = new ApiResponse<object>(null, Enums.ResponseStatus.BadRequest, "isAdmin value must be provided.");
            return BadRequest(response);
        }

        var result = await _newsRepository.UpdateIsAdminAsync(id, updateIsAdminDto.IsAdmin);

        if (result.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            response = new ApiResponse<object>(null, Enums.ResponseStatus.NotFound, result);
            return NotFound(response);
        }

        response = new ApiResponse<object>(result, Enums.ResponseStatus.Success, "isAdmin updated successfully.");
        return Ok(response);
    }
}



