
using Roovia.Models.BusinessHelperModels;
using Roovia.Models.UserCompanyModels;

namespace Roovia.Interfaces
{
    public interface IPermissionService
    {
        // Permission operations
        Task<ResponseModel> GetAllPermissions();
        Task<ResponseModel> GetPermissionById(int id);
        Task<ResponseModel> GetPermissionsByCategory(string category);
        Task<ResponseModel> CreatePermission(Permission permission);
        Task<ResponseModel> UpdatePermission(int id, Permission permission);
        Task<ResponseModel> DeletePermission(int id);
      
        // Role operations
        Task<ResponseModel> GetAllRoles();
        Task<ResponseModel> GetRoleById(int id);
        Task<ResponseModel> GetRoleWithPermissions(int roleId);
        Task<ResponseModel> CreateRole(Role role);
        Task<ResponseModel> UpdateRole(int id, Role role);
        Task<ResponseModel> DeleteRole(int id);
        Task<ResponseModel> CloneRole(int roleId, string newRoleName);

        // Role-Permission operations
        Task<ResponseModel> AssignPermissionToRole(int roleId, int permissionId);
        Task<ResponseModel> RemovePermissionFromRole(int roleId, int permissionId);
        Task<ResponseModel> UpdateRolePermissions(int roleId, List<int> permissionIds);

        // User-Role operations
        Task<ResponseModel> AssignRoleToUser(string userId, int roleId);
        Task<ResponseModel> RemoveRoleFromUser(string userId, int roleId);
        Task<ResponseModel> GetUserRoles(string userId);

        // User-Permission operations
        Task<ResponseModel> GetUserPermissionOverrides(string userId);
        Task<ResponseModel> SetUserPermissionOverride(string userId, int permissionId, bool isGranted, string currentUserId);
        Task<ResponseModel> RemoveUserPermissionOverride(string userId, int permissionId);

        // Permission check
        Task<bool> UserHasPermission(string userId, string permissionName);
        Task<List<string>> GetUserPermissions(string userId);
    }
}