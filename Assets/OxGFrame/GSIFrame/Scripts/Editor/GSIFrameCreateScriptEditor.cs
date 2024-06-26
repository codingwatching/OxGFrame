﻿using UnityEditor;

namespace OxGFrame.GSIFrame.Editor
{
    public static class GSIFrameCreateScriptEditor
    {
        // Template GSIManager Path
        private const string _TPL_GSI_MANAGER_SCRIPT_PATH = "TplScripts/GSIFrame/TplGSIManager.cs.txt";
        // Template GSIBase Path
        private const string _TPL_GSI_BASE_SCRIPT_PATH = "TplScripts/GSIFrame/TplGSI.cs.txt";

        // find current file path
        private static string _pathFinder
        {
            get
            {
                var g = AssetDatabase.FindAssets("t:Script GSIFrameCreateScriptEditor");
                return AssetDatabase.GUIDToAssetPath(g[0]);
            }
        }

        #region GSIFrame Script Create
        [MenuItem(itemName: "Assets/Create/OxGFrame/GSI Frame/Template Scripts/Template GSI.cs (Game Stage)", isValidateFunction: false, priority: 51)]
        public static void CreateScriptTplGSIBase()
        {
            string currentPath = _pathFinder;
            string finalPath = currentPath.Replace("GSIFrameCreateScriptEditor.cs", "") + _TPL_GSI_BASE_SCRIPT_PATH;

            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(finalPath, "NewTplGSI.cs");
        }

        [MenuItem(itemName: "Assets/Create/OxGFrame/GSI Frame/Template Scripts/Template GSIManager.cs (Game Stage Manager)", isValidateFunction: false, priority: 51)]
        public static void CreateScriptTplGSIManager()
        {
            string currentPath = _pathFinder;
            string finalPath = currentPath.Replace("GSIFrameCreateScriptEditor.cs", "") + _TPL_GSI_MANAGER_SCRIPT_PATH;

            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(finalPath, "NewTplGSIManager.cs");
        }
        #endregion
    }
}