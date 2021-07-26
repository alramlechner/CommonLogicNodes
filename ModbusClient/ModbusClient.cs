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
    static class FunctionCodeEnum
    {
        public const string FC_03 = "Read Holding Registers (03)";
        public const string FC_04 = "Read Input Registers (04)";
        public static string[] VALUES = new[] { FC_03, FC_04 };
    }
    static class DataTypeEnum
    {
        public const string INT16_UNSIGNED = "16bit integer";
        public const string INT16_SIGNED = "16bit integer (signed)";
        public const string INT32 = "integer (32bit)";
        public const string FLOAT = "float (32bit)";
        public const string LONG = "long (64bit)";
        public const string DOUBLE = "double (64bit)";

        public static string[] VALUES = new[] { INT16_SIGNED,  INT16_UNSIGNED, INT32, FLOAT, LONG, DOUBLE };
    }

    static class ByteOrderEnum
    {
        public const string HIGH_LOW = "big-endian";
        public const string LOW_HIGH = "little-endian";

        public static string[] VALUES = new[] { HIGH_LOW, LOW_HIGH };
    }

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
        // [Parameter(DisplayOrder = 6, InitOrder = 6, IsDefaultShown = false)]
        // public IntValueObject ReadCount1 { get; private set; }

        [Parameter(DisplayOrder = 7, InitOrder = 7, IsDefaultShown = false)]
        public EnumValueObject FunctionCode { get; private set; }

        [Parameter(DisplayOrder = 8, InitOrder = 8, IsDefaultShown = false)]
        public EnumValueObject DataType { get; private set; }

        [Parameter(DisplayOrder = 9, InitOrder = 9, IsDefaultShown = false)]
        public EnumValueObject RegisterOrder { get; private set; }

        [Output]
        public DoubleValueObject OutputValue1 { get; private set; }
        [Output]
        public StringValueObject ErrorMessage { get; private set; }

        private ISchedulerService SchedulerService;

        public ModbusClientNode(INodeContext context)
        {
            context.ThrowIfNull("context");
            ITypeService typeService = context.GetService<ITypeService>();

            this.TimeSpan = typeService.CreateInt(PortTypes.Integer, "Abfrageinterval", 60);
            this.ModbusHost = typeService.CreateString(PortTypes.String, "Modbus TCP Host");
            this.ModbusPort = typeService.CreateInt(PortTypes.Integer, "Port", 502);
            this.ModbusID = typeService.CreateInt(PortTypes.Integer, "Geräte ID", 1);
            this.ModbusID.MinValue = 1;
            this.ModbusID.MaxValue = 256;

            // --------------------------------------------------------------------------------------- //
            this.ModbusAddress1 = typeService.CreateInt(PortTypes.Integer, "Register Addresse", 1);
            this.ModbusAddress1.MinValue = 1;
            this.ModbusAddress1.MaxValue = 65535;

            this.FunctionCode = typeService.CreateEnum("ModbusFunction", "Funktion", FunctionCodeEnum.VALUES, FunctionCodeEnum.FC_03);

            this.DataType = typeService.CreateEnum("ModbusDataType", "Datentyp", DataTypeEnum.VALUES, DataTypeEnum.INT32);

            this.RegisterOrder = typeService.CreateEnum("ModbusRegisterOrder", "Register Reihenfolge", ByteOrderEnum.VALUES, ByteOrderEnum.LOW_HIGH);

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
                int registerToRead;
                switch(DataType.Value)
                {
                    case DataTypeEnum.INT32:
                    case DataTypeEnum.FLOAT:
                        registerToRead = 2;
                        break;
                    case DataTypeEnum.LONG:
                    case DataTypeEnum.DOUBLE:
                        registerToRead = 4;
                        break;
                    case DataTypeEnum.INT16_SIGNED:
                    case DataTypeEnum.INT16_UNSIGNED:
                    default:
                        registerToRead = 1;
                        break;
                }
                ModbusClient.RegisterOrder regOrder;
                if (!RegisterOrder.HasValue || RegisterOrder.Value == ByteOrderEnum.LOW_HIGH)
                {
                    regOrder = ModbusClient.RegisterOrder.LowHigh;
                } else
                {
                    regOrder = ModbusClient.RegisterOrder.HighLow;
                }
                ModbusClient modbusClient = null;
                try
                {
                    modbusClient = new ModbusClient(ModbusHost.Value, ModbusPort.Value);
                    modbusClient.ConnectionTimeout = 5000;
                    modbusClient.Connect();
                    modbusClient.UnitIdentifier = (byte)ModbusID.Value;

                    int[] readHoldingRegisters;
                    switch (FunctionCode.Value)
                    {
                        case FunctionCodeEnum.FC_04:
                            readHoldingRegisters = modbusClient.ReadInputRegisters(ModbusAddress1.Value, registerToRead);
                            break;
                        case FunctionCodeEnum.FC_03:
                        default:
                            readHoldingRegisters = modbusClient.ReadHoldingRegisters(ModbusAddress1.Value, registerToRead);
                            break;
                    }

                    double result = 0;
                    string result_str = "";

                    switch (DataType.Value)
                    {
                        case DataTypeEnum.INT32:
                            // probably signed ...
                            result = ModbusClient.ConvertRegistersToInt(readHoldingRegisters, regOrder);
                            break;
                        case DataTypeEnum.FLOAT:
                            result = ModbusClient.ConvertRegistersToFloat(readHoldingRegisters, regOrder);
                            break;
                        case DataTypeEnum.LONG:
                            result = ModbusClient.ConvertRegistersToLong(readHoldingRegisters, regOrder);
                            break;
                        case DataTypeEnum.DOUBLE:
                            result = ModbusClient.ConvertRegistersToDouble(readHoldingRegisters, regOrder);
                            break;
                        case DataTypeEnum.INT16_SIGNED:
                            result = readHoldingRegisters[0];
                            break;
                        case DataTypeEnum.INT16_UNSIGNED:
                            // unsigned
                            for (int i = 0; i < (readHoldingRegisters.Length); i++)
                            {
                                int tmp = readHoldingRegisters[i];
                                if (tmp == -32768) // fix for 0x00
                                    tmp = 0;
                                if (tmp < 0) // no negative values !
                                    tmp = tmp + (int)Math.Pow(2, 16);

                                result = result + (tmp * Math.Pow(2, (16 * ((readHoldingRegisters.Length) - (i + 1)))));
                                result_str = result_str + " 0x" + tmp.ToString("X4");
                            }
                            break;
                        default:
                            result_str = "internal: invalid datatype";
                            break;
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
