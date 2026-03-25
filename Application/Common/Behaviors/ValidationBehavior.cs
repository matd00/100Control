using FluentValidation;
using MediatR;
using Domain.Common;

namespace Application.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> _validators)
    {
        this._validators = _validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var validationResults = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
            var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();

            if (failures.Count != 0)
            {
                // Here we assume TResponse is a Result or Result<T>
                if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
                {
                    var resultType = typeof(TResponse).GetGenericArguments()[0];
                    var failureMethod = typeof(Result<>).MakeGenericType(resultType).GetMethod("Failure", new[] { typeof(string) });
                    var errorMessage = string.Join("; ", failures.Select(f => f.ErrorMessage));
                    
                    if (failureMethod != null)
                    {
                        var result = failureMethod.Invoke(null, new object[] { errorMessage });
                        if (result != null)
                        {
                            return (TResponse)result;
                        }
                    }
                }
                else if (typeof(TResponse) == typeof(Result))
                {
                    var errorMessage = string.Join("; ", failures.Select(f => f.ErrorMessage));
                    return (TResponse)(object)Result.Failure(errorMessage);
                }
                
                throw new ValidationException(failures);
            }
        }
        return await next();
    }
}
