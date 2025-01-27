using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Azure;
using Dapper;
using Microsoft.Data.SqlClient;
using MyApi.Data.Models;

namespace MyApi.Data.Repositories
{
    public interface INewsRepository
    {
        Task<ApiResponse<ActiveNewsResponse>> GetActiveNewsAsync(int pageIndex, int pageSize);
        Task<IEnumerable<GetTopNews>> GetTopNewsAsync(int take, int skip);
        Task<IEnumerable<Tag>> GetAllTagsAsync(); 
        Task<IEnumerable<News>> GetNewsByTagAsync(string tagName);
        Task<(IEnumerable<Inquiry> Inquiries, int TotalCount)> GetInquiriesAsync(
            int pageNumber,
            int pageSize,
            string gender = null,
            string country = null,
            string status = null,
            string sortDirection = "ASC"
        );
        Task UpdateInquiryStatusAsync(int inquiryId, string action);
        Task<User> ValidateUserAsync(string userName, string password);
        Task<UserDetails> GetUserDetailsAsync(string userName);

        Task<bool> UpdateUserDetailsAsync(UpdateUser user, string profilePicturePath);
        Task AddInquiryAsync(InsertInquiry inquiry);
        Task<bool> RegisterUserAsync(UserRegistration user, string profilePictureFileName);
        Task<List<RecentUsers>> GetTop5RecentUsersAsync();
        Task<int> AddNewsAsync(NewsRequest newsRequest, string bigImagePath, string smallImagePath);
        Task UpdateNewsImagesAsync(int newsID, string bigImagePath, string smallImagePath);
        Task<IEnumerable<TagSuggestion>> GetTagSuggestionsAsync(string query);
        Task<IEnumerable<Country>> GetCountryListAsync();
        Task<IEnumerable<CodeLookup>> GetGenderOptionsAsync();
        Task<bool> ValidateOTPAsync(string email, string otp);
        Task<string> GenerateOTPAsync(string email, int expiryDuration = 15);
        Task<string> ResetPassword(string email, string newPassword);
        Task<List<UserDetails>> GetUsersWithPaginationAsync(int pageNumber, int pageSize);
        Task<string> UpdateIsAdminAsync(int userId, bool isAdmin);
    }

    public class NewsRepository : INewsRepository
    {
        private readonly string _connectionString;

        public NewsRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        // Fetch paginated active news
        public async Task<ApiResponse<ActiveNewsResponse>> GetActiveNewsAsync(int pageIndex, int pageSize)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@PageIndex", pageIndex, DbType.Int32);
            parameters.Add("@PageSize", pageSize, DbType.Int32);
            parameters.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

            // Query the news content from the database
            var newsContent = await connection.QueryAsync<News>(
                "GetAllActiveNewsAng",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            // Retrieve the total count of news records
            var totalCount = parameters.Get<int>("@TotalCount");

            var response = new ActiveNewsResponse
            {
                NewsContent = newsContent,
                TotalCount = totalCount
            };

            return new ApiResponse<ActiveNewsResponse>(response);
        }


        // Fetch top news for the slider
        public async Task<IEnumerable<GetTopNews>> GetTopNewsAsync(int take, int skip)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@Take", take, DbType.Int32);
            parameters.Add("@Skip", skip, DbType.Int32);

            return await connection.QueryAsync<GetTopNews>(
                "GetTopNews",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        // Fetch News Categories
        public async Task<IEnumerable<Tag>> GetAllTagsAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            var tags = await connection.QueryAsync<Tag>(
                "GetAllTags",
                commandType: CommandType.StoredProcedure
            );
            return tags;
        }

        public async Task<IEnumerable<News>> GetNewsByTagAsync(string tagName)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@CategoryName", tagName, DbType.String);

            var news = await connection.QueryAsync<News>(
                "GetNewsByCategory",
                parameters,
                commandType: CommandType.StoredProcedure
            );
            return news;
        }

        public async Task<(IEnumerable<Inquiry> Inquiries, int TotalCount)> GetInquiriesAsync(
            int pageNumber,
            int pageSize,
            string gender = null,
            string country = null,
            string status = null,
            string sortDirection = "ASC"
         )
        {
            var inquiries = new List<Inquiry>();
            int totalCount = 0;

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand("[GetAllInquiries]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@PageNumber", pageNumber);
                    command.Parameters.AddWithValue("@PageSize", pageSize);
                    if (!string.IsNullOrEmpty(gender)) command.Parameters.AddWithValue("@Gender", gender);
                    if (!string.IsNullOrEmpty(country)) command.Parameters.AddWithValue("@Country", country);
                    if (!string.IsNullOrEmpty(status)) command.Parameters.AddWithValue("@Status", status);
                    command.Parameters.AddWithValue("@SortDirection", sortDirection);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            inquiries.Add(new Inquiry
                            {
                                ID = (int)reader.GetInt64(0),
                                FirstName = reader.IsDBNull(1) ? null : reader.GetString(1),
                                LastName = reader.IsDBNull(2) ? null : reader.GetString(2),
                                Gender = reader.IsDBNull(3) ? null : reader.GetString(3),
                                Country = reader.IsDBNull(4) ? null : reader.GetString(4),
                                Status = reader.IsDBNull(5) ? null : reader.GetString(5),
                            });
                        }

                        if (await reader.NextResultAsync() && await reader.ReadAsync())
                        {
                            totalCount = reader.GetInt32(0);
                        }
                    }
                }
            }

            return (inquiries, totalCount);
        }

        public async Task UpdateInquiryStatusAsync(int inquiryId, string action)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand("UpdateInquiries", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@InquiryID", inquiryId);
                    command.Parameters.AddWithValue("@Action", action);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }




        public async Task<User> ValidateUserAsync(string userName, string password)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand("sp_ValidateUser", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@userName", userName);
                    command.Parameters.AddWithValue("@password", password); // Use hashed password if applicable

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.Read())
                        {
                            return new User
                            {
                                UserID = reader.GetInt32(0),
                                UserName = reader.GetString(1),
                                Role = reader.GetInt32(2),
                                IsActive = reader.GetBoolean(3)
                            };
                        }
                    }
                }
            }
            return null;
        }

        public async Task<UserDetails> GetUserDetailsAsync(string userName)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand("GetUserDetails", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@UserName", userName);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new UserDetails
                            {
                                UserID = reader.GetInt32(reader.GetOrdinal("UserID")),
                                Name = reader.GetString(reader.GetOrdinal("Name")),
                                EmailID = reader.GetString(reader.GetOrdinal("EmailID")),
                                PhoneNumber = reader.GetString(reader.GetOrdinal("PhoneNumber")),
                                Password = reader.GetString(reader.GetOrdinal("Password")),
                                ProfilePicture = reader.GetString(reader.GetOrdinal("ProfilePicture")),
                                IsAdmin = reader.GetBoolean(reader.GetOrdinal("IsAdmin")),
                                Role = reader.GetInt32(reader.GetOrdinal("Role"))
                            };
                        }
                    }
                }
            }

            return null; // Return null if no user is found
        }

        public async Task<bool> UpdateUserDetailsAsync(UpdateUser user, string profilePicturePath)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@UserID", user.UserID);
                parameters.Add("@Username", user.UserName);
                parameters.Add("@Name", user.Name);
                parameters.Add("@Email", user.EmailID);
                parameters.Add("@PhoneNumber", user.PhoneNumber);
                parameters.Add("@Password", user.Password);
                parameters.Add("@ProfilePicture", profilePicturePath); // Pass profile picture path

                // Execute the stored procedure
                int rowsAffected = await db.ExecuteAsync("UpdateUserDetails", parameters, commandType: CommandType.StoredProcedure);

                return rowsAffected > 0;
            }
        }


        public async Task AddInquiryAsync(InsertInquiry inquiry)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                // Open connection
                await connection.OpenAsync();

                // Define the command to execute the stored procedure
                using (var command = new SqlCommand("InsertInquiries", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Add parameters to the command
                    command.Parameters.Add(new SqlParameter("@FirstName", inquiry.FirstName ?? (object)DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@LastName", inquiry.LastName ?? (object)DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@Email", inquiry.Email ?? (object)DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@PhoneNumber", inquiry.PhoneNumber ?? (object)DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@Company", inquiry.Company ?? (object)DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@State", inquiry.State ?? (object)DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@Gender", inquiry.Gender ?? (object)DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@Country", inquiry.Country ?? (object)DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@City", inquiry.City ?? (object)DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@Comments", inquiry.Comments ?? (object)DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@CreatedBy", inquiry.FirstName ?? (object)DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@CreatedDate", DateTime.UtcNow)); 

                    // Execute the stored procedure
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<bool> RegisterUserAsync(UserRegistration user, string profilePicturePath)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = new SqlCommand("[dbo].[sp_RegisterUser]", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@Name", user.Name);
                cmd.Parameters.AddWithValue("@UserName", user.UserName);
                cmd.Parameters.AddWithValue("@Email", user.Email);
                cmd.Parameters.AddWithValue("@Password", user.Password);
                cmd.Parameters.AddWithValue("@DepartmentID", user.DepartmentID);
                cmd.Parameters.AddWithValue("@PhoneNumber", user.PhoneNumber);
                cmd.Parameters.AddWithValue("@ProfilePicture", profilePicturePath);

                try
                {
                    conn.Open();
                    await cmd.ExecuteNonQueryAsync();
                    return true;
                }
                catch (SqlException ex)
                {
                    // Log the error or throw it for further debugging
                    Console.WriteLine($"SQL Error: {ex.Message}");
                    throw; // Optional: Re-throw to catch in the controller
                }
            }
        }

        public async Task<List<RecentUsers>> GetTop5RecentUsersAsync()
        {
            var users = new List<RecentUsers>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand("GetTop5RecentUsers", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            users.Add(new RecentUsers
                            {
                                UserID = reader.GetInt32(reader.GetOrdinal("UserID")),
                                Name = reader.GetString(reader.GetOrdinal("Name")),
                                ProfilePicture = reader.GetString(reader.GetOrdinal("ProfilePicture")),
                                CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                                TotalUsers = reader.GetInt32(reader.GetOrdinal("TotalUsers"))
                            });
                        }
                    }
                }
            }

            return users;
        }

        public async Task<int> AddNewsAsync(NewsRequest newsRequest, string bigImagePath, string smallImagePath)
        {
            int newNewsID = 0;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("[dbo].[AddNewsContent]", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Title", newsRequest.Title);
                    cmd.Parameters.AddWithValue("@BigImage", bigImagePath ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@SmallImage", smallImagePath ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ShortDesc", newsRequest.ShortDesc);
                    cmd.Parameters.AddWithValue("@NewsContent", newsRequest.NewsContent);
                    cmd.Parameters.AddWithValue("@PostingDate", newsRequest.PostingDate);
                    cmd.Parameters.AddWithValue("@CopywriteText", newsRequest.CopyWriteText);
                    cmd.Parameters.AddWithValue("@AuthorID", newsRequest.AuthorID);
                    cmd.Parameters.AddWithValue("@CreatedDate", DateTime.Now);
                    cmd.Parameters.AddWithValue("@CreatedBy", newsRequest.CreatedBy);
                    cmd.Parameters.AddWithValue("@TagNames", newsRequest.TagName);

                    conn.Open();
                    newNewsID = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }
            }
            return newNewsID;
        }

        public async Task UpdateNewsImagesAsync(int newsID, string bigImagePath, string smallImagePath)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))  // _connectionString is your connection string to the database
                {
                    await connection.OpenAsync();

                    var query = @"
                UPDATE NewsContent
                SET BigImage = @BigImage, SmallImage = @SmallImage
                WHERE NewsID = @NewsID";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@BigImage", bigImagePath ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@SmallImage", smallImagePath ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@NewsID", newsID);

                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error updating image paths", ex);
            }
        }

        public async Task<IEnumerable<TagSuggestion>> GetTagSuggestionsAsync(string query)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var parameters = new { Query = query };
                var tags = await connection.QueryAsync<TagSuggestion>(
                    "EXEC dbo.GetTagSuggestions @Query", parameters);
                return tags;
            }
        }

        public async Task<IEnumerable<Country>> GetCountryListAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "schcommon.GetCountryList";
                return await connection.QueryAsync<Country>(query, commandType: CommandType.StoredProcedure);
            }
        }

        public async Task<IEnumerable<CodeLookup>> GetGenderOptionsAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                return await connection.QueryAsync<CodeLookup>(
                    "dbo.GetCodeLoopUpForRadioButtonList", commandType: CommandType.StoredProcedure);
            }
        }


        public async Task<string> GenerateOTPAsync(string email, int expiryDuration = 15)
        {
            string generatedOtp = string.Empty;
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    var command = new SqlCommand("GeneratePasswordResetOTP", connection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@ExpiryDuration", expiryDuration); // Default to 15 minutes

                    connection.Open();
                    generatedOtp = await command.ExecuteScalarAsync() as string;
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                // Log error if needed
                throw new Exception("Error generating OTP", ex);
            }

            return generatedOtp;
        }

        public async Task<bool> ValidateOTPAsync(string email, string otp)
        {
            bool isValid = false;
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    var command = new SqlCommand("ValidateOTP", connection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@OTP", otp);

                    connection.Open();
                    var result = await command.ExecuteScalarAsync();
                    connection.Close();

                    if (result != null && Convert.ToInt32(result) == 1)
                    {
                        isValid = true;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error if needed
                throw new Exception("Error validating OTP", ex);
            }

            return isValid;
        }

        public async Task<string> ResetPassword(string email, string newPassword)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand("dbo.ResetPassword", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@NewPassword", newPassword);

                    var result = command.ExecuteScalar();  // Executes the stored procedure and returns a result

                    return result?.ToString() ?? "Password reset failed";
                }
            }
        }

        public async Task<List<UserDetails>> GetUsersWithPaginationAsync(int pageNumber, int pageSize)
        {
            var users = new List<UserDetails>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand("dbo.GetUsersWithPagination", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        // Add parameters for pagination
                        command.Parameters.AddWithValue("@PageNumber", pageNumber);
                        command.Parameters.AddWithValue("@PageSize", pageSize);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                users.Add(new UserDetails
                                {
                                    UserID = reader.GetInt32("UserID"),
                                    ProfilePicture = reader.GetString("ProfilePicture"),
                                    Name = reader.GetString("Name"),
                                    EmailID = reader.GetString("EmailID"),
                                    IsAdmin = reader.GetBoolean("IsAdmin"),
                                    CreatedDate = reader.GetDateTime("CreatedDate"),
                                    Role = reader.GetInt32("Role")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return users;
        }

        public async Task<string> UpdateIsAdminAsync(int userId, bool isAdmin)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand("dbo.UpdateIsAdmin", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        // Add parameters for user ID and isAdmin
                        command.Parameters.AddWithValue("@UserId", userId);
                        command.Parameters.AddWithValue("@IsAdmin", isAdmin);

                        var result = (int)await command.ExecuteScalarAsync();

                        if (result == 1)
                        {
                            return "isAdmin updated successfully.";
                        }
                        else
                        {
                            return "Update failed. No rows were affected."; 
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return "null";
            }
        }


    }
}
