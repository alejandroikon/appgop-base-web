using GOP.Domain.Common;

namespace GOP.Domain.Errors;

public static partial class DomainErrors
{
    public static class Contrato
    {
        public static readonly Error NotFound = new(
            "Contrato.NotFound",
            "El contrato no fue encontrado.");
    }
}
