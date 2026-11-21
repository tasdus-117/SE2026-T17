using Mirror;
using SmallArena.Input;
using SmallArena.Protocol;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SmallArena.Client
{
    /// <summary>Opt-in raycast-intent adapter. No prefab installs this until TV1/TV2 supply the real sink.</summary>
    [DisallowMultipleComponent]
    public sealed class HeldFireInput : MonoBehaviour
    {
        private PlayerInputHandler input;
        private Camera aimCamera;
        private Transform muzzle;
        private IShotIntentSink sink;
        private readonly HeldFireCadence cadence = new HeldFireCadence();
        private Mouse pointer;

        public void Configure(PlayerInputHandler input, Camera camera, Transform muzzle, IShotIntentSink sink)
        {
            Detach();
            this.input = input; aimCamera = camera; this.muzzle = muzzle; this.sink = sink;
            if (isActiveAndEnabled) Attach();
        }
        private void Attach()
        {
            cadence.Reset();
            if (input != null) input.GameplayInvalidated += Invalidate;
            if (SinkExists) sink.Invalidated += Invalidate;
        }
        private bool SinkExists => sink != null && (!(sink is Object obj) || obj != null);
        private void Detach()
        {
            if (input != null) input.GameplayInvalidated -= Invalidate;
            if (SinkExists) sink.Invalidated -= Invalidate;
            Invalidate();
        }
        private void Invalidate() { cadence.Reset(); pointer = null; }
        private void OnEnable() => Attach();
        private void OnDisable() => Detach();
        private void OnDestroy() => Detach();
        private void Update()
        {
            if (pointer != Mouse.current) { cadence.Reset(); pointer = Mouse.current; }
            var held = false;
            var allowed = input != null && input.TryReadHeldFire(out held) && aimCamera != null && muzzle != null
                && SinkExists && sink.CanSubmit && Time.timeScale > 0
                && (!NetworkClient.active || NetworkClient.ready);
            if (!cadence.TryRequest(allowed, held, Time.unscaledTimeAsDouble,
                SinkExists ? sink.RequestIntervalSeconds : double.NaN)) return;
            // Re-evaluate the camera ray and anchor at each attempt; never retain an old aim.
            var ray = aimCamera.ScreenPointToRay(pointer.position.ReadValue());
            if (MouseAim.TryDirection(ray, muzzle.position, out var aim))
                sink.SubmitAim(aim.x, aim.z);
        }
    }
}
