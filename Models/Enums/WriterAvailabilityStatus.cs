namespace ScholarRescue.Models.Enums
{
    /// <summary>
    /// Writer availability for new assignments based on workload capacity.
    /// </summary>
    public enum WriterAvailabilityStatus
    {
        /// <summary>0–60% workload, open for new orders.</summary>
        Available = 0,

        /// <summary>61–80% workload, may accept selective orders.</summary>
        Busy = 1,

        /// <summary>81–100% workload, cannot accept new orders without override.</summary>
        Full = 2,

        /// <summary>Administratively suspended from taking orders.</summary>
        Suspended = 3,

        /// <summary>Writer has manually set themselves as offline.</summary>
        Offline = 4
    }
}