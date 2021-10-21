using Application.Dto;
using System.Threading.Tasks;

namespace Application.ServicesInterfaces
{
    public interface ICaptureService
    {
        public Task<ResponseDto> CaptureAsync(CaptureDto captureData);
    }
}
