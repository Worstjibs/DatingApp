using System;
using System.Threading.Tasks;
using API.Extensions;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace API.Helpers {
    public class LogUserActivity : IAsyncActionFilter {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
            var resultContext = await next();

            // If the request is not from a logged in user, return
            if (!resultContext.HttpContext.User.Identity.IsAuthenticated) return;

            // Get the username, using out ClaimsPrincipleExtensions method
            var userId = resultContext.HttpContext.User.GetUserId();

            // Get the UserRepository using GetService from DependencyInjection
            var repo = resultContext.HttpContext.RequestServices.GetService<IUserRepository>();

            // Get the User from the Repository and set LastActive
            var user = await repo.GetUserByIdAsync(userId);
            user.LastActive = DateTime.Now;
            await repo.SaveAllAsync();
        }
    }
}