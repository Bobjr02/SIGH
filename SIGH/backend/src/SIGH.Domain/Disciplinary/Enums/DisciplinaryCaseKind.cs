namespace SIGH.Domain.Disciplinary.Enums;

/// <summary>
/// Process: processo em etapas (rascunho, abertura, investigação...), mantido no código mas fora do menu.
/// Record: medida registrada pelo fluxo simplificado do RH (registrar → aprovar/rejeitar), que executa
/// as mesmas etapas do domínio numa transação.
/// </summary>
public enum DisciplinaryCaseKind
{
    Process = 1,
    Record = 2
}
