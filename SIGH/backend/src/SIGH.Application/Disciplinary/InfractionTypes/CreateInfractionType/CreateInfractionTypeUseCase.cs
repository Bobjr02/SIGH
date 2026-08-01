using FluentValidation;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.Common;
using SIGH.Application.Interfaces.Repositories.Disciplinary;
using SIGH.Domain.Disciplinary.Entities;

namespace SIGH.Application.Disciplinary.InfractionTypes.CreateInfractionType;

public class CreateInfractionTypeUseCase : ICreateInfractionTypeUseCase
{
    private readonly IApplicationDbContext _context;
    private readonly IInfractionTypeRepository _repository;
    private readonly IValidator<CreateInfractionTypeRequest>? _validator;

    public CreateInfractionTypeUseCase(
        IApplicationDbContext context,
        IInfractionTypeRepository repository,
        IValidator<CreateInfractionTypeRequest>? validator = null)
    {
        _context = context;
        _repository = repository;
        _validator = validator;
    }

    public async Task<Result<CreateInfractionTypeResponse>> ExecuteAsync(CreateInfractionTypeRequest request, CancellationToken cancellationToken = default)
    {
        if (_validator != null)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return Result<CreateInfractionTypeResponse>.Failure(string.Join("; ", errors), "ValidationError");
            }
        }

        var exists = await _repository.ExistsByCodeAsync(request.Code, cancellationToken);
        if (exists)
        {
            return Result<CreateInfractionTypeResponse>.Failure("Já existe um tipo de infração cadastrado com este código.", DisciplinaryErrors.DuplicateInfractionCode);
        }

        var infractionType = InfractionType.Create(
            request.Code,
            request.Name,
            request.DefaultSeverity,
            request.Description,
            request.LegalReference);

        await _repository.AddAsync(infractionType, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var response = new CreateInfractionTypeResponse(
            infractionType.Id,
            infractionType.Code,
            infractionType.Name,
            infractionType.DefaultSeverity,
            infractionType.IsActive);

        return Result<CreateInfractionTypeResponse>.Ok(response, "Tipo de infração criado com sucesso.");
    }
}
