using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace BlazAdmin.ServerRender
{
    public class UserService : UserServiceBase<IdentityUser, IdentityRole>
    {
        private readonly ResourceAccessor resourceAccessor;

        public UserService(SignInManager<IdentityUser> signInManager, RoleManager<IdentityRole> roleManager, DbContext dbContext, ResourceAccessor resourceAccessor) : base(signInManager, roleManager, dbContext)
        {
            this.resourceAccessor = resourceAccessor;
        }

        public override async Task<string> CreateRoleAsync(RoleModel role)
        {
            var identityRole = new IdentityRole(role.Name);
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var result = await RoleManager.CreateAsync(identityRole);
                if (!result.Succeeded)
                {
                    return GetResultMessage(result);
                }
                DbContext.Set<RoleResource>().AddRange(role.Resources.Select(x => new RoleResource()
                {
                    ResourceId = x,
                    RoleId = identityRole.Id
                }));
                await DbContext.SaveChangesAsync();
                scope.Complete();
                return GetResultMessage(result);
            }
        }

        public override async Task<string> CreateUserAsync(string username, string email, string password)
        {
            var user = new IdentityUser(username);
            user.Email = email;
            var result = await SignInManager.UserManager.CreateAsync(user, password);
            return GetResultMessage(result);
        }

        public override async ValueTask<string> DeleteRolesAsync(params string[] ids)
        {
            var roles = RoleManager.Roles.Where(x => ids.Contains(x.Id)).ToArray();
            foreach (IdentityRole item in roles)
            {
                var result = await RoleManager.DeleteAsync(item);
                if (result.Succeeded)
                {
                    continue;
                }
                return GetResultMessage(result);
            }
            return string.Empty;
        }

        public override async Task<string> DeleteUsersAsync(params string[] userIds)
        {
            var users = SignInManager.UserManager.Users.Where(x => userIds.Contains(x.Id)).ToArray();
            foreach (IdentityUser item in users)
            {
                var result = await SignInManager.UserManager.DeleteAsync(item);
                if (result.Succeeded)
                {
                    continue;
                }
                return GetResultMessage(result);
            }
            return string.Empty;
        }

        public override List<RoleModel> GetRoles()
        {
            var roles = RoleManager.Roles.Select(x => new RoleModel()
            {
                Name = x.Name,
                Id = x.Id,
            }).ToList();
            var roleIds = roles.Select(x => x.Id).ToArray();
            var resources = DbContext.Set<RoleResource>().Where(x => roleIds.Contains(x.RoleId)).ToArray();
            foreach (var role in roles)
            {
                role.Resources = resources.Where(x => x.RoleId == role.Id).Select(x => x.ResourceId).ToList();
            }
            return roles.ToList();
        }

        public override string GetRolesWithResources(params string[] resources)
        {
            var roleIds = DbContext.Set<RoleResource>().Where(x => resources.Contains(x.ResourceId)).Select(x => x.RoleId).ToArray();
            var roleNames = RoleManager.Roles.Where(x => roleIds.Contains(x.Id)).Select(x => x.Name).ToArray();
            return string.Join(",", roleNames);
        }

        public override async Task<string> UpdateUserAsync(UserModel userModel)
        {
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var user = await SignInManager.UserManager.FindByIdAsync(userModel.Id);
                if (user == null)
                {
                    return "当前用户不存在";
                }
                user.UserName = userModel.Username;
                user.Email = userModel.Email;
                var existRoles = await SignInManager.UserManager.GetRolesAsync(user);
                var result = await SignInManager.UserManager.RemoveFromRolesAsync(user, existRoles);
                if (!result.Succeeded)
                {
                    return GetResultMessage(result);
                }
                var newRoles = RoleManager.Roles.Where(x => userModel.Roles.Contains(x.Id)).Select(x => x.Name).ToArray();
                result = await SignInManager.UserManager.AddToRolesAsync(user, newRoles);
                if (!result.Succeeded)
                {
                    return GetResultMessage(result);
                }
                result = await SignInManager.UserManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    return GetResultMessage(result);
                }
                scope.Complete();
            }
            return string.Empty;
        }
    }
}
