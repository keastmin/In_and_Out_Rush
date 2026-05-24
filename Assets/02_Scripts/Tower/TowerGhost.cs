using UnityEngine;

namespace KIM.Dev
{
    public class TowerGhost : MonoBehaviour
    {
        [SerializeField] private MeshRenderer[] _meshRenderers;
        [SerializeField] private Material _enableMat;
        [SerializeField] private Material _disableMat;
        [SerializeField] private Transform _buffTransform;
        [SerializeField] private bool _showLegacyBuffRangeCircle = false;

        private void Awake()
        {
            CacheMeshRenderers();
        }

        private void OnValidate()
        {
            CacheMeshRenderers();
        }

        public void InitializePreview()
        {
            CacheMeshRenderers();

            if (TryGetComponent(out Tower tower))
            {
                tower.OnCancelClickThisObject();
            }

            Behaviour[] behaviours = GetComponentsInChildren<Behaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                Behaviour behaviour = behaviours[i];
                if (behaviour == null || behaviour == this)
                    continue;

                behaviour.enabled = false;
            }

            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }

            if (_buffTransform != null)
            {
                _buffTransform.gameObject.SetActive(_showLegacyBuffRangeCircle);
            }

            DisableTower();
        }

        public void EnableTower()
        {
            ApplyMaterial(_enableMat);
        }

        public void DisableTower()
        {
            ApplyMaterial(_disableMat);
        }

        public void SetGhostBuffRange(float range)
        {
            if (_buffTransform != null && _showLegacyBuffRangeCircle)
            {
                Vector3 localScale = new Vector3(range, range, range);
                _buffTransform.localScale = localScale;
            }
        }

        private void CacheMeshRenderers()
        {
            _meshRenderers = GetComponentsInChildren<MeshRenderer>(true);
        }

        private void ApplyMaterial(Material material)
        {
            if (material == null || _meshRenderers == null)
                return;

            for (int i = 0; i < _meshRenderers.Length; i++)
            {
                MeshRenderer meshRenderer = _meshRenderers[i];
                if (meshRenderer == null)
                    continue;

                Material[] materials = meshRenderer.materials;
                if (materials == null || materials.Length == 0)
                {
                    meshRenderer.material = material;
                    continue;
                }

                for (int j = 0; j < materials.Length; j++)
                {
                    materials[j] = material;
                }

                meshRenderer.materials = materials;
            }
        }
    }
}