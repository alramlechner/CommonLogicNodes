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

namespace Name.Lechners.GiraSdk.InfluxDbNode
{

    public class Write : LogicNodeBase
    {
        [Parameter(DisplayOrder = 1, IsRequired = true, IsDefaultShown = false)]
        public StringValueObject InfluxDbHost { get; private set; }

        [Parameter(DisplayOrder = 2, IsRequired = true, IsDefaultShown = false)]
        public IntValueObject InfluxDbPort { get; private set; }

        [Parameter(DisplayOrder = 3, IsRequired = true, IsDefaultShown = false)]
        public StringValueObject InfluxDbName { get; private set; }

        [Parameter(DisplayOrder = 4, IsRequired = true, IsDefaultShown = true, AsTitle = true)]
        public StringValueObject InfluxMeasureName { get; private set; }

        [Parameter(DisplayOrder = 5, IsRequired = true, IsDefaultShown = false)]
        public StringValueObject InfluxDbMeasureTags { get; private set; }

        // TODO: maybe repeated?
        [Parameter(DisplayOrder = 6, IsRequired = true, IsDefaultShown = false)]
        public StringValueObject InfluxDbMeasureFieldName1 { get; private set; }

        [Input(DisplayOrder = 7, IsInput = true, IsRequired = true)]
        public DoubleValueObject InfluxDbMeasureFieldValue1 { get; private set; }

        [Parameter(DisplayOrder = 8, IsRequired = false, IsDefaultShown = false)]
        public StringValueObject InfluxDbMeasureFieldName2 { get; private set; }

        [Input(DisplayOrder = 9, IsInput = true, IsRequired = false)]
        public DoubleValueObject InfluxDbMeasureFieldValue2 { get; private set; }

        [Output(DisplayOrder = 1, IsRequired = false, IsDefaultShown = false)]
        public IntValueObject ErrorCode { get; private set; }

        [Output(DisplayOrder = 2, IsRequired = false, IsDefaultShown = false)]
        public StringValueObject ErrorMessage { get; private set; }

        public Write(INodeContext context) : base(context)
        {
            context.ThrowIfNull("context");
            ITypeService typeService = context.GetService<ITypeService>();
            this.InfluxDbHost = typeService.CreateString(PortTypes.String, "Influx DB host");
            this.InfluxDbPort = typeService.CreateInt(PortTypes.Integer, "Influx DB Port", 8086);
            this.InfluxDbName = typeService.CreateString(PortTypes.String, "Influx DB name");
            this.InfluxMeasureName = typeService.CreateString(PortTypes.String, "Measure name");
            this.InfluxDbMeasureTags = typeService.CreateString(PortTypes.String, "Measure tags");
            this.InfluxDbMeasureFieldName1 = typeService.CreateString(PortTypes.String, "Measure field name 1");
            this.InfluxDbMeasureFieldValue1 = typeService.CreateDouble(PortTypes.Float, "Measure value 1");
            this.InfluxDbMeasureFieldName2 = typeService.CreateString(PortTypes.String, "Measure field name 2");
            this.InfluxDbMeasureFieldValue2 = typeService.CreateDouble(PortTypes.Float, "Measure value 2");
            this.ErrorCode = typeService.CreateInt(PortTypes.Integer, "HTTP status-code");
            this.ErrorMessage = typeService.CreateString(PortTypes.String, "SMTP Benutzer");
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
            if (!InfluxDbHost.HasValue || !InfluxDbPort.HasValue || !InfluxDbName.HasValue || !InfluxMeasureName.HasValue 
                || !InfluxDbMeasureFieldName1.HasValue || !InfluxDbMeasureFieldValue1.HasValue)
            {
                return;
            }

            // HACK: only 2nd value trigger ...
            if (InfluxDbMeasureFieldName2.HasValue && !InfluxDbMeasureFieldValue2.WasSet)
            {
                return;
            }

            WriteDatapointAsync();
        }

        public void WriteDatapointAsync()
        {
            var thread = new Thread(() => {
                WriteDatapointSync(
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



        public void WriteDatapointSync(Action<int?, string> SetResultCallback)
        {
            String URL = "http://" + InfluxDbHost.Value + ":" + InfluxDbPort.Value + "/write?db=" + InfluxDbName + "&precision=s";
            String Body = InfluxMeasureName.Value;
            if (InfluxDbMeasureTags .HasValue)
            {
                Body += "," + InfluxDbMeasureTags.Value;
            }
            Body += " ";
            Body += InfluxDbMeasureFieldName1.Value + "=" + InfluxDbMeasureFieldValue1.Value;
            if (InfluxDbMeasureFieldName2.HasValue && InfluxDbMeasureFieldValue2.HasValue)
            {
                Body += "," + InfluxDbMeasureFieldName2.Value + "=" + InfluxDbMeasureFieldValue2.Value;
            }

            try
            {
            Uri uri = new Uri(URL);
            HttpWebRequest client = (HttpWebRequest)HttpWebRequest.Create(uri);
                client.Method = "POST";
                client.ContentType = "text/plain";
                using (var request = client.GetRequestStream())
                {
                    using (var writer = new StreamWriter(request))
                    {
                        writer.Write(Body);
                    }
                }

                var response = client.GetResponse();
                using (var result = response.GetResponseStream())
                {
                    using (var reader = new StreamReader(result))
                    {
                        SetResultCallback(null, null);
                    }
                }
            }
            catch (WebException e)
            {
                if (e.Response is HttpWebResponse errorResponse)
                {
                    try
                    {
                        using (var result = errorResponse.GetResponseStream())
                        {
                            using (var reader = new StreamReader(result))
                            {
                                SetResultCallback((int)errorResponse.StatusCode, reader.ReadToEnd());
                                return;
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
                SetResultCallback(998, "Unknown error");
            }
            catch (Exception e)
            {
                SetResultCallback(999, e.Message);
                return;
            }
        }


    }
}
