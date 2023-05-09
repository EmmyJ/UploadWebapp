﻿using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using CsvHelper.TypeConversion;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.Mvc;

namespace UploadWebapp.Controllers
{
    public class DashboardController : Controller
    {
        //
        // GET: /Dashboard/

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult SensorData(string station, string type, int days)
        {
            try
            {
                string filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "ICOSETC_" + station + "_METEOSENS_NRT.csv");
                StreamReader sr = new StreamReader(filePath);
                string fulltext = sr.ReadToEnd().ToString(); //read full file text
                sr.Close();

                string[] rows = fulltext.Split('\n'); //split full file text into rows
                string[] headers = rows[0].Split(',');
                List<string> headersFinal = new List<string>();
                //Dictionary<int, int> colnrs = new Dictionary<int, int>();        
                int[] colnrs = new int[headers.Count()]; ;
                List<string> sba = new List<string>();
                string[] values;
                int hf = 0;
                Dictionary<string, int> groupedCols = new Dictionary<string, int>() {
                    {"TS_", 3},
                    {"TA_", 3},
                    {"SWC_", 4}
                    //,{"RH_1_;RH_3_", 5}
                };

                //group same sensors per plot
                for (int a = 0; a < headers.Count(); a++)
                {
                    string header = headers[a];
                    bool match = false;
                    foreach (var groupedCol in groupedCols)
                    {
                        if (match == false && header.Substring(0, groupedCol.Value) == groupedCol.Key && headersFinal.Count > 0)
                        {
                            int? colI = headersFinal.FindIndex(x => x.StartsWith("," + header.Substring(0, groupedCol.Value + 1), StringComparison.OrdinalIgnoreCase));

                            if (colI < 0)
                            {
                                headersFinal.Add("," + header);
                                colnrs[a] = hf;
                                hf++;
                            }
                            else
                            {
                                headersFinal[colI.Value] = headersFinal[colI.Value] + "," + header;
                                colnrs[a] = colI.Value;
                            }

                            match = true;
                        }
                    }
                    if (!match)
                    {
                        headersFinal.Add("," + header);
                        colnrs[a] = hf;
                        hf++;
                    }
                }

                //create stringbuilder for each sensor and add the headerline
                for (int h = 2; h < headersFinal.Count(); h++)
                {
                    sba.Add("TIMESTAMP" + headersFinal[h]);// + "\n");
                }

                //only use data for last x days
                int lines = (days * 48) + 1;
                lines = lines < rows.Count() ? lines : rows.Count();

                for (int i = rows.Count() - lines; i < rows.Count() - 1; i++)
                {
                    rows[i].Replace(",,", ", ,"); //to not lose empty columns
                    values = rows[i].Split(',');
                    int prevCol = -1;
                    int colNr = 0;

                    for (int v = 2; v < values.Count(); v++)
                    {
                        //add each value to the corresponding stringbuilders
                        colNr = colnrs[v];
                        if (colNr == prevCol)
                        {
                            sba[colNr - 2] += "," + values[v];
                        }
                        else
                        {
                            sba[colNr - 2] += "\n" + values[0] + "," + values[v];
                        }
                        prevCol = colNr;
                    }
                }

                for (int s = 0; s < sba.Count(); s++)
                {
                    string varname = (headersFinal[s + 2].Substring(1));
                    bool match = false;
                    foreach (var item in groupedCols)
                    {
                        if (!match && varname.StartsWith(item.Key))
                        {
                            varname = varname.Substring(0, item.Value + 1);
                            match = true;
                        }
                    }

                    string outfilePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "MeteoSens/" + station + "-MeteoSens-" + varname + ".csv");
                    StreamWriter sw = new StreamWriter(outfilePath);
                    sw.Write(sba[s]);
                    sw.Close();
                    headersFinal[s + 2] = varname;
                }

                ViewBag.title = station + " - " + type;
                ViewBag.station = station;

                return View(headersFinal.Where((v, i) => i > 1).ToArray());
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e);
                return View();
            }
        }

        class TAmodel
        {
            public DateTime TimeStamp { get; set; }
            public double? TA { get; set; }
            public double? TAavg { get; set; }
        }
        sealed class TAMap : ClassMap<TAmodel>
        {
            public TAMap()
            {
                Map(m => m.TimeStamp).Name("TIMESTAMP");
                Map(m => m.TA).Name("TA");
                Map(m => m.TAavg).Name("TA_avg");
            }
        }

        class MeteoNRTmodel
        {
            [Format("yyyy-MM-ddTHH:mm:ssZ")]
            public DateTime TIMESTAMP { get; set; }
            //public DateTime TIMESTAMP_END { get; set; }
            public string monthYear { get; set; }
            public double? TA { get; set; }
            public double? P { get; set; }
            public double? WTD { get; set; }
            public double? TA_1 { get; set; }
            public double? TA_8 { get; set; }
            public double? TS_2 { get; set; }
        }

        sealed class MeteoNRTMap: ClassMap<MeteoNRTmodel>
        {
            public MeteoNRTMap()
            {
                Map(m => m.TIMESTAMP).Name("TIMESTAMP").TypeConverterOption.Format("yyyy-MM-ddTHH:mm:ssZ");
                //Map(m => m.TIMESTAMP_END).Name("TIMESTAMP_END").TypeConverterOption.Format("yyyyMMddHHmm");
                Map(m => m.TA).Name("TA");
                Map(m => m.TA_1).Name("TA_1");
                Map(m => m.TA_8).Name("TA_8");
                Map(m => m.TS_2).Name("TS_2");
                Map(m => m.P).Name("P");
                Map(m => m.WTD).Name("WTD");
            }
        }

        class FluxesNRTmodel
        {
            [Format("yyyy-MM-ddTHH:mm:ssZ")]
            public DateTime TIMESTAMP { get; set; }
            //public DateTime TIMESTAMP_END { get; set; }
            public double? NEE { get; set; }
            public double? NEEpos { get; set; }
            public double? NEEneg { get; set; }
        }

        sealed class FluxesNRTMap : ClassMap<FluxesNRTmodel>
        {
            public FluxesNRTMap()
            {
                Map(m => m.TIMESTAMP).Name("TIMESTAMP").TypeConverterOption.Format("yyyy-MM-ddTHH:mm:ssZ");
                Map(m => m.NEE).Name("NEE");
            }
        }

        public ActionResult generateBraInfoData()
        {
            //meteo
            string filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "ICOSETC_BE-Bra_METEO_NRT.csv");
            var fileReader = new StreamReader(filePath);
            var csvReader = new CsvReader(fileReader, CultureInfo.InvariantCulture); 
            csvReader.Context.RegisterClassMap<MeteoNRTMap>();
            csvReader.Context.TypeConverterOptionsCache.GetOptions<double?>().NullValues.Add("null");
            csvReader.Context.TypeConverterOptionsCache.GetOptions<double?>().NullValues.Add("-9999");
            var options = new TypeConverterOptions { Formats = new[] { "yyyy-MM-ddTHH:mm:ssZ" } };
            var NRT = csvReader.GetRecords<MeteoNRTmodel>().ToList();

            //meteo L2
            filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "ICOSETC_BE-Bra_METEO_L2.csv");
            fileReader = new StreamReader(filePath);
            csvReader = new CsvReader(fileReader, CultureInfo.InvariantCulture);
            csvReader.Context.RegisterClassMap<MeteoNRTMap>();
            csvReader.Context.TypeConverterOptionsCache.GetOptions<double?>().NullValues.Add("null");
            csvReader.Context.TypeConverterOptionsCache.GetOptions<double?>().NullValues.Add("-9999");
            var MeteoL2 = csvReader.GetRecords<MeteoNRTmodel>().ToList();
            DateTime lastYear = DateTime.Today.AddYears(-1);
            var NRTy = MeteoL2.Where(y => y.TIMESTAMP > lastYear).ToList();
            NRT = NRTy.Concat(NRT).ToList();

            DateTime max = NRT.Max(x => x.TIMESTAMP);
            var NRT7 = NRT.Where(n => n.TIMESTAMP > max.AddDays(-7)).ToList();
            var NRTdaily = NRT.GroupBy(m => new { m.TIMESTAMP.Date })
                .Select(g => new MeteoNRTmodel
                {
                    TIMESTAMP = g.Key.Date,
                    TA = g.Average(m => m.TA),
                    P = g.Sum(m => m.P),
                    WTD = g.Average(m => m.WTD)
                }).ToList();
            var NRTmonthly = NRT.GroupBy(m => new { m.TIMESTAMP.Date.Month, m.TIMESTAMP.Date.Year })
                .Select(g => new MeteoNRTmodel
                {
                    monthYear = string.Format("{1}/{0}",g.Key.Month,g.Key.Year),
                    TA = g.Average(m => m.TA),
                    P = g.Sum(m => m.P),
                    WTD = g.Average(m => m.WTD)
                }).ToList();
            var TAdaily = (from n in NRTdaily select new { n.TIMESTAMP, n.TA }).ToList();

            filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "ICOSETC_BE-Bra_METEO_NRT_TA_daily.csv");
            using (var writer = new StreamWriter(filePath))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false}))
            {
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime?>(options);
                csv.WriteRecords(TAdaily);
            }

            var TA7 = (from n in NRT7 select new { n.TIMESTAMP, n.TA }).ToList();
            filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "ICOSETC_BE-Bra_METEO_NRT_TA_7days.csv");
            using (var writer = new StreamWriter(filePath))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false}))
            {
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime?>(options);
                csv.WriteRecords(TA7);
            }

            var T7mix = (from n in NRT7 select new { n.TIMESTAMP, n.TA_1, n.TA_8, n.TS_2 }).ToList();
            filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "ICOSETC_BE-Bra_METEO_NRT_Tmix_7days.csv");
            using (var writer = new StreamWriter(filePath))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false}))
            {
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime?>(options);
                csv.WriteRecords(T7mix);
            }

            var Pdaily = (from n in NRTdaily select new { n.TIMESTAMP, n.P }).ToList();
            filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "ICOSETC_BE-Bra_METEO_NRT_P_daily.csv");
            using (var writer = new StreamWriter(filePath))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false}))
            {
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime?>(options);
                csv.WriteRecords(Pdaily);
            }
            var Pmonthly = (from n in NRTmonthly select new { n.monthYear, n.P }).ToList();
            filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "ICOSETC_BE-Bra_METEO_NRT_P_monthly.csv");
            using (var writer = new StreamWriter(filePath))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false }))
            {
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime?>(options);
                csv.WriteRecords(Pmonthly);
            }

            var P7 = (from n in NRT7 select new { n.TIMESTAMP, n.P }).ToList();
            filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "ICOSETC_BE-Bra_METEO_NRT_P_7days.csv");
            using (var writer = new StreamWriter(filePath))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false}))
            {
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime?>(options);
                csv.WriteRecords(P7);
            }

            var WTDdaily = (from n in NRTdaily select new { n.TIMESTAMP, n.WTD }).ToList();
            filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "ICOSETC_BE-Bra_METEO_NRT_WTD_daily.csv");
            using (var writer = new StreamWriter(filePath))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false}))
            {
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime?>(options);
                csv.WriteRecords(WTDdaily);
            }

            var WTD7 = (from n in NRT7 select new { n.TIMESTAMP, n.WTD }).ToList();
            filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "ICOSETC_BE-Bra_METEO_NRT_WTD_7days.csv");
            using (var writer = new StreamWriter(filePath))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false}))
            {
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime?>(options);
                csv.WriteRecords(WTD7);
            }

            //Fluxes
            filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "ICOSETC_BE-Bra_FLUXES_NRT.csv");
            fileReader = new StreamReader(filePath);
            csvReader = new CsvReader(fileReader, CultureInfo.InvariantCulture);
            csvReader.Context.RegisterClassMap<FluxesNRTMap>();
            csvReader.Context.TypeConverterOptionsCache.GetOptions<double?>().NullValues.Add("null");
            csvReader.Context.TypeConverterOptionsCache.GetOptions<double?>().NullValues.Add("-9999");
            var FLUXES = csvReader.GetRecords<FluxesNRTmodel>().ToList();


            //FluxesL2
            filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "ICOSETC_BE-Bra_FLUXES_L2.csv");
            fileReader = new StreamReader(filePath);
            csvReader = new CsvReader(fileReader, CultureInfo.InvariantCulture);
            csvReader.Context.RegisterClassMap<FluxesNRTMap>();
            csvReader.Context.TypeConverterOptionsCache.GetOptions<double?>().NullValues.Add("null");
            csvReader.Context.TypeConverterOptionsCache.GetOptions<double?>().NullValues.Add("-9999");
            var FLUXESL2 = csvReader.GetRecords<FluxesNRTmodel>().ToList();
            lastYear = DateTime.Today.AddYears(-1);
            var FLUXESy = FLUXESL2.Where(y => y.TIMESTAMP > lastYear).ToList();
            FLUXES = FLUXESy.Concat(FLUXES).ToList();

            max = FLUXES.Max(x => x.TIMESTAMP);
            var FLUXES7 = FLUXES.Where(n => n.TIMESTAMP > max.AddDays(-7)).ToList();
            FLUXES7 = FLUXES7.Where(n => n.TIMESTAMP > max.AddDays(-7))
                .Select(g => new FluxesNRTmodel
                {
                    TIMESTAMP = g.TIMESTAMP,
                    NEE = g.NEE ?? null,
                    NEEneg = g.NEE <= 0 ? g.NEE : null,
                    NEEpos = g.NEE > 0 ? g.NEE : null
                })
                .ToList();
            var FLUXESdaily = FLUXES.GroupBy(m => new { m.TIMESTAMP.Date })
                .Select(g => new FluxesNRTmodel
                {
                    TIMESTAMP = g.Key.Date,
                    NEE = g.Sum(m => m.NEE),
                    NEEneg = g.Sum(m => m.NEE) <= 0 ? g.Sum(m => m.NEE) : null,
                    NEEpos = g.Sum(m => m.NEE) > 0 ? g.Sum(m => m.NEE) : null
                }).ToList();

            var NEEdaily = (from n in FLUXESdaily select new { n.TIMESTAMP, n.NEEneg, n.NEEpos }).ToList();
            filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "ICOSETC_BE-Bra_FLUXES_NRT_NEE_daily.csv");
            using (var writer = new StreamWriter(filePath))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false}))
            {
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime?>(options);
                csv.WriteRecords(NEEdaily);
            }

            var NEE7 = (from n in FLUXES7 select new { n.TIMESTAMP, n.NEEneg, n.NEEpos }).ToList();
            filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["CPdataFolder"].ToString(), "ICOSETC_BE-Bra_FLUXES_NRT_NEE_7days.csv");
            using (var writer = new StreamWriter(filePath))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false}))
            {
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime?>(options);
                csv.WriteRecords(NEE7);
            }

            return null;
        }
    }
}
