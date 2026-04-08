using UnityEngine;
using UnityEngine.AI;

namespace InsectWars.RTS
{
    public class HiveDeposit : MonoBehaviour
    {
        public static HiveDeposit PlayerHive { get; private set; }
        public static HiveDeposit EnemyHive { get; private set; }

        [SerializeField] Team team = Team.Player;
        Vector3? _rallyPoint;
        RottingFruitNode _rallyGatherTarget;
        GameObject _rallyFlag;

        public Team Team => team;
        public Vector3? RallyPoint => _rallyPoint;
        public RottingFruitNode RallyGatherTarget => _rallyGatherTarget;

        void Awake()
        {
            RegisterHive();
        }

        void OnDestroy()
        {
            if (PlayerHive == this) PlayerHive = null;
            if (EnemyHive == this) EnemyHive = null;
        }

        public void Configure(Team t)
        {
            if (PlayerHive == this) PlayerHive = null;
            if (EnemyHive == this) EnemyHive = null;
            team = t;
            RegisterHive();
        }

        void RegisterHive()
        {
            if (team == Team.Player)
                PlayerHive = this;
            else if (team == Team.Enemy)
                EnemyHive = this;
        }

        /// <summary>
        /// Ant Nest uses the hive prefab but strips <see cref="HiveDeposit"/>. Instantiate still runs Awake on the
        /// duplicate component and overwrites static refs; restore the real hive after <c>Destroy(hd)</c>.
        /// </summary>
        public static void RestorePlayerHiveReference(HiveDeposit mainPlayerHive)
        {
            if (mainPlayerHive != null)
                PlayerHive = mainPlayerHive;
        }

        public static void RestoreEnemyHiveReference(HiveDeposit mainEnemyHive)
        {
            if (mainEnemyHive != null)
                EnemyHive = mainEnemyHive;
        }

        public Vector3 DepositPoint
        {
            get
            {
                var ground = new Vector3(transform.position.x, 0f, transform.position.z);
                if (NavMesh.SamplePosition(ground, out var hit, 4f, NavMesh.AllAreas))
                    return hit.position;
                return ground;
            }
        }

        public void SetRallyPoint(Vector3 pos)
        {
            _rallyPoint = pos;
            _rallyGatherTarget = null;
            SyncRallyFlag();
        }

        public void SetRallyGather(Vector3 pos, RottingFruitNode node)
        {
            _rallyPoint = pos;
            _rallyGatherTarget = node;
            SyncRallyFlag();
        }

        public void ClearRally()
        {
            _rallyPoint = null;
            _rallyGatherTarget = null;
            SyncRallyFlag();
        }

        void SyncRallyFlag()
        {
            if (_rallyPoint == null)
            {
                if (_rallyFlag != null) _rallyFlag.SetActive(false);
                return;
            }

            if (_rallyFlag == null)
            {
                _rallyFlag = new GameObject("RallyFlag");

                var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pole.name = "Pole";
                pole.transform.SetParent(_rallyFlag.transform, false);
                pole.transform.localPosition = new Vector3(0f, 0.6f, 0f);
                pole.transform.localScale = new Vector3(0.08f, 0.6f, 0.08f);
                Object.Destroy(pole.GetComponent<Collider>());
                ApplyFlagMat(pole, new Color(0.9f, 0.9f, 0.7f));

                var banner = GameObject.CreatePrimitive(PrimitiveType.Cube);
                banner.name = "Banner";
                banner.transform.SetParent(_rallyFlag.transform, false);
                banner.transform.localPosition = new Vector3(0.2f, 1.05f, 0f);
                banner.transform.localScale = new Vector3(0.35f, 0.22f, 0.05f);
                Object.Destroy(banner.GetComponent<Collider>());
                ApplyFlagMat(banner, new Color(0.3f, 1f, 0.45f, 0.9f));
            }

            _rallyFlag.SetActive(true);
            _rallyFlag.transform.position = _rallyPoint.Value;
        }

        static void ApplyFlagMat(GameObject go, Color c)
        {
            var sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Unlit");
            var m = new Material(sh);
            if (m.HasProperty("_Color")) m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            var r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = m;
        }
    }
}
