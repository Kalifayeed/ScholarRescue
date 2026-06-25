namespace ScholarRescue.Models.Enums
{
    /// <summary>
    /// Writer performance rank, auto-promoted based on completed orders.
    /// </summary>
    public enum WriterRank
    {
        /// <summary>0-20 completed orders</summary>
        Beginner = 0,
        /// <summary>21-100 completed orders</summary>
        Intermediate = 1,
        /// <summary>101-300 completed orders</summary>
        Advanced = 2,
        /// <summary>301-1000 completed orders</summary>
        Expert = 3,
        /// <summary>1000+ completed orders</summary>
        Elite = 4
    }
}