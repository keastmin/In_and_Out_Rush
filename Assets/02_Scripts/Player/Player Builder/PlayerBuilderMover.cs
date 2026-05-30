using Unity.Cinemachine;
using UnityEngine;

namespace KIM.Dev
{
    public class PlayerBuilderMover : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera _builderCam;
        [SerializeField] private float _moveSpeed = 10f;
        [SerializeField] private float _movePixel = 5f;

        #region API

        /// <summary>
        /// 빌더의 무버 초기화 함수
        /// </summary>
        /// <param name="cineCam"></param>
        public void InitBuilderMover(CinemachineCamera cineCam)
        {
            _builderCam = cineCam;
        }

        /// <summary>
        /// 빌더의 카메라를 이동시키는 함수
        /// </summary>
        public void Move()
        {

        }

        #endregion
    }

}