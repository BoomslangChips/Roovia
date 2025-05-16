using Roovia.Models.BusinessHelperModels;
using Roovia.Models.UserCompanyModels;

namespace Roovia.Interfaces
{
    public interface IPermissionService
    {
        #region Permission Operations

        Task<ResponseModel> GetAllPermissions();

        Task<ResponseModel> GetPermissionById(int id);

        Task<ResponseModel> GetPermissionsByCategory(string category);

        Task<ResponseModel> CreatePermission(Permission permission);

        Task<ResponseModel> UpdatePermission(int id, Permission updatedPermission);

        Task<ResponseModel> DeletePermission(int id);

        #endregion Permission Operations

        #region Role Operations

        Task<ResponseModel> GetAllRoles();

        Task<ResponseModel> GetRoleById(int id);

        Task<ResponseModel> GetRoleWithPermissions(int roleId);

        Task<ResponseModel> CreateRole(Role role);

        Task<ResponseModel> UpdateRole(int id, Role updatedRole);

        Task<ResponseModel> DeleteRole(int id);

        Task<ResponseModel> CloneRole(int roleId, string newRoleName);

        #endregion Role Operations

        #region Role-Permission Operations

        Task<ResponseModel> AssignPermissionToRole(int roleId, int permissionId);

        Task<ResponseModel> RemovePermissionFromRole(int roleId, int permissionId);

        Task<ResponseModel> UpdateRolePermissions(int roleId, List<int> permissionIds);

        #endregion Role-Permission Operations

        #region User-Role Operations

        Task<ResponseModel> AssignRoleToUser(string userId, int roleId);

        Task<ResponseModel> RemoveRoleFromUser(string userId, int roleId);

        Task<ResponseModel> GetUserRoles(string userId);

        #endregion User-Role Operations

        #region Permission Checks

        Task<bool> UserHasPermission(string userId, string permissionName);

        Task<List<string>> GetUserPermissions(string userId);

        #endregion Permission Checks

        #region User Permission Overrides

        Task<ResponseModel> SetUserPermissionOverride(string userId, int permissionId, bool isGranted);

        Task<ResponseModel> SetUserPermissionOverride(string userId, int permissionId, bool isGranted, string currentUserId);

        Task<ResponseModel> RemoveUserPermissionOverride(string userId, int permissionId);

        Task<ResponseModel> GetUserPermissionOverrides(string userId);

        #endregion User Permission Overrides
    }
}