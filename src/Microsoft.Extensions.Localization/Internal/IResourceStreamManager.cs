using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Localization.Internal
{
    public interface IResourceStreamManager
    {
        string GetResourceStreamCacheKey(CultureInfo culture);

        string GetResourceStreamName(CultureInfo culture);

        Stream GetResourceStream(CultureInfo culture);
    }
}
