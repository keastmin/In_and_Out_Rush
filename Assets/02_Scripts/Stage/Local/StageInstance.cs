using System;

namespace Dev.Local
{
    public class StageInstance
    {
        public static StageInstance Instance { get; private set; }

        private int _mineral, _gas;

        public float Health;
        public float Stamina;
        public float MovementSpeed;
        public Territory Territory;
        public TerritoryVisible TerritoryVisible;
        public TerritoryExpansion TerritoryExpansion;
        public Track Track;
        public TrackVisible TrackVisible;

        public int Mineral
        {
            get => _mineral;
            set
            {
                _mineral = value;
                OnMineralChanged?.Invoke(_mineral);
            }
        }

        public int Gas
        {
            get => _gas;
            set
            {
                _gas = value;
                OnGasChanged?.Invoke(_gas);
            }
        }

        public event Action<int> OnMineralChanged;
        public event Action<int> OnGasChanged;

        public void InitializeTerritoryExpansion()
            => TerritoryExpansion = new(Territory);

        public static void Create()
        {
            if (Instance != null)
                return;

            Instance = new StageInstance();
        }

        public static void Clear()
        {
            Instance = null;
        }
    }
}