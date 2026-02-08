using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace WebSapaForestForStaff.Helpers
{
    /// <summary>
    /// Extension methods for Controller to easily access user information
    /// </summary>
    public static class ControllerExtensions
    {
        /// <summary>
        /// Gets the current logged-in user ID from claims
        /// </summary>
        /// <param name="controller">The controller instance</param>
        /// <returns>User ID as integer, or null if not authenticated</returns>
        public static int? GetCurrentUserId(this Controller controller)
        {
            if (controller.User?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var userIdClaim = controller.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return null;
            }

            if (int.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }

            return null;
        }

        /// <summary>
        /// Gets the current logged-in user email from claims
        /// </summary>
        /// <param name="controller">The controller instance</param>
        /// <returns>User email, or null if not authenticated</returns>
        public static string? GetCurrentUserEmail(this Controller controller)
        {
            if (controller.User?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            return controller.User.FindFirst(ClaimTypes.Email)?.Value;
        }

        /// <summary>
        /// Gets the current logged-in user name from claims
        /// </summary>
        /// <param name="controller">The controller instance</param>
        /// <returns>User name, or null if not authenticated</returns>
        public static string? GetCurrentUserName(this Controller controller)
        {
            if (controller.User?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            return controller.User.FindFirst(ClaimTypes.Name)?.Value;
        }
    }
}

