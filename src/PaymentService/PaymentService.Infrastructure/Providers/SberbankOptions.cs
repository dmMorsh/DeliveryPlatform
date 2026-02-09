namespace PaymentService.Infrastructure.Providers;

public sealed class SberbankOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string RegisterPath { get; set; } = "/register.do";
    public string RegisterPreAuthPath { get; set; } = "/registerPreAuth.do";
    public string StatusPath { get; set; } = "/getOrderStatusExtended.do";
    public string DepositPath { get; set; } = "/deposit.do";
    public string ReversePath { get; set; } = "/reverse.do";
    public string RefundPath { get; set; } = "/refund.do";
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
    public string FailUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
}
