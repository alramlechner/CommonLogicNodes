using EasyModbus;
using LogicModule.Nodes.Helpers;
using LogicModule.ObjectModel;
using LogicModule.ObjectModel.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace alram_lechner_gmx_at.logic.Modbus
{

    public class ModbusClientNode : LogicNodeBase
    {

        [Parameter(DisplayOrder = 1, InitOrder = 1, IsDefaultShown = false)]
        public IntValueObject TimeSpan { get; private set; }

        [Parameter(DisplayOrder = 2, InitOrder = 2, IsDefaultShown = false)]
        public StringValueObject ModbusHost { get; private set; }
        [Parameter(DisplayOrder = 3, InitOrder = 3, IsDefaultShown = false)]
        public IntValueObject ModbusPort { get; private set; }
        [Parameter(DisplayOrder = 4, InitOrder = 4, IsDefaultShown = false)]
        public IntValueObject ModbusID { get; private set; }

        // Modbus Register
        [Parameter(DisplayOrder = 5, InitOrder = 5, IsDefaultShown = false)]
        public IntValueObject ModbusAddress1 { get; private set; }
        [Parameter(DisplayOrder = 6, InitOrder = 6, IsDefaultShown = false)]
        public IntValueObject ReadCount1 { get; private set; }

        [Output]
        public DoubleValueObject OutputValue1 { get; private set; }
        [Output]
        public StringValueObject ErrorMessage { get; private set; }

        private ISchedulerService SchedulerService;

        public ModbusClientNode(INodeContext context)
        {
            context.ThrowIfNull("context");
            ITypeService typeService = context.GetService<ITypeService>();

            this.TimeSpan = typeService.CreateInt(PortTypes.Integer, "Restart (Sek.)", 60);
            this.ModbusHost = typeService.CreateString(PortTypes.String, "Modbus TCP Server");
            this.ModbusPort = typeService.CreateInt(PortTypes.Integer, "Port", 502);
            this.ModbusID = typeService.CreateInt(PortTypes.Integer, "Modbus ID", 3);

            // --------------------------------------------------------------------------------------- //
            this.ModbusAddress1 = typeService.CreateInt(PortTypes.Integer, "Modbus Addresse", 0);
            this.ModbusAddress1.MinValue = 0;
            this.ModbusAddress1.MaxValue = 65535;
            this.ReadCount1 = typeService.CreateInt(PortTypes.Integer, "Anzahl words", 2);
            this.ReadCount1.MinValue = 1;
            this.ReadCount1.MaxValue = 2;

            this.OutputValue1 = typeService.CreateDouble(PortTypes.Number, "Register Wert");

            this.ErrorMessage = typeService.CreateString(PortTypes.String, "RAW / Error");

            SchedulerService = context.GetService<ISchedulerService>();
        }
        public override void Startup()
        {
            this.SchedulerService.InvokeIn(new TimeSpan(0, 0, TimeSpan.Value), FetchFromModbusServer);
        }

        public override void Execute()
        {
        }

        private void FetchFromModbusServer()
        {
            if (ModbusHost.HasValue && ModbusAddress1.Value > 0)
            {
                ModbusClient modbusClient = null;
                try
                {
                    modbusClient = new ModbusClient(ModbusHost.Value, ModbusPort.Value);
                    modbusClient.Connect();
                    modbusClient.UnitIdentifier = (byte)ModbusID.Value;

                    // Register 1
                    // read + 1 register ???!!! bug in the EasyModbus ????!!! lowest byte 0x00 !!!
                    // [0] Hugh Byte
                    // [1] Low Byte
                    // [2] Dummy (High Byte)
                    // [3] Dummy (Low Byte) = 0x00 in X1 (not in the X1 SImulation!)
                    int[] readHoldingRegisters = modbusClient.ReadHoldingRegisters(ModbusAddress1.Value, ReadCount1.Value + 2);

                    double result = 0;
                    string result_str = "";
                    //byte[] tmp;
                    for (int i = 0; i < (readHoldingRegisters.Length - 2); i++)
                    {
                        int tmp = readHoldingRegisters[i];
                        if (tmp == -32768) // fix for 0x00
                            tmp = 0;
                        if (tmp < 0) // no negative values !
                            tmp = tmp + (int)Math.Pow(2, 16);

                        result = result + (tmp * Math.Pow(2, (16 * ((readHoldingRegisters.Length - 2) - (i + 1)))));
                        result_str = result_str + " 0x" + tmp.ToString("X4");
                    }
                    OutputValue1.Value = result;
                    ErrorMessage.Value = result_str;

                    this.SchedulerService.InvokeIn(new TimeSpan(0, 0, TimeSpan.Value), FetchFromModbusServer);

                }
                catch (Exception e)
                {
                    this.ErrorMessage.Value = e.ToString();
                    this.SchedulerService.InvokeIn(new TimeSpan(0, 1, 0), FetchFromModbusServer);
                }
                finally
                {
                    if (modbusClient != null)
                    {
                        modbusClient.Disconnect();
                    }

                }

            }
        }
    }
}
