//Stylized Water 2
//Staggart Creations (http://staggart.xyz)
//Copyright protected under Unity Asset Store EULA

using System;
using System.Collections.Generic;
using System.Net;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using UnityEngine;

namespace StylizedWater2
{
    public class AssetInfo
    {
        private const string THIS_FILE_GUID = "d5972a1c9cddd9941aec37a3343647aa";
        
        public const string ASSET_NAME = "Stylized Water 2";
        public const string ASSET_ID = "170386";
        public const string ASSET_ABRV = "SW2";

        public const string INSTALLED_VERSION = "1.6.3";
        
        public const int SHADER_GENERATOR_VERSION_MAJOR = 1;
        public const int SHADER_GENERATOR_MINOR = 2; 
        public const int SHADER_GENERATOR_PATCH = 0;
        
        public const string MIN_UNITY_VERSION = "2021.3.16f1";
        public const string MIN_URP_VERSION = "12.1.6";

        private const string VERSION_FETCH_URL = "http://www.staggart.xyz/backend/versions/stylizedwater2.php";
        public const string DOC_URL = "http://staggart.xyz/unity/stylized-water-2/sws-2-docs/";
        public const string FORUM_URL = "https://forum.unity.com/threads/999132/";
        public const string EMAIL_URL = "mailto:contact@staggart.xyz?subject=Stylized Water 2";

        public static bool IS_UPDATED = true;
        public static bool supportedVersion = true;
        public static bool compatibleVersion = true;
        public static bool alphaVersion = false;

#if !URP //Enabled when com.unity.render-pipelines.universal is below MIN_URP_VERSION
        [InitializeOnLoad]
        sealed class PackageInstaller : Editor
        {
            [InitializeOnLoadMethod]
            public static void Initialize()
            {
                GetLatestCompatibleURPVersion();

                if (EditorUtility.DisplayDialog(ASSET_NAME + " v" + INSTALLED_VERSION, "This package requires the Universal Render Pipeline " + MIN_URP_VERSION + " or newer, would you like to install or update it now?", "OK", "Later"))
                {
					Debug.Log("Universal Render Pipeline <b>v" + lastestURPVersion + "</b> will start installing in a moment. Please refer to the URP documentation for set up instructions");
					Debug.Log("After installing and setting up URP, you must Re-import the Shaders folder!");
					
                    InstallURP();
                }
            }

            private static PackageInfo urpPackage;

            private static string lastestURPVersion;

#if SWS_DEV
            [MenuItem("SWS/Get latest URP version")]
#endif
            private static void GetLatestCompatibleURPVersion()
            {
                if(urpPackage == null) urpPackage = GetURPPackage();
                if(urpPackage == null) return;
                
                lastestURPVersion = urpPackage.versions.latestCompatible;
                
#if SWS_DEV
                Debug.Log("Latest compatible URP version: " + lastestURPVersion);
#endif
            }

            private static void InstallURP()
            {
                if(urpPackage == null) urpPackage = GetURPPackage();
                if(urpPackage == null) return;
                
                lastestURPVersion = urpPackage.versions.latestCompatible;

                AddRequest addRequest = Client.Add(URP_PACKAGE_ID + "@" + lastestURPVersion);
                
                //Update Core and Shader Graph packages as well, doesn't always happen automatically
                for (int i = 0; i < urpPackage.dependencies.Length; i++)
                {
#if SWS_DEV
                    Debug.Log("Updating URP dependency <i>" + urpPackage.dependencies[i].name + "</i> to " + urpPackage.dependencies[i].version);
#endif
                    addRequest = Client.Add(urpPackage.dependencies[i].name + "@" + urpPackage.dependencies[i].version);
                }
                
                //Wait until finished
                while(!addRequest.IsCompleted || addRequest.Status == StatusCode.InProgress) { }
                
                WaterShaderImporter.ReimportAll();
            }
        }
#endif
        
        public const string URP_PACKAGE_ID = "com.unity.render-pipelines.universal";

        public static PackageInfo GetURPPackage()
        {
            SearchRequest request = Client.Search(URP_PACKAGE_ID);
                
            while (request.Status == StatusCode.InProgress) { /* Waiting... */ }

            if (request.Status == StatusCode.Failure)
            {
                Debug.LogError("Failed to retrieve URP package from Package Manager...");
                return null;
            }

            return request.Result[0];
        }

        //Sorry, as much as I hate to intrude on an entire project, this is the only way in Unity to track importing or updating an asset
        public class ImportOrUpdateAsset : AssetPostprocessor
        {
            private static bool OldShadersPresent()
            {
                return Shader.Find("Universal Render Pipeline/FX/Stylized Water 2") || Shader.Find("Hidden/StylizedWater2/Deleted");
            }
            
            private void OnPreprocessAsset()
            {
                var oldShaders = false;
                //Importing/updating the Stylized Water 2 asset
                if (assetPath.EndsWith("StylizedWater2/Editor/AssetInfo.cs") || assetPath.EndsWith("sc.stylizedwater2/Editor/AssetInfo.cs"))
                {
                    oldShaders = OldShadersPresent();
                }
                //These files change every version, so will trigger when updating or importing the first time
                if (
                    //Importing the Underwater Rendering extension
                    assetPath.EndsWith("UnderwaterRenderer.cs"))
                    //Any further extensions...
                {
                    OnImportExtension("Underwater Rendering");
                    
                    oldShaders = OldShadersPresent();
                }

                if (assetPath.EndsWith("WaterDynamicEffectsRenderFeature.cs"))
                {
                    OnImportExtension("Dynamic Effects");
                }
                
                if (oldShaders)
                {
                    //Old non-templated shader(s) still present.
                    if (EditorUtility.DisplayDialog(AssetInfo.ASSET_NAME, "Updating to v1.4.0+. Obsolete shader files were detected." +
                                                                          "\n\n" +
                                                                          "The water shader(s) are a now C# generated, thus has moved to a different file." +
                                                                          "\n\n" +
                                                                          "Materials in your project using the old shader must switch to it." +
                                                                          "\n\n" +
                                                                          "This process is automatic. The obsolete files will also be deleted." +
                                                                          "\n\n" +
                                                                          "Rest assured, no settings or functionality will be lost!", "OK", "Cancel"))
                    {
                        UpgradeMaterials();
                    }
                }

            }

            private void OnImportExtension(string name)
            {
                Debug.Log($"[Stylized Water 2] {name} extension installed/deleted or updated. Reimporting water shader(s) to toggle integration.");
                
                //Re-import any .watershader files, since these depend on the installation state of extensions
                WaterShaderImporter.ReimportAll();
            }
        }

        #if SWS_DEV
        [MenuItem("SWS/Upgrading/Upgrade materials to 1.4.0+")]
        #endif
        private static void UpgradeMaterials()
        {
            Shader oldShader = Shader.Find("Universal Render Pipeline/FX/Stylized Water 2");
            Shader oldShaderTess = Shader.Find("Universal Render Pipeline/FX/Stylized Water 2 (Tessellation)");
            
            Shader newShader = Shader.Find("Stylized Water 2/Standard");
            Shader newShaderTess = Shader.Find("Stylized Water 2/Standard (Tessellation)");

            int upgradedMaterialCount = 0;
            
            //Possible the script imported before the water shader even did
            //Or the water shader was already deleted, yet this function is triggered by an old underwater-rendering shader.
            if (newShader != null || oldShader != null)
            {
                string[] materials = AssetDatabase.FindAssets("t: Material");

                int totalMaterials = materials.Length;
                
                for (int i = 0; i < totalMaterials; i++)
                {
                    Material mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(materials[i]));

                    EditorUtility.DisplayProgressBar(ASSET_NAME, $"Checking \"{mat.name}\" ({i}/{totalMaterials})", (float)i / (float)totalMaterials);
                    
                    if (mat.shader == oldShader)
                    {
                        int queue = mat.renderQueue;
                        mat.shader = newShader;
                        mat.renderQueue = queue;
                        EditorUtility.SetDirty(mat);
                        upgradedMaterialCount++;
                    }
                    
                    if (mat.shader == oldShaderTess)
                    {
                        int queue = mat.renderQueue;
                        mat.shader = newShaderTess;
                        mat.renderQueue = queue;
                        
                        EditorUtility.SetDirty(mat);
                        upgradedMaterialCount++;
                    }
                }
            }
            
            EditorUtility.ClearProgressBar();

            List<string> deletedFiles = new List<string>();
            //Delete old files
            {
                void DeleteFile(string path)
                {
                    if (path != string.Empty)
                    {
                        deletedFiles.Add(path);
                        AssetDatabase.DeleteAsset(path);
                    }
                }

                DeleteFile(AssetDatabase.GetAssetPath(oldShader));
                DeleteFile(AssetDatabase.GetAssetPath(oldShaderTess));
                
                //UnderwaterMask.shader
                DeleteFile(AssetDatabase.GUIDToAssetPath("85caab243c23a1e489cd0bdcaf742560"));
                
                //UnderwaterShading.shader
                DeleteFile(AssetDatabase.GUIDToAssetPath("33652c9d694d8004bb649164aafc9ecc"));
                
                //Waterline.shader
                DeleteFile(AssetDatabase.GUIDToAssetPath("aef8d53c098c94044b72ea6bda9bb6bc"));
                
                //UnderwaterPost.shader
                DeleteFile(AssetDatabase.GUIDToAssetPath("7b53b35c69174ce092d2712e3db77f0e"));
            }

            if (upgradedMaterialCount > 0 || deletedFiles.Count > 0)
            {
                if (EditorUtility.DisplayDialog(AssetInfo.ASSET_NAME, $"Converted {upgradedMaterialCount} materials to use the new water shader." + 
                                                                      $"\n\nObsolete shader files ({deletedFiles.Count}) deleted:\n\n" +
                                                                      String.Join(Environment.NewLine, deletedFiles), 
                    "OK")) { }
            }
            
            AssetDatabase.SaveAssets();

            Debug.Log("<b>[Stylized Water 2]</b> Upgrade of materials and project complete!");
        }
        
        public static bool MeetsMinimumVersion(string versionMinimum)
        {
            Version curVersion = new Version(INSTALLED_VERSION);
            Version minVersion = new Version(versionMinimum);

            return curVersion >= minVersion;
        }

        public static void OpenAssetStore(string url = null)
        {
            if (url == string.Empty) url = "https://assetstore.unity.com/packages/slug/" + ASSET_ID;
            
            Application.OpenURL(url + "?aid=1011l7Uk8&pubref=sw2editor");
        }

        public static void OpenReviewsPage()
        {
            Application.OpenURL($"https://assetstore.unity.com/packages/slug/{ASSET_ID}?aid=1011l7Uk8&pubref=sw2editor#reviews");
        }
        
        public static void OpenInPackageManager()
        {
            Application.OpenURL("com.unity3d.kharma:content/" + ASSET_ID);
        }
        
        public static void OpenForumPage()
        {
            Application.OpenURL(FORUM_URL + "/page-999");
        }
        
        public static string GetRootFolder()
        {
            //Get script path
            string scriptFilePath = AssetDatabase.GUIDToAssetPath(THIS_FILE_GUID);

            //Truncate to get relative path
            string rootFolder = scriptFilePath.Replace("Editor/AssetInfo.cs", string.Empty);

#if SWS_DEV
            //Debug.Log("<b>Package root</b> " + rootFolder);
#endif

            return rootFolder;
        }

        public static class VersionChecking
        {
            public static string GetUnityVersion()
            {
                string version = UnityEditorInternal.InternalEditorUtility.GetFullUnityVersion();
                
                //Remove GUID in parenthesis 
                return version.Substring(0, version.LastIndexOf(" ("));
            }
            
            public static void CheckUnityVersion()
            {
                #if !UNITY_2020_3_OR_NEWER
                compatibleVersion = false;
                #endif
                
                #if !UNITY_2021_3_OR_NEWER
                supportedVersion = false;
                #endif

                alphaVersion = GetUnityVersion().Contains("f") == false;
            }

            public static string fetchedVersionString;
            public static System.Version fetchedVersion;
            private static bool showPopup;

            public enum VersionStatus
            {
                UpToDate,
                Outdated
            }

            public enum QueryStatus
            {
                Fetching,
                Completed,
                Failed
            }
            public static QueryStatus queryStatus = QueryStatus.Completed;

#if SWS_DEV
            [MenuItem("SWS/Check for update")]
#endif
            public static void GetLatestVersionPopup()
            {
                CheckForUpdate(true);
            }

            private static int VersionStringToInt(string input)
            {
                //Remove all non-alphanumeric characters from version 
                input = input.Replace(".", string.Empty);
                input = input.Replace(" BETA", string.Empty);
                return int.Parse(input, System.Globalization.NumberStyles.Any);
            }

            public static void CheckForUpdate(bool showPopup = false)
            {
                VersionChecking.showPopup = showPopup;

                queryStatus = QueryStatus.Fetching;

                using (WebClient webClient = new WebClient())
                {
                    webClient.DownloadStringCompleted += new System.Net.DownloadStringCompletedEventHandler(OnRetrievedServerVersion);
                    webClient.DownloadStringAsync(new System.Uri(VERSION_FETCH_URL), fetchedVersionString);
                }
            }

            private static void OnRetrievedServerVersion(object sender, DownloadStringCompletedEventArgs e)
            {
                if (e.Error == null && !e.Cancelled)
                {
                    fetchedVersionString = e.Result;
                    fetchedVersion = new System.Version(fetchedVersionString);
                    System.Version installedVersion = new System.Version(INSTALLED_VERSION);

                    //Success
                    IS_UPDATED = (installedVersion >= fetchedVersion) ? true : false;

#if SWS_DEV
                    Debug.Log("<b>PackageVersionCheck</b> Up-to-date = " + IS_UPDATED + " (Installed:" + INSTALLED_VERSION + ") (Remote:" + fetchedVersionString + ")");
#endif

                    queryStatus = QueryStatus.Completed;

                    if (VersionChecking.showPopup)
                    {
                        if (!IS_UPDATED)
                        {
                            if (EditorUtility.DisplayDialog(ASSET_NAME + ", version " + INSTALLED_VERSION, "An updated version is available: " + fetchedVersionString, "Open Package Manager", "Close"))
                            {
                                OpenInPackageManager();
                            }
                        }
                        else
                        {
                            if (EditorUtility.DisplayDialog(ASSET_NAME + ", version " + INSTALLED_VERSION, "Installed version is up-to-date!", "Close")) { }
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("[" + ASSET_NAME + "] Contacting update server failed: " + e.Error.Message);
                    queryStatus = QueryStatus.Failed;

                    //When failed, assume installation is up-to-date
                    IS_UPDATED = true;
                }
            }
        }
    }
}