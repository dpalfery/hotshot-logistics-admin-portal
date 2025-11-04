namespace HotshotLogistics.Core.Enums
{
    /// <summary>
    /// Represents the runtime status of a driver.
    /// </summary>
    public enum DriverStatus
    {
        /// <summary>
        /// Driver is available to accept jobs.
        /// </summary>
        Available = 0,

        /// <summary>
        /// Driver is currently delivering (between pickup and delivery).
        /// </summary>
        Delivering = 1,

        /// <summary>
        /// Driver is offline / not available.
        /// </summary>
        Offline = 2,

        /// <summary>
        /// Driver is en route to pickup or delivery.
        /// </summary>
        EnRoute = 3,

        /// <summary>
        /// Driver is on duty but not currently assigned.
        /// </summary>
        OnDuty = 4,

        /// <summary>
        /// Driver is off duty.
        /// </summary>
        OffDuty = 5,

        /// <summary>
        /// Driver is temporarily unavailable (break, etc.).
        /// </summary>
        Unavailable = 6
    }
}
