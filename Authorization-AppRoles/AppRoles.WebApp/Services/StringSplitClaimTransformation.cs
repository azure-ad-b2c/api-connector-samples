using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;

namespace AppRoles.WebApp.Services
{
    public class StringSplitClaimsTransformation : IClaimsTransformation
    {
        private readonly string claimType;

        public StringSplitClaimsTransformation(string claimType)
        {
            this.claimType = claimType;
        }
        
        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            // Find all claims of the requested claim type, split their values by spaces
            // and then take the ones that aren't yet on the principal individually.
            var claims = principal.FindAll(this.claimType)
                .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .Where(s => !principal.HasClaim(this.claimType, s)).ToList();

            // Add all new claims to the principal's identity.
            ((ClaimsIdentity)principal.Identity).AddClaims(claims.Select(s => new Claim(this.claimType, s)));
            return Task.FromResult(principal);
        }
    }
}