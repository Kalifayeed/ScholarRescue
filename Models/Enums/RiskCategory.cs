namespace ScholarRescue.Models.Enums
{
    /// <summary>
    /// Categories of risk detected by the AI Risk Engine.
    /// </summary>
    public enum RiskCategory
    {
        Fraud = 0,
        CommissionAvoidance = 1,
        ClientRisk = 2,
        WriterRisk = 3,
        DisputeRisk = 4,
        DeadlineRisk = 5,
        PaymentRisk = 6,
        CommunicationRisk = 7,
        PolicyViolation = 8,
        OperationalRisk = 9
    }
}