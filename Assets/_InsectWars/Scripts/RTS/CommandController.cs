using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace InsectWars.RTS
{
    /// <summary>
    /// Right-click: move / attack / gather. Left-click with pending command: move, attack-move, patrol, gather.
    /// </summary>
    public class CommandController : MonoBehaviour
    {
        Camera _cam;
        Vector2 _lmbDownPos;
        bool _lmbTracked;

        void Awake()
        {
            _cam = Camera.main;
        }

        static bool PointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        void Update()
        {
            if (Mouse.current == null) return;
            if (_cam == null) _cam = Camera.main;

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                _lmbDownPos = Mouse.current.position.ReadValue();
                _lmbTracked = true;
            }

            if (Mouse.current.leftButton.wasReleasedThisFrame && _lmbTracked)
            {
                _lmbTracked = false;
                var end = Mouse.current.position.ReadValue();
                if (Vector2.Distance(_lmbDownPos, end) < 10f)
                    TryLeftClickCommand(end);
            }

            if (!Mouse.current.rightButton.wasPressedThisFrame) return;
            if (PointerOverUi()) return;
            BottomBar.Instance?.SetPending(PendingCommand.None);

            if (SelectionController.Instance == null) return;
            var ray = _cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            // Collide mode detects units and resources that use trigger colliders.
            // Distance 1500 ensures the terrain is always reachable regardless of camera height.
            if (!Physics.Raycast(ray, out var hit, 1500f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide)) return;

            var selectedHive = SelectionController.Instance.SelectedHive;
            if (selectedHive != null)
            {
                var rallyFruit = hit.collider.GetComponentInParent<RottingFruitNode>();
                if (rallyFruit != null && !rallyFruit.Depleted)
                    selectedHive.SetRallyGather(hit.point, rallyFruit);
                else
                    selectedHive.SetRallyPoint(hit.point);
                return;
            }

            if (SelectionController.Instance.SelectedBuilding != null)
            {
                var rallyFruit = hit.collider.GetComponentInParent<RottingFruitNode>();
                foreach (var b in SelectionController.Instance.SelectedBuildingsOfActiveType)
                {
                    if (rallyFruit != null && !rallyFruit.Depleted)
                        b.SetRallyGather(hit.point, rallyFruit);
                    else
                        b.SetRallyPoint(hit.point);
                }
                return;
            }

            var fruit = hit.collider.GetComponentInParent<RottingFruitNode>();
            if (fruit != null && !fruit.Depleted && SelectionController.Instance.HasWorkerSelected())
            {
                IssueGroupGatherOrMove(hit.point, fruit);
                return;
            }

            var enemy = hit.collider.GetComponentInParent<InsectUnit>();
            if (enemy != null && enemy.Team == Team.Enemy && enemy.IsAlive)
            {
                foreach (var u in SelectionController.Instance.SelectedPlayerUnits())
                    u.OrderAttack(enemy);
                return;
            }

            var enemyBuilding = hit.collider.GetComponentInParent<ProductionBuilding>();
            if (enemyBuilding != null && enemyBuilding.Team == Team.Enemy && enemyBuilding.IsAlive)
            {
                foreach (var u in SelectionController.Instance.SelectedPlayerUnits())
                    u.OrderAttackBuilding(enemyBuilding);
                return;
            }

            var enemyHive = hit.collider.GetComponentInParent<HiveDeposit>();
            if (enemyHive != null && enemyHive.Team == Team.Enemy && enemyHive.IsAlive)
            {
                foreach (var u in SelectionController.Instance.SelectedPlayerUnits())
                    u.OrderAttackHive(enemyHive);
                return;
            }

            // No special target found. If the primary hit was a trigger collider (e.g. a
            // player-unit capsule or a resource trigger sitting in front of the terrain),
            // re-cast ignoring triggers so we get the actual ground surface point.
            // This prevents "OrderMove(unit.position)" when right-clicking near own units.
            Vector3 moveTarget = hit.point;
            if (hit.collider.isTrigger)
            {
                if (Physics.Raycast(ray, out var groundHit, 1500f,
                        Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    moveTarget = groundHit.point;
            }

            IssueGroupMove(moveTarget);
        }

        void TryLeftClickCommand(Vector2 screen)
        {
            var pending = BottomBar.Pending;
            if (pending == PendingCommand.None && !PatrolCoordinator.WaitingForSecondPoint)
                return;
            if (PointerOverUi()) return;
            if (SelectionController.Instance == null || BottomBar.Instance == null) return;

            if (pending == PendingCommand.PlaceBuilding)
            {
                PlaceBuildingAtCursor(screen);
                return;
            }

            var hasSel = false;
            foreach (var _ in SelectionController.Instance.SelectedPlayerUnits())
            {
                hasSel = true;
                break;
            }
            if (!hasSel) return;

            var ray = _cam.ScreenPointToRay(screen);
            if (!Physics.Raycast(ray, out var hit, 1500f)) return;

            if (pending == PendingCommand.Gather)
            {
                var fruit = hit.collider.GetComponentInParent<RottingFruitNode>();
                if (fruit != null && !fruit.Depleted)
                {
                    foreach (var u in SelectionController.Instance.SelectedPlayerUnits())
                    {
                        if (u.Definition != null && u.Definition.canGather)
                            u.OrderGather(fruit);
                    }
                    BottomBar.Instance.SetPending(PendingCommand.None);
                    return;
                }
                return;
            }

            if (pending == PendingCommand.Patrol || PatrolCoordinator.WaitingForSecondPoint)
            {
                if (PatrolCoordinator.TryHandlePatrolClick(hit.point, out var a, out var b))
                    return;
                foreach (var u in SelectionController.Instance.SelectedPlayerUnits())
                    u.OrderPatrol(a, b);
                BottomBar.Instance.SetPending(PendingCommand.None);
                return;
            }

            var friend = hit.collider.GetComponentInParent<InsectUnit>();
            if (friend != null && friend.Team == Team.Player && friend.IsAlive)
                return;

            var enemy = hit.collider.GetComponentInParent<InsectUnit>();
            if (enemy != null && enemy.Team == Team.Enemy && enemy.IsAlive)
            {
                if (pending == PendingCommand.Attack)
                {
                    foreach (var u in SelectionController.Instance.SelectedPlayerUnits())
                        u.OrderAttack(enemy);
                    BottomBar.Instance.SetPending(PendingCommand.None);
                }
                return;
            }

            if (pending == PendingCommand.Attack)
            {
                var bldL = hit.collider.GetComponentInParent<ProductionBuilding>();
                if (bldL != null && bldL.IsAlive)
                {
                    foreach (var u in SelectionController.Instance.SelectedPlayerUnits())
                        u.OrderAttackBuilding(bldL);
                    BottomBar.Instance.SetPending(PendingCommand.None);
                    return;
                }
                var hiveL = hit.collider.GetComponentInParent<HiveDeposit>();
                if (hiveL != null && hiveL.IsAlive)
                {
                    foreach (var u in SelectionController.Instance.SelectedPlayerUnits())
                        u.OrderAttackHive(hiveL);
                    BottomBar.Instance.SetPending(PendingCommand.None);
                    return;
                }
            }

            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Resources"))
            {
                if (pending == PendingCommand.Move)
                {
                    var resFruit = hit.collider.GetComponentInParent<RottingFruitNode>();
                    if (resFruit != null && !resFruit.Depleted)
                    {
                        IssueGroupGatherOrMove(hit.point, resFruit);
                        BottomBar.Instance.SetPending(PendingCommand.None);
                    }
                }
                return;
            }

            if (pending == PendingCommand.Move)
            {
                IssueGroupMove(hit.point);
                BottomBar.Instance.SetPending(PendingCommand.None);
            }
            else if (pending == PendingCommand.Attack)
            {
                foreach (var u in SelectionController.Instance.SelectedPlayerUnits())
                    u.OrderAttackMove(hit.point);
                BottomBar.Instance.SetPending(PendingCommand.None);
            }
        }

        void PlaceBuildingAtCursor(Vector2 screen)
        {
            var ray = _cam.ScreenPointToRay(screen);
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (!plane.Raycast(ray, out var enter)) return;

            var flatPos = ray.GetPoint(enter);
            var terrain = Terrain.activeTerrain;
            float terrainY = terrain != null ? terrain.SampleHeight(flatPos) : flatPos.y;
            var worldPos = new Vector3(flatPos.x, terrainY, flatPos.z);

            var buildType = BottomBar.PendingBuildingType;

            if (!IsValidBuildLocation(worldPos, buildType))
                return;

            int cost = ProductionBuilding.GetBuildCost(buildType);
            if (PlayerResources.Instance == null || !PlayerResources.Instance.TrySpend(cost))
                return;

            var building = ProductionBuilding.Place(worldPos, buildType);

            InsectUnit builder = null;
            foreach (var u in SelectionController.Instance.SelectedPlayerUnits())
            {
                if (u.Definition != null && u.Definition.canGather)
                {
                    builder = u;
                    break;
                }
            }
            if (builder != null) builder.OrderBuild(building);

            BottomBar.Instance.SetPending(PendingCommand.None);
        }

        /// <summary>
        /// Validates whether a building can be placed at the given world position.
        /// Used by both CommandController and BottomBar ghost preview.
        /// </summary>
        public static bool IsValidBuildLocation(Vector3 worldPos, BuildingType buildType)
        {
            if (!BuildZoneRegistry.IsInBuildZone(worldPos))
                return false;

            var terrain = Terrain.activeTerrain;
            if (terrain != null)
            {
                var td = terrain.terrainData;
                var tp = terrain.transform.position;
                float normX = (worldPos.x - tp.x) / td.size.x;
                float normZ = (worldPos.z - tp.z) / td.size.z;
                if (normX >= 0f && normX <= 1f && normZ >= 0f && normZ <= 1f)
                {
                    float steepness = td.GetSteepness(normX, normZ);
                    if (steepness > 20f) return false;
                }
            }

            float footprint = ProductionBuilding.GetFootprintRadius(buildType);
            var colliders = Physics.OverlapSphere(worldPos, footprint, ~0, QueryTriggerInteraction.Collide);
            foreach (var col in colliders)
            {
                if (col.GetComponentInParent<ProductionBuilding>() != null) return false;
                if (col.GetComponentInParent<HiveDeposit>() != null) return false;
                if (col.GetComponentInParent<RottingFruitNode>() != null) return false;
                if (col.GetComponentInParent<InsectUnit>() != null) return false;
            }

            if (buildType == BuildingType.AntNest)
            {
                const float minAppleDistance = 8f;
                foreach (var fruit in RtsSimRegistry.FruitNodes)
                {
                    if (fruit == null) continue;
                    if (Vector3.Distance(worldPos, fruit.transform.position) < minAppleDistance)
                        return false;
                }
            }

            // TODO: fog-of-war explored check
            return true;
        }

        void IssueGroupMove(Vector3 center)
        {
            var units = new List<InsectUnit>();
            foreach (var u in SelectionController.Instance.SelectedPlayerUnits())
                units.Add(u);

            if (units.Count <= 1)
            {
                foreach (var u in units)
                    u.OrderMove(center);
                return;
            }

            var offsets = ComputeFormationOffsets(units.Count);
            for (int i = 0; i < units.Count; i++)
            {
                var agent = units[i].Agent;
                float r = agent != null ? agent.radius : 0.4f;
                var dest = center + offsets[i] * (r * 2.2f);
                units[i].OrderMove(dest);
            }
        }

        void IssueGroupGatherOrMove(Vector3 center, RottingFruitNode fruit)
        {
            var movers = new List<InsectUnit>();
            foreach (var u in SelectionController.Instance.SelectedPlayerUnits())
            {
                if (u.Definition != null && u.Definition.canGather)
                    u.OrderGather(fruit);
                else
                    movers.Add(u);
            }

            if (movers.Count <= 1)
            {
                foreach (var u in movers)
                    u.OrderMove(center);
                return;
            }

            var offsets = ComputeFormationOffsets(movers.Count);
            for (int i = 0; i < movers.Count; i++)
            {
                var agent = movers[i].Agent;
                float r = agent != null ? agent.radius : 0.4f;
                var dest = center + offsets[i] * (r * 2.2f);
                movers[i].OrderMove(dest);
            }
        }

        static List<Vector3> ComputeFormationOffsets(int count)
        {
            var offsets = new List<Vector3>(count);
            if (count <= 0) return offsets;

            offsets.Add(Vector3.zero);
            int placed = 1;
            int ring = 1;
            while (placed < count)
            {
                int perRing = Mathf.Max(6, ring * 6);
                for (int i = 0; i < perRing && placed < count; i++)
                {
                    float angle = Mathf.PI * 2f * i / perRing;
                    offsets.Add(new Vector3(Mathf.Cos(angle) * ring, 0f, Mathf.Sin(angle) * ring));
                    placed++;
                }
                ring++;
            }
            return offsets;
        }
    }
}
