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
        public UserService(SignInManager<IdentityUser> signInManager, RoleManager<IdentityRole> roleManager) : base(signInManager, roleManager)
        {
        }

        public override async Task<string> CreateRoleAsync(string roleName, string id)
        {
            var role = new IdentityRole(roleName);
            role.Id = id;
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
