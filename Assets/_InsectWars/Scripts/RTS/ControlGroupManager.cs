using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace InsectWars.RTS
{
    /// <summary>
    /// SC2-style control groups (keys 1-0 → slots 0-9).
    /// Ctrl/Cmd+Number = set, Shift+Number = add to group,
    /// Alt+Number = append group to selection, Number = recall,
    /// double-tap Number = recall + center camera.
    /// </summary>
    public class ControlGroupManager : MonoBehaviour
    {
        public static ControlGroupManager Instance { get; private set; }

        const int GroupCount = 10;
        const float DoubleTapThreshold = 0.3f;

        readonly HashSet<InsectUnit>[] _groups = new HashSet<InsectUnit>[GroupCount];
        readonly float[] _lastRecallTime = new float[GroupCount];

        /// <summary>Index of the group most recently recalled (or -1).</summary>
        public int ActiveGroup { get; private set; } = -1;

        public IReadOnlyCollection<InsectUnit> GetGroup(int index)
        {
            if (index < 0 || index >= GroupCount) return null;
            return _groups[index];
        }

        void Awake()
        {
            Instance = this;
            for (int i = 0; i < GroupCount; i++)
                _groups[i] = new HashSet<InsectUnit>();
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

                break; // only one group key per frame
            }
        }

        void LateUpdate()
        {
            PruneDeadUnits();
        }

        // ──────────── Group Operations ────────────

        void SetGroup(int index)
        {
            var sc = SelectionController.Instance;
            if (sc == null) return;

            _groups[index].Clear();
            foreach (var u in sc.SelectedPlayerUnits())
                _groups[index].Add(u);

            ActiveGroup = _groups[index].Count > 0 ? index : ActiveGroup;
        }

        void AddToGroup(int index)
        {
            var sc = SelectionController.Instance;
            if (sc == null) return;

            foreach (var u in sc.SelectedPlayerUnits())
                _groups[index].Add(u);
        }

        void AppendGroupToSelection(int index)
        {
            var sc = SelectionController.Instance;
            if (sc == null) return;
            if (_groups[index].Count == 0) return;

            sc.AddToSelection(_groups[index]);
            ActiveGroup = index;
        }

        public void RecallGroup(int index)
        {
            var sc = SelectionController.Instance;
            if (sc == null) return;
            if (_groups[index].Count == 0) return;

            float now = Time.unscaledTime;
            bool doubleTap = ActiveGroup == index
                && (now - _lastRecallTime[index]) <= DoubleTapThreshold;

            _lastRecallTime[index] = now;
            ActiveGroup = index;

            sc.SetSelection(_groups[index]);

            if (doubleTap)
                CenterCameraOnGroup(index);
        }

        void CenterCameraOnGroup(int index)
        {
            if (_groups[index].Count == 0) return;

            var centroid = Vector3.zero;
            int count = 0;
            foreach (var u in _groups[index])
            {
                if (u == null || !u.IsAlive) continue;
                centroid += u.transform.position;
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

        void PruneDeadUnits()
        {
            for (int i = 0; i < GroupCount; i++)
                _groups[i].RemoveWhere(u => u == null || !u.IsAlive);
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
