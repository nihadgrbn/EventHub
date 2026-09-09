using FluentValidation;
using EventHub.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Api.Middleware;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is ValidationException or ConflictException or UnauthorizedException)
        {
            _logger.LogWarning(
                "Request failed with {ExceptionType} for {Path}.",
                exception.GetType().Name,
                httpContext.Request.Path);
        }
        else
        {
            _logger.LogError(
                exception,
                "An unhandled exception occurred while processing the request {Path}.",
                httpContext.Request.Path);
        }

        var (statusCode, title, errors) = exception switch
        {
            ValidationException validationException =>
                (StatusCodes.Status400BadRequest,
                    "One or more validation errors occurred.",
                    validationException.Errors
                        .GroupBy(error => error.PropertyName)
                        .ToDictionary(
                            group => group.Key,
                            group => group.Select(error => error.ErrorMessage).ToArray())),
            NotFoundException notFoundException =>
                (StatusCodes.Status404NotFound, notFoundException.Message, null),
            ConflictException conflictException =>
                (StatusCodes.Status409Conflict, conflictException.Message, null),
            UnauthorizedException unauthorizedException =>
                (StatusCodes.Status401Unauthorized, unauthorizedException.Message, null),
            ForbiddenException forbiddenException =>
                (StatusCodes.Status403Forbidden, forbiddenException.Message, null),
            _ =>
                (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", null)
        };

        httpContext.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = $"https://httpstatuses.com/{statusCode}",
            Instance = httpContext.Request.Path
        };

        if (errors is not null)
        {
            problemDetails.Extensions["errors"] = errors;
        }

        await _problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });

        return true;
    }
}
