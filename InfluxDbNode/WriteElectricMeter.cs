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

    public class WriteElectricMeter : LogicNodeBase
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
        public DoubleValueObject MainMeterValue { get; private set; }

        [Input(DisplayOrder = 8, IsInput = true, IsRequired = true)]
        public DoubleValueObject CurrentPowerValue { get; private set; }

        [Input(DisplayOrder = 9, IsInput = true, IsRequired = true)]
        public DoubleValueObject DailyMeterValue { get; private set; }

        // Output
        [Output(DisplayOrder = 2, IsRequired = false, IsDefaultShown = false)]
        public BoolValueObject ResetDailyMeterCounter { get; private set; }

        [Output(DisplayOrder = 3, IsRequired = false, IsDefaultShown = false)]
        public IntValueObject ErrorCode { get; private set; }

        [Output(DisplayOrder = 4, IsRequired = false, IsDefaultShown = false)]
        public StringValueObject ErrorMessage { get; private set; }

        private ITypeService TypeService = null;

        private double LastDailyMeterCounterValueSent = -1;
        private DateTime LastDailyMeterCounterValueTime = new DateTime(2010, 1, 1);

        public WriteElectricMeter(INodeContext context) : base(context)
        {
            context.ThrowIfNull("context");
            this.TypeService = context.GetService<ITypeService>();
            this.InfluxDbUrl = TypeService.CreateString(PortTypes.String, "Influx DB URL", "http://<hostname>:<port>/write?db=<database>");
            this.InfluxMeasureName = TypeService.CreateString(PortTypes.String, "Measure name", "sensor");
            this.InfluxMeasureTags = TypeService.CreateString(PortTypes.String, "Tags", "room=kitchen,device=washingmachine");
            this.MainMeterValue = TypeService.CreateDouble(PortTypes.Number, "Main counter (kWh)");
            this.CurrentPowerValue = TypeService.CreateDouble(PortTypes.Number, "Current power (W)");
            this.DailyMeterValue = TypeService.CreateDouble(PortTypes.Number, "Daily counter (Wh)");
            this.ResetDailyMeterCounter = TypeService.CreateBool(PortTypes.Bool, "Reset daily counter", false);
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

            if (this.CurrentPowerValue.HasValue && this.CurrentPowerValue.WasSet)
            {
                WriteDatapointAsync("power", this.CurrentPowerValue.Value);
            }

            if (this.MainMeterValue.HasValue && this.MainMeterValue.WasSet)
            {
                WriteDatapointAsync("meter", this.MainMeterValue.Value);
            }

            if (this.DailyMeterValue.HasValue && this.DailyMeterValue.WasSet)
            {

                if (!(LastDailyMeterCounterValueSent == this.DailyMeterValue.Value
                    && DateTime.Compare(LastDailyMeterCounterValueTime, DateTime.Now.Subtract(TimeSpan.FromSeconds(10))) > 0))
                {
                    WriteDatapointAsync("intermediatecounter", this.DailyMeterValue.Value);
                    LastDailyMeterCounterValueSent = this.DailyMeterValue.Value;
                    LastDailyMeterCounterValueTime = DateTime.Now;
                } else
                {
                    // debugging only!
                    WriteDatapointAsync("ignored_intermediatecounter", this.DailyMeterValue.Value);
                }

                if (this.DailyMeterValue.Value > 0)
                {
                    this.ResetDailyMeterCounter.Value = true;
                }
            }

        }

        public void WriteDatapointAsync(String fieldName, double value)
        {
            var thread = new Thread(() =>
            {
                InfluxWriterHelper.WriteDatapointSync(
                    this.InfluxDbUrl.Value,
                    this.InfluxMeasureName.Value,
                    this.InfluxMeasureTags.HasValue ? this.InfluxMeasureTags.Value : null,
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
