using ECommerce.Api.Security;
using ECommerce.Application.Payments;
using ECommerce.Application.Payments.DTOs;
using ECommerce.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly PaymentService _paymentService;
    private readonly WebhookSignatureVerifier _signatureVerifier;

    public PaymentsController(
        PaymentService paymentService,
        WebhookSignatureVerifier signatureVerifier)
    {
        _paymentService = paymentService;
        _signatureVerifier = signatureVerifier;
    }

    [HttpPost("webhook")]
    public async Task<ActionResult<PaymentResponse>> Webhook(
        [FromHeader(Name = "X-Signature")] string? signature,
        [FromBody] PaymentWebhookRequest request,
        CancellationToken cancellationToken)
    {
        if (!_signatureVerifier.IsValid(request, signature))
        {
            throw new InvalidCredentialsException();
        }

        var payment = await _paymentService.HandleWebhookAsync(request, cancellationToken);
        return Ok(payment);
    }
}