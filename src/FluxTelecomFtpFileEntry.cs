namespace Sufficit.Gateway.FluxTelecom.SMS
{
    /// <summary>
    /// Parsed row from the Flux Telecom FTP partial list.
    /// </summary>
    public class FluxTelecomFtpFileEntry
    {
        /// <summary>
        /// Campaign label shown by the portal row.
        /// </summary>
        public string Campaign { get; set; } = string.Empty;

        /// <summary>
        /// Uploaded source file name.
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Inclusion date text exactly as rendered by the portal.
        /// </summary>
        public string IncludedAtText { get; set; } = string.Empty;

        /// <summary>
        /// Total deliveries reported for the uploaded file.
        /// </summary>
        public int? TotalDeliveries { get; set; }

        /// <summary>
        /// First proposal count reported by the portal.
        /// </summary>
        public int? FirstProposalCount { get; set; }

        /// <summary>
        /// Second proposal count reported by the portal.
        /// </summary>
        public int? SecondProposalCount { get; set; }

        /// <summary>
        /// Third proposal count reported by the portal.
        /// </summary>
        public int? ThirdProposalCount { get; set; }

        /// <summary>
        /// Fourth proposal count reported by the portal.
        /// </summary>
        public int? FourthProposalCount { get; set; }

        /// <summary>
        /// Fifth proposal count reported by the portal.
        /// </summary>
        public int? FifthProposalCount { get; set; }

        /// <summary>
        /// Sixth proposal count reported by the portal.
        /// </summary>
        public int? SixthProposalCount { get; set; }
    }
}
