using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public record AddNewTagsDto(
    [Required] string SurveyId,
    [Required] HashSet<string> Tags
);