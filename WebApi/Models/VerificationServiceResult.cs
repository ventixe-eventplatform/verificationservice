namespace WebApi.Models;

public class VerificationServiceResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? Message { get; set; }
}
