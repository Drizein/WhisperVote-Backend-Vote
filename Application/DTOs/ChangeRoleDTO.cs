using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs;

public record ChangeRoleDTO(
    [Required]
    string userId,
    
    [Required]
    Role role
    );