using WebSapaForestForStaff.DTOs;
using WebSapaForestForStaff.DTOs.Positions;

namespace WebSapaForestForStaff.Services.Api.Interfaces
{
    /// <summary>
    /// Interface for position management API operations
    /// </summary>
    public interface IPositionApiService : IBaseApiService
    {
        /// <summary>
        /// Gets all positions
        /// </summary>
        Task<List<Position>?> GetPositionsAsync();

        /// <summary>
        /// Searches positions with filters and pagination
        /// </summary>
        Task<PositionListResponse?> SearchPositionsAsync(PositionSearchRequest request);

        /// <summary>
        /// Gets a position by ID
        /// </summary>
        Task<PositionDto?> GetPositionAsync(int id);

        /// <summary>
        /// Creates a new position
        /// </summary>
        Task<bool> CreatePositionAsync(PositionCreateRequest request);

        /// <summary>
        /// Updates an existing position
        /// </summary>
        Task<bool> UpdatePositionAsync(PositionUpdateRequest request);

        /// <summary>
        /// Deletes a position by ID
        /// </summary>
        Task<bool> DeletePositionAsync(int id);

        /// <summary>
        /// Changes position status (active/inactive)
        /// </summary>
        Task<bool> ChangePositionStatusAsync(int id, int status);
    }
}

