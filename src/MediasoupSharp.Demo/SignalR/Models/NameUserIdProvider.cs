using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace MediasoupSharp.Demo.SignalR.Models
{
    /// <summary>
    /// NameUserIdProvider
    /// </summary>
    public class NameUserIdProvider : IUserIdProvider
    {
        /// <summary>
        /// GetUserId
        /// </summary>
        public string? GetUserId(HubConnectionContext connection)
        {
            var userId = connection.User?.FindFirst("id")?.Value ??
                connection.User?.FindFirst("name")?.Value ??
                connection.User?.FindFirst(ClaimTypes.Name)?.Value;
            return userId;
        }
    }
}
