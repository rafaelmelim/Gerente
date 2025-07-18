using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Gerente.Services;

namespace Gerente.Filters
{
    public class AccessControlFilter : IAuthorizationFilter
    {
        private readonly AccessControlService _accessControlService;
        private readonly string _requiredAccess;

        public AccessControlFilter(AccessControlService accessControlService, string requiredAccess)
        {
            _accessControlService = accessControlService;
            _requiredAccess = requiredAccess;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var userId = context.HttpContext.Session.GetInt32("UserId");
            
            if (!userId.HasValue)
            {
                context.Result = new RedirectToActionResult("Index", "Login", null);
                return;
            }

            bool hasAccess = _requiredAccess.ToLower() switch
            {
                "configuracoes" => _accessControlService.HasAccessToConfigurations(userId.Value),
                "usuarios" => _accessControlService.HasAccessToUsers(userId.Value),
                "projetos" => _accessControlService.HasAccessToProjects(userId.Value),
                "relatorios" => _accessControlService.HasAccessToReports(userId.Value),
                "total" => _accessControlService.HasTotalAccess(userId.Value),
                _ => false
            };

            if (!hasAccess)
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Home", null);
            }
        }
    }

    public class RequireConfigurationsAccessAttribute : TypeFilterAttribute
    {
        public RequireConfigurationsAccessAttribute() : base(typeof(AccessControlFilter))
        {
            Arguments = new object[] { "configuracoes" };
        }
    }

    public class RequireUsersAccessAttribute : TypeFilterAttribute
    {
        public RequireUsersAccessAttribute() : base(typeof(AccessControlFilter))
        {
            Arguments = new object[] { "usuarios" };
        }
    }

    public class RequireProjectsAccessAttribute : TypeFilterAttribute
    {
        public RequireProjectsAccessAttribute() : base(typeof(AccessControlFilter))
        {
            Arguments = new object[] { "projetos" };
        }
    }

    public class RequireReportsAccessAttribute : TypeFilterAttribute
    {
        public RequireReportsAccessAttribute() : base(typeof(AccessControlFilter))
        {
            Arguments = new object[] { "relatorios" };
        }
    }

    public class RequireTotalAccessAttribute : TypeFilterAttribute
    {
        public RequireTotalAccessAttribute() : base(typeof(AccessControlFilter))
        {
            Arguments = new object[] { "total" };
        }
    }
} 