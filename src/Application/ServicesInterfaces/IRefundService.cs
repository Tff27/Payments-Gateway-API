using Application.Dto;
using System.Threading.Tasks;

namespace Application.ServicesInterfaces
{
    public interface IRefundService
    {
        public Task<ResponseDto> RefundAsync(RefundDto captureData);
    }
}
