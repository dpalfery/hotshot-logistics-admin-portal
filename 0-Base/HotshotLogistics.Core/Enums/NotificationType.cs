namespace HotshotLogistics.Core.Enums;

/// <summary>
/// Represents the type of notification.
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// General information notification.
    /// </summary>
    Information = 0,

    /// <summary>
    /// Job creation notification.
    /// </summary>
    JobCreated = 1,

    /// <summary>
    /// Job assignment notification.
    /// </summary>
    JobAssignment = 2,

    /// <summary>
    /// Job status update notification.
    /// </summary>
    JobStatusUpdate = 3,

    /// <summary>
    /// Job completion notification.
    /// </summary>
    JobCompleted = 4,

    /// <summary>
    /// Job cancellation notification.
    /// </summary>
    JobCancelled = 5,

    /// <summary>
    /// Payment notification.
    /// </summary>
    Payment = 6,

    /// <summary>
    /// Invoice notification.
    /// </summary>
    Invoice = 7,

    /// <summary>
    /// System alert notification.
    /// </summary>
    SystemAlert = 8,

    /// <summary>
    /// Driver location update notification.
    /// </summary>
    LocationUpdate = 9,

    /// <summary>
    /// Emergency notification.
    /// </summary>
    Emergency = 10,

    /// <summary>
    /// Invoice generated notification.
    /// </summary>
    InvoiceGenerated = 11,

    /// <summary>
    /// Payment received notification.
    /// </summary>
    PaymentReceived = 12,

    /// <summary>
    /// Route deviation notification.
    /// </summary>
    RouteDeviation = 13
}
