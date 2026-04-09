
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using VTOS.Application.Abstractions;

namespace VTOS.Infrastructure.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        public Guid UserId { get; }

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            var user = httpContextAccessor.HttpContext?.User;
            var userIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
            {
                UserId = userId;
            }
            // For anonymous/guest users, UserId remains Guid.Empty
        }
    }

}
