using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public record CreateSurveyDto(
    [Required] string Title,
    [Required] string Description,
    [Required] [Length(2, int.MaxValue-2)] List<string> Options,
    [Required] DateTime Runtime,
    [Required] List<string> Tags,
    string Information
);