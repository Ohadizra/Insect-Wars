using System.Collections.Generic;
using InsectWars.Data;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

namespace InsectWars.RTS
{
    public enum BuildingType
    {
        MantisBranch,
        AntNest
    }

    public class ProductionBuilding : MonoBehaviour
    {
        static readonly List<ProductionBuilding> s_all = new();
        public static IReadOnlyList<ProductionBuilding> All => s_all;

        BuildingType _type;
        Team _team = Team.Player;
        Vector3? _rallyPoint;
        RottingFruitNode _rallyGatherTarget;
        GameObject _rallyFlag;

        public BuildingType Type => _type;
        public Team Team => _team;
        public Vector3? RallyPoint => _rallyPoint;
        public RottingFruitNode RallyGatherTarget => _rallyGatherTarget;

        public string DisplayName => _type switch
        {
            BuildingType.MantisBranch => "Manti's\nBranch",
            BuildingType.AntNest => "Ant's\nNest",
            _ => _type.ToString()
        };

        public UnitArchetype ProducedArchetype => _type switch
        {
            BuildingType.MantisBranch => UnitArchetype.BasicFighter,
            BuildingType.AntNest => UnitArchetype.Worker,
            _ => UnitArchetype.Worker
        };

        public int UnitCost => _type switch
        {
            BuildingType.MantisBranch => 100,
            BuildingType.AntNest => 50,
            _ => 50
        };

        public string UnitName => _type switch
        {
            BuildingType.MantisBranch => "Mantis",
            BuildingType.AntNest => "Worker",
            _ => "Unit"
        };

        public static int GetBuildCost(BuildingType type) => type switch
        {
            BuildingType.MantisBranch => 150,
            BuildingType.AntNest => 400,
            _ => 100
        };

        void OnDestroy()
        {
            s_all.Remove(this);
            if (_rallyFlag != null) Destroy(_rallyFlag);
        }

        public void Initialize(BuildingType type, Team team = Team.Player)
        {
            _type = type;
            _team = team;
            s_all.Add(this);
        }

        public InsectUnit ProduceUnit()
        {
            if (_team == Team.Player && PlayerResources.Instance != null && !PlayerResources.Instance.TrySpend(UnitCost))
                return null;

            var center = new Vector3(transform.position.x, 0f, transform.position.z);
            var extent = transform.localScale.x * 0.5f + 1.5f;
            var angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            var offset = new Vector3(Mathf.Cos(angle) * extent, 0f, Mathf.Sin(angle) * extent);
            var spawnPos = center + offset;
            if (NavMesh.SamplePosition(spawnPos, out var hit, 4f, NavMesh.AllAreas))
                spawnPos = hit.position;

            var unit = SkirmishDirector.SpawnUnit(spawnPos, _team, ProducedArchetype);
            if (unit == null) return null;

            if (_rallyGatherTarget != null && !_rallyGatherTarget.Depleted &&
                unit.Definition != null && unit.Definition.canGather)
                unit.OrderGather(_rallyGatherTarget);
            else if (_rallyPoint.HasValue)
                unit.OrderMove(_rallyPoint.Value);

            return unit;
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
                Destroy(pole.GetComponent<Collider>());
                ApplyMat(pole, new Color(0.9f, 0.9f, 0.7f));

                var banner = GameObject.CreatePrimitive(PrimitiveType.Cube);
                banner.name = "Banner";
                banner.transform.SetParent(_rallyFlag.transform, false);
                banner.transform.localPosition = new Vector3(0.2f, 1.05f, 0f);
                banner.transform.localScale = new Vector3(0.35f, 0.22f, 0.05f);
                Destroy(banner.GetComponent<Collider>());
                ApplyMat(banner, TeamPalette.GetTeamColor(_team)); // Team color banner
            }

            _rallyFlag.SetActive(true);
            _rallyFlag.transform.position = _rallyPoint.Value;
        }

        public static ProductionBuilding Place(Vector3 position, BuildingType type, Team team = Team.Player)
        {
            if (type == BuildingType.AntNest)
            {
                var lib = SkirmishDirector.ActiveVisualLibrary;
                if (lib != null && lib.hivePrefab != null)
                    return PlaceAntNestFromPrefab(position, lib.hivePrefab, team);
            }

            var go = new GameObject($"Building_{type}");
            go.transform.position = position;

            Color buildingColor;
            Vector3 scale;
            switch (type)
            {
                case BuildingType.MantisBranch:
                    buildingColor = new Color(0.45f, 0.7f, 0.3f);
                    scale = new Vector3(3f, 3.5f, 3f);
                    break;
                case BuildingType.AntNest:
                    buildingColor = new Color(0.5f, 0.35f, 0.2f);
                    scale = new Vector3(3.5f, 2f, 3.5f);
                    break;
                default:
                    buildingColor = Color.gray;
                    scale = new Vector3(3f, 2f, 3f);
                    break;
            }

            go.transform.localScale = scale;

            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Visual";
            visual.transform.SetParent(go.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            visual.transform.localScale = Vector3.one;
            Destroy(visual.GetComponent<Collider>());
            ApplyMat(visual, buildingColor);
            
            // Straps for buildings
            var strapColor = TeamPalette.GetTeamColor(team);
            var strap1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            strap1.name = "Strap_Top";
            strap1.transform.SetParent(go.transform, false);
            strap1.transform.localPosition = new Vector3(0f, 1.02f, 0f);
            strap1.transform.localScale = new Vector3(0.5f, 0.05f, 0.5f);
            Destroy(strap1.GetComponent<Collider>());
            ApplyMat(strap1, strapColor);

            var strap2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            strap2.name = "Strap_Side";
            strap2.transform.SetParent(go.transform, false);
            strap2.transform.localPosition = new Vector3(0.52f, 0.5f, 0f);
            strap2.transform.localScale = new Vector3(0.05f, 0.4f, 0.15f);
            Destroy(strap2.GetComponent<Collider>());
            ApplyMat(strap2, strapColor);

            var col = go.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.5f, 0f);
            col.size = Vector3.one;

            var obs = go.AddComponent<NavMeshObstacle>();
            obs.carving = true;
            obs.shape = NavMeshObstacleShape.Box;
            obs.size = new Vector3(1f, 1f, 1f);
            obs.center = new Vector3(0f, 0.5f, 0f);

            var building = go.AddComponent<ProductionBuilding>();
            building.Initialize(type, team);

            return building;
        }

        static ProductionBuilding PlaceAntNestFromPrefab(Vector3 position, GameObject hivePrefab, Team team = Team.Player)
        {
            var go = Object.Instantiate(hivePrefab);
            go.name = "Building_AntNest";
            go.transform.position = new Vector3(position.x, 1f, position.z);
            go.tag = "Untagged";

            var hd = go.GetComponent<HiveDeposit>();
            if (hd != null) Destroy(hd);
            var hv = go.GetComponent<HiveVisual>();
            if (hv != null) Destroy(hv);

            if (go.GetComponent<Collider>() == null)
            {
                var col = go.AddComponent<BoxCollider>();
                col.center = new Vector3(0f, 0.5f, 0f);
                col.size = new Vector3(2f, 2f, 2f);
            }

            if (go.GetComponent<NavMeshObstacle>() == null)
            {
                var obs = go.AddComponent<NavMeshObstacle>();
                obs.carving = true;
                obs.shape = NavMeshObstacleShape.Box;
                obs.size = new Vector3(2f, 2f, 2f);
                obs.center = new Vector3(0f, 0.5f, 0f);
            }
            
            // Apply skin color to prefab renderers
            var skinColor = TeamPalette.GetShellColor(team);
            foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.gameObject.name == "TeamStrap") continue;
                var mats = renderer.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    var m = new Material(mats[i]);
                    if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", skinColor);
                    else if (m.HasProperty("_Color")) m.color = skinColor;
                    mats[i] = m;
                }
                renderer.sharedMaterials = mats;
            }

            // Straps for AntNest prefab
            var strap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            strap.name = "TeamStrap";
            strap.transform.SetParent(go.transform, false);
            strap.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            strap.transform.localScale = new Vector3(1.5f, 0.05f, 1.5f);
            Destroy(strap.GetComponent<Collider>());
            ApplyMat(strap, TeamPalette.GetTeamColor(team));

            var building = go.AddComponent<ProductionBuilding>();
            building.Initialize(BuildingType.AntNest, team);
            return building;
            }

        static void ApplyMat(GameObject go, Color c)
        {
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            var m = new Material(sh);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.color = c;
            var r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = m;
        }
    }
}
