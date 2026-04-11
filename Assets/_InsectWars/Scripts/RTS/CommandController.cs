using UnityEngine;
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
            if (!Physics.Raycast(ray, out var hit, 500f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return;

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

            var selectedBuilding = SelectionController.Instance.SelectedBuilding;
            if (selectedBuilding != null)
            {
                var rallyFruit = hit.collider.GetComponentInParent<RottingFruitNode>();
                if (rallyFruit != null && !rallyFruit.Depleted)
                    selectedBuilding.SetRallyGather(hit.point, rallyFruit);
                else
                    selectedBuilding.SetRallyPoint(hit.point);
                return;
            }

            var fruit = hit.collider.GetComponentInParent<RottingFruitNode>();
            if (fruit != null && !fruit.Depleted && SelectionController.Instance.HasWorkerSelected())
            {
                foreach (var u in SelectionController.Instance.SelectedPlayerUnits())
                {
                    if (u.Definition != null && u.Definition.canGather)
                        u.OrderGather(fruit);
                    else
                        u.OrderMove(hit.point); // Non-gatherers should still move there
                }
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

            foreach (var u in SelectionController.Instance.SelectedPlayerUnits())
                u.OrderMove(hit.point);
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
            if (!Physics.Raycast(ray, out var hit, 500f)) return;

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
                var enemyBuildingL = hit.collider.GetComponentInParent<ProductionBuilding>();
                if (enemyBuildingL != null && enemyBuildingL.Team == Team.Enemy && enemyBuildingL.IsAlive)
                {
                    foreach (var u in SelectionController.Instance.SelectedPlayerUnits())
                        u.OrderAttackBuilding(enemyBuildingL);
                    BottomBar.Instance.SetPending(PendingCommand.None);
                    return;
                }
                var enemyHiveL = hit.collider.GetComponentInParent<HiveDeposit>();
                if (enemyHiveL != null && enemyHiveL.Team == Team.Enemy && enemyHiveL.IsAlive)
                {
                    foreach (var u in SelectionController.Instance.SelectedPlayerUnits())
                        u.OrderAttackHive(enemyHiveL);
                    BottomBar.Instance.SetPending(PendingCommand.None);
                    return;
                }
            }

            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Resources"))
                return;

            if (pending == PendingCommand.Move)
            {
                foreach (var u in SelectionController.Instance.SelectedPlayerUnits())
                    u.OrderMove(hit.point);
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

            if (!BuildZoneRegistry.IsInBuildZone(worldPos))
                return;

            var buildType = BottomBar.PendingBuildingType;
            int cost = ProductionBuilding.GetBuildCost(buildType);

            if (PlayerResources.Instance == null || !PlayerResources.Instance.TrySpend(cost))
                return;

            var building = ProductionBuilding.Place(worldPos, buildType);

            foreach (var u in SelectionController.Instance.SelectedPlayerUnits())
            {
                if (u.Definition != null && u.Definition.canGather)
                    u.OrderBuild(building);
            }

            BottomBar.Instance.SetPending(PendingCommand.None);
        }
    }
}
