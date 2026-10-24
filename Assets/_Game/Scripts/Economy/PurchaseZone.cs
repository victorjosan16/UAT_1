using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Zonă de cumpărare unică (utilaje, muncitori, rafinărie). După plată activează obiectele-țintă
    /// și dispare. Pentru deblocări în lanț, pune următoarea zonă (inactivă) printre ținte.
    /// Atenție: ținta nu trebuie să fie copilul acestei zone (zona se dezactivează).
    /// </summary>
    public class PurchaseZone : PayZone
    {
        [SerializeField] private string id = "item";
        [SerializeField] private string title = "Obiect";
        [SerializeField, Min(1)] private long cost = 100;
        [SerializeField] private GameObject[] unlockTargets;

        public string Id => id;

        protected override string SaveKey => "buy_" + id;
        protected override string Title => Loc.T(title);
        protected override long Cost => cost;

        private void Awake()
        {
            if (SaveSystem.Instance != null && SaveSystem.Instance.Data.IsUnlocked(id)) ApplyUnlocked(false);
        }

        protected override void OnFullyPaid()
        {
            if (SaveSystem.Instance != null)
            {
                SaveSystem.Instance.Data.Unlock(id);
                SaveSystem.Instance.MarkDirty();
            }
            ApplyUnlocked(true);
        }

        private void ApplyUnlocked(bool announce)
        {
            if (unlockTargets != null)
                foreach (GameObject target in unlockTargets)
                    if (target != null) target.SetActive(true);

            if (announce)
            {
                GameEvents.RaisePurchased(id, title);
                GameEvents.RaiseToast(Loc.F("Deblocat: {0}!", Loc.T(title)));
            }
            gameObject.SetActive(false);
        }
    }
}
