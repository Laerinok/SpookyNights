using Vintagestory.API.Common;

namespace Spookynights
{
    public sealed class SpookyNights : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.Logger.Notification("🌟 Mon premier mod de code C# est chargé !");
        }
    }
}