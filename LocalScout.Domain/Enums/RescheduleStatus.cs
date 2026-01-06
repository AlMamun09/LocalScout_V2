namespace LocalScout.Domain.Enums
{
    public enum RescheduleStatus
    {
        Pending,    // Proposal awaiting response
        Accepted,   // Proposal was accepted
        Rejected,   // Proposal was rejected
        Expired     // Proposal expired without response
    }
}
