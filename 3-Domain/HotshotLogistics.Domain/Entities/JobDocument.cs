// <copyright file="JobDocument.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Domain.Entities
{
    using System;

    /// <summary>
    /// Represents a document associated with a job.
    /// </summary>
    public class JobDocument
    {
        /// <summary>
        /// Gets or sets the unique identifier for the document.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the job identifier this document belongs to.
        /// </summary>
        public string JobId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of document.
        /// </summary>
        public JobDocumentType DocumentType { get; set; }

        /// <summary>
        /// Gets or sets the original filename.
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the file size in bytes.
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// Gets or sets the MIME type of the file.
        /// </summary>
        public string MimeType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the storage URL or path where the document is stored.
        /// </summary>
        public string StorageUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the document.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp when the document was uploaded.
        /// </summary>
        public DateTime UploadedAt { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the user who uploaded the document.
        /// </summary>
        public string UploadedBy { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the document is required for the job.
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the document has been verified.
        /// </summary>
        public bool IsVerified { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the document was verified.
        /// </summary>
        public DateTime? VerifiedAt { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the user who verified the document.
        /// </summary>
        public string VerifiedBy { get; set; } = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="JobDocument"/> class.
        /// </summary>
        public JobDocument()
        {
            UploadedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Marks the document as verified.
        /// </summary>
        /// <param name="verifiedBy">The identifier of the user verifying the document.</param>
        public void MarkAsVerified(string verifiedBy)
        {
            IsVerified = true;
            VerifiedAt = DateTime.UtcNow;
            VerifiedBy = verifiedBy;
        }

        /// <summary>
        /// Validates the document data.
        /// </summary>
        /// <returns>True if the document is valid, false otherwise.</returns>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Id) &&
                   !string.IsNullOrWhiteSpace(JobId) &&
                   !string.IsNullOrWhiteSpace(FileName) &&
                   !string.IsNullOrWhiteSpace(StorageUrl) &&
                   FileSize > 0 &&
                   UploadedAt != default;
        }
    }

    /// <summary>
    /// Enumeration of job document types.
    /// </summary>
    public enum JobDocumentType
    {
        /// <summary>
        /// Proof of delivery document.
        /// </summary>
        ProofOfDelivery = 1,

        /// <summary>
        /// Bill of lading document.
        /// </summary>
        BillOfLading = 2,

        /// <summary>
        /// Invoice document.
        /// </summary>
        Invoice = 3,

        /// <summary>
        /// Receipt document.
        /// </summary>
        Receipt = 4,

        /// <summary>
        /// Photo documentation.
        /// </summary>
        Photo = 5,

        /// <summary>
        /// Signature document.
        /// </summary>
        Signature = 6,

        /// <summary>
        /// Inspection report.
        /// </summary>
        InspectionReport = 7,

        /// <summary>
        /// Other document type.
        /// </summary>
        Other = 99
    }
}
