using System.Runtime.InteropServices;

namespace Application.DTOs;

public record OptionsDto(
    string Value,
    int Count,
    string OptionId
);