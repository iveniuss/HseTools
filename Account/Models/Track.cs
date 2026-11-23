using System.ComponentModel.DataAnnotations;

namespace Account.Models;

public class Track
{
    [MaxLength(32)]
    public required string Id { get; set; }
    public required string Name { get; set; }
    public ICollection<MainGroup> MainGroups { get; set; } = new List<MainGroup>();
}