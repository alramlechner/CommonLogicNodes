using LogicModule.Nodes.Helpers;
using LogicModule.ObjectModel;
using LogicModule.ObjectModel.TypeSystem;
using MailKit.Net.Smtp;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace alram_lechner_gmx_at.logic.Mail
{

    static class EncryptionTypes
    {
        public const string NONE = "Unverschlüsselt";
        public const string SSL = "SSL";
        public const string STARTTLS = "STARTTLS";
        public static string[] VALUES = new[] { NONE,SSL,STARTTLS };
    }

    public class SendMail : LogicNodeBase
    {
        [Input(DisplayOrder = 1, IsInput = true, IsRequired = true)]
        public BoolValueObject SendTrigger { get; private set; }

        [Parameter(DisplayOrder = 2, InitOrder = 1, IsDefaultShown = false)]
        public StringValueObject To { get; private set; }

        [Parameter(DisplayOrder = 3, InitOrder = 1, IsDefaultShown = false)]
        public StringValueObject From { get; private set; }

        [Parameter(DisplayOrder = 4, InitOrder = 1, IsDefaultShown = false)]
        public StringValueObject SmtpHost { get; private set; }

        [Parameter(DisplayOrder = 5, InitOrder = 1, IsDefaultShown = false)]
        public IntValueObject SmtpPort { get; private set; }

        [Parameter(DisplayOrder = 6, InitOrder = 1, IsDefaultShown = false)]
        public EnumValueObject Encryption { get; private set; }

        [Parameter(DisplayOrder = 7, InitOrder = 1, IsDefaultShown = false, IsRequired = false)]
        public StringValueObject SmtpUser { get; private set; }

        [Parameter(DisplayOrder = 8, InitOrder = 1, IsDefaultShown = false, IsRequired = false)]
        public StringValueObject SmtpPassword { get; private set; }

        [Input(DisplayOrder = 9, InitOrder = 1, IsDefaultShown = false)]
        public StringValueObject Subject { get; private set; }
        
        [Input(DisplayOrder = 10, InitOrder = 1, IsDefaultShown = false)]
        public StringValueObject MailBody { get; private set; }

        [Output(DisplayOrder = 1)]
        public StringValueObject ErrorMessage { get; private set; }

        public SendMail(INodeContext context) : base(context)
        {
            context.ThrowIfNull("context");
            ITypeService typeService = context.GetService<ITypeService>();
            this.SendTrigger = typeService.CreateBool(PortTypes.Bool, "Trigger");
            this.To = typeService.CreateString(PortTypes.String, "Empfängeradresse");
            this.From = typeService.CreateString(PortTypes.String, "Senderadresse");
            this.SmtpHost = typeService.CreateString(PortTypes.String, "SMTP Server");
            this.SmtpPort = typeService.CreateInt(PortTypes.Integer, "SMTP Port");
            this.ErrorMessage = typeService.CreateString(PortTypes.String, "Fehlertext");
            this.Encryption = typeService.CreateEnum("SmtpEncryption", "Verschlüsselung", EncryptionTypes.VALUES);
            this.SmtpUser = typeService.CreateString(PortTypes.String, "SMTP Benutzer");
            this.SmtpPassword = typeService.CreateString(PortTypes.String, "SMTP Kennwort");
            this.Subject = typeService.CreateString(PortTypes.String, "Betreff");
            this.MailBody = typeService.CreateString(PortTypes.String, "Mailtext");
        }

        public override void Startup()
        {

        }

        public override void Execute()
        {
            if (!SendTrigger.HasValue || !SendTrigger.WasSet || !To.HasValue || !From.HasValue || !SmtpHost.HasValue || !SmtpPort.HasValue
                || !Encryption.HasValue)
            {
                return;
            }

            // TODO: schedule as async task ...
            try
            {
                SendMessage();
                this.ErrorMessage.Value = "";
            }
            catch (Exception e)
            {
                this.ErrorMessage.Value = e.ToString();
            }
        }

        public void SendMessage()
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(From.Value));
            message.To.Add(new MailboxAddress(To.Value));
            if (Subject.HasValue)
            {
                message.Subject = Subject.Value;
            } else
            {
                message.Subject = Subject.Value;
            }

            if (MailBody.HasValue)
            {
                message.Body = new TextPart("plain")
                {
                    Text = MailBody.Value
                };
            } else
            {
                message.Body = new TextPart("plain")
                {
                    Text = ""
                };
            }

            using (var client = new SmtpClient())
            {
                // For demo-purposes, accept all SSL certificates (in case the server supports STARTTLS)
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                MailKit.Security.SecureSocketOptions socketOptions;
                switch (Encryption.Value)
                {
                    case EncryptionTypes.SSL:
                        socketOptions = MailKit.Security.SecureSocketOptions.SslOnConnect;
                        break;
                    case EncryptionTypes.STARTTLS:
                        socketOptions = MailKit.Security.SecureSocketOptions.StartTls;
                        break;
                    case EncryptionTypes.NONE:
                        socketOptions = MailKit.Security.SecureSocketOptions.None;
                        break;
                    default:
                        socketOptions = MailKit.Security.SecureSocketOptions.Auto;
                        break;
                }

                client.Connect(SmtpHost.Value, SmtpPort.Value, socketOptions);

                // Note: only needed if the SMTP server requires authentication
                if (SmtpUser.HasValue && SmtpPassword.HasValue && SmtpUser.Value.Length >= 1 && SmtpPassword.Value.Length >= 1)
                {
                    client.Authenticate(SmtpUser.Value, SmtpPassword.Value);
                }

                client.Send(message);
                client.Disconnect(true);
            }
        }

        public override ValidationResult Validate(string language)
        {
            return base.Validate(language);
        }

    }
}
