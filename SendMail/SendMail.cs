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

namespace Name.Lechners.GiraSdk.Mail
{

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
        }

        public override void Startup()
        {

        }

        public override void Execute()
        {
            if (!SendTrigger.HasValue || !SendTrigger.WasSet || !To.HasValue || !From.HasValue || !SmtpHost.HasValue || !SmtpPort.HasValue)
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
            message.Subject = "X1 Mail ...";

            message.Body = new TextPart("plain")
            {
                Text = @"Hi,

Erste Mail vom X1!
"
            };

            using (var client = new SmtpClient())
            {
                // For demo-purposes, accept all SSL certificates (in case the server supports STARTTLS)
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                
                client.Connect(SmtpHost.Value, SmtpPort.Value, MailKit.Security.SecureSocketOptions.None);

                // Note: only needed if the SMTP server requires authentication
                // client.Authenticate("joey", "password");

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
