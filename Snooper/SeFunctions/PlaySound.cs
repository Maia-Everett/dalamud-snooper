// (c) 2021 Ottermandias (ChatAlerts)
using Dalamud.Game;
using Dalamud.Plugin.Services;

namespace Snooper.SeFunctions
{
    public delegate ulong PlaySoundDelegate(Sounds id, ulong unk1, ulong unk2);

    public sealed class PlaySound : SeFunctionBase<PlaySoundDelegate>
    {
        public PlaySound(ISigScanner sigScanner, IGameInteropProvider interop)
            : base(sigScanner, interop, "E8 ?? ?? ?? ?? 4D 39 BE")
        { }

        public void Play(Sounds id)
            => Invoke(id, 0ul, 0ul);
    }
}
