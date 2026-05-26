using UnityEngine;

namespace KIM.Dev
{
    public interface ICanClickObject
    {
        public void OnLeftMouseDownThisObject(); // 좌클릭으로 눌렀을 때
        public void OnLeftMouseUpThisObject(); // 좌클릭을 떼어 클릭으로 이어졌을 때
        public void OnCancelClickThisObject(); // 클릭을 취소할 때
    }
}