namespace GOP.Application.Features.Wells.Queries.PreviewUwi;

public sealed record UwiPreviewDto(
    string Uwi,
    bool IsUnique,
    UwiComponentsDto Components
);

public sealed record UwiComponentsDto(
    string DptoCode,
    string MpioCode,
    string Sigla,
    string Numero,
    string ClusterCode,
    string AnguloCode,
    string TrayectoriaCode,
    string ObjetivoCode,
    string TerminacionCode
);
