using UnityEngine;

namespace KIM.Dev
{
    public static class TowerPropertyEffectApplier
    {
        public static void ApplyDamageAndEffect(
            Collider target,
            TowerPropertiesType propertyType,
            Tower sourceTower,
            float baseDamage,
            float finalDamage)
        {
            if (target == null)
                return;

            if (target.TryGetComponent(out IDamageable damageable))
                damageable.TakeDamage(finalDamage);

            ApplyEffect(target, propertyType, sourceTower, baseDamage, finalDamage);
        }

        public static void ApplyEffect(
            Collider target,
            TowerPropertiesType propertyType,
            Tower sourceTower,
            float baseDamage,
            float finalDamage)
        {
            if (target == null || propertyType == TowerPropertiesType.None)
                return;

            ITowerPropertyEffectReceiver receiver = target.GetComponent<ITowerPropertyEffectReceiver>();
            receiver ??= target.GetComponentInParent<ITowerPropertyEffectReceiver>();

            if (receiver == null)
                return;

            var context = new TowerPropertyEffectContext(
                propertyType,
                sourceTower,
                target,
                target.transform.position,
                baseDamage,
                finalDamage);
            receiver.ApplyTowerPropertyEffect(context);
        }
    }
}
