using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriEngineEditor.Content
{
    enum AssetType
    {
        Unknown,
        Animation,
        Audio,
        Material,
        Mesh,
        Skeleton,
        Texture
    }

    abstract class Asset : ViewModelBase
    {
        public AssetType Type { get; private set; }

        public Asset(AssetType type)
        {
            Debug.Assert(type != AssetType.Unknown, "Asset type cannot be unknown");
            Type = type;
        }
    }
}
