using Newtonsoft.Json;
using System.Reflection;
using Xunit.Sdk;
using System.IO;
using System.Collections.Generic;
using System;

namespace EnsNormalize.Tests
{
    public class NormJsonFileDataAttribute : DataAttribute
    {
        private readonly string _filePath;

       
        /// <summary>
        /// Load data from a JSON file as the data source for a theory
        /// </summary>
        /// <param name="filePath">The absolute or relative path to the JSON file to load</param>
        public NormJsonFileDataAttribute(string filePath)
        {
            _filePath = filePath;
        }

        /// <inheritDoc />
        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            if (testMethod == null) { throw new ArgumentNullException(nameof(testMethod)); }

#if NET5_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
            // Get the absolute path to the JSON file
            var path = Path.IsPathRooted(_filePath)
                ? _filePath
                : Path.GetRelativePath(Directory.GetCurrentDirectory(), _filePath);
#else

            var path = Path.IsPathRooted(_filePath)
                      ? _filePath
                      : FileUtils.GetRelativePath(Directory.GetCurrentDirectory(), _filePath);
#endif

            if (!File.Exists(path))
            {
                throw new ArgumentException($"Could not find file at path: {path}");
            }

            // Load the file
            var fileData = File.ReadAllText(_filePath);
            var returnList = new List<object[]>();
           
            //whole file is the data
            var testCases = JsonConvert.DeserializeObject<List<NormTestCase>>(fileData);
            return testCases.ConvertAll(x => x.ToObjectArray()); 
              
           
        }
    }
}