using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventHub.Application.Authentication.Commands.ForgotPassword
{
    public record ForgotPasswordCommand(string Email) : IRequest;
}
