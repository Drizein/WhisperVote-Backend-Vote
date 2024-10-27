using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public record ReportSurveyDto(
    [Required] string SurveyId,
    [Required] string Reason
);