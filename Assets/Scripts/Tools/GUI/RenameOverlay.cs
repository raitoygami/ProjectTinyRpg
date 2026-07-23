#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[Serializable]
public class RenameOverlay {
    [SerializeField] private bool m_UserAcceptedRename;
    [SerializeField] private string m_Name;
    [SerializeField] private string m_OriginalName;
    [SerializeField] private Rect m_EditFieldRect;
    [SerializeField] private int m_UserData;
    [SerializeField] private bool m_IsWaitingForDelay;
    [SerializeField] private bool m_IsRenaming = false;
    [SerializeField] private EventType m_OriginalEventType = EventType.Ignore;
    [SerializeField] private bool m_IsRenamingFilename = false;

    [SerializeField] private bool m_TrimLeadingAndTrailingWhitespace = false;

    [NonSerialized] private bool m_UndoRedoWasPerformed;

    private string k_RenameOverlayFocusName = "RenameOverlayField";

    // property interface
    public string name {
        get => m_Name;
        internal set => m_Name = value;
    }

    public string originalName => m_OriginalName;

    public bool userAcceptedRename => m_UserAcceptedRename;

    public int userData => m_UserData;

    public Rect editFieldRect {
        get => m_EditFieldRect;
        set => m_EditFieldRect = value;
    }

    public bool isRenamingFilename {
        get => m_IsRenamingFilename;
        set => m_IsRenamingFilename = value;
    }

    public bool trimLeadingAndTrailingWhitespace {
        get => m_TrimLeadingAndTrailingWhitespace;
        set => m_TrimLeadingAndTrailingWhitespace = value;
    }

    private static GUIStyle s_DefaultTextFieldStyle = null;

    // Returns true if started renaming
    public bool BeginRename(string newName, int userCustomData, float delay) {
        if (m_IsRenaming) {
            return false;
        }

        m_Name = newName;
        m_OriginalName = newName;
        m_UserData = userCustomData;
        m_UserAcceptedRename = false;
        m_IsWaitingForDelay = delay > 0f;
        m_IsRenaming = true;
        m_EditFieldRect = new Rect(0, 0, 0, 0);
        
        BeginRenameInternalCallback();
        return true;
    }

    private void BeginRenameInternalCallback() {
        m_IsWaitingForDelay = false;

        Undo.undoRedoEvent -= UndoRedoWasPerformed;
        Undo.undoRedoEvent += UndoRedoWasPerformed;
    }

    public void EndRename(bool acceptChanges) {
        EditorGUIUtility.editingTextField = false;
        if (!m_IsRenaming)
            return;

        Undo.undoRedoEvent -= UndoRedoWasPerformed;
        EditorApplication.update -= BeginRenameInternalCallback;
        

        if (isRenamingFilename)
            m_Name = InternalEditorUtility.RemoveInvalidCharsFromFileName(m_Name, true);

        if (trimLeadingAndTrailingWhitespace) {
            var trimmedName = m_Name.Trim();
            if (!string.Equals(trimmedName, m_Name, StringComparison.Ordinal)) {
                m_Name = trimmedName;
            }
        }

        m_IsRenaming = false;
        m_IsWaitingForDelay = false;
        m_UserAcceptedRename = acceptChanges;

    }

    public void Clear() {
        m_IsRenaming = false;
        m_UserAcceptedRename = false;
        m_Name = "";
        m_OriginalName = "";
        m_EditFieldRect = new Rect();
        m_UserData = 0;
        m_IsWaitingForDelay = false;
        m_OriginalEventType = EventType.Ignore;
        Undo.undoRedoEvent -= UndoRedoWasPerformed;
    }

    void UndoRedoWasPerformed(in UndoRedoInfo info) {
        // If undo/redo was performed then close the rename overlay as it does not support undo/redo
        // We need to delay the EndRename until next OnGUI as clients poll the state of the rename overlay state there
        m_UndoRedoWasPerformed = true;
    }

    public bool HasKeyboardFocus() {
        return (GUI.GetNameOfFocusedControl() == k_RenameOverlayFocusName);
    }

    public bool IsRenaming() {
        return m_IsRenaming;
    }

    // Should be called as early as possible in an EditorWindow using this RenameOverlay
    // Returns: false if rename was ended due to input while waiting for delay
    public bool OnEvent() {
        if (!m_IsRenaming)
            return true;

        // Workaround for Event not having the original eventType stored
        m_OriginalEventType = Event.current.type;

        // Clear state if necessary while waiting for rename (0.5 second)
        if (!m_IsWaitingForDelay || m_OriginalEventType is not (EventType.MouseDown or EventType.KeyDown)) return true;
        EndRename(false);
        return false;

    }

    public bool OnGUI() {
        return OnGUI(new GUIStyle());
    }

    public bool OnGUI(GUIStyle textFieldStyle) {
        if (m_IsWaitingForDelay) {
            // Delayed start
            return true;
        }

        // Ended from outside
        if (!m_IsRenaming) {
            return false;
        }

        if (m_UndoRedoWasPerformed) {
            m_UndoRedoWasPerformed = false;
            EndRename(false);
            return false;
        }

        if (m_EditFieldRect.width <= 0 || m_EditFieldRect.height <= 0) {
            HandleUtility.Repaint();
            return true;
        }

        var evt = Event.current;
        if (evt.type == EventType.KeyDown) {
            switch (evt.keyCode) {
                case KeyCode.Escape:
                    evt.Use();
                    EndRename(false);
                    return false;
                case KeyCode.Return or KeyCode.KeypadEnter:
                    evt.Use();
                    EndRename(true);
                    return false;
            }
        }

        if (m_OriginalEventType == EventType.MouseDown && (Event.current.type == EventType.Used ||
                                                           !m_EditFieldRect.Contains(Event.current
                                                               .mousePosition))) {
            EndRename(true);
            return false;
        }

        m_Name = DoTextField(m_Name, textFieldStyle);

        if (evt.type == EventType.ScrollWheel)
            evt.Use();

        return true;
    }

    private string DoTextField(string text, GUIStyle textFieldStyle) {
        s_DefaultTextFieldStyle ??= "PR TextField";

        var style = textFieldStyle ?? s_DefaultTextFieldStyle;
        var rect = EditorGUI.IndentedRect(m_EditFieldRect);
        rect.xMin -= style.padding.left;
        return EditorGUI.TextField(rect, text);
    }

    private Rect GetScreenRect() {
        return GUIUtility.GUIToScreenRect(m_EditFieldRect);
    }
}
#endif