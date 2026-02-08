using WebSapaForestForStaff.DTOs;
using WebSapaForestForStaff.DTOs.UserManagement;

namespace WebSapaForestForStaff.Services.Api.Interfaces
{
    /// <summary>
    /// Interface for user management API operations
    /// </summary>
    public interface IUserApiService : IBaseApiService
    {
        /// <summary>
        /// Gets all users
        /// </summary>
        Task<List<User>?> GetUsersAsync();

        /// <summary>
        /// Gets total count of users
        /// </summary>
        Task<int> GetTotalUsersAsync();

        /// <summary>
        /// Gets a user by ID
        /// </summary>
        Task<User?> GetUserAsync(int id);

        /// <summary>
        /// Updates a user (legacy method using User object)
        /// </summary>
        Task<bool> UpdateUserAsync(User user);

        /// <summary>
        /// Deletes a user by ID
        /// </summary>
        Task<bool> DeleteUserAsync(int id);

        /// <summary>
        /// Changes user status (active/inactive)
        /// </summary>
        Task<bool> ChangeUserStatusAsync(int id, int status);

        /// <summary>
        /// Gets users with pagination and search filters
        /// </summary>
        Task<UserListResponse?> GetUsersWithPaginationAsync(UserSearchRequest request);

        /// <summary>
        /// Gets detailed user information by ID
        /// </summary>
        Task<UserDetailsResponse?> GetUserDetailsAsync(int id);

        /// <summary>
        /// Creates a new user
        /// </summary>
        Task<bool> CreateUserAsync(UserCreateRequest request);

        /// <summary>
        /// Updates a user using UserUpdateRequest
        /// </summary>
        Task<bool> UpdateUserAsync(UserUpdateRequest request);

        /// <summary>
        /// Resets user password by admin
        /// </summary>
        Task<bool> ResetUserPasswordAsync(PasswordResetRequest request);

        /// <summary>
        /// Gets all available roles
        /// </summary>
        Task<List<Role>?> GetRolesAsync();
    }
}

