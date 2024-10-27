using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs;

public record OpenRequestChangeRoleDto(
    [Required] string Id,
    [Required] DateTime CreatedAt,
    [Required] string RequesterId,
    [Required] string Reason,
    [Required] string Role
);