using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
public enum VolumeMode
{
    Color, 
    Gradient
}

public enum GradientType
{
    Linear = 0,
    Angular = 1,
    Cylindrical = 2,
    Spherical = 3
}

[ExecuteInEditMode]
public class ColorVolume : MonoBehaviour
{
    private const int SampleCount = 128;

    public VolumeMode Mode;
    public GradientType GradientType;

    public Color Color = Color.black;
    public Gradient Gradient = new Gradient();
    public bool InvertGradient = false;

    [Range(0.0f, 1.0f)]
    public float GradientOffset = 0.0f;

    private MaterialPropertyBlock properties;
    private Collider[] contained = new Collider[64];
    private Crazyflie[] hitDrones = new Crazyflie[64];
    private int containedDroneCount = 0;
    private Texture2D gradientTexture;


    private void Start()
    {
    }

    private void Update()
    {
        this.UpdateContainment();
        this.UpdateMaterialProperties();

        Graphics.DrawMesh(EditorResources.CubeMesh, transform.localToWorldMatrix, 
                            EditorResources.ColorVolumeMaterial, gameObject.layer, null, 0, properties);
    }

    private void UpdateMaterialProperties()
    {
        if (properties == null)
            properties = new MaterialPropertyBlock();

        /*
        Vector3 halfExtent = 0.5f * Vector3.one;
        Vector3 boundsMax = transform.TransformPoint(halfExtent);
        Vector3 boundsMin = transform.TransformPoint(-halfExtent);*/

        properties.SetInt("_useGradient", (this.Mode == VolumeMode.Gradient ? 1 : 0));
        //properties.SetVector("boundsMax", boundsMax);
        //properties.SetVector("boundsMin", boundsMin);
        //properties.SetVector("up", transform.up);
        //properties.SetVector("forward", transform.forward);
        //properties.SetVector("right", transform.right);
        //properties.SetVector("origin", transform.position);
        properties.SetVector("scale", transform.lossyScale);
        properties.SetMatrix("transformMatrix", transform.worldToLocalMatrix);
        

        if (this.Mode == VolumeMode.Color)
        {
            properties.SetColor("_Color", Color);
        }
        else
        {
            GradientTexture.UpdateTexture(Gradient, ref gradientTexture, SampleCount, GradientOffset, InvertGradient);
            properties.SetTexture("_gradient", gradientTexture);
            properties.SetInt("_gradientMode", (int)GradientType);
        }
    }


    public void Apply()
    {
        UpdateContainment();
        for (int i = 0; i < containedDroneCount; i++)
        {
            Crazyflie drone = this.hitDrones[i];
            drone.SetColorKeyframe(Evaluate(drone.transform.position), drone.Time);
        }
        EditorApplication.QueuePlayerLoopUpdate();
    }

    public void ApplyAndRemove()
    {
        Apply();
        Undo.DestroyObjectImmediate(gameObject);
    }

    private void OnValidate()
    {
        UpdateMaterialProperties();
    }

    private void OnDrawGizmos()
    {
        DrawShape(Palette.UltraTranslucent);
    }

    private void OnDrawGizmosSelected()
    {
        DrawShape(Palette.Translucent, true);
        DrawDrones();
    }

    private void DrawShape(Color color, bool wireOnly = false)
    {
        Color previousColor = Gizmos.color;
        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.color = color;
        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);

        if (!wireOnly)
        {
            Gizmos.color = Palette.HyperTranslucent;
            Gizmos.DrawCube(Vector3.zero, Vector3.one);
        }
        
        Gizmos.matrix = previousMatrix;
        Gizmos.color = previousColor;
    }

    private Color Evaluate(Vector3 position)
    {
        if (this.Mode == VolumeMode.Color)
            return Color;

        Vector3 localPosition = this.transform.InverseTransformPoint(position);
        float sampleIndex = 0.0f;

        switch (GradientType)
        {
            case GradientType.Linear:
                sampleIndex = Mathf.InverseLerp(-0.5f, 0.5f, localPosition.x);
                break;

            case GradientType.Angular:
                float angle = Mathf.Atan2(localPosition.z, localPosition.x);
                float normalized = (angle + Mathf.PI) / (2.0f * Mathf.PI);
                sampleIndex = Mathf.Clamp01(normalized);
                break;

            case GradientType.Cylindrical:
                float radialDistance = new Vector2(localPosition.x, localPosition.z).magnitude;
                sampleIndex = Mathf.Pow(radialDistance / 0.525f, 1.25f);
                break;

            case GradientType.Spherical:
                float sphericalDistance = localPosition.magnitude;
                sampleIndex = Mathf.Pow(sphericalDistance / 0.525f, 1.25f);
                break;
        }
        float index = InvertGradient ? (1.0f - sampleIndex) : sampleIndex;
        index = (GradientOffset > 0.0f && GradientOffset < 1.0f) ? Mathf.Repeat(index + GradientOffset, 1.0f) : index;

        return Gradient.Evaluate(index);
    }

    private void DrawDrones()
    {
        for (int i = 0; i < containedDroneCount; i++)
        {
            Crazyflie drone = this.hitDrones[i];
            Color color = Evaluate(drone.transform.position);
            CrazyflieEditor.DrawDroneBounds(drone, color * 1.5f, false);

            color.a = 0.5f;
            CrazyflieEditor.DrawDroneBounds(drone, color, true);
        }
    }

    private void UpdateContainment()
    {
        int hitCount = Physics.OverlapBoxNonAlloc(transform.position, 0.5f * transform.lossyScale, this.contained, transform.rotation);
        this.containedDroneCount = 0;
        for(int i = 0; i < hitCount; i++)
        {
            Collider collider = contained[i];
            Crazyflie drone = collider.GetComponent<Crazyflie>();

            if (drone == null)
                continue;

            this.hitDrones[containedDroneCount] = drone;
            this.containedDroneCount += 1;
        }
    }
}