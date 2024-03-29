﻿using EasyModbus;
using LogicModule.Nodes.Helpers;
using LogicModule.ObjectModel;
using LogicModule.ObjectModel.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace alram_lechner_gmx_at.logic.HuaweiModbus
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

    public class HuaweiModbusClientNode : LogicNodeBase
    {

        [Parameter(DisplayOrder = 1, InitOrder = 1, IsDefaultShown = false)]
        public IntValueObject TimeSpan { get; private set; }

        [Parameter(DisplayOrder = 2, InitOrder = 2, IsDefaultShown = false)]
        public StringValueObject ModbusHost { get; private set; }
        [Parameter(DisplayOrder = 3, InitOrder = 3, IsDefaultShown = false)]
        public IntValueObject ModbusPort { get; private set; }

        [Input(DisplayOrder = 1, IsInput = true, IsRequired = false)]
        public DoubleValueObject chargePowerMax { get; private set; }

        [Input(DisplayOrder = 2, IsInput = true, IsRequired = false)]
        public DoubleValueObject dischargePowerMax { get; private set; }

        [Input(DisplayOrder = 3, IsInput = true, IsRequired = false)]
        public DoubleValueObject chargingCutoff { get; private set; }

        [Input(DisplayOrder = 4, IsInput = true, IsRequired = false)]
        public DoubleValueObject dischargingCutoff { get; private set; }

        [Output]
        public DoubleValueObject currentPVPower { get; private set; }

        [Output]
        public DoubleValueObject currentACPower { get; private set; }

        [Output]
        public DoubleValueObject currentGridPower { get; private set; }

        [Output]
        public DoubleValueObject currentBatteryPower { get; private set; }

        [Output]
        public DoubleValueObject todayPVEnergy { get; private set; }

        [Output]
        public DoubleValueObject totalPVEnergy { get; private set; }

        [Output]
        public DoubleValueObject inverterTemperature { get; private set; }

        [Output]
        public DoubleValueObject mppt1Voltage { get; private set; }

        [Output]
        public DoubleValueObject mppt1Current { get; private set; }

        [Output]
        public DoubleValueObject mppt2Voltage { get; private set; }

        [Output]
        public DoubleValueObject mppt2Current { get; private set; }

        [Output]
        public DoubleValueObject totalGridImportedEnergy { get; private set; }

        [Output]
        public DoubleValueObject totalGridExportedEnergy { get; private set; }

        [Output]
        public DoubleValueObject currentBatterySOC { get; private set; }

        [Output]
        public DoubleValueObject todaysPeakPVPower { get; private set; }

        [Output]
        public DoubleValueObject currentReactivePower { get; private set; }

        [Output]
        public DoubleValueObject currentBatteryStatus { get; private set; }

        [Output]
        public DoubleValueObject todayBatteryChargedEnergy { get; private set; }

        [Output]
        public DoubleValueObject todayBatteryDischargedEnergy { get; private set; }

        [Output]
        public DoubleValueObject batteryTemperature { get; private set; }

        [Output]
        public StringValueObject ErrorMessage { get; private set; }

        private ISchedulerService SchedulerService;

        public HuaweiModbusClientNode(INodeContext context)
        {
            context.ThrowIfNull("context");
            ITypeService typeService = context.GetService<ITypeService>();

            this.TimeSpan = typeService.CreateInt(PortTypes.Integer, "Abfrageinterval", 60);
            this.ModbusHost = typeService.CreateString(PortTypes.String, "Modbus TCP Host");
            this.ModbusPort = typeService.CreateInt(PortTypes.Integer, "Port", 502);

            this.chargePowerMax = typeService.CreateDouble(PortTypes.Number, "Max. battery charge power (W)");
            this.dischargePowerMax = typeService.CreateDouble(PortTypes.Number, "Max. battery discharge power (W)");
            this.chargingCutoff = typeService.CreateDouble(PortTypes.Number, "Charging cutoff capacity (%)");
            this.dischargingCutoff = typeService.CreateDouble(PortTypes.Number, "Discharging cutoff capacity (%)");

            this.currentPVPower = typeService.CreateDouble(PortTypes.Number, "Current PV power (inverter)");
            this.currentACPower = typeService.CreateDouble(PortTypes.Number, "Current AC power (inverter)");
            this.currentGridPower = typeService.CreateDouble(PortTypes.Number, "Current grid power (smartmeter)");
            this.currentBatteryPower = typeService.CreateDouble(PortTypes.Number, "Current battery power (inverter)");
            this.todayPVEnergy = typeService.CreateDouble(PortTypes.Number, "Today PV energy");
            this.totalPVEnergy = typeService.CreateDouble(PortTypes.Number, "Total PV energy");
            this.inverterTemperature = typeService.CreateDouble(PortTypes.Number, "Inverter temperature");
            this.mppt1Voltage = typeService.CreateDouble(PortTypes.Number, "MPPT 1 voltage");
            this.mppt1Current = typeService.CreateDouble(PortTypes.Number, "MPPT 1 current");
            this.mppt2Voltage = typeService.CreateDouble(PortTypes.Number, "MPPT 2 voltage");
            this.mppt2Current = typeService.CreateDouble(PortTypes.Number, "MPPT 2 current");
            this.totalGridImportedEnergy = typeService.CreateDouble(PortTypes.Number, "Total energy imported (smartmeter)");
            this.totalGridExportedEnergy = typeService.CreateDouble(PortTypes.Number, "Total energy exported (smartmeter)");
            this.currentBatterySOC = typeService.CreateDouble(PortTypes.Number, "Current battery SoC");
            this.todaysPeakPVPower = typeService.CreateDouble(PortTypes.Number, "Today PV peak power");
            this.currentReactivePower = typeService.CreateDouble(PortTypes.Number, "Current reactive power");
            this.currentBatteryStatus = typeService.CreateDouble(PortTypes.Number, "Current battery status");
            this.todayBatteryChargedEnergy = typeService.CreateDouble(PortTypes.Number, "Today battery charged energy");
            this.todayBatteryDischargedEnergy = typeService.CreateDouble(PortTypes.Number, "Today battery discharged engergy");
            this.batteryTemperature = typeService.CreateDouble(PortTypes.Number, "Battery temperature");

            this.ErrorMessage = typeService.CreateString(PortTypes.String, "RAW / Error");
            SchedulerService = context.GetService<ISchedulerService>();
        }
        public override void Startup()
        {
            this.SchedulerService.InvokeIn(new TimeSpan(0, 0, TimeSpan.Value), FetchFromModbusServer);
        }

        public override void Execute()
        {
            if ((this.chargePowerMax.HasValue && this.chargePowerMax.WasSet))
            {
                writeRegister(47075, (int)this.chargePowerMax.Value, DataTypeEnum.INT32);
            }
            if ((this.dischargePowerMax.HasValue && this.dischargePowerMax.WasSet))
            {
                writeRegister(47077, (int)this.dischargePowerMax.Value, DataTypeEnum.INT32);
            }
            if ((this.chargingCutoff.HasValue && this.chargingCutoff.WasSet))
            {
                writeRegister(47081, (int)this.chargingCutoff.Value, DataTypeEnum.INT16_UNSIGNED);
            }
            if ((this.dischargingCutoff.HasValue && this.dischargingCutoff.WasSet))
            {
                writeRegister(47082, (int)this.dischargingCutoff.Value, DataTypeEnum.INT16_UNSIGNED);
            }
        }

        private void writeRegister(int register, int value, String dataType)
        {
            ModbusClient modbusClient = null;
            try
            {
                modbusClient = new ModbusClient(ModbusHost.Value, ModbusPort.Value);
                modbusClient.ConnectionTimeout = 5000;
                modbusClient.Connect();
                modbusClient.UnitIdentifier = 1;

                // needed?
                System.Threading.Thread.Sleep(700);
                switch (dataType)
                {
                    case DataTypeEnum.INT32:
                        int[] toWrite = ModbusClient.ConvertIntToRegisters(value, ModbusClient.RegisterOrder.HighLow);
                        modbusClient.WriteMultipleRegisters(register, toWrite);
                        break;
                    case DataTypeEnum.INT16_UNSIGNED:
                        modbusClient.WriteSingleRegister(register, value);
                        break;
                    default:
                        this.ErrorMessage.Value = "INTERNAL: unsupported datatype";
                        break;
                }
            }
            catch (Exception e)
            {
                this.ErrorMessage.Value = e.ToString();
            }
            finally
            {
                if (modbusClient != null)
                {
                    modbusClient.Disconnect();
                }
            }
        }

    private int readRegister(ModbusClient modbusClient, int startRegister, String dataType)
        {
            ModbusClient.RegisterOrder regOrder;
            regOrder = ModbusClient.RegisterOrder.HighLow;

            int registerToRead;
            switch(dataType)
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

            int[] readHoldingRegisters;
            int retry = 5;

            while(true)
                try
                {
                    readHoldingRegisters = modbusClient.ReadHoldingRegisters(startRegister, registerToRead);
                    break;
                } catch (System.IO.IOException e)
                {
                    retry--;
                    if (retry == 0)
                    {
                        return -1;
                    }
                    System.Threading.Thread.Sleep(500);
                }

            double result = 0;
            string result_str = "";

            switch (dataType)
            {
                case DataTypeEnum.INT32:
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

            ErrorMessage.Value += result_str + ";";
            return (int)result;
        }

        private void FetchFromModbusServer()
        {
            Thread thread1 = new Thread(FetchFromModbusServerAsync);
            thread1.Start();
        }

        private void FetchFromModbusServerAsync()
        {
            ErrorMessage.Value = "";
            if (ModbusHost.HasValue)
            {
                ModbusClient modbusClient = null;
                try
                {
                    modbusClient = new ModbusClient(ModbusHost.Value, ModbusPort.Value);
                    modbusClient.ConnectionTimeout = 5000;
                    modbusClient.Connect();
                    modbusClient.UnitIdentifier = 1;

                    // see: https://knx-user-forum.de/forum/%C3%B6ffentlicher-bereich/knx-eib-forum/1643359-gira-x1-und-modbus-tcp-mit-logikbaustein/page7#post1844442
                    System.Threading.Thread.Sleep(700);

                    this.mppt1Voltage.Value = readRegister(modbusClient, 32016, DataTypeEnum.INT16_SIGNED) / 10.0;
                    this.mppt1Current.Value = readRegister(modbusClient, 32017, DataTypeEnum.INT16_SIGNED) / 100.0;
                    this.mppt2Voltage.Value = readRegister(modbusClient, 32018, DataTypeEnum.INT16_SIGNED) / 10.0;
                    this.mppt2Current.Value = readRegister(modbusClient, 32019, DataTypeEnum.INT16_SIGNED) / 100.0;
                    this.currentPVPower.Value = readRegister(modbusClient, 32064, DataTypeEnum.INT32);
                    this.todaysPeakPVPower.Value = readRegister(modbusClient, 32078, DataTypeEnum.INT32);
                    this.currentACPower.Value = readRegister(modbusClient, 32080, DataTypeEnum.INT32);
                    this.currentReactivePower.Value = readRegister(modbusClient, 32082, DataTypeEnum.INT32);
                    this.inverterTemperature.Value = readRegister(modbusClient, 32087, DataTypeEnum.INT16_SIGNED) / 10.0; 
                    this.totalPVEnergy.Value = readRegister(modbusClient, 32106, DataTypeEnum.INT32) / 100.0; // unsigned!
                    this.todayPVEnergy.Value = readRegister(modbusClient, 32114, DataTypeEnum.INT32) / 100.0; // unsigned!

                    this.currentBatteryStatus.Value = readRegister(modbusClient, 37000, DataTypeEnum.INT16_UNSIGNED);
                    this.currentBatteryPower.Value = readRegister(modbusClient, 37001, DataTypeEnum.INT32);
                    this.currentBatterySOC.Value = readRegister(modbusClient, 37004, DataTypeEnum.INT16_UNSIGNED) / 10.0;
                    this.todayBatteryChargedEnergy.Value = readRegister(modbusClient, 37015, DataTypeEnum.INT32); // unsigned!
                    this.todayBatteryDischargedEnergy.Value = readRegister(modbusClient, 37017, DataTypeEnum.INT32); // unsigned!
                    this.batteryTemperature.Value = readRegister(modbusClient, 37022, DataTypeEnum.INT16_SIGNED) / 10.0;
                    this.currentGridPower.Value = readRegister(modbusClient, 37113, DataTypeEnum.INT32);
                    this.totalGridExportedEnergy.Value = readRegister(modbusClient, 37119, DataTypeEnum.INT32) / 100.0;
                    this.totalGridImportedEnergy.Value = readRegister(modbusClient, 37121, DataTypeEnum.INT32) / 100.0;

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
