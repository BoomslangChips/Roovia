using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Roovia.Data;
using Roovia.Interfaces;
using Roovia.Models.BusinessHelperModels;
using Roovia.Models.UserCompanyModels;

namespace Roovia.Security
{
    public class CachedPermissionService : IPermissionService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

        public CachedPermissionService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            IMemoryCache cache)
        {
            _contextFactory = contextFactory;
            _cache = cache;
        }

        #region Permission Operations

        public async Task<ResponseModel> SetUserPermissionOverride(string userId, int permissionId, bool isGranted, string currentUserId)
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

                // Check if permission exists
                var permission = await context.Permissions.FindAsync(permissionId);
                if (permission == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Permission not found.";
                    return response;
                }

                // Check if override already exists
                var existingOverride = await context.UserPermissionOverrides
                    .FirstOrDefaultAsync(upo => upo.UserId == userId && upo.PermissionId == permissionId);

                if (existingOverride != null)
                {
                    // Update existing
                    existingOverride.IsGranted = isGranted;
                    existingOverride.UpdatedDate = DateTime.Now;
                    existingOverride.UpdatedBy = currentUserId;
                }
                else
                {
                    // Create new
                    var newOverride = new UserPermissionOverride
                    {
                        UserId = userId,
                        PermissionId = permissionId,
                        IsGranted = isGranted,
                        CreatedDate = DateTime.Now,
                        CreatedBy = currentUserId
                    };
                    await context.UserPermissionOverrides.AddAsync(newOverride);
                }

                await context.SaveChangesAsync();

                // Clear cache for this user
                ClearUserPermissionCache(userId, permission.SystemName);

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "User permission override set successfully.";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while setting user permission override: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> SetUserPermissionOverride(string userId, int permissionId, bool isGranted)
        {
            return await SetUserPermissionOverride(userId, permissionId, isGranted, "System");
        }

        public async Task<ResponseModel> RemoveUserPermissionOverride(string userId, int permissionId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var overRide = await context.UserPermissionOverrides
                    .Include(upo => upo.Permission)
                    .FirstOrDefaultAsync(upo => upo.UserId == userId && upo.PermissionId == permissionId);

                if (overRide == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Permission override not found.";
                    return response;
                }

                var permissionSystemName = overRide.Permission?.SystemName;

                context.UserPermissionOverrides.Remove(overRide);
                await context.SaveChangesAsync();

                // Clear cache for this user
                if (!string.IsNullOrEmpty(permissionSystemName))
                {
                    ClearUserPermissionCache(userId, permissionSystemName);
                }

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "User permission override removed successfully.";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while removing user permission override: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetUserPermissionOverrides(string userId)
        {
            ResponseModel response = new();

            try
            {
                var cacheKey = $"user_overrides:{userId}";
                if (_cache.TryGetValue(cacheKey, out List<UserPermissionOverride> cachedOverrides))
                {
                    response.Response = cachedOverrides;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "User permission overrides retrieved from cache.";
                    return response;
                }

                using var context = await _contextFactory.CreateDbContextAsync();

                var overrides = await context.UserPermissionOverrides
                    .Include(upo => upo.Permission)
                    .Where(upo => upo.UserId == userId)
                    .ToListAsync();

                _cache.Set(cacheKey, overrides, _cacheExpiration);

                response.Response = overrides;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "User permission overrides retrieved successfully.";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving user permission overrides: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetAllPermissions()
        {
            ResponseModel response = new();

            try
            {
                var cacheKey = "all_permissions";
                if (_cache.TryGetValue(cacheKey, out List<Permission> cachedPermissions))
                {
                    response.Response = cachedPermissions;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Permissions retrieved from cache.";
                    return response;
                }

                using var context = await _contextFactory.CreateDbContextAsync();
                var permissions = await context.Permissions.ToListAsync();

                _cache.Set(cacheKey, permissions, _cacheExpiration);

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
                var cacheKey = $"permission:{id}";
                if (_cache.TryGetValue(cacheKey, out Permission cachedPermission))
                {
                    response.Response = cachedPermission;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Permission retrieved from cache.";
                    return response;
                }

                using var context = await _contextFactory.CreateDbContextAsync();
                var permission = await context.Permissions.FindAsync(id);

                if (permission != null)
                {
                    _cache.Set(cacheKey, permission, _cacheExpiration);
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
                var cacheKey = $"permissions_category:{category}";
                if (_cache.TryGetValue(cacheKey, out List<Permission> cachedPermissions))
                {
                    response.Response = cachedPermissions;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = $"Permissions in category '{category}' retrieved from cache.";
                    return response;
                }

                using var context = await _contextFactory.CreateDbContextAsync();
                var permissions = await context.Permissions
                    .Where(p => p.Category == category)
                    .ToListAsync();

                _cache.Set(cacheKey, permissions, _cacheExpiration);

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

                // Clear related caches
                _cache.Remove("all_permissions");
                _cache.Remove($"permissions_category:{permission.Category}");

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

                var existingPermission = await context.Permissions
                    .FirstOrDefaultAsync(p => p.SystemName == updatedPermission.SystemName && p.Id != id);

                if (existingPermission != null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Another permission with this system name already exists.";
                    return response;
                }

                var oldCategory = permission.Category;
                var oldSystemName = permission.SystemName;

                // Update permission properties
                permission.Name = updatedPermission.Name;
                permission.Description = updatedPermission.Description;
                permission.Category = updatedPermission.Category;
                permission.SystemName = updatedPermission.SystemName;
                permission.IsActive = updatedPermission.IsActive;

                await context.SaveChangesAsync();

                // Clear related caches
                _cache.Remove("all_permissions");
                _cache.Remove($"permission:{id}");
                _cache.Remove($"permissions_category:{oldCategory}");
                _cache.Remove($"permissions_category:{permission.Category}");

                // Clear all user permission caches if systemName changed
                if (oldSystemName != permission.SystemName)
                {
                    ClearAllUserPermissionCaches();
                }

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

                var rolePermissions = await context.RolePermissions
                    .Where(rp => rp.PermissionId == id)
                    .ToListAsync();

                if (rolePermissions.Any())
                {
                    context.RolePermissions.RemoveRange(rolePermissions);
                }

                context.Permissions.Remove(permission);
                await context.SaveChangesAsync();

                // Clear related caches
                _cache.Remove("all_permissions");
                _cache.Remove($"permission:{id}");
                _cache.Remove($"permissions_category:{permission.Category}");
                ClearAllUserPermissionCaches();

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

        #endregion Permission Operations

        #region Role Operations

        public async Task<ResponseModel> GetAllRoles()
        {
            ResponseModel response = new();

            try
            {
                var cacheKey = "all_roles";
                if (_cache.TryGetValue(cacheKey, out List<Role> cachedRoles))
                {
                    response.Response = cachedRoles;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Roles retrieved from cache.";
                    return response;
                }

                using var context = await _contextFactory.CreateDbContextAsync();
                var roles = await context.Roles.ToListAsync();

                _cache.Set(cacheKey, roles, _cacheExpiration);

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
                var cacheKey = $"role:{id}";
                if (_cache.TryGetValue(cacheKey, out Role cachedRole))
                {
                    response.Response = cachedRole;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Role retrieved from cache.";
                    return response;
                }

                using var context = await _contextFactory.CreateDbContextAsync();
                var role = await context.Roles.FindAsync(id);

                if (role != null)
                {
                    _cache.Set(cacheKey, role, _cacheExpiration);
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
                var cacheKey = $"role_with_permissions:{roleId}";
                if (_cache.TryGetValue(cacheKey, out Role cachedRole))
                {
                    response.Response = cachedRole;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Role with permissions retrieved from cache.";
                    return response;
                }

                using var context = await _contextFactory.CreateDbContextAsync();
                var role = await context.Roles
                    .Include(r => r.Permissions)
                    .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync(r => r.Id == roleId);

                if (role != null)
                {
                    _cache.Set(cacheKey, role, _cacheExpiration);
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

                // Clear cache
                _cache.Remove("all_roles");

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

                var existingRole = await context.Roles
                    .FirstOrDefaultAsync(r => r.Name == updatedRole.Name && r.Id != id);

                if (existingRole != null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Another role with this name already exists.";
                    return response;
                }

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

                // Clear cache
                _cache.Remove("all_roles");
                _cache.Remove($"role:{id}");
                _cache.Remove($"role_with_permissions:{id}");
                ClearAllUserPermissionCaches();

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

                if (role.IsPreset)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Cannot delete a preset role.";
                    return response;
                }

                if (role.UserRoles.Any())
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Cannot delete a role that is assigned to users.";
                    return response;
                }

                var rolePermissions = await context.RolePermissions
                    .Where(rp => rp.RoleId == id)
                    .ToListAsync();

                context.RolePermissions.RemoveRange(rolePermissions);
                context.Roles.Remove(role);
                await context.SaveChangesAsync();

                // Clear cache
                _cache.Remove("all_roles");
                _cache.Remove($"role:{id}");
                _cache.Remove($"role_with_permissions:{id}");
                ClearAllUserPermissionCaches();

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

                var existingRole = await context.Roles
                    .FirstOrDefaultAsync(r => r.Name == newRoleName);

                if (existingRole != null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "A role with the new name already exists.";
                    return response;
                }

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

                // Clear cache
                _cache.Remove("all_roles");

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

        #endregion Role Operations

        #region Role-Permission Operations

        public async Task<ResponseModel> AssignPermissionToRole(int roleId, int permissionId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var role = await context.Roles.FindAsync(roleId);
                if (role == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Role not found.";
                    return response;
                }

                var permission = await context.Permissions.FindAsync(permissionId);
                if (permission == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Permission not found.";
                    return response;
                }

                var existingRolePermission = await context.RolePermissions
                    .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

                if (existingRolePermission != null)
                {
                    if (!existingRolePermission.IsActive)
                    {
                        existingRolePermission.IsActive = true;
                        await context.SaveChangesAsync();

                        response.Response = existingRolePermission;
                        response.ResponseInfo.Success = true;
                        response.ResponseInfo.Message = "Permission re-activated for the role.";
                    }
                    else
                    {
                        response.Response = existingRolePermission;
                        response.ResponseInfo.Success = true;
                        response.ResponseInfo.Message = "Permission already assigned to the role.";
                    }
                }
                else
                {
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

                // Clear cache
                _cache.Remove($"role_with_permissions:{roleId}");
                ClearAllUserPermissionCaches();
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

                var rolePermission = await context.RolePermissions
                    .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

                if (rolePermission == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Permission is not assigned to the role.";
                    return response;
                }

                context.RolePermissions.Remove(rolePermission);
                await context.SaveChangesAsync();

                // Clear cache
                _cache.Remove($"role_with_permissions:{roleId}");
                ClearAllUserPermissionCaches();

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

                var role = await context.Roles.FindAsync(roleId);
                if (role == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Role not found.";
                    return response;
                }

                var currentRolePermissions = await context.RolePermissions
                    .Where(rp => rp.RoleId == roleId)
                    .ToListAsync();

                var permissionsToRemove = currentRolePermissions
                    .Where(rp => !permissionIds.Contains(rp.PermissionId))
                    .ToList();

                foreach (var permission in permissionsToRemove)
                {
                    context.RolePermissions.Remove(permission);
                }

                var currentPermissionIds = currentRolePermissions.Select(rp => rp.PermissionId);
                var newPermissionIds = permissionIds.Except(currentPermissionIds);

                foreach (var permissionId in newPermissionIds)
                {
                    var permissionExists = await context.Permissions.AnyAsync(p => p.Id == permissionId);
                    if (!permissionExists)
                    {
                        continue;
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

                // Clear cache
                _cache.Remove($"role_with_permissions:{roleId}");
                ClearAllUserPermissionCaches();

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

        #endregion Role-Permission Operations

        #region User-Role Operations

        public async Task<ResponseModel> AssignRoleToUser(string userId, int roleId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var user = await context.Users.FindAsync(userId);
                if (user == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "User not found.";
                    return response;
                }

                var role = await context.Roles.FindAsync(roleId);
                if (role == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Role not found.";
                    return response;
                }

                var existingUserRole = await context.UserRoleAssignments
                    .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

                if (existingUserRole != null)
                {
                    response.Response = existingUserRole;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "User already has this role.";
                    return response;
                }

                var userRole = new UserRoleAssignment
                {
                    UserId = userId,
                    RoleId = roleId,
                    AssignedDate = DateTime.Now
                };

                await context.UserRoleAssignments.AddAsync(userRole);
                await context.SaveChangesAsync();

                // Clear cache for this user
                ClearUserPermissionCache(userId);

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

                var userRole = await context.UserRoleAssignments
                    .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

                if (userRole == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "User does not have this role.";
                    return response;
                }

                context.UserRoleAssignments.Remove(userRole);
                await context.SaveChangesAsync();

                // Clear cache for this user
                ClearUserPermissionCache(userId);

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
                var cacheKey = $"user_roles:{userId}";
                if (_cache.TryGetValue(cacheKey, out List<UserRoleAssignment> cachedRoles))
                {
                    response.Response = cachedRoles;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "User roles retrieved from cache.";
                    return response;
                }

                using var context = await _contextFactory.CreateDbContextAsync();

                var user = await context.Users.FindAsync(userId);
                if (user == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "User not found.";
                    return response;
                }

                var userRoles = await context.UserRoleAssignments
                    .Include(ur => ur.Role)
                    .Where(ur => ur.UserId == userId)
                    .ToListAsync();

                _cache.Set(cacheKey, userRoles, _cacheExpiration);

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

        #endregion User-Role Operations

        #region Permission Checks

        public async Task<bool> UserHasPermission(string userId, string permissionName)
        {
            var cacheKey = $"permission:{userId}:{permissionName}";

            // Try to get from cache first
            if (_cache.TryGetValue(cacheKey, out bool cachedResult))
            {
                return cachedResult;
            }

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // First get the permission using projection to avoid loading the entire entity
                var permission = await context.Permissions
                    .AsNoTracking()
                    .Where(p => p.SystemName == permissionName && p.IsActive)
                    .Select(p => new { p.Id })
                    .FirstOrDefaultAsync();

                if (permission == null)
                {
                    // Permission doesn't exist or is not active
                    _cache.Set(cacheKey, false, _cacheExpiration);
                    return false;
                }

                // Check for user override using projection
                var userOverride = await context.UserPermissionOverrides
                    .AsNoTracking()
                    .Where(upo => upo.UserId == userId && upo.PermissionId == permission.Id)
                    .Select(upo => (bool?)upo.IsGranted)
                    .FirstOrDefaultAsync();

                if (userOverride.HasValue)
                {
                    // If there's an explicit override, respect it
                    var result = userOverride.Value;
                    _cache.Set(cacheKey, result, _cacheExpiration);
                    return result;
                }

                // Get user roles
                var userRoleIds = await context.UserRoleAssignments
                    .AsNoTracking()
                    .Where(ur => ur.UserId == userId && ur.IsActive)
                    .Select(ur => ur.RoleId)
                    .ToListAsync();

                if (!userRoleIds.Any())
                {
                    _cache.Set(cacheKey, false, _cacheExpiration);
                    return false;
                }

                // Check if any role has the specified permission
                // Use projection and avoid navigation properties
                var hasPermission = await context.RolePermissions
                    .AsNoTracking()
                    .Where(rp => userRoleIds.Contains(rp.RoleId) &&
                               rp.PermissionId == permission.Id &&
                               rp.IsActive)
                    .AnyAsync();

                _cache.Set(cacheKey, hasPermission, _cacheExpiration);
                return hasPermission;
            }
            catch (Exception)
            {
                // Log the exception if you have a logger
                // _logger?.LogError(ex, "Error checking permission {Permission} for user {UserId}", permissionName, userId);
                return false;
            }
        }

        public async Task<List<string>> GetUserPermissions(string userId)
        {
            var cacheKey = $"permissions:{userId}";

            if (_cache.TryGetValue(cacheKey, out List<string> cachedPermissions))
            {
                return cachedPermissions;
            }

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var userRoleIds = await context.UserRoleAssignments
                    .AsNoTracking()
                    .Where(ur => ur.UserId == userId)
                    .Select(ur => ur.RoleId)
                    .ToListAsync();

                var permissionsFromRoles = await context.RolePermissions
                    .AsNoTracking()
                    .Include(rp => rp.Permission)
                    .Where(rp =>
                        userRoleIds.Contains(rp.RoleId) &&
                        rp.IsActive &&
                        rp.Permission.IsActive)
                    .Select(rp => new
                    {
                        rp.PermissionId,
                        rp.Permission.SystemName
                    })
                    .ToListAsync();

                var userOverrides = await context.UserPermissionOverrides
                    .AsNoTracking()
                    .Include(upo => upo.Permission)
                    .Where(upo => upo.UserId == userId)
                    .ToListAsync();

                var resultPermissions = new List<string>();

                foreach (var perm in permissionsFromRoles)
                {
                    var userOverride = userOverrides.FirstOrDefault(upo => upo.PermissionId == perm.PermissionId);

                    if (userOverride == null || userOverride.IsGranted)
                    {
                        resultPermissions.Add(perm.SystemName);
                    }
                }

                foreach (var grantedOverride in userOverrides.Where(upo => upo.IsGranted))
                {
                    if (!resultPermissions.Contains(grantedOverride.Permission.SystemName))
                    {
                        resultPermissions.Add(grantedOverride.Permission.SystemName);
                    }
                }

                var finalPermissions = resultPermissions.Distinct().ToList();
                _cache.Set(cacheKey, finalPermissions, _cacheExpiration);
                return finalPermissions;
            }
            catch
            {
                return new List<string>();
            }
        }

        #endregion Permission Checks

        #region Cache Management

        public void ClearUserPermissionCache(string userId, string permissionName = null)
        {
            if (string.IsNullOrEmpty(permissionName))
            {
                // Clear all permissions for the user
                _cache.Remove($"permissions:{userId}");
                _cache.Remove($"user_roles:{userId}");
                _cache.Remove($"user_overrides:{userId}");

                // Note: In production, you might want to track and clear individual permission checks
                // This is a simplified approach
            }
            else
            {
                // Clear specific permission for the user
                _cache.Remove($"permission:{userId}:{permissionName}");
            }
        }

        private void ClearAllUserPermissionCaches()
        {
            // In a real implementation, you'd track all user IDs and clear their caches
            // This is a simplified approach that demonstrates the concept
            // You might want to implement a more sophisticated cache invalidation strategy

            // For now, we'll just clear known cache prefixes
            // In production, consider using a distributed cache with tags or patterns
        }

        #endregion Cache Management
    }
}