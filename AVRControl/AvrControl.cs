using LogicModule.Nodes.Helpers;
using LogicModule.ObjectModel;
using LogicModule.ObjectModel.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace alram_lechner_gmx_at.logic.AvrControl
{
    public class RunMacro : LogicNodeBase
    {
        [Parameter(DisplayOrder = 1, IsRequired = true, IsDefaultShown = false)]
        public StringValueObject AvrControlIp { get; private set; }

        [Input(DisplayOrder = 2, IsInput = true, IsRequired = false)]
        public StringValueObject MacroName { get; private set; }

        [Input(DisplayOrder = 3, IsInput = true, IsRequired = false)]
        public BoolValueObject InputPower { get; private set; }

        [Input(DisplayOrder = 4, IsInput = true, IsRequired = false)]
        public BoolValueObject VolumeRelativ { get; private set; }

        [Input(DisplayOrder = 5, IsInput = true, IsRequired = false)]
        public BoolValueObject SpeakerBEnable { get; private set; }

        [Output(DisplayOrder = 1, IsDefaultShown = true)]
        public BoolValueObject OutputPower { get; private set; }

        [Output(DisplayOrder = 2, IsDefaultShown = true)]
        public BoolValueObject OutputSpeakerBEnable { get; private set; }


        [Output(DisplayOrder = 2, IsRequired = false, IsDefaultShown = false)]
        public StringValueObject ErrorMessage { get; private set; }


        private ITypeService typeService;
        private UdpClient udpReceiverClient;
        private IPEndPoint ipEndpointAvrControl;

        public RunMacro(INodeContext context) : base(context)
        {
            context.ThrowIfNull("context");
            this.typeService = context.GetService<ITypeService>();
            this.AvrControlIp = typeService.CreateString(PortTypes.String, "IP of AVR Control", "192.168.17.14");
            this.MacroName = this.typeService.CreateString(PortTypes.String, "Macroname", "KITCHEN_RADIO");
            this.MacroName.MaxLength = 20;            
            this.InputPower = this.typeService.CreateBool(PortTypes.Binary, "Power Switch", false);
            this.OutputPower = this.typeService.CreateBool(PortTypes.Binary, "Power Status", false);
            this.VolumeRelativ = this.typeService.CreateBool(PortTypes.Binary, "Volume relative", false);
            this.SpeakerBEnable = this.typeService.CreateBool(PortTypes.Binary, "Speaker B enable", false);
            this.OutputSpeakerBEnable = this.typeService.CreateBool(PortTypes.Binary, "Speaker B status", false);
            this.ErrorMessage = this.typeService.CreateString(PortTypes.String, "Errormessage", "");
        }

        public void ReceiveCallback(IAsyncResult ar)
        {
            byte[] receiveBytes = udpReceiverClient.EndReceive(ar, ref ipEndpointAvrControl);
            string receiveString = Encoding.ASCII.GetString(receiveBytes);
            if (receiveString.StartsWith("POWER:")) 
            {
                // POWER:00000::off
                OutputPower.Value = receiveString.EndsWith("on");
            } else if (receiveString.StartsWith("SPEAKER_B:")) 
            {
                // SPEAKER_B:00001::on
                OutputSpeakerBEnable.Value = receiveString.EndsWith("on");
            }
            else if (receiveString.StartsWith("MUTE:"))
            {
                // OutputMute.Value = receiveString.EndsWith("on");
            }
            else
            {
                ErrorMessage.Value = "Unkown status from AVR: '" + receiveString + "'";
            }

            // needed?
            udpReceiverClient.BeginReceive(new AsyncCallback(ReceiveCallback), null);
        }

        
        public override void Startup()
        {
            // Receive a message and write it to the console.
            ipEndpointAvrControl = new IPEndPoint(IPAddress.Parse("0.0.0.0") , 14000);
            this.udpReceiverClient = new UdpClient(ipEndpointAvrControl);
            udpReceiverClient.BeginReceive(new AsyncCallback(ReceiveCallback), null);
        }
   

        public override void Execute()
        {
            if (this.MacroName.HasValue && this.MacroName.WasSet)
            {
                SendCommand("macro " + this.MacroName.Value);
            }
            if (this.InputPower.HasValue && this.InputPower.WasSet)
            {
                if (this.InputPower.Value == true)
                {
                    SendCommand("avr power on");
                }
                else
                {
                    SendCommand("avr power off");
                    // SendCommand("macro KITCHEN_RADIO_OFF");
                }
            }
            if (this.VolumeRelativ.HasValue && this.VolumeRelativ.WasSet)
            {
                if (this.VolumeRelativ.Value)
                {
                    SendCommand("AVR VOLUP");
                } else
                {
                    SendCommand("AVR VOLDOWN");
                }
            }
            if (this.SpeakerBEnable.HasValue && this.SpeakerBEnable.WasSet)
            {
                if (this.SpeakerBEnable.Value)
                {
                    SendCommand("AVR SPEAKER B ON");
                }
                else
                {
                    SendCommand("AVR SPEAKER B OFF");
                }
            }
        }

        public void SendCommand(String command)
        {
            if (!AvrControlIp.HasValue)
            {
                return;
            }

            if (!command.EndsWith("\r"))
            {
                command += "\r";
            }

            // send command per UDP to port 14000
            UdpClient udpClient = new UdpClient();
            Byte[] sendBytes = Encoding.ASCII.GetBytes(command);
            try
            {
                udpClient.Send(sendBytes, sendBytes.Length, AvrControlIp.Value, 14000);
            }
            catch (Exception e)
            {
                ErrorMessage.Value = e.ToString();
                // Console.WriteLine(e.ToString());
            }
        }

        public override ValidationResult Validate(string language)
        {
            return base.Validate(language);
        }

    }
}
