using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.DTOs;
using SIGH.Application.Interfaces.Repositories.Disciplinary;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.GetDisciplinaryCaseById;

public class GetDisciplinaryCaseByIdUseCase : IGetDisciplinaryCaseByIdUseCase
{
    private readonly IDisciplinaryCaseRepository _caseRepository;

    public GetDisciplinaryCaseByIdUseCase(IDisciplinaryCaseRepository caseRepository)
    {
        _caseRepository = caseRepository;
    }

    public async Task<Result<GetDisciplinaryCaseByIdResponse>> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _caseRepository.GetByIdWithDetailsAsync(id, cancellationToken);
        if (item == null)
        {
            return Result<GetDisciplinaryCaseByIdResponse>.Failure("Processo disciplinar não encontrado.");
        }

        var occurrences = item.Occurrences.Select(o => new OccurrenceDto(
            o.Id,
            o.OccurrenceDate,
            o.ReportedAt,
            o.Description,
            o.ReportedByUserId,
            o.InfractionTypeId,
            o.Severity,
            o.Location,
            o.Status)).ToList();

        var employees = item.Employees.Select(e => new CaseEmployeeDto(
            e.Id,
            e.EmployeeId,
            e.Role,
            e.IsPrimaryAccused,
            e.Notes)).ToList();

        var evidences = item.Evidences.Select(e => new EvidenceDto(
            e.Id,
            e.EvidenceType,
            e.Title,
            e.Description,
            e.StorageReference,
            e.OriginalFileName,
            e.ContentType,
            e.FileSize,
            e.DisciplinaryOccurrenceId,
            e.CollectedAt,
            e.CollectedByUserId,
            e.IntegrityHash,
            e.Status)).ToList();

        var decisions = item.Decisions.Select(d => new DecisionDto(
            d.Id,
            d.DecisionType,
            d.Summary,
            d.Reasoning,
            d.DecidedByUserId,
            d.DecidedAt,
            d.ApprovedByUserId,
            d.ApprovedAt,
            d.Status)).ToList();

        var measures = item.Measures.Select(m => new MeasureDto(
            m.Id,
            m.DisciplinaryDecisionId,
            m.EmployeeId,
            m.MeasureType,
            m.Reason,
            m.EffectiveFrom,
            m.EffectiveUntil,
            m.AppliedByUserId,
            m.AppliedAt,
            m.Status,
            m.Notes)).ToList();

        var response = new GetDisciplinaryCaseByIdResponse(
            item.Id,
            item.CaseNumber,
            item.CompanyId,
            item.Title,
            item.Description,
            item.Status,
            item.Priority,
            item.OpenedAt,
            item.OpenedByUserId,
            item.ResponsibleEmployeeId,
            item.DueDate,
            item.ClosedAt,
            item.CancelledAt,
            item.CancellationReason,
            item.ConclusionSummary,
            occurrences,
            employees,
            evidences,
            decisions,
            measures);

        return Result<GetDisciplinaryCaseByIdResponse>.Ok(response);
    }
}
