using Microsoft.AspNetCore.SignalR;

namespace LoggingApi.Hubs;

public class LoggingHub : Hub
{
    public async Task JoinLogGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        await Clients.Group(groupName).SendAsync("UserJoined", $"{Context.ConnectionId} joined {groupName}");
    }

    public async Task LeaveLogGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        await Clients.Group(groupName).SendAsync("UserLeft", $"{Context.ConnectionId} left {groupName}");
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
