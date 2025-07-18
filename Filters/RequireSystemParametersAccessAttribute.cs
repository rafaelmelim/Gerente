using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Gerente.Services;

namespace Gerente.Filters
{
    public class RequireSystemParametersAccessAttribute : TypeFilterAttribute
    {
        public RequireSystemParametersAccessAttribute() : base(typeof(RequireSystemParametersAccessFilter))
        {
        }
    }

    public class RequireSystemParametersAccessFilter : IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var accessControlService = context.HttpContext.RequestServices.GetService<AccessControlService>();
            var userId = context.HttpContext.Session.GetInt32("UserId");

            if (!userId.HasValue || accessControlService == null || !accessControlService.HasAccessToSystemParameters(userId.Value))
            {
                context.Result = new RedirectToActionResult("Index", "Home", null);
                return;
            }
        }
    }
} 