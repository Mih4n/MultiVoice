namespace MultiVoice.Server.Auth;

public record PendingLogin(string ConnectionId, int Code, DateTime Created);
