﻿using System.Net.Mail;
using instock_server_application.Email.Dtos;
using instock_server_application.Email.Services.Interfaces;

namespace instock_server_application.Email.Services; 

public class EmailService : IEmailService {
    private readonly SmtpClient _smtpClient;

    public EmailService(SmtpClient smtpClient) {
        _smtpClient = smtpClient;
    }

    public EmailResponseDto SendEmailAsync(string subject, string message) {
        EmailResponseDto emailResponseDto = new EmailResponseDto();
        
        string email = "instockapplication@gmail.com";

        try {
            _smtpClient.SendMailAsync(
                new MailMessage(
                    from: email,
                    to: email,
                    subject,
                    message
                )
            );

            emailResponseDto.Message = "Your message has been sent! Thank you for contacting us.";
            return emailResponseDto;
        }
        catch (SmtpException e) {
            emailResponseDto.StatusCode = (int)e.StatusCode;
            emailResponseDto.Message =
                "Oops, something went wrong! We were not able to send your message. Please try again later";
            return emailResponseDto;
        }
    }
}