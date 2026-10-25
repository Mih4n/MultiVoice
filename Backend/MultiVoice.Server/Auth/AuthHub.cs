using Microsoft.AspNetCore.SignalR;

namespace MultiVoice.Server.Auth;

public class AuthHub(LoginService login) : Hub
{
    public Task<int> RequestCode(string name) => login.RequestCode(name, Context.ConnectionId);

    public string? Resume(string token) => login.Resolve(token)?.Name;
}
