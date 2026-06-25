using ScholarRescue.Models.Enums;

namespace ScholarRescue.Models.Enums
{
    /// <summary>
    /// Helper extensions for CitationFormat enum to provide display names.
    /// </summary>
    public static class CitationFormatExtensions
    {
        /// <summary>
        /// Returns a human-friendly display name for the citation format.
        /// </summary>
        public static string ToDisplayName(this CitationFormat format)
        {
            return format switch
            {
                CitationFormat.APA_7th => "APA 7th Edition",
                CitationFormat.APA_6th => "APA 6th Edition",
                CitationFormat.MLA => "MLA",
                CitationFormat.Harvard => "Harvard",
                CitationFormat.Chicago => "Chicago",
                CitationFormat.Turabian => "Turabian",
                CitationFormat.IEEE => "IEEE",
                CitationFormat.Vancouver => "Vancouver",
                CitationFormat.OSCOLA => "OSCOLA",
                CitationFormat.AMA => "AMA",
                CitationFormat.ACS => "ACS",
                CitationFormat.Other => "Other",
                _ => "Unknown"
            };
        }
    }
}
