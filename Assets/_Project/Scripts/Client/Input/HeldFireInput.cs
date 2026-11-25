using Mirror;
using SmallArena.Input;
using SmallArena.Protocol;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SmallArena.Client
{
    /// <summary>Fresh mouse aim collector; the installed sink owns networking and server permission.</summary>
    [DisallowMultipleComponent]
    public sealed class HeldFireInput : MonoBehaviour
    {
        [SerializeField] private PlayerInputHandler input;
        private Camera aimCamera;
        [SerializeField] private Transform muzzle;
        [SerializeField] private MonoBehaviour sinkProvider = null;
        private IShotIntentSink sink;
        private readonly HeldFireCadence cadence = new HeldFireCadence();
        private Mouse pointer;
        private void Awake() { if (sinkProvider != null) sink = sinkProvider as IShotIntentSink; }

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
            if (sinkProvider != null && aimCamera == null) { aimCamera = Camera.main; cadence.Reset(); }
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
