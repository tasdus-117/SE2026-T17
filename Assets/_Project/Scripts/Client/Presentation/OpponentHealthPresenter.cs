using Mirror;
using SmallArena.Client.UI;
using SmallArena.Networking;
using UnityEngine;

namespace SmallArena.Client.Presentation
{
    public sealed class OpponentHealthPresenter : NetworkBehaviour
    {
        [SerializeField] private GameObject healthBarPrefab = null;
        private NetworkHealthBarView bar;
        public override void OnStartClient()
        {
            if (isLocalPlayer || healthBarPrefab == null) return;
            var owner = Instantiate(healthBarPrefab, transform);
            owner.transform.localPosition = new Vector3(0, 2.3f, 0);
            bar = owner.GetComponent<NetworkHealthBarView>();
            bar.Bind(GetComponent<NetworkHealth>());
        }
        private void LateUpdate()
        {
            if (bar != null && Camera.main != null) bar.transform.rotation = Camera.main.transform.rotation;
        }
        public override void OnStartLocalPlayer() => OnStopClient();
        public override void OnStopClient()
        {
            if (bar != null) Destroy(bar.gameObject);
            bar = null;
        }
    }
}
