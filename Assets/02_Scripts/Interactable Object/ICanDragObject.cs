using UnityEngine;

namespace KIM.Dev
{
    public interface ICanDragObject
    {
        public void OnDragSelectedThisObject(); // 드래그 중에 영역 안에 있을 때의 함수
        public void OnDragOverThisObject(); // 드래그 중에 영역에서 벗어났을 때의 함수
        public void OnDragCompleteThisObject(); // 드래그가 끝났고 그 시점에 이 오브젝트가 드래그 영역 안에 있을 때의 함수
    }
}