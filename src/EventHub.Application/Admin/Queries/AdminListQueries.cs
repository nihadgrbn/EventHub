using EventHub.Application.Admin.Models;
using EventHub.Application.Common.Models;
using FluentValidation;
using MediatR;

namespace EventHub.Application.Admin.Queries;

public sealed record GetAdminUsersQuery(
    string? Search = null,
    string? Role = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PaginatedList<AdminUserDto>>;

public sealed record GetAdminEventsQuery(
    string? Search = null,
    string? Status = null,
    Guid? OrganizerId = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PaginatedList<AdminEventDto>>;

public sealed record GetAdminPurchasesQuery(
    Guid? AttendeeId = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PaginatedList<AdminPurchaseDto>>;

public sealed record GetAdminPaymentsQuery(
    string? Status = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PaginatedList<AdminPaymentDto>>;

public sealed record GetAdminReservationsQuery(
    string? Status = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PaginatedList<AdminReservationDto>>;

public sealed record GetAdminPaymentDashboardQuery : IRequest<AdminPaymentDashboardDto>;

public sealed record GetAdminPaymentDetailsQuery(Guid PaymentId) : IRequest<AdminPaymentDetailsDto?>;

public sealed record GetAdminWebhooksQuery(
    string? Status = null,
    Guid? PaymentId = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PaginatedList<AdminWebhookDto>>;

public sealed record GetAdminOutboxMessagesQuery(
    string? Status = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PaginatedList<AdminOutboxDto>>;

public sealed class GetAdminUsersQueryValidator : AbstractValidator<GetAdminUsersQuery>
{
    public GetAdminUsersQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Search).MaximumLength(150);
    }
}

public sealed class GetAdminEventsQueryValidator : AbstractValidator<GetAdminEventsQuery>
{
    public GetAdminEventsQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Search).MaximumLength(150);
    }
}

public sealed class GetAdminPurchasesQueryValidator : AbstractValidator<GetAdminPurchasesQuery>
{
    public GetAdminPurchasesQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class GetAdminPaymentsQueryValidator : AbstractValidator<GetAdminPaymentsQuery>
{
    public GetAdminPaymentsQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class GetAdminReservationsQueryValidator : AbstractValidator<GetAdminReservationsQuery>
{
    public GetAdminReservationsQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class GetAdminWebhooksQueryValidator : AbstractValidator<GetAdminWebhooksQuery>
{
    public GetAdminWebhooksQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class GetAdminOutboxMessagesQueryValidator : AbstractValidator<GetAdminOutboxMessagesQuery>
{
    public GetAdminOutboxMessagesQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
