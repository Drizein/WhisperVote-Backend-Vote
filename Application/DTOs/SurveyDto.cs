using System.ComponentModel.DataAnnotations;
using Domain.Entities;

namespace Application.DTOs;

public record SurveyDto(
    [Required] string Title,
    [Required] string Description,
    [Required] List<Tag> Tags,
    [Required] string Information,
    List<OptionsDto> Options,
    int TotalVotes,
    DateTime? Runtime,
    string SurveyId
);