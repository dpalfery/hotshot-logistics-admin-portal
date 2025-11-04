

namespace HotshotLogistics.Domain.ValueObjects;

/// <summary>
/// Represents a contact person for a customer.
/// </summary>
public class Contact 
{
    /// <summary>
    /// Gets or sets the contact's name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the contact's title/position.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the contact's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the contact's phone number.
    /// </summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this is the primary contact.
    /// </summary>
    public bool IsPrimary { get; set; }
}
