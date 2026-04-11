using System;

namespace Sufficit.Gateway.FluxTelecom.SMS
{
    /// <summary>
    /// Payload used to upload a contact file into the Flux Telecom FTP area.
    /// </summary>
    public class FluxTelecomFtpUploadRequest
    {
        /// <summary>
        /// Integration identifier expected by the FTP upload form.
        /// </summary>
        public int IntegrationId { get; set; }

        /// <summary>
        /// Original file name sent in the multipart upload.
        /// </summary>
        public string FileName { get; set; } = default!;

        /// <summary>
        /// Binary file content uploaded to the portal.
        /// </summary>
        public byte[] Content { get; set; } = default!;

        /// <summary>
        /// MIME type sent with the uploaded file part.
        /// </summary>
        public string ContentType { get; set; } = "application/octet-stream";

        /// <summary>
        /// Validates the request before it is posted to the portal.
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(FileName))
                throw new ArgumentException("FileName is required.", nameof(FileName));

            if (Content == null || Content.Length == 0)
                throw new ArgumentException("Content is required.", nameof(Content));
        }
    }
}
