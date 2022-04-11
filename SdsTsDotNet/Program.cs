using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using OSIsoft.Data;
using OSIsoft.Data.Reflection;
using OSIsoft.Identity;

namespace SdsTsDotNet
{
    public static class Program
    {
        private static IConfiguration _configuration;
        private static Exception _toThrow;

        public static void Main()
        {
            MainAsync().GetAwaiter().GetResult();
        }

        public static async Task<bool> MainAsync(bool test = false)
        {
            ISdsMetadataService metadataService = null;

            #region settings
            string typeValueTimeName = "Value_Time";
            string typePressureTemperatureTimeName = "Pressure_Temp_Time";

            string streamPressureName = "Pressure_Tank1";
            string streamTempName = "Temperature_Tank1";
            string streamTank0 = "Vessel";
            string streamTank1 = "Tank1";
            string streamTank2 = "Tank2";
            #endregion

            try
            {
                #region configurationSettings

                _configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .AddJsonFile("appsettings.test.json", optional: true)
                    .Build();

                string tenantId = _configuration["TenantId"];
                string namespaceId = _configuration["NamespaceId"];
                string resource = _configuration["Resource"];
                string clientId = _configuration["ClientId"];
                string clientSecret = _configuration["ClientSecret"];
                #endregion

                (_configuration as ConfigurationRoot).Dispose();
                Uri uriResource = new Uri(resource);

                // Step 1 
                // Get Sds Services to communicate with server
                #region step1
                AuthenticationHandler authenticationHandler = new AuthenticationHandler(uriResource, clientId, clientSecret);

                SdsService sdsService = new SdsService(new Uri(resource), authenticationHandler);
                metadataService = sdsService.GetMetadataService(tenantId, namespaceId);
                ISdsDataService dataService = sdsService.GetDataService(tenantId, namespaceId);
                ISdsTableService tableService = sdsService.GetTableService(tenantId, namespaceId);
                #endregion

                // Step 2
                #region step2b
                SdsType type = SdsTypeBuilder.CreateSdsType<TimeData>();
                type.Id = typeValueTimeName;
                type = await metadataService.GetOrCreateTypeAsync(type).ConfigureAwait(false);
                #endregion

                // Step 3
                // create an SdsStream
                #region step3
                var pressure_stream = new SdsStream
                {
                    Id = streamPressureName,
                    TypeId = type.Id,
                    Description = "A stream for pressure data of tank1",
                };
                pressure_stream = await metadataService.GetOrCreateStreamAsync(pressure_stream).ConfigureAwait(false);

                SdsStream temperature_stream = new SdsStream
                {
                    Id = streamTempName,
                    TypeId = type.Id,
                    Description = "A stream for temperature data of tank1",
                };
                temperature_stream = await metadataService.GetOrCreateStreamAsync(temperature_stream).ConfigureAwait(false);
                #endregion

                // Step 4
                // insert simple data
                #region step4c
                await dataService.InsertValuesAsync(pressure_stream.Id, GetPressureData()).ConfigureAwait(false);
                await dataService.InsertValuesAsync(streamTempName, GetTemperatureData()).ConfigureAwait(false);
                #endregion
            
                // Step 5
                // create complex type
                #region step5b
                SdsType tankType = SdsTypeBuilder.CreateSdsType<PressureTemperatureData>();
                tankType.Id = typePressureTemperatureTimeName;
                tankType = await metadataService.GetOrCreateTypeAsync(tankType).ConfigureAwait(false);
                #endregion

                // Step 6
                // create complex type stream
                #region step6
                SdsStream tankStream = new SdsStream
                {
                    Id = streamTank1,
                    TypeId = tankType.Id,
                    Description = "A stream for data of tank1",
                };
                tankStream = await metadataService.GetOrCreateStreamAsync(tankStream).ConfigureAwait(false);
                #endregion

                // Step 7
                // insert complex data
                #region step7
                IList<PressureTemperatureData> data = GetData();
                await dataService.InsertValuesAsync(streamTank1, data).ConfigureAwait(false);
                #endregion

                // Step 8 and Step 9
                //  view data
                // note: step 9 is not done in this example as the JSON conversion by the library takes care of it automatically for you
                #region step8
                List<PressureTemperatureData> sortedData = data.OrderBy(entry => entry.Time).ToList();
                PressureTemperatureData firstTime = sortedData.First();
                PressureTemperatureData lastTime = sortedData.Last();

                List<TimeData> resultsPressure = (await dataService.GetWindowValuesAsync<TimeData>(
                    streamPressureName, 
                    firstTime.Time.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture), 
                    lastTime.Time.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture))
                    .ConfigureAwait(false))
                    .ToList();

                Console.WriteLine("Values from Pressure of Tank1:");
                foreach (TimeData evnt in resultsPressure)
                {
                    Console.WriteLine(JsonConvert.SerializeObject(evnt));
                }

                List<PressureTemperatureData> resultsTank = (await dataService.GetWindowValuesAsync<PressureTemperatureData>(
                    streamTank1, 
                    firstTime.Time.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture), 
                    lastTime.Time.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture))
                    .ConfigureAwait(false))
                    .ToList();

                Console.WriteLine("Values from Tank1:");
                foreach (PressureTemperatureData evnt in resultsTank)
                {
                    Console.WriteLine(JsonConvert.SerializeObject(evnt));
                }
                #endregion

                if (test)
                {
                    // Testing to make sure we get back expected stuff
                    if (!string.Equals(JsonConvert.SerializeObject(resultsPressure.First()), "{\"Time\":\"2017-01-11T22:21:23.43Z\",\"Value\":346.0}", StringComparison.OrdinalIgnoreCase))
                        throw new Exception("Value retrieved isn't expected value for pressure of Tank1");

                    if (!string.Equals(JsonConvert.SerializeObject(resultsTank.First()), "{\"Time\":\"2017-01-11T22:21:23.43Z\",\"Pressure\":346.0,\"Temperature\":91.0}", StringComparison.OrdinalIgnoreCase))
                        throw new Exception("Value retrieved isn't expected value for Temperature from Tank1");
                }

                // Step 10
                //  view summary data
                #region step10
                List<SdsInterval<PressureTemperatureData>> resultsTankSummary = (await dataService.GetIntervalsAsync<PressureTemperatureData>(
                    streamTank1,
                    firstTime.Time.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture),
                    lastTime.Time.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture),
                    1)
                    .ConfigureAwait(false))
                    .ToList();

                Console.WriteLine("Summaries from Tank1:");
                foreach (SdsInterval<PressureTemperatureData> evnt in resultsTankSummary)
                {
                    Console.WriteLine(JsonConvert.SerializeObject(evnt.Summaries));
                }
                #endregion

                // Step 11
                //  Bulk calls
                #region step11a
                SdsStream tankStream0 = new SdsStream
                {
                    Id = streamTank0,
                    TypeId = tankType.Id,
                };
                tankStream = await metadataService.GetOrCreateStreamAsync(tankStream0).ConfigureAwait(false);

                SdsStream tankStream2 = new SdsStream
                {
                    Id = streamTank2,
                    TypeId = tankType.Id,
                    Description = "A stream for data of tank2",
                };
                tankStream2 = await metadataService.GetOrCreateStreamAsync(tankStream2).ConfigureAwait(false);

                IList<PressureTemperatureData> data2 = GetData2();
                List<PressureTemperatureData> sortedData2 = data2.OrderBy(entry => entry.Time).ToList();
                PressureTemperatureData firstTime2 = sortedData2.First();
                PressureTemperatureData lastTime2 = sortedData2.Last();

                await dataService.InsertValuesAsync(tankStream2.Id, data2).ConfigureAwait(false);
                await dataService.InsertValuesAsync(tankStream0.Id, GetData()).ConfigureAwait(false);

                #endregion

                Thread.Sleep(200); // slight rest here for consistency

                #region step11b
                IEnumerable<IList<PressureTemperatureData>> results2Tanks = await dataService.GetJoinValuesAsync<PressureTemperatureData>(
                    new string[] { tankStream0.Id, tankStream2.Id },
                    SdsJoinType.Outer,
                    firstTime2.Time.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture),
                    lastTime2.Time.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture))
                    .ConfigureAwait(false);

                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine($"Bulk Values:   {tankStream0.Id}  then {tankStream2.Id}: ");
                Console.WriteLine();
                foreach (IList<PressureTemperatureData> tankResult in results2Tanks)
                {
                    foreach (PressureTemperatureData dataEntry in tankResult)
                    {
                        Console.WriteLine(JsonConvert.SerializeObject(dataEntry));
                    }

                    Console.WriteLine();
                }

                #endregion
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                _toThrow = ex;
                throw;
            }
            finally
            {
                if (metadataService != null)
                {
                    // Step 12
                    // delete everything
                    #region step12
                    await RunInTryCatch(metadataService.DeleteStreamAsync, streamPressureName).ConfigureAwait(false);
                    await RunInTryCatch(metadataService.DeleteStreamAsync, streamTempName).ConfigureAwait(false);
                    await RunInTryCatch(metadataService.DeleteStreamAsync, streamTank0).ConfigureAwait(false);
                    await RunInTryCatch(metadataService.DeleteStreamAsync, streamTank1).ConfigureAwait(false);
                    await RunInTryCatch(metadataService.DeleteStreamAsync, streamTank2).ConfigureAwait(false);

                    await RunInTryCatch(metadataService.DeleteTypeAsync, typeValueTimeName).ConfigureAwait(false);
                    await RunInTryCatch(metadataService.DeleteTypeAsync, typePressureTemperatureTimeName).ConfigureAwait(false);
                    #endregion 

                    Thread.Sleep(10); // slight rest here for consistency

                    // Check deletes
                    await RunInTryCatchExpectException(metadataService.GetStreamAsync, streamPressureName).ConfigureAwait(false);
                    await RunInTryCatchExpectException(metadataService.GetStreamAsync, streamTempName).ConfigureAwait(false);
                    await RunInTryCatchExpectException(metadataService.GetStreamAsync, streamTank0).ConfigureAwait(false);
                    await RunInTryCatchExpectException(metadataService.GetStreamAsync, streamTank1).ConfigureAwait(false);
                    await RunInTryCatchExpectException(metadataService.GetStreamAsync, streamTank2).ConfigureAwait(false);

                    await RunInTryCatchExpectException(metadataService.GetTypeAsync, typeValueTimeName).ConfigureAwait(false);
                    await RunInTryCatchExpectException(metadataService.GetTypeAsync, typePressureTemperatureTimeName).ConfigureAwait(false);
                }
            }

            if (test && _toThrow != null)
                throw _toThrow;
            
            return _toThrow == null;
        }

        #region step4b
        public static IList<TimeData> GetPressureData()
        {
            IList<PressureTemperatureData> data = GetData();
            return data.Select(entry => new TimeData() { Time = entry.Time, Value = entry.Pressure }).ToList();
        }

        public static IList<TimeData> GetTemperatureData()
        {
            IList<PressureTemperatureData> data = GetData();
            return data.Select(entry => new TimeData() { Time = entry.Time, Value = entry.Temperature }).ToList();
        }
        #endregion

        #region step4a
        public static IList<PressureTemperatureData> GetData()
        {
            List<PressureTemperatureData> values = new List<PressureTemperatureData>
            {
                new PressureTemperatureData() { Pressure = 346, Temperature = 91, Time = DateTime.Parse("2017-01-11T22:21:23.430Z", CultureInfo.InvariantCulture) },
                new PressureTemperatureData() { Pressure = 0, Temperature = 0, Time = DateTime.Parse("2017-01-11T22:22:23.430Z", CultureInfo.InvariantCulture) },
                new PressureTemperatureData() { Pressure = 386, Temperature = 93, Time = DateTime.Parse("2017-01-11T22:24:23.430Z", CultureInfo.InvariantCulture) },
                new PressureTemperatureData() { Pressure = 385, Temperature = 92, Time = DateTime.Parse("2017-01-11T22:25:23.430Z", CultureInfo.InvariantCulture) },
                new PressureTemperatureData() { Pressure = 385, Temperature = 0, Time = DateTime.Parse("2017-01-11T22:28:23.430Z", CultureInfo.InvariantCulture) },
                new PressureTemperatureData() { Pressure = 384.2, Temperature = 92, Time = DateTime.Parse("2017-01-11T22:26:23.430Z", CultureInfo.InvariantCulture) },
                new PressureTemperatureData() { Pressure = 384.2, Temperature = 92.2, Time = DateTime.Parse("2017-01-11T22:27:23.430Z", CultureInfo.InvariantCulture) },
                new PressureTemperatureData() { Pressure = 396, Temperature = 0, Time = DateTime.Parse("2017-01-11T22:28:29.430Z", CultureInfo.InvariantCulture) },
            };
            return values;
        }
        #endregion

        public static IList<PressureTemperatureData> GetData2()
        {
            List<PressureTemperatureData> values = new List<PressureTemperatureData>
            {
                new PressureTemperatureData() { Pressure = 345, Temperature = 89, Time = DateTime.Parse("2017-01-11T22:20:23.430Z", CultureInfo.InvariantCulture) },
                new PressureTemperatureData() { Pressure = 356, Temperature = 0, Time = DateTime.Parse("2017-01-11T22:21:23.430Z", CultureInfo.InvariantCulture) },
                new PressureTemperatureData() { Pressure = 354, Temperature = 88, Time = DateTime.Parse("2017-01-11T22:22:23.430Z", CultureInfo.InvariantCulture) },
                new PressureTemperatureData() { Pressure = 374, Temperature = 87, Time = DateTime.Parse("2017-01-11T22:28:23.430Z", CultureInfo.InvariantCulture) },
                new PressureTemperatureData() { Pressure = 384.2, Temperature = 88, Time = DateTime.Parse("2017-01-11T22:26:23.430Z", CultureInfo.InvariantCulture) },
                new PressureTemperatureData() { Pressure = 384.2, Temperature = 92.2, Time = DateTime.Parse("2017-01-11T22:27:23.430Z", CultureInfo.InvariantCulture) },
                new PressureTemperatureData() { Pressure = 396, Temperature = 87, Time = DateTime.Parse("2017-01-11T22:28:29.430Z", CultureInfo.InvariantCulture) },
            };
            return values;
        }

        /// <summary>
        /// Use this to run a method that you don't want to stop the program if there is an exception
        /// </summary>
        /// <param name="methodToRun">The method to run.</param>
        /// <param name="value">The value to put into the method to run</param>
        private static async Task RunInTryCatch(Func<string, Task> methodToRun, string value)
        {
            try
            {
                await methodToRun(value).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Got error in {methodToRun.Method.Name} with value {value} but continued on:" + ex.Message);
                if (_toThrow == null)
                {
                    _toThrow = ex;
                }
            }
        }

        /// <summary>
        /// Use this to run a method that you don't want to stop the program if there is an exception, and you expect an exception
        /// </summary>
        /// <param name="methodToRun">The method to run.</param>
        /// <param name="value">The value to put into the method to run</param>
        private static async Task RunInTryCatchExpectException(Func<string, Task> methodToRun, string value)
        {
            try
            {
                await methodToRun(value).ConfigureAwait(false);

                Console.WriteLine($"Got error.  Expected {methodToRun.Method.Name} with value {value} to throw an error but it did not:");
            }
            catch
            {
            }
        }
    }
}
