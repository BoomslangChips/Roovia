using Microsoft.EntityFrameworkCore;
using Roovia.Data;
using Roovia.Interfaces;
using Roovia.Models.Helper;
using Roovia.Models.Users;

namespace Roovia.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public PermissionService(
            ApplicationDbContext context,
            IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _context = context;
            _contextFactory = contextFactory;
        }

        #region Permission Operations

        public async Task<ResponseModel> GetAllPermissions()
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var permissions = await context.Permissions.ToListAsync();

                response.Response = permissions;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Permissions retrieved successfully.";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving permissions: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetPermissionById(int id)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var permission = await context.Permissions.FindAsync(id);

                if (permission != null)
                {
                    response.Response = permission;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Permission retrieved successfully.";
                }
                else
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Permission not found.";
                }
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving the permission: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetPermissionsByCategory(string category)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var permissions = await context.Permissions
                    .Where(p => p.Category == category)
                    .ToListAsync();

                response.Response = permissions;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = $"Permissions in category '{category}' retrieved successfully.";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving permissions by category: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> CreatePermission(Permission permission)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // Check if a permission with the same system name already exists
                var existingPermission = await context.Permissions
                    .FirstOrDefaultAsync(p => p.SystemName == permission.SystemName);

                if (existingPermission != null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "A permission with this system name already exists.";
                    return response;
                }

                await context.Permissions.AddAsync(permission);
                await context.SaveChangesAsync();

                response.Response = permission;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Permission created successfully.";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while creating the permission: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> UpdatePermission(int id, Permission updatedPermission)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var permission = await context.Permissions.FindAsync(id);

                if (permission == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Permission not found.";
                    return response;
                }

                // Check if another permission (not this one) has the same system name
                var existingPermission = await context.Permissions
                    .FirstOrDefaultAsync(p => p.SystemName == updatedPermission.SystemName && p.Id != id);

                if (existingPermission != null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Another permission with this system name already exists.";
                    return response;
                }

                // Update permission properties
                permission.Name = updatedPermission.Name;
                permission.Description = updatedPermission.Description;
                permission.Category = updatedPermission.Category;
                permission.SystemName = updatedPermission.SystemName;
                permission.IsActive = updatedPermission.IsActive;

                await context.SaveChangesAsync();

                response.Response = permission;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Permission updated successfully.";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating the permission: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> DeletePermission(int id)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var permission = await context.Permissions.FindAsync(id);

                if (permission == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Permission not found.";
                    return response;
                }

                // Check if permission is assigned to any roles
                var rolePermissions = await context.RolePermissions
                    .Where(rp => rp.PermissionId == id)
                    .ToListAsync();

                if (rolePermissions.Any())
                {
                    context.RolePermissions.RemoveRange(rolePermissions);
                }

                context.Permissions.Remove(permission);
                await context.SaveChangesAsync();

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Permission deleted successfully.";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while deleting the permission: " + ex.Message;
            }

            return response;
        }

        #endregion

        #region Role Operations

        public async Task<ResponseModel> GetAllRoles()
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var roles = await context.Roles.ToListAsync();

                response.Response = roles;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Roles retrieved successfully.";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving roles: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetRoleById(int id)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var role = await context.Roles.FindAsync(id);

                if (role != null)
                {
                    response.Response = role;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Role retrieved successfully.";
                }
                else
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Role not found.";
                }
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving the role: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetRoleWithPermissions(int roleId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var role = await context.Roles
                    .Include(r => r.Permissions)
                    .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync(r => r.Id == roleId);

                if (role != null)
                {
                    response.Response = role;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Role with permissions retrieved successfully.";
                }
                else
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Role not found.";
                }
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving the role with permissions: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> CreateRole(Role role)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // Check if a role with the same name already exists
                var existingRole = await context.Roles
                    .FirstOrDefaultAsync(r => r.Name == role.Name);

                if (existingRole != null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "A role with this name already exists.";
                    return response;
                }

                role.CreatedOn = DateTime.Now;
                await context.Roles.AddAsync(role);
                await context.SaveChangesAsync();

                response.Response = role;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Role created successfully.";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while creating the role: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> UpdateRole(int id, Role updatedRole)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var role = await context.Roles.FindAsync(id);

                if (role == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Role not found.";
                    return response;
                }

                // Check if another role (not this one) has the same name
                var existingRole = await context.Roles
                    .FirstOrDefaultAsync(r => r.Name == updatedRole.Name && r.Id != id);

                if (existingRole != null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Another role with this name already exists.";
                    return response;
                }

                // Check if trying to modify a preset role
                if (role.IsPreset && !updatedRole.IsPreset)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Cannot change a preset role to a custom role.";
                    return response;
                }

                // Update role properties
                role.Name = updatedRole.Name;
                role.Description = updatedRole.Description;
                role.IsActive = updatedRole.IsActive;
                role.UpdatedDate = DateTime.Now;
                role.UpdatedBy = updatedRole.UpdatedBy;

                await context.SaveChangesAsync();

                response.Response = role;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Role updated successfully.";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating the role: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> DeleteRole(int id)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var role = await context.Roles
                    .Include(r => r.UserRoles)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (role == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Role not found.";
                    return response;
                }

                // Check if the role is a preset
                if (role.IsPreset)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Cannot delete a preset role.";
                    return response;
                }

                // Check if the role is assigned to any users
                if (role.UserRoles.Any())
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Cannot delete a role that is assigned to users.";
                    return response;
                }

                // Delete role permissions
                var rolePermissions = await context.RolePermissions
                    .Where(rp => rp.RoleId == id)
                    .ToListAsync();

                context.RolePermissions.RemoveRange(rolePermissions);

                // Delete the role
                context.Roles.Remove(role);
                await context.SaveChangesAsync();

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Role deleted successfully.";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while deleting the role: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> CloneRole(int roleId, string newRoleName)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // Get the source role with permissions
                var sourceRole = await context.Roles
                    .Include(r => r.Permissions)
                    .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync(r => r.Id == roleId);

                if (sourceRole == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Source role not found.";
                    return response;
                }

                // Check if a role with the new name already exists
                var existingRole = await context.Roles
                    .FirstOrDefaultAsync(r => r.Name == newRoleName);

                if (existingRole != null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "A role with the new name already exists.";
                    return response;
                }

                // Create the new role
                var newRole = new Role
                {
                    Name = newRoleName,
                    Description = $"Clone of {sourceRole.Name}",
                    IsPreset = false,
                    IsActive = sourceRole.IsActive,
                    CreatedOn = DateTime.Now,
                    CreatedBy = sourceRole.CreatedBy
                };

                await context.Roles.AddAsync(newRole);
                await context.SaveChangesAsync();

                // Copy permissions
                foreach (var rolePermission in sourceRole.Permissions)
                {
                    var newRolePermission = new RolePermission
                    {
                        RoleId = newRole.Id,
                        PermissionId = rolePermission.PermissionId,
                        IsActive = rolePermission.IsActive
                    };

                    await context.RolePermissions.AddAsync(newRolePermission);
                }

                await context.SaveChangesAsync();

                response.Response = newRole;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Role cloned successfully.";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while cloning the role: " + ex.Message;
            }

            return response;
        }

        #endregion

        #region Role-Permission Operations

        public async Task<ResponseModel> AssignPermissionToRole(int roleId, int permissionId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // Check if role exists
                var role = await context.Roles.FindAsync(roleId);
                if (role == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Role not found.";
                    return response;
                }

                // Check if permission exists
                var permission = await context.Permissions.FindAsync(permissionId);
                if (permission == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Permission not found.";
                    return response;
                }

                // Check if permission is already assigned to role
                var existingRolePermission = await context.RolePermissions
                    .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

                if (existingRolePermission != null)
                {
                    // If it exists but is inactive, make it active
                    if (!existingRolePermission.IsActive)
                    {
                        existingRolePermission.IsActive = true;
                        await context.SaveChangesAsync();

                        response.Response = existingRolePermission;
                        response.ResponseInfo.Success = true;
                        response.ResponseInfo.Message = "Permission re-activated for the role.";
                        return response;
                    }

                    // If already active, nothing to do
                    response.Response = existingRolePermission;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Permission already assigned to the role.";
                    return response;
                }

                // Create new role-permission relationship
                var rolePermission = new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = permissionId,
                    IsActive = true
                };

                await context.RolePermissions.AddAsync(rolePermission);
                await context.SaveChangesAsync();

                response.Response = rolePermission;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Permission assigned to role successfully.";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while assigning permission to role: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> RemovePermissionFromRole(int roleId, int permissionId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // Find the role-permission relationship
                var rolePermission = await context.RolePermissions
                    .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

                if (rolePermission == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Permission is not assigned to the role.";
                    return response;
                }

                // Remove the relationship
                context.RolePermissions.Remove(rolePermission);
                await context.SaveChangesAsync();

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Permission removed from role successfully.";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while removing permission from role: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> UpdateRolePermissions(int roleId, List<int> permissionIds)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // Check if role exists
                var role = await context.Roles.FindAsync(roleId);
                if (role == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Role not found.";
                    return response;
                }

                // Get current role permissions
                var currentRolePermissions = await context.RolePermissions
                    .Where(rp => rp.RoleId == roleId)
                    .ToListAsync();

                // Remove permissions that are not in the new list
                var permissionsToRemove = currentRolePermissions
                    .Where(rp => !permissionIds.Contains(rp.PermissionId))
                    .ToList();

                foreach (var permission in permissionsToRemove)
                {
                    context.RolePermissions.Remove(permission);
                }

                // Add new permissions that are not in the current list
                var currentPermissionIds = currentRolePermissions.Select(rp => rp.PermissionId);
                var newPermissionIds = permissionIds.Except(currentPermissionIds);

                foreach (var permissionId in newPermissionIds)
                {
                    // Verify permission exists
                    var permissionExists = await context.Permissions.AnyAsync(p => p.Id == permissionId);
                    if (!permissionExists)
                    {
                        continue; // Skip if permission doesn't exist
                    }

                    var rolePermission = new RolePermission
                    {
                        RoleId = roleId,
                        PermissionId = permissionId,
                        IsActive = true
                    };

                    await context.RolePermissions.AddAsync(rolePermission);
                }

                await context.SaveChangesAsync();

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Role permissions updated successfully.";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating role permissions: " + ex.Message;
            }

            return response;
        }

        #endregion

        #region User-Role Operations

        public async Task<ResponseModel> AssignRoleToUser(string userId, int roleId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // Check if user exists
                var user = await context.Users.FindAsync(userId);
                if (user == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "User not found.";
                    return response;
                }

                // Check if role exists
                var role = await context.Roles.FindAsync(roleId);
                if (role == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Role not found.";
                    return response;
                }

                // Check if user already has this role
                var existingUserRole = await context.UserRoleAssignments
                    .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

                if (existingUserRole != null)
                {
                    response.Response = existingUserRole;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "User already has this role.";
                    return response;
                }

                // Create new user-role relationship
                var userRole = new UserRoleAssignment
                {
                    UserId = userId,
                    RoleId = roleId,
                    AssignedDate = DateTime.Now
                };

                await context.UserRoleAssignments.AddAsync(userRole);
                await context.SaveChangesAsync();

                response.Response = userRole;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Role assigned to user successfully.";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while assigning role to user: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> RemoveRoleFromUser(string userId, int roleId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // Find the user-role relationship
                var userRole = await context.UserRoleAssignments
                    .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

                if (userRole == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "User does not have this role.";
                    return response;
                }

                // Remove the relationship
                context.UserRoleAssignments.Remove(userRole);
                await context.SaveChangesAsync();

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Role removed from user successfully.";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while removing role from user: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetUserRoles(string userId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // Check if user exists
                var user = await context.Users.FindAsync(userId);
                if (user == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "User not found.";
                    return response;
                }

                // Get user roles with role details
                var userRoles = await context.UserRoleAssignments
                    .Include(ur => ur.Role)
                    .Where(ur => ur.UserId == userId)
                    .ToListAsync();

                response.Response = userRoles;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "User roles retrieved successfully.";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving user roles: " + ex.Message;
            }

            return response;
        }

        #endregion

        #region Permission Checks

        public async Task<bool> UserHasPermission(string userId, string permissionName)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // Get user roles
                var userRoles = await context.UserRoleAssignments
                    .Where(ur => ur.UserId == userId)
                    .Select(ur => ur.RoleId)
                    .ToListAsync();

                if (!userRoles.Any())
                {
                    return false;
                }

                // Check if any role has the specified permission
                var hasPermission = await context.RolePermissions
                    .Include(rp => rp.Permission)
                    .AnyAsync(rp =>
                        userRoles.Contains(rp.RoleId) &&
                        rp.Permission.SystemName == permissionName &&
                        rp.IsActive &&
                        rp.Permission.IsActive);

                return hasPermission;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<string>> GetUserPermissions(string userId)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // Get user roles
                var userRoles = await context.UserRoleAssignments
                    .Where(ur => ur.UserId == userId)
                    .Select(ur => ur.RoleId)
                    .ToListAsync();

                if (!userRoles.Any())
                {
                    return new List<string>();
                }

                // Get all active permissions from all user roles
                var permissions = await context.RolePermissions
                    .Include(rp => rp.Permission)
                    .Where(rp =>
                        userRoles.Contains(rp.RoleId) &&
                        rp.IsActive &&
                        rp.Permission.IsActive)
                    .Select(rp => rp.Permission.SystemName)
                    .Distinct()
                    .ToListAsync();

                return permissions;
            }
            catch
            {
                return new List<string>();
            }
        }

        #endregion
    }
}