using SmallArena.Domain;
using UnityEngine;
namespace SmallArena.Client.UI
{
    public sealed class OfflineHealthPresenter : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour healthSource = null;
        [SerializeField] private GameObject healthBarPrefab = null;
        private void Start()
        {
            if (healthBarPrefab == null || !(healthSource is IHealthSource health)) return;
            var bar = Instantiate(healthBarPrefab, transform);
            bar.transform.localPosition = new Vector3(0, 2.3f, 0);
            bar.GetComponent<NetworkHealthBarView>().Bind(health);
        }
    }
}
