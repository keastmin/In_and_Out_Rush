using UnityEngine;

namespace KIM.Dev
{
    public readonly struct CenterTowerSkillEffectContext
    {
        public CenterTowerSkillEffectContext(
            TowerPropertiesType propertyType,
            CenterTower sourceTower,
            TrackMonster target,
            Vector3 explosionPosition,
            float explosionRange,
            float stunDuration)
        {
            PropertyType = propertyType;
            SourceTower = sourceTower;
            Target = target;
            ExplosionPosition = explosionPosition;
            ExplosionRange = explosionRange;
            StunDuration = stunDuration;
        }

        public TowerPropertiesType PropertyType { get; }
        public CenterTower SourceTower { get; }
        public TrackMonster Target { get; }
        public Vector3 ExplosionPosition { get; }
        public float ExplosionRange { get; }
        public float StunDuration { get; }
    }
}
