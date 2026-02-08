using WebSapaForestForStaff.DTOs;

namespace WebSapaForestForStaff.Services.Api.Interfaces
{
    /// <summary>
    /// Interface for user profile API operations
    /// </summary>
    public interface IProfileApiService : IBaseApiService
    {
        /// <summary>
        /// Gets the current user's profile
        /// </summary>
        Task<User?> GetUserProfileAsync();

        /// <summary>
        /// Updates the current user's profile
        /// </summary>
        Task<User?> UpdateUserProfileAsync(UserProfileUpdateRequest request);
    }
}

