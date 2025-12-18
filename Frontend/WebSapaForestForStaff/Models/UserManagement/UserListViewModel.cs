using WebSapaForestForStaff.DTOs;
using WebSapaForestForStaff.DTOs.UserManagement;
using DTOsRole = WebSapaForestForStaff.DTOs.Role;

namespace WebSapaForestForStaff.Models.UserManagement
{
    public class UserListViewModel
    {
        public UserListResponse? UserList { get; set; }
        public List<DTOsRole>? AvailableRoles { get; set; }
        public UserSearchRequest? SearchRequest { get; set; }
    }
}

