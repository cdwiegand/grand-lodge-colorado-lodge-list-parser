using System;

namespace ColoradoGrandLodgeMapParser
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Run().GetAwaiter().GetResult();
        }

        public static async Task Run() { 
            string url = "http://www.coloradofreemasons.org/lodges/lodges2.php";
            string cacheFile = System.IO.Path.Join(System.IO.Path.GetTempPath(), "co_grand_lodge_cache.html");
            Console.WriteLine("Cache: " + cacheFile);
            string html = "";
            if (System.IO.File.Exists(cacheFile))
                html = await System.IO.File.ReadAllTextAsync(cacheFile);
            if (string.IsNullOrEmpty(html))
            {
                html = await (new HttpClient()).GetStringAsync(url);
                await System.IO.File.WriteAllTextAsync(cacheFile, html);
            }

            // now parse
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);
            var table = doc.DocumentNode.DescendantNodes().FirstOrDefault(p => p.Name.ToLower() == "table");

            List<LodgeModel> lodges = new List<LodgeModel>();
            LodgeModel currentLodge = new LodgeModel();
            lodges.Add(currentLodge);

            // now run through TR's, but when we get two TRs with colspan=2 in a row with no inner content we're on a new Lodge!
            bool lastTrWasTotallyEmpty = false;
            int lineNumberWithinLodge = 0;
            List<string> lines = new List<string>();
            var trs = table.Descendants().Where(p => p.Name.ToLower() == "tr").ToList();
            for (int idx = 0; idx < trs.Count(); idx++)
            {
                string lineText = trs[idx].InnerText;
                if (lineText != null && lineText == "&nbsp;") lineText = "";
                if (string.IsNullOrWhiteSpace(lineText))
                {
                    lineNumberWithinLodge = 0; // reset
                    if (lastTrWasTotallyEmpty)
                    {
                        // two of them! next lodge!
                        currentLodge = new LodgeModel();
                        lodges.Add(currentLodge);
                        lastTrWasTotallyEmpty = false;
                    } else
                        lastTrWasTotallyEmpty = true;
                    continue; // next line!
                } else
                {
                    // ok, we have content, let's pull another line
                    switch (lineNumberWithinLodge)
                    {
                        case 0: // name, number
                            currentLodge.Name = trs[idx].FirstChild.InnerText;
                            currentLodge.Number = int.TryParse(string.Join("", trs[idx].ChildNodes.Skip(1).First().InnerText.Where(p => char.IsNumber(p))), out int i2) ? i2 : 0;
                            break;
                        case 1: // street address
                            currentLodge.StreetAddress = trs[idx].FirstChild.InnerText;
                            break;
                        case 2: // city, state
                            currentLodge.City = trs[idx].FirstChild.InnerText.Replace(", Colorado.","");
                            // don't need state, they're all Colorado.
                            break;
                        case 3: // zip, "map to lodge"
                            currentLodge.ZipCode = trs[idx].FirstChild.InnerText;
                            break;
                        case 4: // lat, lon
                            currentLodge.GeoLat = decimal.TryParse(trs[idx].FirstChild.InnerText.Replace("Lat: ",""), out decimal i1) ? i1 : 0;
                            currentLodge.GeoLong = decimal.TryParse(trs[idx].ChildNodes.Skip(1).First().InnerText.Replace("Lon: ", ""), out decimal i3) ? i3 : 0;
                            break;
                        case 5: // meeting schedule
                            currentLodge.MeetingSchedule = trs[idx].InnerText.Replace("\u0027","");
                            break;
                        case 6: // recess schedule
                            currentLodge.RecessInfo = trs[idx].InnerText.Replace("\u0027", "");
                            break;
                        case 7: // website
                            currentLodge.Website = trs[idx].InnerText.Replace("\u0027", "");
                            break;
                    }

                    lineNumberWithinLodge++;
                }
            }

            // now dump all lodges
            var result = System.Text.Json.JsonSerializer.Serialize(lodges);

            // Console.Write(result);
            using (TextWriter tw = new StreamWriter("/Users/chris/Desktop/lodges.csv"))
            using (CsvHelper.CsvWriter w = new CsvHelper.CsvWriter(tw, new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.CurrentCulture)))
            {
                w.WriteHeader<LodgeModel>();
                w.NextRecord();
                w.WriteRecords(lodges);
            }
        }
    }
}