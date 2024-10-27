using Domain.Enums;

namespace Domain.Entities;

public class RequestChangeRole : _BaseEntity
{
    public Role Role { get; set; }
    public Guid UserId { get; set; }
    public Guid ApprovedBy { get; set; }
    public string Reason { get; set; }
    public bool? IsApproved { get; set; }


    public RequestChangeRole()
    {
    }

    public RequestChangeRole(Role role, Guid userId, string reason)
    {
        Role = role;
        UserId = userId;
        Reason = reason;
        IsApproved = null;
    }
}