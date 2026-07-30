using SIGH.Domain.Common;
using SIGH.Domain.Disciplinary.Constants;
using SIGH.Domain.Disciplinary.Enums;
using SIGH.Domain.Exceptions;

namespace SIGH.Domain.Disciplinary.Entities;

public class DisciplinaryCase : AuditableEntity
{
    private readonly List<DisciplinaryOccurrence> _occurrences = [];
    private readonly List<DisciplinaryCaseEmployee> _employees = [];
    private readonly List<DisciplinaryEvidence> _evidences = [];
    private readonly List<DisciplinaryMeasure> _measures = [];
    private readonly List<DisciplinaryDecision> _decisions = [];

    public string CaseNumber { get; private set; } = string.Empty;
    public Guid CompanyId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DisciplinaryCaseStatus Status { get; private set; } = DisciplinaryCaseStatus.Draft;
    public DisciplinaryCasePriority Priority { get; private set; } = DisciplinaryCasePriority.Normal;
    public DateTimeOffset OpenedAt { get; private set; }
    public Guid OpenedByUserId { get; private set; }
    public Guid? ResponsibleEmployeeId { get; private set; }
    public DateTimeOffset? DueDate { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public string? CancellationReason { get; private set; }
    public string? ConclusionSummary { get; private set; }

    public DisciplinaryDecision? Decision => _decisions.FirstOrDefault(d => d.Status == DecisionStatus.Draft || d.Status == DecisionStatus.PendingApproval || d.Status == DecisionStatus.Approved);
    public Guid? DecisionId => Decision?.Id;

    public IReadOnlyCollection<DisciplinaryOccurrence> Occurrences => _occurrences.AsReadOnly();
    public IReadOnlyCollection<DisciplinaryCaseEmployee> Employees => _employees.AsReadOnly();
    public IReadOnlyCollection<DisciplinaryEvidence> Evidences => _evidences.AsReadOnly();
    public IReadOnlyCollection<DisciplinaryMeasure> Measures => _measures.AsReadOnly();
    public IReadOnlyCollection<DisciplinaryDecision> Decisions => _decisions.AsReadOnly();

    // EF Core
    protected DisciplinaryCase() : base() { }

    protected DisciplinaryCase(Guid id) : base(id) { }

    public static DisciplinaryCase Create(
        string caseNumber,
        Guid companyId,
        string title,
        string description,
        DateTimeOffset openedAt,
        Guid openedByUserId,
        DisciplinaryCasePriority priority = DisciplinaryCasePriority.Normal,
        Guid? responsibleEmployeeId = null,
        DateTimeOffset? dueDate = null,
        DisciplinaryCaseStatus initialStatus = DisciplinaryCaseStatus.Draft)
    {
        if (string.IsNullOrWhiteSpace(caseNumber))
            throw new BusinessRuleValidationException("O número do processo disciplinar é obrigatório.");

        var trimmedCaseNumber = caseNumber.Trim();
        if (trimmedCaseNumber.Length > DisciplinaryDomainConstants.CaseNumberMaxLength)
            throw new BusinessRuleValidationException($"O número do processo não pode exceder {DisciplinaryDomainConstants.CaseNumberMaxLength} caracteres.");

        if (companyId == Guid.Empty)
            throw new BusinessRuleValidationException("O ID da empresa é obrigatório.");

        if (openedByUserId == Guid.Empty)
            throw new BusinessRuleValidationException("O usuário responsável pela abertura é obrigatório.");

        if (openedAt == default)
            throw new BusinessRuleValidationException("A data de abertura é inválida.");

        if (priority == DisciplinaryCasePriority.Undefined || !Enum.IsDefined(priority))
            throw new BusinessRuleValidationException("A prioridade é inválida.");

        if (initialStatus == DisciplinaryCaseStatus.Undefined || !Enum.IsDefined(initialStatus))
            throw new BusinessRuleValidationException("O status inicial do processo disciplinar é inválido.");

        if (string.IsNullOrWhiteSpace(title))
            throw new BusinessRuleValidationException("O título do processo disciplinar é obrigatório.");

        var trimmedTitle = title.Trim();
        if (trimmedTitle.Length > DisciplinaryDomainConstants.TitleMaxLength)
            throw new BusinessRuleValidationException($"O título do processo não pode exceder {DisciplinaryDomainConstants.TitleMaxLength} caracteres.");

        if (string.IsNullOrWhiteSpace(description))
            throw new BusinessRuleValidationException("A descrição do processo disciplinar é obrigatória.");

        var trimmedDescription = description.Trim();
        if (trimmedDescription.Length > DisciplinaryDomainConstants.DescriptionMaxLength)
            throw new BusinessRuleValidationException($"A descrição do processo não pode exceder {DisciplinaryDomainConstants.DescriptionMaxLength} caracteres.");

        if (dueDate.HasValue && dueDate.Value == default)
            throw new BusinessRuleValidationException("A data de vencimento é inválida.");

        if (dueDate.HasValue && dueDate.Value < openedAt)
            throw new BusinessRuleValidationException("A data de vencimento não pode ser anterior à data de abertura.");

        if (initialStatus != DisciplinaryCaseStatus.Draft && initialStatus != DisciplinaryCaseStatus.Open)
            throw new BusinessRuleValidationException("O processo disciplinar só pode ser criado no estado Rascunho ou Aberto.");

        return new DisciplinaryCase
        {
            CaseNumber = trimmedCaseNumber,
            CompanyId = companyId,
            Title = trimmedTitle,
            Description = trimmedDescription,
            OpenedAt = openedAt,
            OpenedByUserId = openedByUserId,
            Priority = priority,
            ResponsibleEmployeeId = responsibleEmployeeId,
            DueDate = dueDate,
            Status = initialStatus
        };
    }

    public void Open()
    {
        EnsureNotCompletedOrCancelled();

        if (Status != DisciplinaryCaseStatus.Draft)
            throw new BusinessRuleValidationException("Apenas processos em Rascunho podem ser abertos.");

        Status = DisciplinaryCaseStatus.Open;
    }

    public void StartInvestigation()
    {
        EnsureNotCompletedOrCancelled();

        if (Status != DisciplinaryCaseStatus.Open)
            throw new BusinessRuleValidationException("A investigação só pode ser iniciada a partir de um processo Aberto.");

        if (_occurrences.Count == 0)
            throw new BusinessRuleValidationException("O processo deve possuir ao menos uma ocorrência antes de iniciar a investigação.");

        Status = DisciplinaryCaseStatus.UnderInvestigation;
    }

    public void SubmitForDecision()
    {
        EnsureNotCompletedOrCancelled();

        if (Status != DisciplinaryCaseStatus.UnderInvestigation)
            throw new BusinessRuleValidationException("O processo deve estar Sob Investigação para ser submetido à decisão.");

        if (!_employees.Any(e => e.Role == CaseEmployeeRole.Accused))
            throw new BusinessRuleValidationException("O processo deve possuir ao menos um funcionário acusado antes de ser submetido à decisão.");

        Status = DisciplinaryCaseStatus.AwaitingDecision;
    }

    public void RegisterDecision(DisciplinaryDecision decision)
    {
        EnsureNotCompletedOrCancelled();

        if (decision == null)
            throw new BusinessRuleValidationException("A decisão é obrigatória.");

        if (decision.DisciplinaryCaseId != Id)
            throw new BusinessRuleValidationException("A decisão pertence a outro processo disciplinar.");

        if (Status != DisciplinaryCaseStatus.AwaitingDecision)
            throw new BusinessRuleValidationException("Uma decisão só pode ser registrada para um processo Aguardando Decisão.");

        if (decision.Status != DecisionStatus.Draft)
            throw new BusinessRuleValidationException("A nova decisão deve ser criada com o status Rascunho.");

        if (Decision != null)
            throw new BusinessRuleValidationException("Já existe uma decisão ativa vinculada a este processo disciplinar.");

        _decisions.Add(decision);
        Status = DisciplinaryCaseStatus.AwaitingDecision;
    }

    public void UpdateDecisionBeforeApproval(DecisionType decisionType, string summary, string reasoning)
    {
        EnsureNotCompletedOrCancelled();

        if (Status != DisciplinaryCaseStatus.AwaitingDecision)
            throw new BusinessRuleValidationException("Alterações em decisão só são permitidas para processos Aguardando Decisão.");

        if (Decision == null)
            throw new BusinessRuleValidationException("Nenhuma decisão ativa foi encontrada para este processo.");

        Decision.UpdateBeforeApproval(decisionType, summary, reasoning);
    }

    public void SubmitDecisionForApproval()
    {
        EnsureNotCompletedOrCancelled();

        if (Status != DisciplinaryCaseStatus.AwaitingDecision)
            throw new BusinessRuleValidationException("Submissão de decisão só é permitida para processos Aguardando Decisão.");

        if (Decision == null)
            throw new BusinessRuleValidationException("Nenhuma decisão ativa foi encontrada para este processo.");

        Decision.SubmitForApproval();
    }

    public void ApproveDecision(Guid approvedByUserId, DateTimeOffset approvedAt)
    {
        EnsureNotCompletedOrCancelled();

        if (Status != DisciplinaryCaseStatus.AwaitingDecision)
            throw new BusinessRuleValidationException("Aprovação de decisão só é permitida para processos Aguardando Decisão.");

        if (approvedByUserId == Guid.Empty)
            throw new BusinessRuleValidationException("O usuário aprovador é obrigatório.");

        if (approvedAt == default)
            throw new BusinessRuleValidationException("A data de aprovação é inválida.");

        if (Decision == null)
            throw new BusinessRuleValidationException("Nenhuma decisão ativa foi encontrada para este processo.");

        Decision.Approve(approvedByUserId, approvedAt);
        Status = DisciplinaryCaseStatus.Decided;
    }

    public void RejectDecision(string reason)
    {
        EnsureNotCompletedOrCancelled();

        if (Status != DisciplinaryCaseStatus.AwaitingDecision)
            throw new BusinessRuleValidationException("Rejeição de decisão só é permitida para processos Aguardando Decisão.");

        if (Decision == null)
            throw new BusinessRuleValidationException("Nenhuma decisão ativa foi encontrada para este processo.");

        if (Decision.Status != DecisionStatus.PendingApproval)
            throw new BusinessRuleValidationException("Apenas decisões pendentes de aprovação podem ser rejeitadas.");

        Decision.Reject(reason);
        Status = DisciplinaryCaseStatus.AwaitingDecision;
    }

    public void InvalidateDecision(string reason)
    {
        EnsureNotCompletedOrCancelled();

        if (Status != DisciplinaryCaseStatus.Decided)
            throw new BusinessRuleValidationException("Invalidação de decisão só é permitida para processos Decididos.");

        if (Decision == null || Decision.Status != DecisionStatus.Approved)
            throw new BusinessRuleValidationException("Apenas processos com decisão aprovada podem ter a decisão invalidada.");

        Decision.Invalidate(reason);
        Status = DisciplinaryCaseStatus.AwaitingDecision;
    }

    public void Complete(string conclusionSummary, DateTimeOffset closedAt)
    {
        EnsureNotCompletedOrCancelled();

        if (closedAt == default)
            throw new BusinessRuleValidationException("A data de encerramento é inválida.");

        if (Status != DisciplinaryCaseStatus.Decided)
            throw new BusinessRuleValidationException("O processo deve estar Decidido para poder ser concluído.");

        if (Decision == null || Decision.Status != DecisionStatus.Approved)
            throw new BusinessRuleValidationException("Não é possível concluir o processo sem uma decisão aprovada.");

        if (string.IsNullOrWhiteSpace(conclusionSummary))
            throw new BusinessRuleValidationException("O resumo da conclusão é obrigatório.");

        var trimmedSummary = conclusionSummary.Trim();
        if (trimmedSummary.Length > DisciplinaryDomainConstants.ConclusionSummaryMaxLength)
            throw new BusinessRuleValidationException($"O resumo da conclusão não pode exceder {DisciplinaryDomainConstants.ConclusionSummaryMaxLength} caracteres.");

        ConclusionSummary = trimmedSummary;
        ClosedAt = closedAt;
        Status = DisciplinaryCaseStatus.Completed;
    }

    public void Cancel(string cancellationReason, DateTimeOffset cancelledAt)
    {
        if (cancelledAt == default)
            throw new BusinessRuleValidationException("A data de cancelamento é inválida.");

        if (Status == DisciplinaryCaseStatus.Completed)
            throw new BusinessRuleValidationException("O processo disciplinar concluído não pode ser cancelado.");

        if (Status == DisciplinaryCaseStatus.Cancelled)
            throw new BusinessRuleValidationException("O processo disciplinar já está cancelado.");

        if (Status == DisciplinaryCaseStatus.Decided)
            throw new BusinessRuleValidationException("Não é possível cancelar um processo no estado Decidido. É necessário invalidar a decisão primeiro.");

        if (Status != DisciplinaryCaseStatus.Draft &&
            Status != DisciplinaryCaseStatus.Open &&
            Status != DisciplinaryCaseStatus.UnderInvestigation &&
            Status != DisciplinaryCaseStatus.AwaitingDecision)
            throw new BusinessRuleValidationException("O processo disciplinar não está em um estado que permita cancelamento.");

        if (string.IsNullOrWhiteSpace(cancellationReason))
            throw new BusinessRuleValidationException("O motivo do cancelamento é obrigatório.");

        var trimmedReason = cancellationReason.Trim();
        if (trimmedReason.Length > DisciplinaryDomainConstants.CancellationReasonMaxLength)
            throw new BusinessRuleValidationException($"O motivo do cancelamento não pode exceder {DisciplinaryDomainConstants.CancellationReasonMaxLength} caracteres.");

        CancellationReason = trimmedReason;
        CancelledAt = cancelledAt;
        Status = DisciplinaryCaseStatus.Cancelled;
    }

    public void ChangePriority(DisciplinaryCasePriority priority)
    {
        EnsureNotCompletedOrCancelled();

        if (priority == DisciplinaryCasePriority.Undefined || !Enum.IsDefined(priority))
            throw new BusinessRuleValidationException("A prioridade é inválida.");

        Priority = priority;
    }

    public void ChangeResponsible(Guid? responsibleEmployeeId)
    {
        EnsureNotCompletedOrCancelled();
        ResponsibleEmployeeId = responsibleEmployeeId;
    }

    public void ChangeDueDate(DateTimeOffset? dueDate)
    {
        EnsureNotCompletedOrCancelled();

        if (dueDate.HasValue && dueDate.Value == default)
            throw new BusinessRuleValidationException("A data de vencimento é inválida.");

        if (dueDate.HasValue && dueDate.Value < OpenedAt)
            throw new BusinessRuleValidationException("A data de vencimento não pode ser anterior à data de abertura.");

        DueDate = dueDate;
    }

    public void UpdateDetails(string title, string description)
    {
        EnsureNotCompletedOrCancelled();

        if (string.IsNullOrWhiteSpace(title))
            throw new BusinessRuleValidationException("O título do processo disciplinar é obrigatório.");

        var trimmedTitle = title.Trim();
        if (trimmedTitle.Length > DisciplinaryDomainConstants.TitleMaxLength)
            throw new BusinessRuleValidationException($"O título do processo não pode exceder {DisciplinaryDomainConstants.TitleMaxLength} caracteres.");

        if (string.IsNullOrWhiteSpace(description))
            throw new BusinessRuleValidationException("A descrição do processo disciplinar é obrigatória.");

        var trimmedDescription = description.Trim();
        if (trimmedDescription.Length > DisciplinaryDomainConstants.DescriptionMaxLength)
            throw new BusinessRuleValidationException($"A descrição do processo não pode exceder {DisciplinaryDomainConstants.DescriptionMaxLength} caracteres.");

        Title = trimmedTitle;
        Description = trimmedDescription;
    }

    public void AddOccurrence(DisciplinaryOccurrence occurrence)
    {
        EnsureNotCompletedOrCancelled();

        if (occurrence == null)
            throw new BusinessRuleValidationException("A ocorrência é obrigatória.");

        if (occurrence.DisciplinaryCaseId != Id)
            throw new BusinessRuleValidationException("A ocorrência pertence a outro processo disciplinar.");

        if (_occurrences.Any(o => o.Id == occurrence.Id))
            throw new BusinessRuleValidationException("Esta ocorrência já está associada ao processo.");

        _occurrences.Add(occurrence);
    }

    public void UpdateOccurrence(Guid occurrenceId, string description, string? location = null, ConfidentialityLevel confidentialityLevel = ConfidentialityLevel.Internal)
    {
        EnsureNotCompletedOrCancelled();

        var occurrence = _occurrences.FirstOrDefault(o => o.Id == occurrenceId)
            ?? throw new BusinessRuleValidationException("A ocorrência informada não pertence a este processo disciplinar.");

        occurrence.UpdateDetails(description, location, confidentialityLevel);
    }

    public void ValidateOccurrence(Guid occurrenceId)
    {
        EnsureNotCompletedOrCancelled();

        var occurrence = _occurrences.FirstOrDefault(o => o.Id == occurrenceId)
            ?? throw new BusinessRuleValidationException("A ocorrência informada não pertence a este processo disciplinar.");

        occurrence.MarkAsValidated();
    }

    public void RejectOccurrence(Guid occurrenceId)
    {
        EnsureNotCompletedOrCancelled();

        var occurrence = _occurrences.FirstOrDefault(o => o.Id == occurrenceId)
            ?? throw new BusinessRuleValidationException("A ocorrência informada não pertence a este processo disciplinar.");

        occurrence.MarkAsRejected();
    }

    public void RemoveOccurrence(Guid occurrenceId)
    {
        EnsureNotCompletedOrCancelled();

        if (Status != DisciplinaryCaseStatus.Draft && Status != DisciplinaryCaseStatus.Open)
            throw new BusinessRuleValidationException("Não é possível remover ocorrências de um processo sob investigação ou posterior.");

        var occurrence = _occurrences.FirstOrDefault(o => o.Id == occurrenceId);
        if (occurrence != null)
        {
            _occurrences.Remove(occurrence);
        }
    }

    public void AddEmployee(DisciplinaryCaseEmployee employee)
    {
        EnsureNotCompletedOrCancelled();

        if (employee == null)
            throw new BusinessRuleValidationException("O funcionário é obrigatório.");

        if (employee.DisciplinaryCaseId != Id)
            throw new BusinessRuleValidationException("O vínculo do funcionário pertence a outro processo disciplinar.");

        if (_employees.Any(e => e.EmployeeId == employee.EmployeeId && e.Role == employee.Role))
            throw new BusinessRuleValidationException("O funcionário já está associado ao processo com este papel.");

        _employees.Add(employee);
    }

    public void RegisterEmployeeStatement(Guid caseEmployeeId, string statement, DateTimeOffset recordedAt)
    {
        EnsureNotCompletedOrCancelled();

        var employee = _employees.FirstOrDefault(e => e.Id == caseEmployeeId)
            ?? throw new BusinessRuleValidationException("O funcionário informado não pertence a este processo disciplinar.");

        employee.RegisterStatement(statement, recordedAt);
    }

    public void UpdateEmployeeRole(Guid caseEmployeeId, CaseEmployeeRole newRole)
    {
        EnsureNotCompletedOrCancelled();

        var employee = _employees.FirstOrDefault(e => e.Id == caseEmployeeId)
            ?? throw new BusinessRuleValidationException("O funcionário informado não pertence a este processo disciplinar.");

        employee.UpdateRole(newRole);
    }

    public void RemoveEmployee(Guid caseEmployeeId)
    {
        EnsureNotCompletedOrCancelled();

        var employee = _employees.FirstOrDefault(e => e.Id == caseEmployeeId);
        if (employee != null)
        {
            _employees.Remove(employee);
        }
    }

    public void AddEvidence(DisciplinaryEvidence evidence)
    {
        EnsureNotCompletedOrCancelled();

        if (evidence == null)
            throw new BusinessRuleValidationException("A evidência é obrigatória.");

        if (evidence.DisciplinaryCaseId != Id)
            throw new BusinessRuleValidationException("A evidência pertence a outro processo disciplinar.");

        if (evidence.DisciplinaryOccurrenceId.HasValue && !_occurrences.Any(o => o.Id == evidence.DisciplinaryOccurrenceId.Value))
            throw new BusinessRuleValidationException("A ocorrência vinculada à evidência não pertence a este processo disciplinar.");

        if (_evidences.Any(e => e.Id == evidence.Id))
            throw new BusinessRuleValidationException("Esta evidência já está associada ao processo.");

        _evidences.Add(evidence);
    }

    public void UpdateEvidenceDescription(Guid evidenceId, string? description)
    {
        EnsureNotCompletedOrCancelled();

        var evidence = _evidences.FirstOrDefault(e => e.Id == evidenceId)
            ?? throw new BusinessRuleValidationException("A evidência informada não pertence a este processo disciplinar.");

        evidence.UpdateDescription(description);
    }

    public void VerifyEvidence(Guid evidenceId)
    {
        EnsureNotCompletedOrCancelled();

        var evidence = _evidences.FirstOrDefault(e => e.Id == evidenceId)
            ?? throw new BusinessRuleValidationException("A evidência informada não pertence a este processo disciplinar.");

        evidence.MarkAsVerified();
    }

    public void RejectEvidence(Guid evidenceId)
    {
        EnsureNotCompletedOrCancelled();

        var evidence = _evidences.FirstOrDefault(e => e.Id == evidenceId)
            ?? throw new BusinessRuleValidationException("A evidência informada não pertence a este processo disciplinar.");

        evidence.MarkAsRejected();
    }

    public void RemoveEvidence(Guid evidenceId)
    {
        EnsureNotCompletedOrCancelled();

        var evidence = _evidences.FirstOrDefault(e => e.Id == evidenceId);
        if (evidence != null)
        {
            _evidences.Remove(evidence);
        }
    }

    public void AddMeasure(DisciplinaryMeasure measure)
    {
        EnsureNotCompletedOrCancelled();

        if (measure == null)
            throw new BusinessRuleValidationException("A medida disciplinar é obrigatória.");

        if (measure.DisciplinaryCaseId != Id)
            throw new BusinessRuleValidationException("A medida disciplinar pertence a outro processo disciplinar.");

        if (DecisionId == null || measure.DisciplinaryDecisionId != DecisionId.Value)
            throw new BusinessRuleValidationException("A medida disciplinar deve estar vinculada à decisão ativa do processo.");

        if (Status != DisciplinaryCaseStatus.Decided)
            throw new BusinessRuleValidationException("Medidas disciplinares só podem ser registradas em processos Decididos.");

        if (Decision == null || Decision.Status != DecisionStatus.Approved)
            throw new BusinessRuleValidationException("A medida disciplinar não pode ser registrada antes da aprovação da decisão.");

        if (!_employees.Any(e => e.EmployeeId == measure.EmployeeId && e.Role == CaseEmployeeRole.Accused))
            throw new BusinessRuleValidationException("A medida disciplinar só pode ser aplicada a um funcionário vinculado como acusado ao processo.");

        if (_measures.Any(m => m.EmployeeId == measure.EmployeeId && m.MeasureType == measure.MeasureType && m.Status != DisciplinaryMeasureStatus.Cancelled))
            throw new BusinessRuleValidationException("Já existe uma medida disciplinar deste tipo vinculada ao funcionário neste processo.");

        _measures.Add(measure);
    }

    public void ApplyMeasure(Guid measureId, DateTimeOffset appliedAt, Guid appliedByUserId)
    {
        EnsureNotCompletedOrCancelled();

        var measure = _measures.FirstOrDefault(m => m.Id == measureId)
            ?? throw new BusinessRuleValidationException("A medida disciplinar informada não pertence a este processo disciplinar.");

        measure.Apply(appliedAt, appliedByUserId);
    }

    public void CompleteMeasure(Guid measureId)
    {
        EnsureNotCompletedOrCancelled();

        var measure = _measures.FirstOrDefault(m => m.Id == measureId)
            ?? throw new BusinessRuleValidationException("A medida disciplinar informada não pertence a este processo disciplinar.");

        measure.Complete();
    }

    public void CancelMeasure(Guid measureId, string reason)
    {
        EnsureNotCompletedOrCancelled();

        var measure = _measures.FirstOrDefault(m => m.Id == measureId)
            ?? throw new BusinessRuleValidationException("A medida disciplinar informada não pertence a este processo disciplinar.");

        measure.Cancel(reason);
    }

    private void EnsureNotCompletedOrCancelled()
    {
        if (Status == DisciplinaryCaseStatus.Completed)
            throw new BusinessRuleValidationException("O processo disciplinar concluído não pode ser alterado.");

        if (Status == DisciplinaryCaseStatus.Cancelled)
            throw new BusinessRuleValidationException("O processo disciplinar cancelado não pode ser alterado.");
    }
}
