using FluentAssertions;
using SIGH.Domain.Employees.Helpers;
using Xunit;

namespace SIGH.UnitTests.Employees;

public class CpfValidationTests
{
    [Theory]
    [InlineData("111.444.777-35")]
    [InlineData("11144477735")]
    [InlineData("529.982.247-25")]
    [InlineData("52998224725")]
    public void IsValid_WithValidCpf_ShouldReturnTrue(string cpf)
    {
        var result = CpfValidator.IsValid(cpf);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("000.000.000-00")]
    [InlineData("111.111.111-11")]
    [InlineData("222.222.222-22")]
    [InlineData("333.333.333-33")]
    [InlineData("444.444.444-44")]
    [InlineData("555.555.555-55")]
    [InlineData("666.666.666-66")]
    [InlineData("777.777.777-77")]
    [InlineData("888.888.888-88")]
    [InlineData("999.999.999-99")]
    public void IsValid_WithAllDigitsEqual_ShouldReturnFalse(string cpf)
    {
        var result = CpfValidator.IsValid(cpf);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("111.444.777-00")]
    [InlineData("12345678901")]
    [InlineData("99988877766")]
    public void IsValid_WithInvalidCheckDigits_ShouldReturnFalse(string cpf)
    {
        var result = CpfValidator.IsValid(cpf);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345")]
    [InlineData("123456789012")]
    public void IsValid_WithInvalidLengthOrEmpty_ShouldReturnFalse(string cpf)
    {
        var result = CpfValidator.IsValid(cpf);

        result.Should().BeFalse();
    }

    [Fact]
    public void Normalize_ShouldStripMaskCharactersAndSpaces()
    {
        var rawCpf = " 111.444.777-35 ";

        var normalized = CpfValidator.Normalize(rawCpf);

        normalized.Should().Be("11144477735");
    }
}
