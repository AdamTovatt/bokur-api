﻿using BokurApi.Models;
using Microsoft.AspNetCore.Authorization;

namespace BokurApi.Helpers
{
    public class HasScopeHandler : AuthorizationHandler<HasScopeRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, HasScopeRequirement requirement)
        {
            // If user does not have the scope claim, get out of here
            if (!context.User.HasClaim(c => c.Type == "permissions" && c.Issuer == requirement.Issuer))
                return Task.CompletedTask;

            string[] permissions = context.User.FindFirst(c => c.Type == "permissions" && c.Issuer == requirement.Issuer)!.Value.Split(' ');

            // Succeed if the scope array contains the required scope
            if (permissions.Any(s => s == requirement.Scope))
                context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }
}
