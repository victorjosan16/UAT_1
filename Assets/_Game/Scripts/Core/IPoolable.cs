namespace QuarryEmpire
{
    /// <summary>
    /// Interfață pentru componentele care trebuie resetate când obiectul
    /// este scos din pool (OnSpawned) sau pus înapoi în pool (OnDespawned).
    /// </summary>
    public interface IPoolable
    {
        void OnSpawned();
        void OnDespawned();
    }
}
