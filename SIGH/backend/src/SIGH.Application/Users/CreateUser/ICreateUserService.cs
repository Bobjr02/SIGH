namespace SIGH.Application.Users.CreateUser;

public interface ICreateUserService
{
    Task<CreateUserResponse> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
}
