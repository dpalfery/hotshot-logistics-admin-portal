namespace HotshotLogistics.Core.Enums
{
    /// <summary>
    /// Represents the status of a job.
    /// </summary>
    public enum JobStatus
    {
        /// <summary>
        /// Job has been created but not yet assigned to a driver.
        /// </summary>
        Pending,

        /// <summary>
        /// Job has been assigned to a driver.
        /// </summary>
        Assigned,

        /// <summary>
        /// Driver is en route to pickup/delivery location.
        /// </summary>
        EnRoute,

        /// <summary>
        /// Job has been completed and cargo received by customer.
        /// </summary>
        Received
    }
}
