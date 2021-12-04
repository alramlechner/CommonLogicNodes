using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace alram_lechner_gmx_at.logic.InfluxDb2
{
    class InfluxWriterHelper
    {

        public static void WriteDatapointSync(String influxUrl, String measureName, String measureTags, String fieldName, double value, Action<int?, string> SetResultCallback)
        {
            UriBuilder uriBuilder = null;
            try
            {
                uriBuilder = new UriBuilder(influxUrl);
            } catch (UriFormatException e)
            {
                SetResultCallback(997, e.Message + "; URI was " + influxUrl + "; tags: " + measureTags);
                return;
            }
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
                        Body = measureName;
                        if (measureTags != null && measureTags.Length > 0)
                        {
                            Body += "," + measureTags;
                        }

                        Body += " ";
                        Body += fieldName + "=" + value.ToString("G", CultureInfo.InvariantCulture);

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
