// ============================================================================
// Author: Daniil Vdovin
// Project: SSRFGuard
// Description: Custom exception for SSRF validation failures
// License: MIT License
// Copyright (c) 2026 Daniil Vdovin
// ============================================================================

namespace SSRFGuard.Exceptions;

/// <summary>
/// Represents an exception that is thrown when a URL fails SSRF (Server-Side Request Forgery) validation.
/// This exception contains information about the blocked URL and the reason for blocking.
/// </summary>
public class SsrfValidationException : Exception
{
    /// <summary>
    /// Gets the URL that was blocked by SSRF validation.
    /// </summary>
    public string BlockedUrl { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SsrfValidationException"/> class
    /// with a specified blocked URL and reason.
    /// </summary>
    /// <param name="url">The URL that failed validation.</param>
    /// <param name="reason">The reason why the URL was blocked.</param>
    public SsrfValidationException(string url, string reason)
        : base($"SSRF validation failed: {reason}")
    {
        BlockedUrl = url;
    }
}
