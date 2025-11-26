using LocalScout.Infrastructure.Constants; // For RoleNames
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LocalScout.Web.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var user = Context.User;
            // Automatically add Admins to the "Admins" group
            if (user.IsInRole(RoleNames.Admin))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
            }

            await base.OnConnectedAsync();
        }

        public async Task SendRequestNotification(string message)
        {
            await Clients.Group("Admins").SendAsync("ReceiveRequestNotification", message);
        }

        public async Task SendStatusUpdate(string providerId, string status, string message)
        {
            await Clients.User(providerId).SendAsync("ReceiveStatusUpdate", status, message);
        }
    }
}
