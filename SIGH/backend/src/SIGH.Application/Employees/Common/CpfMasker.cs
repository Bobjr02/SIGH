using SIGH.Domain.Employees.Helpers;

namespace SIGH.Application.Employees.Common;

public static class CpfMasker
{
    public static string Mask(string? rawCpf)
    {
        if (string.IsNullOrWhiteSpace(rawCpf))
            return string.Empty;

        var normalized = CpfValidator.Normalize(rawCpf);
        if (normalized.Length != 11)
            return "***.***.***-**";

        // Mostra só os 6 dígitos do meio: ***.982.247-**
        return $"***.{normalized.Substring(3, 3)}.{normalized.Substring(6, 3)}-**";
    }
}
