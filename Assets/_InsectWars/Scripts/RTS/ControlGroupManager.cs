using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace InsectWars.RTS
{
    /// <summary>
    /// SC2-style control groups (keys 1-0 → slots 0-9).
    /// Groups can contain units, production buildings, and/or the player hive.
    /// Ctrl/Cmd+Number = set, Shift+Number = add to group,
    /// Alt+Number = append group to selection, Number = recall,
    /// double-tap Number = recall + center camera.
    /// </summary>
    public class ControlGroupManager : MonoBehaviour
    {
        public static ControlGroupManager Instance { get; private set; }

        const int GroupCount = 10;
        const float DoubleTapThreshold = 0.3f;

        readonly HashSet<InsectUnit>[] _units = new HashSet<InsectUnit>[GroupCount];
        readonly HashSet<ProductionBuilding>[] _buildings = new HashSet<ProductionBuilding>[GroupCount];
        readonly HiveDeposit[] _hives = new HiveDeposit[GroupCount];
        readonly float[] _lastRecallTime = new float[GroupCount];

        /// <summary>Index of the group most recently recalled (or -1).</summary>
        public int ActiveGroup { get; private set; } = -1;

        public IReadOnlyCollection<InsectUnit> GetGroupUnits(int index)
        {
            if (index < 0 || index >= GroupCount) return null;
            return _units[index];
        }

        public IReadOnlyCollection<ProductionBuilding> GetGroupBuildings(int index)
        {
            if (index < 0 || index >= GroupCount) return null;
            return _buildings[index];
        }

        public HiveDeposit GetGroupHive(int index)
        {
            if (index < 0 || index >= GroupCount) return null;
            return _hives[index];
        }

        /// <summary>Total entity count in the group (units + buildings + hive).</summary>
        public int GetGroupCount(int index)
        {
            if (index < 0 || index >= GroupCount) return 0;
            int n = _units[index].Count + _buildings[index].Count;
            if (_hives[index] != null) n++;
            return n;
        }

        void Awake()
        {
            Instance = this;
            for (int i = 0; i < GroupCount; i++)
            {
                _units[i] = new HashSet<InsectUnit>();
                _buildings[i] = new HashSet<ProductionBuilding>();
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            bool isMac = SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX;
            bool createMod = isMac
                ? kb.leftCommandKey.isPressed || kb.rightCommandKey.isPressed
                : kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed;
            bool addMod = kb.leftShiftKey.isPressed;
            bool appendSelectMod = kb.leftAltKey.isPressed;

            for (int i = 0; i < GroupCount; i++)
            {
                if (!GetNumberKey(kb, i).wasPressedThisFrame) continue;

                if (createMod)
                    SetGroup(i);
                else if (addMod)
                    AddToGroup(i);
                else if (appendSelectMod)
                    AppendGroupToSelection(i);
                else
                    RecallGroup(i);

                break;
            }
        }

        void LateUpdate()
        {
            PruneDead();
        }

        // ──────────── Group Operations ────────────

        void SetGroup(int index)
        {
            var sc = SelectionController.Instance;
            if (sc == null) return;

            _units[index].Clear();
            _buildings[index].Clear();
            _hives[index] = null;

            foreach (var u in sc.SelectedPlayerUnits())
                _units[index].Add(u);

            foreach (var b in sc.SelectedBuildings)
            {
                if (b != null && b.Team == Team.Player && b.IsAlive)
                    _buildings[index].Add(b);
            }

            var hive = sc.SelectedHive;
            if (hive != null && hive.Team == Team.Player && hive.IsAlive)
                _hives[index] = hive;

            if (GetGroupCount(index) > 0)
                ActiveGroup = index;
        }

        void AddToGroup(int index)
        {
            var sc = SelectionController.Instance;
            if (sc == null) return;

            foreach (var u in sc.SelectedPlayerUnits())
                _units[index].Add(u);

            foreach (var b in sc.SelectedBuildings)
            {
                if (b != null && b.Team == Team.Player && b.IsAlive)
                    _buildings[index].Add(b);
            }

            var hive = sc.SelectedHive;
            if (hive != null && hive.Team == Team.Player && hive.IsAlive)
                _hives[index] = hive;
        }

        void AppendGroupToSelection(int index)
        {
            var sc = SelectionController.Instance;
            if (sc == null) return;
            if (GetGroupCount(index) == 0) return;

            if (_units[index].Count > 0)
                sc.AddToSelection(_units[index]);
            else if (_buildings[index].Count > 0)
                sc.SelectBuildings(_buildings[index]);
            else if (_hives[index] != null)
                sc.SelectHive(_hives[index]);

            ActiveGroup = index;
        }

        public void RecallGroup(int index)
        {
            var sc = SelectionController.Instance;
            if (sc == null) return;
            if (GetGroupCount(index) == 0) return;

            float now = Time.unscaledTime;
            bool doubleTap = ActiveGroup == index
                && (now - _lastRecallTime[index]) <= DoubleTapThreshold;

            _lastRecallTime[index] = now;
            ActiveGroup = index;

            if (_units[index].Count > 0)
                sc.SetSelection(_units[index]);
            else if (_buildings[index].Count > 0)
                sc.SelectBuildings(_buildings[index]);
            else if (_hives[index] != null)
                sc.SelectHive(_hives[index]);

            if (doubleTap)
                CenterCameraOnGroup(index);
        }

        void CenterCameraOnGroup(int index)
        {
            var centroid = Vector3.zero;
            int count = 0;

            foreach (var u in _units[index])
            {
                if (u == null || !u.IsAlive) continue;
                centroid += u.transform.position;
                count++;
            }
            foreach (var b in _buildings[index])
            {
                if (b == null || !b.IsAlive) continue;
                centroid += b.transform.position;
                count++;
            }
            if (_hives[index] != null && _hives[index].IsAlive)
            {
                centroid += _hives[index].transform.position;
                count++;
            }

            if (count == 0) return;
            centroid /= count;

            var cam = Camera.main;
            if (cam == null) return;
            var ctrl = cam.GetComponent<RTSCameraController>();
            if (ctrl == null) ctrl = cam.GetComponentInParent<RTSCameraController>();
            if (ctrl != null)
                ctrl.FocusWorldPosition(centroid);
        }

        void PruneDead()
        {
            for (int i = 0; i < GroupCount; i++)
            {
                _units[i].RemoveWhere(u => u == null || !u.IsAlive);
                _buildings[i].RemoveWhere(b => b == null || !b.IsAlive);
                if (_hives[i] != null && (_hives[i] == null || !_hives[i].IsAlive))
                    _hives[i] = null;
            }
        }

        // ──────────── Input Helpers ────────────

        static KeyControl GetNumberKey(Keyboard kb, int groupIndex)
        {
            return groupIndex switch
            {
                0 => kb.digit1Key,
                1 => kb.digit2Key,
                2 => kb.digit3Key,
                3 => kb.digit4Key,
                4 => kb.digit5Key,
                5 => kb.digit6Key,
                6 => kb.digit7Key,
                7 => kb.digit8Key,
                8 => kb.digit9Key,
                9 => kb.digit0Key,
                _ => kb.digit1Key
            };
        }
    }
}
