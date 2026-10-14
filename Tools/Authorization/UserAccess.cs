using System.Security.Claims;
using FishyAPI.Tools.DBInteractions;
using Microsoft.AspNetCore.Authorization;
using MySql.Data.MySqlClient;

namespace FishyAPI.Tools.Authorization;

/// <summary>The caller's roles and permissions, resolved from the database once per request.</summary>
public sealed class UserAccess
{
    public static readonly UserAccess Anonymous = new(null, Array.Empty<string>(), new HashSet<Permission>());

    public ulong? UserID { get; }
    public IReadOnlyList<string> Roles { get; }
    public IReadOnlySet<Permission> Permissions { get; }

    public UserAccess(ulong? userId, IReadOnlyList<string> roles, IReadOnlySet<Permission> permissions)
    {
        UserID = userId;
        Roles = roles;
        Permissions = permissions;
    }

    public bool Has(Permission permission) => Permissions.Contains(permission);
}

public static class AccessControl
{
    private const string ItemKey = "FishyAPI.UserAccess";
    private const string PolicyPrefix = "perm:";

    public static string PolicyName(Permission permission) => PolicyPrefix + permission;

    // ── Request side ─────────────────────────────────────────────────────────

    public static UserAccess GetAccess(this HttpContext context)
        => context.Items.TryGetValue(ItemKey, out object? value) && value is UserAccess access ? access : UserAccess.Anonymous;

    public static bool HasPermission(this HttpContext context, Permission permission) => context.GetAccess().Has(permission);

    /// <summary>
    /// Middleware: looks up the caller's current roles in user_roles, so role changes apply on the next request.
    /// API tokens are limited to the grants of the role they were issued for (plus the 'user' baseline)
    /// AND what their owner holds now.
    /// </summary>
    public static IApplicationBuilder UseUserAccess(this IApplicationBuilder app, ConnectionManager connManager)
    {
        return app.Use(async (context, next) =>
        {
            if (context.User.Identity?.IsAuthenticated == true &&
                ulong.TryParse(context.User.FindFirstValue("uid"), out ulong uid))
            {
                List<string> roles;
                using (MySqlConnection conn = connManager.IssueConnection())
                    roles = RoleTools.GetRoleKeysForUser(conn, uid);

                HashSet<Permission> permissions = AccessPolicy.PermissionsFor(roles);
                if (context.User.FindFirstValue("token_type") == "api")
                {
                    // A token acts as the role it was issued for, plus the member baseline every account holds.
                    HashSet<Permission> scope = AccessPolicy.PermissionsFor([context.User.FindFirstValue(ClaimTypes.Role) ?? "", "user"]);
                    permissions.IntersectWith(scope);
                }

                context.Items[ItemKey] = new UserAccess(uid, roles, permissions);
            }
            await next();
        });
    }

    // ── Endpoint side ────────────────────────────────────────────────────────

    /// <summary>Requires a signed-in caller holding <paramref name="permission"/> (401 if anonymous, 403 if missing).</summary>
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, Permission permission)
        where TBuilder : IEndpointConventionBuilder
        => builder.RequireAuthorization(PolicyName(permission));

    /// <summary>Registers one authorization policy per Permission.</summary>
    public static void AddPermissionPolicies(this AuthorizationOptions options)
    {
        foreach (Permission permission in Enum.GetValues<Permission>())
        {
            options.AddPolicy(PolicyName(permission), policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permission)));
        }
    }

    /// <summary>
    /// Startup check: every write endpoint must declare a permission (or explicitly .AllowAnonymous()),
    /// and nothing may use a policy other than a permission. Throws listing the offenders.
    /// </summary>
    public static void ValidateEndpointSecurity(IEndpointRouteBuilder app)
    {
        string[] writeMethods = ["POST", "PUT", "PATCH", "DELETE"];
        List<string> problems = new();

        foreach (RouteEndpoint endpoint in app.DataSources.SelectMany(ds => ds.Endpoints).OfType<RouteEndpoint>())
        {
            string route = endpoint.RoutePattern.RawText ?? endpoint.DisplayName ?? "?";
            IReadOnlyList<string> methods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? Array.Empty<string>();
            List<IAuthorizeData> authorize = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().ToList();
            bool allowAnonymous = endpoint.Metadata.GetMetadata<IAllowAnonymous>() != null;

            foreach (IAuthorizeData data in authorize)
            {
                if (data.Policy != null && !data.Policy.StartsWith(PolicyPrefix))
                    problems.Add($"{string.Join(",", methods)} {route}: uses policy '{data.Policy}' instead of RequirePermission");
                if (!string.IsNullOrEmpty(data.Roles))
                    problems.Add($"{string.Join(",", methods)} {route}: uses Roles='{data.Roles}' instead of RequirePermission");
            }

            if (methods.Any(m => writeMethods.Contains(m)) && authorize.Count == 0 && !allowAnonymous)
                problems.Add($"{string.Join(",", methods)} {route}: write endpoint with no RequirePermission (add one, or .AllowAnonymous() if it is truly public)");
        }

        if (problems.Count > 0)
            throw new InvalidOperationException("Endpoint security check failed:\n  " + string.Join("\n  ", problems));
    }
}

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public Permission Permission { get; }
    public PermissionRequirement(Permission permission) => Permission = permission;
}

public sealed class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.Resource is HttpContext http && http.HasPermission(requirement.Permission))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
