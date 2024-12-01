using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using Lumina;

namespace TravelAgency {
    public class GameFunctions : IDisposable {
        private delegate nint VistaUnlockedDelegate(ushort index, int a2, int a3);

        private delegate nint CreateVfxDelegate(string name);

        private delegate nint PlayVfxDelegate(nint vfx, float idk, int idk2);

        internal delegate nint RemoveVfxDelegate(nint vfx);

        private nint SightseeingMaskPointer { get; }

        private CreateVfxDelegate CreateVfx { get; }
        private PlayVfxDelegate PlayVfxInternal { get; }
        internal RemoveVfxDelegate RemoveVfx { get; }
        private Hook<VistaUnlockedDelegate> VistaUnlockedHook { get; }

        public GameFunctions() {

            var vistaUnlockedPtr = Service.SigScanner.ScanText("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 8B 4C 24 70 E8");
            this.VistaUnlockedHook = Service.GameInteropProvider.HookFromAddress<VistaUnlockedDelegate>(vistaUnlockedPtr, this.OnVistaUnlock);
            this.VistaUnlockedHook.Enable();

            var maskPtr = Service.SigScanner.GetStaticAddressFromSig("44 8B F2 48 8D 0D ?? ?? ?? ?? 8B D7");
            this.SightseeingMaskPointer = maskPtr;

            var createVfxPtr = Service.SigScanner.ScanText("E8 ?? ?? ?? ?? F3 0F 10 35 ?? ?? ?? ?? 48 89 43 08");
            this.CreateVfx = Marshal.GetDelegateForFunctionPointer<CreateVfxDelegate>(createVfxPtr);

            var playVfxPtr = Service.SigScanner.ScanText("E8 ?? ?? ?? ?? 8B 4B 7C 85 C9");
            this.PlayVfxInternal = Marshal.GetDelegateForFunctionPointer<PlayVfxDelegate>(playVfxPtr);

            var removeVfxPtr = Service.SigScanner.ScanText("40 53 48 83 EC 20 48 8B D9 48 8B 89 ?? ?? ?? ?? 48 85 C9 74 28 33 D2");
            this.RemoveVfx = Marshal.GetDelegateForFunctionPointer<RemoveVfxDelegate>(removeVfxPtr);
        }

        public void Dispose() {
            this.VistaUnlockedHook.Dispose();
        }

        private nint OnVistaUnlock(ushort index, int a2, int a3) {
            try {
                Service.Markers.RemoveVfx(index);
            } catch (Exception ex) {
                Service.Log.Error(ex, "Exception in vista unlock");
            }

            return this.VistaUnlockedHook.Original(index, a2, a3);
        }

        public unsafe nint SpawnVfx(string name, Vector3 position) {
            var vfx = this.CreateVfx(name);

            var pos = (float*) (vfx + 80);
            *pos = position.X;
            *(pos + 1) = position.Z;
            *(pos + 2) = position.Y;

            pos = (float*) (vfx + 640);
            *pos = 0;
            *(pos + 1) = 0;
            *(pos + 2) = 0;

            *(long*) (vfx + 56) |= 2;
            *(int*) (vfx + 652) = 0;

            return vfx;
        }

        internal bool PlayVfx(nint vfx) {
            return this.PlayVfxInternal(vfx, 0.0f, -1) == nint.Zero;
        }

        public bool HasVistaUnlocked(short index) {
            if (this.SightseeingMaskPointer == nint.Zero) {
                return false;
            }

            var byteToCheck = index >> 3;
            var bitToCheck = 1 << (index - 8 * byteToCheck);

            var maskPart = Marshal.ReadByte(this.SightseeingMaskPointer + byteToCheck);

            return (maskPart & bitToCheck) != 0;
        }

        public IGameObject? FindObjectByDataId(uint dataId, ObjectKind? kind = null)
        {
            foreach (var gameObject in Svc.Objects)
            {
                if (gameObject.ObjectKind is ObjectKind.Player or ObjectKind.Companion or ObjectKind.MountType
                    or ObjectKind.Retainer or ObjectKind.Housing)
                    continue;

                // multiple objects in the object table can share the same data id for gathering points; only one of those
                // (at most) is visible
                if (gameObject is { ObjectKind: ObjectKind.GatheringPoint, IsTargetable: false })
                    continue;

                if (gameObject.DataId == dataId && (kind == null || kind.Value == gameObject.ObjectKind))
                {
                    return gameObject;
                }
            }

            //_logger.LogWarning("Could not find GameObject with dataId {DataId}", dataId);
            return null;
        }
    }
}
