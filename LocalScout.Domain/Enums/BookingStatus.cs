namespace LocalScout.Domain.Enums
{
    public enum BookingStatus
    {
        PendingProviderReview,  // Initial state - provider can see user contact info
        AcceptedByProvider,     // Provider entered negotiated price - user can now see provider contact info
        AwaitingPayment,        // User must pay
        PaymentReceived,        // Payment completed
        InProgress,             // Work is ongoing
        JobDone,                // Provider marked job as done
        Completed,              // Both parties confirmed completion
        Cancelled,              // Booking cancelled by either party
        Disputed,               // Issue raised by either party
        NeedRescheduling,       // Overlapping requests moved here after another booking accepted
        AutoCancelled           // System auto-cancelled due to provider inaction (3 hours timeout)
    }
}
