namespace BusinessAccessLayer.Constants;

public static class OrderStatusConstants
{
    public const string WaitingConfirmation = "waiting-confirmation";
    public const string Confirmed = "confirmed";
    public const string PendingPayment = "pending-payment";
    public const string Paid = "paid";
    public const string PartiallyPaid = "partially-paid";
    public const string Cancelled = "cancelled";
}

