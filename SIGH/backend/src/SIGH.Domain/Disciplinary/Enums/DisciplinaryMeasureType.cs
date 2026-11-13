namespace SIGH.Domain.Disciplinary.Enums;

public enum DisciplinaryMeasureType
{
    Undefined = 0,
    Guidance = 1,
    VerbalWarning = 2,
    WrittenWarning = 3,
    Suspension = 4,
    TerminationRecommendation = 5,
    Other = 6,
    /// <summary>Demissão por justa causa. Não altera o status do funcionário: o SAP é a fonte da verdade.</summary>
    JustCauseTermination = 7
}
