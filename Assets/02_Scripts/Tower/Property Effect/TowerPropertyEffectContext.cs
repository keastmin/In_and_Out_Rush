using UnityEngine;

namespace KIM.Dev
{
    public readonly struct TowerPropertyEffectContext
    {
        public TowerPropertyEffectContext(
            TowerPropertiesType propertyType,
            Tower sourceTower,
            Collider hitCollider,
            Vector3 hitPosition,
            float baseDamage,
            float finalDamage)
        {
            PropertyType = propertyType;
            SourceTower = sourceTower;
            HitCollider = hitCollider;
            HitPosition = hitPosition;
            BaseDamage = baseDamage;
            FinalDamage = finalDamage;
        }

        public TowerPropertiesType PropertyType { get; }
        public Tower SourceTower { get; }
        public Collider HitCollider { get; }
        public Vector3 HitPosition { get; }
        public float BaseDamage { get; }
        public float FinalDamage { get; }
    }
}
