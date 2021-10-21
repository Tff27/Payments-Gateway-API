using Application.Dto;
using Application.ServicesInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Presentation.API.ViewModels;
using System.Threading.Tasks;
using IAuthorizationService = Application.ServicesInterfaces.IAuthorizationService;

namespace Presentation.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class PaymentsController : ControllerBase
    {
        private readonly IAuthorizationService authorizationService;
        private readonly IVoidService voidService;
        private readonly ICaptureService captureService;
        private readonly IRefundService refundService;

        public PaymentsController(IAuthorizationService authorizationService, IVoidService voidService, ICaptureService captureService, IRefundService refundService)
        {
            this.authorizationService = authorizationService;
            this.voidService = voidService;
            this.captureService = captureService;
            this.refundService = refundService;
        }

        [HttpPost("/authorize")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(AuthorizationResponseDto))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ErrorViewModel))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorViewModel))]
        public async Task<ActionResult> AuthorizeAsync([FromBody] AuthorizationDto authorizationData)
        {
            if (authorizationData is null)
            {
                return this.BadRequest("Missing required authorization information.");
            }

            var result = await this.authorizationService.AuthorizeAsync(authorizationData);

            return this.Ok(result);
        }

        [HttpPost("/void")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(ResponseDto))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ErrorViewModel))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorViewModel))]
        public async Task<ActionResult> VoidAsync([FromBody] VoidDto voidData)
        {
            if (voidData is null)
            {
                return this.BadRequest("Missing required information.");
            }

            var result = await this.voidService.CancelAsync(voidData);

            return this.Ok(result);
        }

        [HttpPost("/capture")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(ResponseDto))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ErrorViewModel))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorViewModel))]
        public async Task<ActionResult> CaptureAsync([FromBody] CaptureDto captureData)
        {
            if (captureData is null)
            {
                return this.BadRequest("Missing required information.");
            }

            var result = await this.captureService.CaptureAsync(captureData);

            return this.Ok(result);
        }

        [HttpPost("/refund")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(ResponseDto))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ErrorViewModel))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorViewModel))]
        public async Task<ActionResult> CaptureAsync([FromBody] RefundDto refundData)
        {
            if (refundData is null)
            {
                return this.BadRequest("Missing required information.");
            }

            var result = await this.refundService.RefundAsync(refundData);

            return this.Ok(result);
        }
    }
}
