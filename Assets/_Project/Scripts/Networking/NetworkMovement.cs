using System;
using Mirror;
using SmallArena.Protocol;
using UnityEngine;

namespace SmallArena.Networking
{
    /// <summary>Owner commands to server intent listeners; no movement simulation or input-device access.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkIdentity))]
    public sealed class NetworkMovement : NetworkBehaviour
    {
        public const double SendInterval = 1d / 60d;

        private uint sequence;
        private uint jumpSequence;
        private double nextSendTime;

        public event Action<MovementInput> ServerInputReceived;

        public override void OnStartLocalPlayer()
        {
            sequence = 0;
            jumpSequence = 0;
            nextSendTime = 0;
        }

        public void SubmitInput(Vector2 move, bool jumpPressed)
        {
            if (!isLocalPlayer || !isClient || !NetworkClient.ready) return;
            if (jumpPressed) jumpSequence = unchecked(jumpSequence + 1);
            var now = NetworkTime.localTime;
            if (now < nextSendTime) return;
            nextSendTime = now + SendInterval;
            CmdSubmitMovement(new MovementInput
            {
                Sequence = unchecked(++sequence), MoveX = move.x, MoveZ = move.y, JumpSequence = jumpSequence
            });
        }

        [Command(channel = Channels.Unreliable, requiresAuthority = true)]
        private void CmdSubmitMovement(MovementInput input) => ServerInputReceived?.Invoke(input);
    }
}
