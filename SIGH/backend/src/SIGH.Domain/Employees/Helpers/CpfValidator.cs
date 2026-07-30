using System.Linq;

namespace SIGH.Domain.Employees.Helpers;

public static class CpfValidator
{
    public static string Normalize(string rawCpf)
    {
        if (string.IsNullOrWhiteSpace(rawCpf))
            return string.Empty;

        return new string(rawCpf.Where(char.IsDigit).ToArray());
    }

    public static bool IsValid(string rawCpf)
    {
        var cpf = Normalize(rawCpf);

        if (cpf.Length != 11)
            return false;

        if (cpf.All(c => c == cpf[0]))
            return false;

        int[] multiplier1 = { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
        int[] multiplier2 = { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

        string tempCpf = cpf.Substring(0, 9);
        int sum = 0;

        for (int i = 0; i < 9; i++)
            sum += (tempCpf[i] - '0') * multiplier1[i];

        int remainder = sum % 11;
        int digit1 = remainder < 2 ? 0 : 11 - remainder;

        tempCpf += digit1;
        sum = 0;

        for (int i = 0; i < 10; i++)
            sum += (tempCpf[i] - '0') * multiplier2[i];

        remainder = sum % 11;
        int digit2 = remainder < 2 ? 0 : 11 - remainder;

        return cpf.EndsWith($"{digit1}{digit2}");
    }
}
