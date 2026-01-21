using UnityEngine;

public class TowerUpgrade
{
    private int _upgradeLavel = 0; // 강화 단계
    private TowerPropertiesType _properties = TowerPropertiesType.None; // 속성

    public TowerPropertiesType Properties => _properties;

    /// <summary>
    /// 타워에 속성을 부여하는 함수
    /// </summary>
    /// <param name="propertiesType">부여할 속성</param>
    /// <returns>속성 부여 성공 여부</returns>
    public bool AddProperties(TowerPropertiesType propertiesType)
    {
        if (Properties != TowerPropertiesType.None) return false;
        if (propertiesType == TowerPropertiesType.None) return false;

        _properties = propertiesType;
        return true;
    }
}
