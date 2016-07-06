using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Text;

namespace Microsoft.Extensions.Localization.Internal
{
    public class AssemblyResourceStringManager : IResourceStringManager
    {
        private const string _assemblyElementDelimiter = ", ";
        private static readonly string[] _assemblyElementDelimiterArray = new[] { _assemblyElementDelimiter };
        private static readonly char[] _assemblyEqualDelimiter = new[] { '=' };

        private AssemblyWrapper _assembly;
        private readonly string _resourceBaseName;
        private readonly IResourceNamesCache _resourceNamesCache;

        public AssemblyResourceStringManager(
            IResourceNamesCache resourceCache,
            AssemblyWrapper resourceAssembly,
            string resourceBaseName)
        {
            _resourceNamesCache = resourceCache;
            _assembly = resourceAssembly;
            _resourceBaseName = resourceBaseName;
        }
        public string GetResourceName(CultureInfo culture)
        {
            var resourceStreamName = _resourceBaseName;
            if (!string.IsNullOrEmpty(culture.Name))
            {
                resourceStreamName += "." + culture.Name;
            }
            resourceStreamName += ".resources";

            return resourceStreamName;
        }

        public string GetResourceCacheKey(CultureInfo culture)
        {
            var resourceStreamName = GetResourceName(culture);
            var assemblyName = ApplyCultureToAssembly(culture);

            return $"Assembly={assemblyName};resourceStreamName={resourceStreamName}";
        }

        public IList<string> GetAllResourceStrings(CultureInfo culture)
        {
            var assembly = GetAssembly(culture);

            if (assembly == null)
            {
                return null;
            }

            var cacheKey = GetResourceCacheKey(culture);

            return _resourceNamesCache.GetOrAdd(cacheKey, _ =>
            {
                var resourceStreamName = GetResourceName(culture);
                using (var resourceStream = assembly.GetManifestResourceStream(resourceStreamName))
                {
                    if (resourceStream == null)
                    {
                        return null;
                    }

                    using (var resources = new ResourceReader(resourceStream))
                    {
                        var names = new List<string>();
                        foreach (DictionaryEntry entry in resources)
                        {
                            var resourceName = (string)entry.Key;
                            names.Add(resourceName);
                        }
                        return names;
                    }
                }
            });
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
                builder.Remove(cultureStartIndex, cultureLength);
                builder.Insert(cultureStartIndex, cultureString);
            }

            var firstSplit = _assembly.FullName.IndexOf(_assemblyElementDelimiter);
            if (firstSplit < 0)
            {
                //Index of end of Assembly name
                firstSplit = _assembly.FullName.Length;
            }
            builder.Insert(firstSplit, ".resources");

            return builder.ToString();
        }
    }
}
