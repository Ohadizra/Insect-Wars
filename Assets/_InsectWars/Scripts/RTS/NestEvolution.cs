using UnityEngine;

namespace InsectWars.RTS
{
    public class NestEvolution : MonoBehaviour
    {
        [SerializeField] GameObject stage2VisualPrefab;
        [SerializeField] int evolveCost = 300;

        public bool IsEvolved { get; private set; }
        public int EvolveCost => evolveCost;

        public bool CanEvolve()
        {
            if (IsEvolved) return false;
            return PlayerResources.Instance != null && PlayerResources.Instance.Calories >= evolveCost;
        }

        public void Evolve()
        {
            if (IsEvolved) return;
            if (PlayerResources.Instance == null || !PlayerResources.Instance.TrySpend(evolveCost)) return;

            IsEvolved = true;

            // Find existing visual and replace it
            Transform visualParent = transform.Find("Visual");
            if (visualParent != null)
            {
                // Disable existing renderer on the "Visual" child if it has one
                var existingRenderer = visualParent.GetComponent<MeshRenderer>();
                if (existingRenderer != null) existingRenderer.enabled = false;

                // Destroy any children that might have been added before
                foreach (Transform child in visualParent)
                {
                    Destroy(child.gameObject);
                }

                if (stage2VisualPrefab != null)
                {
                    GameObject newVisual = Instantiate(stage2VisualPrefab, visualParent);
                    newVisual.transform.localPosition = Vector3.zero;
                    newVisual.transform.localRotation = Quaternion.identity;
                    newVisual.transform.localScale = Vector3.one;
                }
            }
        }
}
}
