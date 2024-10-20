using System.Security.Claims;
using MediasoupSharp.Demo.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediasoupSharp.Demo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TokenController(ITokenService tokenService) : ControllerBase
    {
        [HttpPost("createToken")]
        public string CreateToken(string userIdOrUsername)
        {
            var token = tokenService.GenerateAccessToken([new Claim(ClaimTypes.Name, userIdOrUsername)]);
            return token;
        }
    }
}
