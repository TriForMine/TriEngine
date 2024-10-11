using TriEngineEditor.Content;

namespace TriEngineEditor.Editors
{
    interface IAssetEditor
    {
        Asset Asset { get; }

        void SetAsset(AssetInfo asset);
    }
}