using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Microsoft.Extensions.Localization.Internal
{
    public interface IResourceStringManager
    {
        string GetResourceName(CultureInfo culture);

        IList<string> GetAllResourceStrings(CultureInfo culture);
    }
}
