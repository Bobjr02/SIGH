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

        return $"***.***.***-{normalized.Substring(9, 2)}";
    }
}
