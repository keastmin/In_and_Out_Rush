using System;
using System.Collections.Generic;
using UnityEngine;

namespace KIM.Dev
{
    public class SupplyTowerManager : MonoBehaviour
    {
        public static SupplyTowerManager Instance { get; private set; }

        private List<int> _suppliesNums; // 보급품 번호를 기억해두는 리스트

        public int PendingSupplyCount => _suppliesNums?.Count ?? 0; // 현재 대기 중인 보급품 개수
        public bool HasPendingSupplies => PendingSupplyCount > 0; // 대기 중인 보급품이 하나라도 있는지 여부

        public event Action OnUIRevertAction; // Laboratory UI에서 보급 관련 UI를 초기화하는 이벤트

        // 각 보급품을 나타내는 상수
        public const int SKILL_SUPPLY_NUM = 1;
        public const int WEAPON_SUPPLY_NUM = 2;
        public const int ITEM_SUPPLY_NUM = 3;

        public Dictionary<int, Func<IObtainable>> NumToSupplies; // 보급품 번호와 생성 함수를 연결한 딕셔너리

        private void Awake()
        {
            Instance = this;

            // 딕셔너리 채우기
            NumToSupplies = new Dictionary<int, Func<IObtainable>>
        {
            { SKILL_SUPPLY_NUM, () => new Skill() },
            { WEAPON_SUPPLY_NUM, () => new Weapon() },
            { ITEM_SUPPLY_NUM, () => new Item() },
        };

            _suppliesNums = new List<int>();
        }

        public void FillSupplyList(int num)
        {
            _suppliesNums.Add(num);
        }

        /// <summary>
        /// 연구소 UI의 보급 슬롯을 초기화하는 함수
        /// </summary>
        public void RevertLaboratorySupplySlotRevert()
        {
            OnUIRevertAction?.Invoke();
        }

        /// <summary>
        /// 현재 보급품 리스트를 배열로 바꿔서 반환하는 함수
        /// </summary>
        public int[] GetSupplyNumArray()
        {
            int listCount = _suppliesNums.Count;
            int[] supplyArray = new int[listCount];
            for (int i = 0; i < listCount; i++)
            {
                supplyArray[i] = _suppliesNums[i];
            }

            RevertSupplyManagerSlot();
            return supplyArray;
        }

        /// <summary>
        /// 매니저가 가지고 있는 보급품 목록을 초기화하는 함수
        /// </summary>
        private void RevertSupplyManagerSlot()
        {
            _suppliesNums.Clear();
        }
    }

}