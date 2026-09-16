using EventHub.Application.Common.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventHub.Application.Authentication.Commands.ForgotPassword
{
    public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand>
    {
    private readonly IUserRepository _users;
    private readonly ISecureTokenService _tokens;
        private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;

    public ForgotPasswordCommandHandler(
        IUserRepository users,
        ISecureTokenService tokens,
        IEmailService emailService,
        IUnitOfWork unitOfWork)
        {
        _users = users;
        _tokens = tokens;
            _emailService = emailService;
        _unitOfWork = unitOfWork;
        }

        public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _users.GetByEmailAsync(request.Email, cancellationToken);

            // Əgər istifadəçi yoxdursa, təhlükəsizlik məqsədilə xəta qaytarmırıq (Hackerlərdən qorunmaq üçün).
            // Sadəcə səssizcə prosesi bitiririk.
            if (user == null)
                return;

            var resetToken = _tokens.GenerateToken();
            user.PasswordResetTokenHash = _tokens.HashToken(resetToken);
            user.PasswordResetTokenExpires = _tokens.GetPasswordResetTokenExpiry();
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 3. Frontend-in (və ya gələcək UI-ın) ünvanı. Hələlik bura fiktiv bir link qoyuruq.
            // Gələcəkdə bunu appsettings.json-dan oxuyacağıq.
            var resetUrl = "https://localhost:7248/api/Auth/reset-password-page";

            // C# Uri.EscapeDataString ilə tokenin içindəki xüsusi simvolları (+, /, =) URL-ə uyğunlaşdırırıq
            var encodedToken = Uri.EscapeDataString(resetToken);
            var resetLink = $"{resetUrl}?email={Uri.EscapeDataString(user.Email)}&token={encodedToken}";

            // 4. E-poçtu göndəririk
            var emailBody = $@"
            <h2>Şifrə Sıfırlama Sorğusu</h2>
            <p>Salam {user.FirstName},</p>
            <p>EventHub hesabınızın şifrəsini sıfırlamaq üçün aşağıdakı linkə daxil olun:</p>
            <p><a href='{resetLink}'>Şifrəmi Yenilə</a></p>
            <p>Əgər bu sorğunu siz etməmisinizsə, lütfən bu məktubu görməzlikdən gəlin.</p>";

            await _emailService.SendEmailAsync(user.Email, "EventHub - Password reset", emailBody, isHtml: true);
        }
    }
}
