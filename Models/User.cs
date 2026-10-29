using System;
using System.Collections.Generic;

namespace KeepSafe.Models;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<Warranty> Warranties { get; set; } = new List<Warranty>();
}
