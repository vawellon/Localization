using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Localization.Internal
{
    public class AssemblyResourceStreamManager : IResourceStreamManager
    {
        private const string _assemblyElementDelimiter = ", ";
        private static readonly string[] _assemblyElementDelimiterArray = new[] { _assemblyElementDelimiter };
        private static readonly char[] _assemblyEqualDelimiter = new[] { '=' };

        private Assembly _assembly;
        private readonly string _resourceBaseName;

        public AssemblyResourceStreamManager(
            Assembly resourceAssembly,
            string resourceBaseName)
        {
            _assembly = resourceAssembly;
            _resourceBaseName = resourceBaseName;
        }
        public string GetResourceStreamName(CultureInfo culture)
        {
            var resourceStreamName = _resourceBaseName;
            if (!string.IsNullOrEmpty(culture.Name))
            {
                resourceStreamName += "." + culture.Name;
            }
            resourceStreamName += ".resources";

            return resourceStreamName;
        }

        public string GetResourceStreamCacheKey(CultureInfo culture)
        {
            var resourceStreamName = GetResourceStreamName(culture);
            var assemblyName = ApplyCultureToAssembly(culture);
            return $"Assembly={assemblyName};resourceStreamName={resourceStreamName}";
        }

        public Stream GetResourceStream(CultureInfo culture)
        {
            var assembly = GetAssembly(culture);
            var resourceStreamName = GetResourceStreamName(culture);
            return assembly.GetManifestResourceStream(resourceStreamName);
        }

        protected virtual AssemblyWrapper GetAssembly(CultureInfo culture)
        {
            var assemblyString = ApplyCultureToAssembly(culture);
            Assembly assembly;
            try
            {
                assembly = Assembly.Load(new AssemblyName(assemblyString));
            }
            catch (FileNotFoundException)
            {
                return null;
            }

            return new AssemblyWrapper(assembly);
        }

        // This is all a workaround for https://github.com/dotnet/coreclr/issues/6123
        private string ApplyCultureToAssembly(CultureInfo culture)
        {
            var builder = new StringBuilder(_assembly.FullName);

            var cultureName = culture.Name == string.Empty ? "neutral" : culture.Name;
            var cultureString = $"Culture={cultureName}";

            var cultureStartIndex = _assembly.FullName.IndexOf("Culture", StringComparison.OrdinalIgnoreCase);
            if (cultureStartIndex < 0)
            {
                builder.Append(_assemblyElementDelimiter + cultureString);
            }
            else
            {
                var cultureEndIndex = _assembly.FullName.IndexOf(_assemblyElementDelimiter, cultureStartIndex);
                var cultureLength = cultureEndIndex - cultureStartIndex;
                builder = builder.Remove(cultureStartIndex, cultureLength);
                builder = builder.Insert(cultureStartIndex, cultureString);
            }

            var firstSplit = _assembly.FullName.IndexOf(_assemblyElementDelimiter);
            if (firstSplit < 0)
            {
                //Index of end of Assembly name
                firstSplit = _assembly.FullName.Length;
            }
            builder = builder.Insert(firstSplit, ".resources");

            return builder.ToString();
        }
    }
}
