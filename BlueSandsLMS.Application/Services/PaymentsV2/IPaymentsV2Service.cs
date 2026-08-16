using BlueSandsLMS.Common.DTOs.Payment;
using System.Security.Claims;


namespace BlueSandsLMS.Application.Services.PaymentsV2;

public interface IPaymentsV2Service
{
    Task<SubscribeV2Response> SubscribeAsync(SubscribeV2Request req, ClaimsPrincipal user);
    Task<InitPaymentV2Response> InitializePaymentAsync(InitPaymentV2Request req, ClaimsPrincipal user);
    Task<VerifyPaymentV2Response> VerifyPaymentAsync(string reference);
    Task HandleWebhookAsync(string rawBody, string signatureHeader);
    Task<SubscriptionV2Dto?> GetSubscriptionAsync(Guid schoolId);
    Task<CancelSubscriptionV2Response> CancelSubscriptionAsync(Guid schoolId, ClaimsPrincipal user);
}