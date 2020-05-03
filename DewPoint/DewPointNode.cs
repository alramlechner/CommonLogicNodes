using LogicModule.Nodes.Helpers;
using LogicModule.ObjectModel;
using LogicModule.ObjectModel.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace alram_lechner_gmx_at.logic.DewPoint
{
    public class DewPointNode : LogicNodeBase
    {
        [Input(DisplayOrder = 1, IsInput = true, IsRequired = false)]
        public DoubleValueObject Temperature { get; private set; }

        [Input(DisplayOrder = 2, IsInput = true, IsRequired = false)]
        public DoubleValueObject Humidity { get; private set; }

        [Output(DisplayOrder = 1, IsRequired = true)]
        public DoubleValueObject DewPoint { get; private set; }

        private ITypeService typeService;

        public DewPointNode(INodeContext context) : base(context)
        {
            context.ThrowIfNull("context");

            this.typeService = context.GetService<ITypeService>();

            this.Temperature = this.typeService.CreateDouble(PortTypes.Temperature, "Temperatur (°C)");
            this.Temperature.MinValue = -248;
            this.Temperature.MaxValue = 2000;

            this.Humidity = this.typeService.CreateDouble(PortTypes.Float, "rel. Luftfeuchte (%)");
            this.Humidity.MinValue = 0;
            this.Humidity.MaxValue = 100;

            this.DewPoint = this.typeService.CreateDouble(PortTypes.Float, "Taupunkt (°C)");
        }

        public override void Startup()
        {
        }

        public override void Execute()
        {
            if (!this.Temperature.HasValue || !this.Humidity.HasValue)
            {
                DewPoint.BlockGraph();
                return;
            }
            DewPoint.Value = CalculateDewPoint(Temperature.Value, Humidity.Value);
        }

        /// <summary>
        /// TODO: good candiate for unit tests ...
        /// </summary>
        /// <param name="temperature">temperature (°C)</param>
        /// <param name="humidity">rel .humidity (%)</param>
        /// <returns>dew point (°C)</returns>
        public double CalculateDewPoint(double temperature, double humidity)
        {
            double a, b;
            if (temperature >= 0)
            {
                a = 7.5;
                b = 237.3;
            }
            else
            {
                a = 7.6;
                b = 240.7;
            }
            double sdd = 6.1078 * Math.Pow(10, (a * temperature) / (b + temperature));
            double dd = humidity / 100 * sdd;
            double dewPoint = b * Math.Log10(dd / 6.1078) / (a - Math.Log10(dd / 6.1078));
            return Math.Round(dewPoint, 2);
        }

        public override ValidationResult Validate(string language)
        {
            return base.Validate(language);
        }

    }
}
