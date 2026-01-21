using UnityEngine;

namespace KDM
{
    public interface IBuffRecieve
    {
        public void BuffEnter();
        public void BuffStay();
        public void BuffExit();
    }
}