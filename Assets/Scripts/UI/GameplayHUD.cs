using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SmallArena.UI
{
    /// <summary>
    /// In-game HUD showing health, ammo, and scoreboard
    /// </summary>
    public class GameplayHUD : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] private Slider healthBar;
        [SerializeField] private TMP_Text healthText;
        
        [Header("Ammo")]
        [SerializeField] private TMP_Text ammoText;
        
        [Header("Scoreboard")]
        [SerializeField] private GameObject scoreboardPanel;
        [SerializeField] private Transform scoreboardContainer;
        [SerializeField] private GameObject scoreboardItemPrefab;
        
        [Header("Crosshair")]
        [SerializeField] private Image crosshair;
        
        [Header("Damage Indicator")]
        [SerializeField] private Image damageOverlay;
        [SerializeField] private float damageOverlayDuration = 0.5f;
        
        [Header("Kill Feed")]
        [SerializeField] private Transform killFeedContainer;
        [SerializeField] private GameObject killFeedItemPrefab;
        [SerializeField] private int maxKillFeedItems = 5;
        
        private Combat.HealthSystem localPlayerHealth;
        private Combat.WeaponController localPlayerWeapon;
        private float damageOverlayTimer;
        
        private void Start()
        {
            // Hide damage overlay initially
            if (damageOverlay != null)
            {
                Color color = damageOverlay.color;
                color.a = 0;
                damageOverlay.color = color;
            }
            
            // Hide scoreboard initially
            if (scoreboardPanel != null)
            {
                scoreboardPanel.SetActive(false);
            }
        }
        
        private void Update()
        {
            // Find local player if not set
            if (localPlayerHealth == null || localPlayerWeapon == null)
            {
                FindLocalPlayerComponents();
            }
            
            // Update health
            UpdateHealthDisplay();
            
            // Update ammo
            UpdateAmmoDisplay();
            
            // Handle damage overlay fade
            UpdateDamageOverlay();
            
            // Toggle scoreboard
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                ToggleScoreboard();
            }
        }
        
        private void FindLocalPlayerComponents()
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient)
            {
                return;
            }
            
            var localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject;
            
            if (localPlayer != null)
            {
                if (localPlayerHealth == null)
                {
                    localPlayerHealth = localPlayer.GetComponent<Combat.HealthSystem>();
                    
                    if (localPlayerHealth != null)
                    {
                        localPlayerHealth.OnDamageTaken += OnLocalPlayerDamaged;
                        localPlayerHealth.OnDeath += OnLocalPlayerDeath;
                    }
                }
                
                if (localPlayerWeapon == null)
                {
                    localPlayerWeapon = localPlayer.GetComponentInChildren<Combat.WeaponController>();
                }
            }
        }
        
        private void UpdateHealthDisplay()
        {
            if (localPlayerHealth == null) return;
            
            float healthPercentage = localPlayerHealth.CurrentHealth / localPlayerHealth.MaxHealth;
            
            if (healthBar != null)
            {
                healthBar.value = healthPercentage;
            }
            
            if (healthText != null)
            {
                healthText.text = $"{Mathf.CeilToInt(localPlayerHealth.CurrentHealth)} / {localPlayerHealth.MaxHealth}";
            }
        }
        
        private void UpdateAmmoDisplay()
        {
            if (localPlayerWeapon == null || ammoText == null) return;
            
            int currentAmmo = localPlayerWeapon.GetCurrentAmmo();
            int maxAmmo = localPlayerWeapon.GetMaxAmmo();
            
            if (localPlayerWeapon.IsReloading())
            {
                ammoText.text = "RELOADING...";
                ammoText.color = Color.yellow;
            }
            else
            {
                ammoText.text = $"{currentAmmo} / {maxAmmo}";
                ammoText.color = currentAmmo > 0 ? Color.white : Color.red;
            }
        }
        
        private void UpdateDamageOverlay()
        {
            if (damageOverlay == null) return;
            
            if (damageOverlayTimer > 0)
            {
                damageOverlayTimer -= Time.deltaTime;
                
                Color color = damageOverlay.color;
                color.a = Mathf.Lerp(0, 0.5f, damageOverlayTimer / damageOverlayDuration);
                damageOverlay.color = color;
            }
        }
        
        private void OnLocalPlayerDamaged(float damage, ulong attackerId)
        {
            // Show damage overlay
            damageOverlayTimer = damageOverlayDuration;
            
            // Could add directional damage indicator here
        }
        
        private void OnLocalPlayerDeath(ulong killerId)
        {
            // Show death screen or message
            Debug.Log($"You were killed by player {killerId}");
            
            // Add to kill feed
            AddKillFeedItem(killerId, NetworkManager.Singleton.LocalClientId);
        }
        
        private void ToggleScoreboard()
        {
            if (scoreboardPanel == null) return;
            
            bool isActive = !scoreboardPanel.activeSelf;
            scoreboardPanel.SetActive(isActive);
            
            if (isActive)
            {
                UpdateScoreboard();
            }
        }
        
        private void UpdateScoreboard()
        {
            // Clear existing items
            foreach (Transform child in scoreboardContainer)
            {
                Destroy(child.gameObject);
            }
            
            if (NetworkManager.Singleton == null) return;
            
            // Add all connected players
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                var playerObject = client.PlayerObject;
                if (playerObject == null) continue;
                
                var healthSystem = playerObject.GetComponent<Combat.HealthSystem>();
                if (healthSystem == null) continue;
                
                CreateScoreboardItem(
                    client.ClientId,
                    $"Player {client.ClientId}",
                    healthSystem.KillCount,
                    healthSystem.DeathCount
                );
            }
        }
        
        private void CreateScoreboardItem(ulong clientId, string playerName, int kills, int deaths)
        {
            if (scoreboardItemPrefab == null || scoreboardContainer == null) return;
            
            GameObject item = Instantiate(scoreboardItemPrefab, scoreboardContainer);
            
            var nameText = item.transform.Find("NameText")?.GetComponent<TMP_Text>();
            if (nameText != null)
            {
                nameText.text = playerName;
            }
            
            var killsText = item.transform.Find("KillsText")?.GetComponent<TMP_Text>();
            if (killsText != null)
            {
                killsText.text = kills.ToString();
            }
            
            var deathsText = item.transform.Find("DeathsText")?.GetComponent<TMP_Text>();
            if (deathsText != null)
            {
                deathsText.text = deaths.ToString();
            }
            
            var kdText = item.transform.Find("KDText")?.GetComponent<TMP_Text>();
            if (kdText != null)
            {
                float kd = deaths > 0 ? (float)kills / deaths : kills;
                kdText.text = kd.ToString("F2");
            }
        }
        
        private void AddKillFeedItem(ulong killerId, ulong victimId)
        {
            if (killFeedItemPrefab == null || killFeedContainer == null) return;
            
            // Remove oldest item if at max
            if (killFeedContainer.childCount >= maxKillFeedItems)
            {
                Destroy(killFeedContainer.GetChild(0).gameObject);
            }
            
            GameObject item = Instantiate(killFeedItemPrefab, killFeedContainer);
            
            var feedText = item.GetComponent<TMP_Text>();
            if (feedText != null)
            {
                feedText.text = $"Player {killerId} killed Player {victimId}";
            }
            
            // Auto-remove after a few seconds
            Destroy(item, 5f);
        }
        
        private void OnDestroy()
        {
            if (localPlayerHealth != null)
            {
                localPlayerHealth.OnDamageTaken -= OnLocalPlayerDamaged;
                localPlayerHealth.OnDeath -= OnLocalPlayerDeath;
            }
        }
    }
}
