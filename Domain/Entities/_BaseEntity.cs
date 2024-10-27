namespace Domain.Entities;

public abstract class _BaseEntity
{
    protected Guid _guid { get; set; }
    protected DateTime _createdAt { get; set; } = DateTime.Now;

    public Guid Id
    {
        get => _guid;
        set => _guid = value == Guid.Empty ? Guid.NewGuid() : value;
    }

    public DateTime CreatedAt
    {
        get => _createdAt;
        set => _createdAt = value;
    }
}