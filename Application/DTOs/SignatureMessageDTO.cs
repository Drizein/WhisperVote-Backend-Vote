using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public record SignatureMessageDto(
    [Required] string Message,
    [Required] string SurveyId
);