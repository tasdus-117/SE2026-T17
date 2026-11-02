using SmallArena.Client;
using SmallArena.Client.UI;
using SmallArena.Domain;
using SmallArena.Input;
using SmallArena.Pickups;
using SmallArena.Player;
using SmallArena.Server.Combat;
using UnityEngine;

namespace SmallArena.Core
{
    /// <summary>Checks the real offline gameplay and HUD dependencies before entering Playing.</summary>
    public sealed class OfflineMatchReadinessProbe : MonoBehaviour, IMatchReadinessProbe
    {
        [SerializeField] private GameManager gameManager = null;
        [SerializeField] private Health botHealth = null;
        [SerializeField] private CombatController playerCombat = null;
        [SerializeField] private WeaponPickup weaponPickup = null;
        [SerializeField] private MatchUI matchUI = null;

        public MatchReadinessResult ValidateReadiness()
        {
            if (gameManager == null || botHealth == null || playerCombat == null || weaponPickup == null || matchUI == null)
                return MatchReadinessResult.Fail("Offline match requires Health, combat, pickup and HUD references.");
            if (!botHealth.isActiveAndEnabled || botHealth.MaxHealth != 10 || botHealth.CurrentHealth != 10)
                return MatchReadinessResult.Fail("Bot Health must start alive at 10 / 10 HP.");
            var input = playerCombat.GetComponent<PlayerInputHandler>();
            var movement = playerCombat.GetComponent<PlayerController>();
            if (!playerCombat.isActiveAndEnabled || playerCombat.CurrentDamage != 1 || !playerCombat.CanInteract
                || input == null || !input.isActiveAndEnabled || movement == null || !movement.isActiveAndEnabled)
                return MatchReadinessResult.Fail("Player input, movement and unarmed combat must be initialized.");
            if (!weaponPickup.isActiveAndEnabled || weaponPickup.IsCollected)
                return MatchReadinessResult.Fail("Weapon pickup must start active and uncollected.");
            if (!matchUI.IsReady || !matchUI.IsBoundTo(gameManager, botHealth, playerCombat))
                return MatchReadinessResult.Fail("HUD must be active and bound to this match's Health and combat.");
            if (botHealth.gameObject.scene != gameManager.gameObject.scene
                || playerCombat.gameObject.scene != gameManager.gameObject.scene
                || weaponPickup.gameObject.scene != gameManager.gameObject.scene
                || matchUI.gameObject.scene != gameManager.gameObject.scene)
                return MatchReadinessResult.Fail("Offline dependencies must belong to the same arena scene.");
            return MatchReadinessResult.Success();
        }
    }
}
