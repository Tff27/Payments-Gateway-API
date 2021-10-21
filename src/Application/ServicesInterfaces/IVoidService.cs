using Application.Dto;
using System.Threading.Tasks;

namespace Application.ServicesInterfaces
{
    public interface IVoidService
    {
        public Task<ResponseDto> CancelAsync(VoidDto voidData);
    }
}
