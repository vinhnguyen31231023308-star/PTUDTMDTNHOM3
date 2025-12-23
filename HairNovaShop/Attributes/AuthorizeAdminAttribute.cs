using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using HairNovaShop.Models;

namespace HairNovaShop.Attributes;

public class AuthorizeAdminAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var role = context.HttpContext.Session.GetString("Role");
        
        if (string.IsNullOrEmpty(role) || role != Role.Admin.ToString())
        {
            // User không phải admin, redirect về trang chủ hoặc trả về 403
            context.Result = new RedirectToActionResult("Index", "Home", null)
            {
                Permanent = false
            };
        }
    }
}
