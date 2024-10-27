using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public record CloseReportDto(
    [Required] string ReportSurveyId,
    [Required] string Resolution,
    [Required] bool StrikeUser,
    [Required] bool StrikeSurveyCreator
);