using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Prometej_core.Auth;

namespace Prometej_api.Auth
{
    public sealed class TokenService(IOptions<JwtOptions> options)
    {
        private readonly JwtOptions _options = options.Value;

        public (string Token, DateTimeOffset Expires) Create(UserSession session)
        {
            var expires = DateTimeOffset.UtcNow.AddMinutes(_options.ExpiryMinutes);
            var key = new SymmetricSecurityKey(Convert.FromBase64String(_options.Key));

            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = _options.Issuer,
                Audience = _options.Audience,
                Expires = expires.UtcDateTime,
                Claims = new Dictionary<string, object>
                {
                    ["sub"] = session.UserId.ToString(),
                    ["email"] = session.Email,
                    ["stamp"] = session.Stamp.ToString(),
                },
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
            };

            return (new JsonWebTokenHandler().CreateToken(descriptor), expires);
        }
    }
}
