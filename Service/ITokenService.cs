using FoodiGOAPI.Models;

namespace FoodiGOAPI.Services
{
    public interface ITokenService
    {
        string CreateToken(User user);
    }
}