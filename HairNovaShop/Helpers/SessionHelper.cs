using Microsoft.AspNetCore.Http;
using HairNovaShop.Models;

namespace HairNovaShop.Helpers;

public static class SessionHelper
{
    public static bool IsAdmin(this ISession session)
    {
        var role = session.GetString("Role");
        return !string.IsNullOrEmpty(role) && role == Role.Admin.ToString();
    }

    public static bool IsLoggedIn(this ISession session)
    {
        return !string.IsNullOrEmpty(session.GetString("UserId"));
    }

    public static int? GetUserId(this ISession session)
    {
        var userIdStr = session.GetString("UserId");
        if (int.TryParse(userIdStr, out int userId))
        {
            return userId;
        }
        return null;
    }

    public static string? GetUsername(this ISession session)
    {
        return session.GetString("Username");
    }

    public static string? GetFullName(this ISession session)
    {
        return session.GetString("FullName");
    }

    public static Role GetRole(this ISession session)
    {
        var roleStr = session.GetString("Role");
        if (Enum.TryParse<Role>(roleStr, out Role role))
        {
            return role;
        }
        return Role.User;
    }
}
