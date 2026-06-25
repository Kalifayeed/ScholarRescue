namespace ScholarRescue.Services.Payments
{
    /// <summary>
    /// Paystack payment gateway service interface.
    /// Handles transaction initialization, verification, and webhooks.
    /// </summary>
    public interface IPaystackPaymentService
    {
        /// <summary>Initializes a Paystack checkout transaction for an order.</summary>
        Task<(bool Success, string AuthorizationUrl, string AccessCode, string Reference)> InitializeTransactionAsync(int orderId, string userId, string userEmail);

        /// <summary>Verifies a Paystack transaction reference.</summary>
        Task<(bool Success, decimal Amount, string Status, string GatewayResponse)> VerifyTransactionAsync(string reference);

        /// <summary>Processes an incoming Paystack webhook event.</summary>
        Task ProcessWebhookAsync(string payload, string signature);

        /// <summary>Gets the Paystack public key for the frontend.</summary>
        string GetPublicKey();

        /// <summary>Tests the Paystack API connection.</summary>
        Task<(bool Success, string Message)> TestConnectionAsync();
    }
}