using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// The building line at the top of a quarter's window (Round 78, stage 30): the building's level, what its next level gives with the price
    /// and the days (or that it is at the top level, or being built), the [업그레이드] button, and under the window's lists the note that the
    /// building is closed while under construction. The lobby fills it from the rules.
    /// </summary>
    public sealed class BuildingLineView : MonoBehaviour
    {
        [SerializeField] TMP_Text _level;
        [SerializeField] TMP_Text _effect;
        [SerializeField] Button _upgrade;
        [SerializeField] TMP_Text _upgradeLabel;
        [SerializeField] TMP_Text _locked;

        public Button Upgrade => _upgrade;
        public string Level => _level.text;
        public string Effect => _effect.text;
        public string UpgradeLabel => _upgradeLabel.text;
        public bool LockedShown => _locked.gameObject.activeSelf;

        /// <param name="upgradeLabel">The button's words, or null to hide the button (no next level, or one already being built).</param>
        public void Show(string level, string effect, string upgradeLabel, bool canUpgrade, bool locked)
        {
            _level.text = level;
            _effect.text = effect;
            _upgrade.gameObject.SetActive(upgradeLabel != null);
            _upgradeLabel.text = upgradeLabel ?? string.Empty;
            _upgrade.interactable = canUpgrade;
            _locked.gameObject.SetActive(locked);
        }
    }
}
