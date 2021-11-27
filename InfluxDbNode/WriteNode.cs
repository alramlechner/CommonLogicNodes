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

    public class WriteNode : LogicNodeBase
    {
        [Parameter(DisplayOrder = 1, IsRequired = true, IsDefaultShown = false)]
        public StringValueObject InfluxDbUrl { get; private set; }

        [Parameter(DisplayOrder = 4, IsRequired = true, IsDefaultShown = true)]
        public StringValueObject InfluxMeasureName { get; private set; }

        [Parameter(DisplayOrder = 5, IsRequired = true, IsDefaultShown = true)]
        public StringValueObject InfluxMeasureTags { get; private set; }

        [Parameter(DisplayOrder = 6, IsRequired = true, IsDefaultShown = false)]
        public StringValueObject InfluxMeasureFieldName { get; private set; }

        [Input(DisplayOrder = 7, IsInput = true, IsRequired = true)]
        public DoubleValueObject InfluxMeasureFieldValue { get; private set; }

        [Output(DisplayOrder = 1, IsRequired = false, IsDefaultShown = false)]
        public IntValueObject ErrorCode { get; private set; }

        [Output(DisplayOrder = 2, IsRequired = false, IsDefaultShown = false)]
        public StringValueObject ErrorMessage { get; private set; }

        //IList<bool> changedInputs = new List<bool>();

        private ITypeService typeService = null;
        //private ISchedulerService schedulerService;
        //private SchedulerToken schedulerToken = null;

        public WriteNode(INodeContext context) : base(context)
        {
            context.ThrowIfNull("context");
            typeService = context.GetService<ITypeService>();
            // schedulerService = context.GetService<ISchedulerService>();
            this.InfluxDbUrl = typeService.CreateString(PortTypes.String, "Influx DB URL", "http://<hostname>:<port>/write?db=<database>");
            this.InfluxMeasureName = typeService.CreateString(PortTypes.String, "Measure name", "sensor");
            this.InfluxMeasureTags = typeService.CreateString(PortTypes.String, "Tags", "room=kitchen");
            // UpdateMeasureFieldCount(null, null);
            this.InfluxMeasureFieldName = typeService.CreateString(PortTypes.String, "Measure field name", "temp");
            this.InfluxMeasureFieldValue = typeService.CreateDouble(PortTypes.Number, "Measure value");
            this.ErrorCode = typeService.CreateInt(PortTypes.Integer, "HTTP status-code");
            this.ErrorMessage = typeService.CreateString(PortTypes.String, "Error message");
        }
        /*
        private void MeasureFieldCountUpdated(object sender, ValueChangedEventArgs args)
        {
            int desiredLength = MeasureFieldCount.Value;

            if (MeasureFields.Count < desiredLength * InputsPerField)
            {
                for (int i = MeasureFields.Count; i < desiredLength * InputsPerField; i++)
                {

                    switch (i % InputsPerField)
                    {
                        case 0:
                            IValueObject fieldName = typeService.CreateString(PortTypes.String, String.Format("Field name {0}", i / InputsPerField + 1), "");
                            MeasureFields.Add(fieldName);
                            break;
                        case 1:
                            IValueObject tags = typeService.CreateString(PortTypes.String, String.Format("Tags for {0}", (int)(i / InputsPerField + 1)));
                            MeasureFields.Add(tags);
                            break;
                        case 2:
                            IValueObject fieldValue = typeService.CreateDouble(PortTypes.Float, String.Format("Value for {0}", (int)(i / InputsPerField + 1)), 0);
                            MeasureFields.Add(fieldValue);
                            break;
                        default: break;
                    }
                }

            }
            else
            {
                while (MeasureFields.Count > desiredLength)
                {
                    MeasureFields.RemoveAt(MeasureFields.Count - 1);
                }
            }

            while (changedInputs.Count < desiredLength)
            {
                changedInputs.Add(false);
            }
            while (changedInputs.Count > desiredLength)
            {
                changedInputs.RemoveAt(0);
            }
        }
        */

        public override void Startup()
        {

        }

        public override ValidationResult Validate(string language)
        {
            return base.Validate(language);
        }

        public override void Execute()
        {
            // if (!InfluxDbHost.HasValue || !InfluxDbPort.HasValue || !InfluxDbName.HasValue || !InfluxMeasureName.HasValue)
            if (!InfluxDbUrl.HasValue || !InfluxMeasureName.HasValue ||!InfluxMeasureFieldName.HasValue || !InfluxMeasureFieldValue.HasValue)
            {
                return;
            }
            WriteDatapointAsync();
        }

        public void WriteDatapointAsync()
        {
            // schedulerToken = null;
            var thread = new Thread(() =>
            {
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
            // String URL = "http://" + InfluxDbHost.Value + ":" + InfluxDbPort.Value + "/write?db=" + InfluxDbName + "&precision=s";
            UriBuilder uriBuilder = new UriBuilder(InfluxDbUrl.Value);
            if (uriBuilder.Port == -1)
            {
                uriBuilder.Port = 8086;
            }
            String queryToAppend = "precision=s";
            if (uriBuilder.Query != null && uriBuilder.Query.Length > 1)
                uriBuilder.Query = uriBuilder.Query.Substring(1) + "&" + queryToAppend;
            else
                uriBuilder.Query = queryToAppend;

            // Open HTTP connection:
            String Body = "";
            try
            {
                HttpWebRequest client = (HttpWebRequest)HttpWebRequest.Create(uriBuilder.Uri);
                client.Method = "POST";
                client.ContentType = "text/plain";

                using (var request = client.GetRequestStream())
                {
                    using (var writer = new StreamWriter(request))
                    {
                        Body = InfluxMeasureName.Value;
                        if (InfluxMeasureTags.HasValue)
                        {
                            Body += "," + InfluxMeasureTags.Value;
                        }
                                
                        Body += " ";
                        Body += InfluxMeasureFieldName.Value + "=" + InfluxMeasureFieldValue.Value.ToString("G", CultureInfo.InvariantCulture);

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
                                SetResultCallback((int)errorResponse.StatusCode, reader.ReadToEnd() + "; Line was: " + Body);
                                return;
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
                SetResultCallback(998, "Unknown error" + "; Line was: " + Body);
            }
            catch (Exception e)
            {
                SetResultCallback(999, e.Message + "; Line was: " + Body);
                return;
            }
        }
    }
}
