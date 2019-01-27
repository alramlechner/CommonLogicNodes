using LogicModule.Nodes.Helpers;
using LogicModule.ObjectModel;
using LogicModule.ObjectModel.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Name.Lechners.GiraSdk.Common
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

            double temp = Temperature.Value;
            double rel = Humidity.Value;

            double a, b;
            if (temp >= 0)
            {
                a = 7.5;
                b = 237.3;
            }
            else
            {
                a = 7.6;
                b = 240.7;
            }
            double sdd = 6.1078 * Math.Pow(10, (a * temp) / (b + temp));
            double dd = rel / 100 * sdd;
            double taupunkt = b * Math.Log10(dd / 6.1078) / (a - Math.Log10(dd / 6.1078));
            DewPoint.Value = Math.Round(taupunkt, 2);
        }

        public override ValidationResult Validate(string language)
        {
            return base.Validate(language);
        }

    }
}
