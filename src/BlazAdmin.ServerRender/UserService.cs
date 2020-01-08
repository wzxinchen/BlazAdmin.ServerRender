using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazAdmin.ServerRender
{
    public class UserService : UserServiceBase<IdentityUser, IdentityRole>
    {
        private readonly DbContext dbContext;

        public UserService(SignInManager<IdentityUser> signInManager, RoleManager<IdentityRole> roleManager, DbContext dbContext) : base(signInManager, roleManager)
        {
            this.dbContext = dbContext;
        }

        public override async Task<string> CreateRoleAsync(string roleName)
        {
            var role = new IdentityRole(roleName);
            var result = await RoleManager.CreateAsync(role);
            return GetResultMessage(result);
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

        public override async Task<List<RoleModel>> GetRolesAsync()
        {
            return (await RoleManager.Roles.ToListAsync()).Select(x => new RoleModel()
            {
                Name = x.Name,
                Id = x.Id
            }).ToList();
        }

        public override Task<string> GetRolesAsync(params string[] resources)
        {
            var identityResources = dbContext.Set<IdentityResource>().Where(x => resources.Contains(x.Name)).ToArray();
            var resourceIds = identityResources.Select(x => x.Id).ToArray();
            var roleIds = dbContext.Set<RoleResource>().Where(x => resourceIds.Contains(x.ResourceId)).Select(x => x.RoleId).ToArray();
            var roleNames = RoleManager.Roles.Where(x => roleIds.Contains(x.Id)).Select(x => x.Name).ToArray();
            return Task.FromResult(string.Join(",", roleNames));
        }

        public override async Task<string> UpdateUserAsync(UserModel userModel)
        {
            var user = await SignInManager.UserManager.FindByIdAsync(userModel.Id);
            if (user == null)
            {
                return "当前用户不存在";
            }
            user.UserName = userModel.Username;
            user.Email = userModel.Email;
            var result = await SignInManager.UserManager.UpdateAsync(user);
            return GetResultMessage(result);
        }
    }
}
