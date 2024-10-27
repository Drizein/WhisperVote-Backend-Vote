using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

public class Survey : _BaseEntity
{
    public string Title { get; set; }
    public string Description { get; set; }
    public List<Option> Options { get; set; }
    public DateTime Runtime { get; set; }
    public List<Tag> Tags { get; set; }
    public string Information { get; set; }
    public int TotalVotes { get; set; }
    public Guid CreatorId { get; set; }

    // Parameterless constructor for EF Core
    public Survey()
    {
    }

    public Survey(string title, string description, DateTime runtime,
        string information, Guid creatorId)
    {
        Title = title;
        Description = description;
        Options = [];
        Runtime = runtime;
        Tags = [];
        Information = information;
        CreatorId = creatorId;
    }
}