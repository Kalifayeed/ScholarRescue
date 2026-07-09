using ScholarRescue.Models.Enums;

namespace ScholarRescue.ViewModels.Order
{
    /// <summary>
    /// ViewModel for displaying full details of a single order including pricing breakdown.
    /// </summary>
    public class OrderDetailsViewModel
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public AcademicLevel AcademicLevel { get; set; }
        public string AcademicLevelName { get; set; } = string.Empty;
        public CitationFormat CitationFormat { get; set; }
        public string CitationFormatName { get; set; } = string.Empty;
        public DateTime Deadline { get; set; }
        /// <summary>Number of pages in the client's existing draft (informational).</summary>
        public int? Pages { get; set; }
        /// <summary>Word count of the client's existing draft (informational).</summary>
        public int? WordCount { get; set; }

        /// <summary>Auto-calculated total budget.</summary>
        public decimal Budget { get; set; }

        /// <summary>Base price before surcharges.</summary>
        public decimal BasePrice { get; set; }

        /// <summary>Urgency surcharge total.</summary>
        public decimal UrgencySurcharge { get; set; }

        /// <summary>Base rate per page.</summary>
        public decimal BaseRatePerPage { get; set; }

        /// <summary>Surcharge per page.</summary>
        public decimal SurchargePerPage { get; set; }

        /// <summary>Platform commission (10%).</summary>
        public decimal CommissionAmount { get; set; }

        /// <summary>Writer earnings after commission.</summary>
        public decimal WriterEarnings { get; set; }

        /// <summary>Number of sources requested.</summary>
        public int NumberOfSources { get; set; }

        public PriorityLevel Priority { get; set; }
        public OrderStatus Status { get; set; }
        public OrderPaymentStatus PaymentStatus { get; set; }
        public string? PaystackReference { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string ClientEmail { get; set; } = string.Empty;
        public string? WriterName { get; set; }
        public string? WriterEmail { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Whether payment was deferred (Pay Later). Used to show Pay Now button on Details page.
        /// </summary>
        public bool PaymentDeferred { get; set; }
    }
}
