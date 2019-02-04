using EasyModbus;
using LogicModule.Nodes.Helpers;
using LogicModule.ObjectModel;
using LogicModule.ObjectModel.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Name.Lechners.GiraSdk.Modbus
{

    public class ModbusClientNode : LogicNodeBase
    {

        [Parameter(DisplayOrder = 1, InitOrder = 1, IsDefaultShown = false)]
        public StringValueObject ModbusHost { get; private set; }

        [Parameter(DisplayOrder = 2, InitOrder = 2, IsDefaultShown = false)]
        public IntValueObject ModbusPort { get; private set; }

        [Parameter(DisplayOrder = 3, InitOrder = 3, IsDefaultShown = false)]
        public IntValueObject ModbusAddress { get; private set; }

        [Parameter(DisplayOrder = 4, InitOrder = 4, IsDefaultShown = false)]
        public IntValueObject ReadCount { get; private set; }

        [Output(DisplayOrder = 1)]
        public DoubleValueObject OutputValue { get; private set; }

        [Output(DisplayOrder = 2)]
        public StringValueObject ErrorMessage { get; private set; }

        private ISchedulerService SchedulerService;

        public ModbusClientNode(INodeContext context)
        {
            context.ThrowIfNull("context");
            ITypeService typeService = context.GetService<ITypeService>();
            this.ModbusHost = typeService.CreateString(PortTypes.String, "Modbus TCP Server");
            this.ModbusPort = typeService.CreateInt(PortTypes.Integer, "Port", 502);

            this.ModbusAddress = typeService.CreateInt(PortTypes.Integer, "Modbus Addresse");
            this.ModbusAddress.MinValue = 0;
            this.ModbusAddress.MaxValue = 65535;

            this.ReadCount = typeService.CreateInt(PortTypes.Integer, "Anzahl bytes");
            this.ReadCount.MinValue = 1;
            this.ReadCount.MaxValue = 2;

            this.OutputValue = typeService.CreateDouble(PortTypes.Float, "Wert");
            this.ErrorMessage = typeService.CreateString(PortTypes.String, "Error");

            SchedulerService = context.GetService<ISchedulerService>();
        }
        public override void Startup()
        {
            this.SchedulerService.InvokeIn(new TimeSpan(0, 0, 10), FetchFromModbusServer);
        }

        public override void Execute()
        {
        }

        private void FetchFromModbusServer()
        {
            ModbusClient modbusClient = null;
            try
            {
                modbusClient = new ModbusClient(ModbusHost.Value, ModbusPort.Value);
                modbusClient.Connect();
                OutputValue.Value = (modbusClient.ReadHoldingRegisters(ModbusAddress.Value, ReadCount.Value)[0] / 10.0);
                this.ErrorMessage.Value = "";
                this.SchedulerService.InvokeIn(new TimeSpan(0, 0, 10), FetchFromModbusServer);
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
