using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public record GetReportedSurveyDto(
    [Required] SurveyDto Survey,
    [Required] string Reason,
    [Required] string ReportSurveyId
);