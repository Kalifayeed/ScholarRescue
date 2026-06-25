using ScholarRescue.Models;

namespace ScholarRescue.Services
{
    public interface IWriterRatingService
    {
        /// <summary>Submit a rating for a completed order. One rating per order.</summary>
        Task<WriterRating> SubmitRatingAsync(int orderId, string clientId, int overallRating,
            int qualityRating, int communicationRating, int deadlineRating,
            string? reviewText, bool wouldHireAgain);

        /// <summary>Check if an order has already been rated.</summary>
        Task<bool> HasRatingAsync(int orderId);

        /// <summary>Get writer's average rating and stats.</summary>
        Task<(double AvgRating, int TotalReviews, double RepeatHireRate)> GetWriterStatsAsync(string writerId);

        /// <summary>Get all ratings for a writer.</summary>
        Task<List<WriterRating>> GetWriterRatingsAsync(string writerId, int page = 1, int pageSize = 20);

        /// <summary>Check if writer qualifies as featured.</summary>
        Task<bool> IsFeaturedWriterAsync(string writerId);

        /// <summary>Get top rated writers.</summary>
        Task<List<(string WriterId, string Name, double AvgRating, int Reviews)>> GetTopRatedWritersAsync(int count = 10);

        /// <summary>Get all ratings for admin.</summary>
        Task<(List<WriterRating> Ratings, int Total)> GetAllRatingsAsync(int page = 1, int pageSize = 50,
            string? writerSearch = null, int? minRating = null, DateTime? dateFrom = null, DateTime? dateTo = null);
    }
}