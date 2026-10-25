using Unity.Netcode;
using UnityEngine;

namespace SmallArena.Combat
{
    /// <summary>
    /// Weapon controller with client-side shooting and server-side hit validation
    /// </summary>
    public class WeaponController : NetworkBehaviour
    {
        [Header("Weapon Settings")]
        [SerializeField] private float fireRate = 0.2f;
        [SerializeField] private float damage = 10f;
        [SerializeField] private float range = 100f;
        [SerializeField] private int maxAmmo = 30;
        [SerializeField] private float reloadTime = 2f;
        
        [Header("Visual Effects")]
        [SerializeField] private Transform firePoint;
        [SerializeField] private GameObject muzzleFlashPrefab;
        [SerializeField] private GameObject impactEffectPrefab;
        [SerializeField] private LineRenderer tracerEffect;
        
        // State
        private float nextFireTime;
        private int currentAmmo;
        private bool isReloading;
        
        // Network variables
        private NetworkVariable<int> networkAmmo = new NetworkVariable<int>();
        
        private void Awake()
        {
            currentAmmo = maxAmmo;
            
            if (firePoint == null)
            {
                GameObject firePointObj = new GameObject("FirePoint");
                firePoint = firePointObj.transform;
                firePoint.SetParent(transform);
                firePoint.localPosition = new Vector3(0, 0, 1);
            }
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (IsServer)
            {
                networkAmmo.Value = maxAmmo;
            }
            
            if (!IsOwner)
            {
                enabled = false;
            }
        }
        
        private void Update()
        {
            if (!IsOwner) return;
            
            if (Input.GetButton("Fire1") && Time.time >= nextFireTime && !isReloading)
            {
                Fire();
            }
            
            if (Input.GetKeyDown(KeyCode.R) && !isReloading && currentAmmo < maxAmmo)
            {
                StartReload();
            }
        }
        
        private void Fire()
        {
            if (currentAmmo <= 0)
            {
                // Auto reload if out of ammo
                StartReload();
                return;
            }
            
            nextFireTime = Time.time + fireRate;
            currentAmmo--;
            
            // Client-side prediction
            PerformRaycast(out RaycastHit hit, out bool didHit);
            
            if (didHit)
            {
                // Show immediate feedback
                ShowImpactEffect(hit.point, hit.normal);
            }
            
            ShowMuzzleFlash();
            
            // Send to server for validation
            FireServerRpc(firePoint.position, firePoint.forward, Time.time);
        }
        
        /// <summary>
        /// Server validates and processes the shot
        /// </summary>
        [ServerRpc]
        private void FireServerRpc(Vector3 origin, Vector3 direction, float clientTime)
        {
            // Validate fire rate (anti-cheat)
            if (Time.time < nextFireTime - 0.1f) // Small tolerance for network delay
            {
                Debug.LogWarning($"Client {OwnerClientId} fired too quickly - possible cheat");
                return;
            }
            
            // Validate ammo
            if (networkAmmo.Value <= 0)
            {
                Debug.LogWarning($"Client {OwnerClientId} fired without ammo");
                return;
            }
            
            networkAmmo.Value--;
            
            // Perform authoritative raycast on server
            if (PerformAuthoritativeRaycast(origin, direction, clientTime, out RaycastHit hit, out bool didHit))
            {
                if (didHit)
                {
                    // Process hit
                    ProcessHit(hit);
                    
                    // Notify all clients about the hit
                    NotifyHitClientRpc(hit.point, hit.normal, hit.collider.gameObject);
                }
            }
        }
        
        private bool PerformRaycast(out RaycastHit hit, out bool didHit)
        {
            Ray ray = new Ray(firePoint.position, firePoint.forward);
            didHit = Physics.Raycast(ray, out hit, range);
            
            if (tracerEffect != null)
            {
                StartCoroutine(ShowTracerEffect(ray.origin, didHit ? hit.point : ray.origin + ray.direction * range));
            }
            
            return didHit;
        }
        
        private bool PerformAuthoritativeRaycast(Vector3 origin, Vector3 direction, float clientTime, out RaycastHit hit, out bool didHit)
        {
            // Calculate lag compensation
            float lag = Time.time - clientTime;
            
            // For now, simple raycast. Could implement lag compensation here
            Ray ray = new Ray(origin, direction);
            didHit = Physics.Raycast(ray, out hit, range);
            
            return didHit;
        }
        
        private void ProcessHit(RaycastHit hit)
        {
            // Check if hit a player
            var healthComponent = hit.collider.GetComponent<HealthSystem>();
            if (healthComponent != null)
            {
                // Apply damage through the health system
                healthComponent.TakeDamage(damage, OwnerClientId);
                Debug.Log($"Player {OwnerClientId} hit {hit.collider.name} for {damage} damage");
            }
        }
        
        [ClientRpc]
        private void NotifyHitClientRpc(Vector3 hitPoint, Vector3 hitNormal, NetworkObjectReference hitObjectRef)
        {
            if (IsOwner) return; // Owner already saw the hit
            
            ShowImpactEffect(hitPoint, hitNormal);
        }
        
        private void ShowMuzzleFlash()
        {
            if (muzzleFlashPrefab != null && firePoint != null)
            {
                GameObject flash = Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation);
                Destroy(flash, 0.1f);
            }
        }
        
        private void ShowImpactEffect(Vector3 position, Vector3 normal)
        {
            if (impactEffectPrefab != null)
            {
                GameObject impact = Instantiate(impactEffectPrefab, position, Quaternion.LookRotation(normal));
                Destroy(impact, 1f);
            }
        }
        
        private System.Collections.IEnumerator ShowTracerEffect(Vector3 start, Vector3 end)
        {
            if (tracerEffect != null)
            {
                tracerEffect.enabled = true;
                tracerEffect.SetPosition(0, start);
                tracerEffect.SetPosition(1, end);
                
                yield return new WaitForSeconds(0.05f);
                
                tracerEffect.enabled = false;
            }
        }
        
        private void StartReload()
        {
            if (isReloading) return;
            
            isReloading = true;
            ReloadServerRpc();
            Invoke(nameof(FinishReload), reloadTime);
        }
        
        [ServerRpc]
        private void ReloadServerRpc()
        {
            // Validate reload request
            if (networkAmmo.Value >= maxAmmo)
            {
                return;
            }
            
            Invoke(nameof(CompleteReloadOnServer), reloadTime);
        }
        
        private void CompleteReloadOnServer()
        {
            if (!IsServer) return;
            networkAmmo.Value = maxAmmo;
        }
        
        private void FinishReload()
        {
            isReloading = false;
            currentAmmo = maxAmmo;
        }
        
        public int GetCurrentAmmo()
        {
            return IsServer ? networkAmmo.Value : currentAmmo;
        }
        
        public int GetMaxAmmo()
        {
            return maxAmmo;
        }
        
        public bool IsReloading()
        {
            return isReloading;
        }
    }
}
