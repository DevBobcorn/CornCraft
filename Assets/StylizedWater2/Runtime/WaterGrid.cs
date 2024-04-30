using System;
using System.Collections.Generic;
using UnityEngine;

namespace StylizedWater2
{
    [ExecuteInEditMode]
    [AddComponentMenu("Stylized Water 2/Water Grid")]
    public class WaterGrid : MonoBehaviour
    {
        [Tooltip("Material used on the tile meshes")]
        public Material material;
        
        [Tooltip("When not in play-mode, the water will follow the scene-view camera position.")]
        public bool followSceneCamera = false;
        [Tooltip("If enabled, the object with the \"MainCamera\" tag will be assigned as the follow target when entering play mode")]
        public bool autoAssignCamera;
        [Tooltip("The grid will follow this Transform's position on the XZ axis. Ideally set to the camera's transform.")]
        public Transform followTarget;
        
        [Tooltip("Scale of the entire grid in the length and width")]
        public float scale = 500f;
        [Range(0.15f, 10f)] 
        [Tooltip("Distance between vertices, rather higher than lower")]
        public float vertexDistance = 2f;
        [Min(0)]
        public int rowsColumns = 4;

        [HideInInspector]
        public int m_rowsColumns = 4;
        [SerializeField]
        [HideInInspector]
        private Mesh mesh;
        [SerializeField]
        [HideInInspector]
        private List<WaterObject> objects = new List<WaterObject>();
        
        [NonSerialized]
        private float tileSize;
        [NonSerialized]
        private WaterObject m_waterObject = null;
        [NonSerialized]
        private Transform actualFollowTarget;
        [NonSerialized]
        private Vector3 targetPosition;

        #if UNITY_EDITOR
        public static bool DisplayGrid = true;
        public static bool DisplayWireframe;
        #endif

        private void Reset()
        {
            Recreate();
        }
        
        private void Start()
        {
            if (autoAssignCamera) followTarget = Camera.main ? Camera.main.transform : followTarget;
        }
        
        private void OnEnable()
        {
#if UNITY_EDITOR
            UnityEditor.SceneView.duringSceneGui += OnSceneGUI;
#endif
            m_rowsColumns = rowsColumns;

            //Mesh is serialized with the scene, if component is used as a prefab, regenerate it
            if (mesh == null)
            {
                RecreateMesh();
                ReassignMesh();
            }
        }

#if UNITY_EDITOR
        private void OnDisable()
        {
            UnityEditor.SceneView.duringSceneGui -= OnSceneGUI;
        }
#endif

        void Update()
        {
            if (Application.isPlaying) actualFollowTarget = followTarget;

            if (actualFollowTarget)
            {
                targetPosition = actualFollowTarget.transform.position;

                targetPosition = SnapToGrid(targetPosition, vertexDistance);
                targetPosition.y = this.transform.position.y;
                this.transform.position = targetPosition;
            }
        }

        public void Recreate()
        {
            RecreateMesh();

            bool requireRecreate = (m_rowsColumns != rowsColumns) || objects.Count < (rowsColumns * rowsColumns);
            if (requireRecreate) m_rowsColumns = rowsColumns;

            //Only destroy/recreate objects if grid subdivision has changed
            if (requireRecreate && objects.Count > 0)
            {
                foreach (WaterObject obj in objects)
                {
                    if (obj) DestroyImmediate(obj.gameObject);
                }
                objects.Clear();
            }

            int index = 0;
            for (int x = 0; x < rowsColumns; x++)
            {
                for (int z = 0; z < rowsColumns; z++)
                {
                    if (requireRecreate)
                    {
                        m_waterObject = WaterObject.New(material, mesh);
                        objects.Add(m_waterObject);
                        
                        m_waterObject.transform.parent = this.transform;
                        m_waterObject.name = "WaterTile_x" + x + "z" + z;
                    }
                    else
                    {
                        m_waterObject = objects[index];
                        m_waterObject.AssignMesh(mesh);
                        m_waterObject.AssignMaterial(material);
                    }

                    m_waterObject.transform.localPosition = GridLocalCenterPosition(x, z);
                    m_waterObject.transform.localScale = Vector3.one;

                    index++;
                }
            }
        }

        private void CalculateTileSize()
        {
            rowsColumns = Mathf.Max(rowsColumns, 0);
            float m_scale = scale * this.transform.lossyScale.x;
            tileSize = Mathf.Max(1f, m_scale / rowsColumns);
        }
        
        private void RecreateMesh()
        {
            CalculateTileSize();
            float m_vertexDistance = vertexDistance * this.transform.lossyScale.x;

            mesh = WaterMesh.Create(WaterMesh.Shape.Rectangle, tileSize, m_vertexDistance, tileSize);
        }

        private void ReassignMesh()
        {
            foreach (WaterObject obj in objects)
            {
                obj.AssignMesh(mesh);
            }
        }

        private Vector3 GridLocalCenterPosition(int x, int z)
        {
            return new Vector3(x * tileSize - ((tileSize * (rowsColumns)) * 0.5f) + (tileSize * 0.5f), 0f,
                z * tileSize - ((tileSize * (rowsColumns)) * 0.5f) + (tileSize * 0.5f));
        }

        public static Vector3 SnapToGrid(Vector3 position, float cellSize)
        {
            return new Vector3(SnapToGrid(position.x, cellSize), SnapToGrid(position.y, cellSize), SnapToGrid(position.z, cellSize));
        }

        private static float SnapToGrid(float position, float cellSize)
        {
            return Mathf.FloorToInt(position / cellSize) * (cellSize) + (cellSize * 0.5f);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (DisplayWireframe)
            {
                Gizmos.matrix = this.transform.localToWorldMatrix;
                Gizmos.color = new Color(0, 0, 0, 0.5f);

                foreach (WaterObject waterObject in objects)
                {
                    if(waterObject.meshFilter.sharedMesh) Gizmos.DrawWireMesh(waterObject.meshFilter.sharedMesh, waterObject.transform.localPosition);
                }
            }

            if (DisplayGrid)
            {
                if (tileSize <= 0) CalculateTileSize();
                
                Gizmos.color = new Color(1f, 0.25f, 0.25f, 0.5f);
                Gizmos.matrix = this.transform.localToWorldMatrix;
                
                for (int x = 0; x < rowsColumns; x++)
                {
                    for (int z = 0; z < rowsColumns; z++)
                    {
                        Vector3 pos = GridLocalCenterPosition(x, z);

                        Gizmos.DrawWireCube(pos, new Vector3(tileSize, 0f, tileSize));
                    }
                }
            }
        }

        private void OnSceneGUI(UnityEditor.SceneView sceneView)
        {
            if (followSceneCamera)
            {
                actualFollowTarget = sceneView.camera.transform;
                Update();
            }
            else
            {
                actualFollowTarget = null;
            }
        }
#endif
    }
}