using System.Collections.Generic;
using Arch.Core;

namespace TTRPG.Core.Engine
{
    public interface IUIProvider
    {
        /// <summary>
        /// Determines what information 'viewer' can see about 'target'.
        /// Returns a dictionary of labels and values (e.g. "Health" -> "50/100").
        /// </summary>
        Dictionary<string, string> GetInspectionDetails(World world, Entity viewer, Entity target);
    }
}