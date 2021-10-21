using Application.Dto;
using System.Threading.Tasks;

namespace Application.ServicesInterfaces
{
    public interface IAuthorizationService
    {
        public Task<AuthorizationResponseDto> AuthorizeAsync(AuthorizationDto authorization);
    }
}
