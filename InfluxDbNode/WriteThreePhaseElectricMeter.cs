using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Threading;
using LogicModule.Nodes.Helpers;
using LogicModule.ObjectModel;
using LogicModule.ObjectModel.TypeSystem;
using System.Globalization;

namespace alram_lechner_gmx_at.logic.InfluxDb2
{

    public class WriteThreePhaseElectricMeter : LogicNodeBase
    {
        // Parameter
        [Parameter(DisplayOrder = 1, IsRequired = true, IsDefaultShown = false)]
        public StringValueObject InfluxDbUrl { get; private set; }

        [Parameter(DisplayOrder = 2, IsRequired = true, IsDefaultShown = true)]
        public StringValueObject InfluxMeasureName { get; private set; }

        [Parameter(DisplayOrder = 3, IsRequired = true, IsDefaultShown = true)]
        public StringValueObject InfluxMeasureTags { get; private set; }
        
        // Input
        [Input(DisplayOrder = 7, IsInput = true, IsRequired = true)]
        public DoubleValueObject L1MainMeterValue { get; private set; }

        [Input(DisplayOrder = 8, IsInput = true, IsRequired = true)]
        public DoubleValueObject L2MainMeterValue { get; private set; }

        [Input(DisplayOrder = 9, IsInput = true, IsRequired = true)]
        public DoubleValueObject L3MainMeterValue { get; private set; }

        [Input(DisplayOrder = 10, IsInput = true, IsRequired = true)]
        public DoubleValueObject L1CurrentPowerValue { get; private set; }

        [Input(DisplayOrder = 11, IsInput = true, IsRequired = true)]
        public DoubleValueObject L2CurrentPowerValue { get; private set; }

        [Input(DisplayOrder = 12, IsInput = true, IsRequired = true)]
        public DoubleValueObject L3CurrentPowerValue { get; private set; }

        [Input(DisplayOrder = 13, IsInput = true, IsRequired = true)]
        public DoubleValueObject L1DailyMeterValue { get; private set; }

        [Input(DisplayOrder = 14, IsInput = true, IsRequired = true)]
        public DoubleValueObject L2DailyMeterValue { get; private set; }

        [Input(DisplayOrder = 15, IsInput = true, IsRequired = true)]
        public DoubleValueObject L3DailyMeterValue { get; private set; }

        // Output
        [Output(DisplayOrder = 2, IsRequired = false, IsDefaultShown = false)]
        public BoolValueObject L1ResetDailyMeterCounter { get; private set; }

        [Output(DisplayOrder = 3, IsRequired = false, IsDefaultShown = false)]
        public BoolValueObject L2ResetDailyMeterCounter { get; private set; }

        [Output(DisplayOrder = 4, IsRequired = false, IsDefaultShown = false)]
        public BoolValueObject L3ResetDailyMeterCounter { get; private set; }

        [Output(DisplayOrder = 5, IsRequired = false, IsDefaultShown = false)]
        public IntValueObject ErrorCode { get; private set; }

        [Output(DisplayOrder = 6, IsRequired = false, IsDefaultShown = false)]
        public StringValueObject ErrorMessage { get; private set; }

        private ITypeService TypeService = null;

        private double[] LastDailyMeterCounterValueSent = new double[] { -1, -1, -1 };
        private DateTime[] LastDailyMeterCounterValueTime = new DateTime[] { new DateTime(2010, 1, 1), new DateTime(2010, 1, 1), new DateTime(2010, 1, 1) };

        public WriteThreePhaseElectricMeter(INodeContext context) : base(context)
        {
            context.ThrowIfNull("context");
            this.TypeService = context.GetService<ITypeService>();
            this.InfluxDbUrl = TypeService.CreateString(PortTypes.String, "Influx DB URL", "http://<hostname>:<port>/write?db=<database>");
            this.InfluxMeasureName = TypeService.CreateString(PortTypes.String, "Measure name", "sensor");
            this.InfluxMeasureTags = TypeService.CreateString(PortTypes.String, "Tags", "room=kitchen,device=washingmachine");

            this.L1MainMeterValue = TypeService.CreateDouble(PortTypes.Number, "L1 Main counter (kWh)");
            this.L1CurrentPowerValue = TypeService.CreateDouble(PortTypes.Number, "L1 Current power (W)");
            this.L1DailyMeterValue = TypeService.CreateDouble(PortTypes.Number, "L1 Daily counter (Wh)");
            this.L1ResetDailyMeterCounter = TypeService.CreateBool(PortTypes.Bool, "L1 Reset daily counter", false);

            this.L2MainMeterValue = TypeService.CreateDouble(PortTypes.Number, "L2 Main counter (kWh)");
            this.L2CurrentPowerValue = TypeService.CreateDouble(PortTypes.Number, "L2 Current power (W)");
            this.L2DailyMeterValue = TypeService.CreateDouble(PortTypes.Number, "L2 Daily counter (Wh)");
            this.L2ResetDailyMeterCounter = TypeService.CreateBool(PortTypes.Bool, "L2 Reset daily counter", false);

            this.L3MainMeterValue = TypeService.CreateDouble(PortTypes.Number, "L3 Main counter (kWh)");
            this.L3CurrentPowerValue = TypeService.CreateDouble(PortTypes.Number, "L3 Current power (W)");
            this.L3DailyMeterValue = TypeService.CreateDouble(PortTypes.Number, "L3 Daily counter (Wh)");
            this.L3ResetDailyMeterCounter = TypeService.CreateBool(PortTypes.Bool, "L3 Reset daily counter", false);

            this.ErrorCode = TypeService.CreateInt(PortTypes.Integer, "HTTP status-code");
            this.ErrorMessage = TypeService.CreateString(PortTypes.String, "Error message");
        }

        public override void Startup()
        {
        }

        public override ValidationResult Validate(string language)
        {
            return base.Validate(language);
        }

        public override void Execute()
        {
            if (!InfluxDbUrl.HasValue || !InfluxMeasureName.HasValue)
            {
                return;
            }

            bool writeAggregatedPower = false;
            bool writeAggregatedDailyMeter = false;
            bool writeAggregatedMainMeter = false;

            // L1
            if (this.L1CurrentPowerValue.HasValue && this.L1CurrentPowerValue.WasSet)
            {
                WriteDatapointAsync("power", "phase=L1", this.L1CurrentPowerValue.Value);
                writeAggregatedPower = true;
            }

            if (this.L1MainMeterValue.HasValue && this.L1MainMeterValue.WasSet)
            {
                WriteDatapointAsync("meter", "phase=L1", this.L1MainMeterValue.Value);
                writeAggregatedMainMeter = true;
            }

            if (this.L1DailyMeterValue.HasValue && this.L1DailyMeterValue.WasSet)
            {
                // check if same value has been sent a few seconds before ...
                if (!(LastDailyMeterCounterValueSent[0] == this.L1DailyMeterValue.Value 
                    && DateTime.Compare(LastDailyMeterCounterValueTime[0], DateTime.Now.Subtract(TimeSpan.FromSeconds(10))) > 0))
                {
                    WriteDatapointAsync("intermediatecounter", "phase=L1", this.L1DailyMeterValue.Value);
                    LastDailyMeterCounterValueSent[0] = this.L1DailyMeterValue.Value;
                    LastDailyMeterCounterValueTime[0] = DateTime.Now;
                    writeAggregatedDailyMeter = true;
                }
                if (this.L1DailyMeterValue.Value > 0)
                {
                    this.L1ResetDailyMeterCounter.Value = true;
                }
            }

            // L2
            if (this.L2CurrentPowerValue.HasValue && this.L2CurrentPowerValue.WasSet)
            {
                WriteDatapointAsync("power", "phase=L2", this.L2CurrentPowerValue.Value);
                writeAggregatedPower = true;
            }

            if (this.L2MainMeterValue.HasValue && this.L2MainMeterValue.WasSet)
            {
                WriteDatapointAsync("meter", "phase=L2", this.L2MainMeterValue.Value);
                writeAggregatedMainMeter = true;
            }

            if (this.L2DailyMeterValue.HasValue && this.L2DailyMeterValue.WasSet)
            {
                // check if same value has been sent a few seconds before ...
                if (!(LastDailyMeterCounterValueSent[1] == this.L2DailyMeterValue.Value
                    && DateTime.Compare(LastDailyMeterCounterValueTime[1], DateTime.Now.Subtract(TimeSpan.FromSeconds(10))) > 0))
                {
                    WriteDatapointAsync("intermediatecounter", "phase=L2", this.L2DailyMeterValue.Value);
                    LastDailyMeterCounterValueSent[1] = this.L2DailyMeterValue.Value;
                    LastDailyMeterCounterValueTime[1] = DateTime.Now;
                    writeAggregatedDailyMeter = true;
                }

                if (this.L2DailyMeterValue.Value > 0)
                {
                    this.L2ResetDailyMeterCounter.Value = true;
                }
            }

            // L3
            if (this.L3CurrentPowerValue.HasValue && this.L3CurrentPowerValue.WasSet)
            {
                WriteDatapointAsync("power", "phase=L3", this.L3CurrentPowerValue.Value);
                writeAggregatedPower = true;
            }

            if (this.L3MainMeterValue.HasValue && this.L3MainMeterValue.WasSet)
            {
                WriteDatapointAsync("meter", "phase=L3", this.L3MainMeterValue.Value);
                writeAggregatedMainMeter = true;
            }

            if (this.L3DailyMeterValue.HasValue && this.L3DailyMeterValue.WasSet)
            {
                // check if same value has been sent a few seconds before ...
                if (!(LastDailyMeterCounterValueSent[2] == this.L3DailyMeterValue.Value
                    && DateTime.Compare(LastDailyMeterCounterValueTime[2], DateTime.Now.Subtract(TimeSpan.FromSeconds(10))) > 0))
                {
                    WriteDatapointAsync("intermediatecounter", "phase=L3", this.L3DailyMeterValue.Value);
                    LastDailyMeterCounterValueSent[2] = this.L3DailyMeterValue.Value;
                    LastDailyMeterCounterValueTime[2] = DateTime.Now;
                }

                if (this.L3DailyMeterValue.Value > 0)
                {
                    this.L3ResetDailyMeterCounter.Value = true;
                }
                writeAggregatedDailyMeter = true;
            }

            // aggregated values:
            if (writeAggregatedPower)
            {
                WriteDatapointAsync("power", "phase=ALL", this.L1CurrentPowerValue.Value + this.L2CurrentPowerValue.Value + this.L3CurrentPowerValue.Value);
            }

            if (writeAggregatedMainMeter)
            {
                WriteDatapointAsync("meter", "phase=ALL", this.L1MainMeterValue.Value + this.L2MainMeterValue.Value + this.L3MainMeterValue.Value);
            }

            if (writeAggregatedDailyMeter)
            {
                WriteDatapointAsync("intermediatecounter", "phase=ALL", this.L1DailyMeterValue.Value + this.L2DailyMeterValue.Value + this.L3DailyMeterValue.Value);
            }
        }

        public void WriteDatapointAsync(String fieldName, String additionalTag, double value)
        {
            var thread = new Thread(() =>
            {
                String tags = this.InfluxMeasureTags.HasValue ? this.InfluxMeasureTags.Value : "";
                tags += "," + additionalTag;
                InfluxWriterHelper.WriteDatapointSync(
                    this.InfluxDbUrl.Value,
                    this.InfluxMeasureName.Value,
                    tags,
                    fieldName,
                    value,
                    (errorCode, errorMessage) =>
                    {
                        if (errorCode != null)
                        {
                            ErrorCode.Value = errorCode.Value;
                        }
                        if (errorMessage != null)
                        {
                            ErrorMessage.Value = errorMessage;
                        }
                    });
            });
            thread.Start();
        }
    }

}
