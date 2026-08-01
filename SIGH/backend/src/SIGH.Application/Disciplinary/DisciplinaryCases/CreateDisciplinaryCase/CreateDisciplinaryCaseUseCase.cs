using FluentValidation;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.Common;
using SIGH.Application.Interfaces;
using SIGH.Application.Interfaces.Repositories.Disciplinary;
using SIGH.Domain.Disciplinary.Entities;
using SIGH.Domain.Disciplinary.Enums;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.CreateDisciplinaryCase;

public class CreateDisciplinaryCaseUseCase : ICreateDisciplinaryCaseUseCase
{
    private readonly IApplicationDbContext _context;
    private readonly IDisciplinaryCaseRepository _caseRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IValidator<CreateDisciplinaryCaseRequest>? _validator;

    public CreateDisciplinaryCaseUseCase(
        IApplicationDbContext context,
        IDisciplinaryCaseRepository caseRepository,
        IDateTimeProvider dateTimeProvider,
        IValidator<CreateDisciplinaryCaseRequest>? validator = null)
    {
        _context = context;
        _caseRepository = caseRepository;
        _dateTimeProvider = dateTimeProvider;
        _validator = validator;
    }

    public async Task<Result<CreateDisciplinaryCaseResponse>> ExecuteAsync(CreateDisciplinaryCaseRequest request, CancellationToken cancellationToken = default)
    {
        if (_validator != null)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return Result<CreateDisciplinaryCaseResponse>.Failure(string.Join("; ", errors), "ValidationError");
            }
        }

        var exists = await _caseRepository.ExistsByCaseNumberAsync(request.CompanyId, request.CaseNumber, cancellationToken);
        if (exists)
        {
            return Result<CreateDisciplinaryCaseResponse>.Failure("Já existe um processo disciplinar com este número nesta empresa.", DisciplinaryErrors.DuplicateCaseNumber);
        }

        var now = _dateTimeProvider.UtcNow;

        var disciplinaryCase = DisciplinaryCase.Create(
            caseNumber: request.CaseNumber,
            companyId: request.CompanyId,
            title: request.Title,
            description: request.Description,
            openedAt: now,
            openedByUserId: request.CreatedByUserId,
            priority: request.Priority,
            responsibleEmployeeId: request.ResponsibleEmployeeId,
            dueDate: request.DueDate,
            initialStatus: DisciplinaryCaseStatus.Draft);

        await _caseRepository.AddAsync(disciplinaryCase, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var response = new CreateDisciplinaryCaseResponse(
            disciplinaryCase.Id,
            disciplinaryCase.CaseNumber,
            disciplinaryCase.CompanyId,
            disciplinaryCase.Title,
            disciplinaryCase.Status,
            disciplinaryCase.Priority,
            disciplinaryCase.OpenedAt,
            disciplinaryCase.OpenedByUserId,
            disciplinaryCase.ResponsibleEmployeeId,
            disciplinaryCase.DueDate);

        return Result<CreateDisciplinaryCaseResponse>.Ok(response, "Processo disciplinar criado com sucesso.");
    }
}
