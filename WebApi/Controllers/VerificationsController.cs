using Microsoft.AspNetCore.Mvc;
using WebApi.Models;
using WebApi.Services;

namespace WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class VerificationsController(IVerificationService verificationService) : ControllerBase
{
    private readonly IVerificationService _verificationService = verificationService;

    [HttpPost("send")]
    public async Task<IActionResult> Send(SendVerificationCodeRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { Error = "Recipient email is required."});

        var result = await _verificationService.SendVerificationCodeAsync(request);
        return result.Success ? Ok(result) : StatusCode(500, result.Error);
    }

    [HttpPost("verify")]
    public IActionResult Verify(VerifyVerificationCodeRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { Error = "Invalid or expired code." });

        var result = _verificationService.VerifyVerificationCode(request);
        return result.Success ? Ok(result) : StatusCode(500, result.Error);
    }
}
