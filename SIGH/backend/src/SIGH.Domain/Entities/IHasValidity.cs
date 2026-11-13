using SIGH.Domain.Exceptions;

namespace SIGH.Domain.Entities;

/// <summary>Vínculo com vigência opcional: [ValidFrom, ValidUntil).</summary>
public interface IHasValidity
{
    DateTimeOffset? ValidFrom { get; }
    DateTimeOffset? ValidUntil { get; }
}

public static class ValidityExtensions
{
    public static bool IsValidAt(this IHasValidity item, DateTimeOffset now) =>
        (item.ValidFrom is null || item.ValidFrom <= now) && (item.ValidUntil is null || now < item.ValidUntil);

    public static bool IsExpiredAt(this IHasValidity item, DateTimeOffset now) =>
        item.ValidUntil is not null && item.ValidUntil <= now;

    /// <summary>Próximo instante, depois de <paramref name="now"/>, em que a vigência muda (começa ou termina).</summary>
    public static DateTimeOffset? NextChangeAfter(this IHasValidity item, DateTimeOffset now)
    {
        DateTimeOffset? next = null;
        if (item.ValidFrom is { } from && from > now)
            next = from;
        if (item.ValidUntil is { } until && until > now && (next is null || until < next))
            next = until;
        return next;
    }

    public static void EnsureValidityRange(DateTimeOffset? validFrom, DateTimeOffset? validUntil)
    {
        if (validFrom is { } from && validUntil is { } until && until <= from)
            throw new BusinessRuleValidationException("O fim da validade deve ser posterior ao início.");
    }
}
