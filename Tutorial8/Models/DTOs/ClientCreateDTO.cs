using System.ComponentModel.DataAnnotations;

namespace Tutorial8.Models.DTOs;

public class ClientCreateDto
{
    [Required, StringLength(120)] public string FirstName { get; set; } = null!;

    [Required, StringLength(120)] public string LastName { get; set; } = null!;

    [Required, EmailAddress, StringLength(120)]
    public string Email { get; set; } = null!;

    [Required, Phone, StringLength(120)] public string Telephone { get; set; } = null!;

    [Required, StringLength(120, MinimumLength = 5)]
    public string Pesel { get; set; } = null!;
}