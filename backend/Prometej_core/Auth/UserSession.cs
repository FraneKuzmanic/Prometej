namespace Prometej_core.Auth
{
    // What a session token is made from, and what the token of each request is checked against.
    public record UserSession(int UserId, string Email, string Role, Guid Stamp);
}
