namespace Application.DTOs;

public record GetSurveyDto(
    string PublicKey,
    SurveyDto Survey
);