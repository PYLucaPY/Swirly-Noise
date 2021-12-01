using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComputeDrawer : MonoBehaviour
{
    public RenderTexture _texture;
    public ComputeShader _shader;

    public float scrollSpeed;
    public int detailIterations = 5;

    public float frequencyMultPerIteration = 2;
    public float apmlitudeMultPerIteration = 0.4f;

    public float initialFrequency;
    public float initialAmplitude;

    public float baseFrequency;

    public float screenSinMag;
    public float screenSinSpeed;

    public Color mainColour;
    public Color secondaryColour;

    public Color targetMainColour;
    public Color targetSecondaryColour;

    public Gradient colourGradient;
    public RenderTexture gradientTexture;

    [Range(1, 1920)] public int width;
    [Range(1, 1080)] public int height;

    public float mainColourSmoothSpeed;
    public float secondaryColourSmoothSpeed;

    private Vector3 mainColourVelocity;
    private Vector3 secondaryColourVelocity;

    Color GetMainColourTarget()
    {
        return Random.ColorHSV(
            0, 1,
            1, 0.75f,
            1, 1,
            0, 1 
        );
    }

    Color GetSecondaryColourTarget()
    {
        return Random.ColorHSV(
            0, 1,
            1, 0.75f,
            1, 1,
            0, 1 
        );
    }

    private void Start() {
        targetMainColour = GetMainColourTarget();
        targetSecondaryColour = GetSecondaryColourTarget();
    }

    void InitRenderTexture()
    {
        _texture = new RenderTexture(
            width, height, 0, RenderTextureFormat.ARGBFloat,
            RenderTextureReadWrite.Linear
        );

        _texture.enableRandomWrite = true;
        _texture.autoGenerateMips = false;

        _texture.filterMode = FilterMode.Point;
        _texture.wrapMode = TextureWrapMode.Clamp;

        _texture.Create();
    }

    public void CreateGradient()
    {
        gradientTexture = new RenderTexture(
            width, 1, 0, RenderTextureFormat.ARGBFloat,
            RenderTextureReadWrite.Linear
        );

        gradientTexture.enableRandomWrite = true;
        gradientTexture.autoGenerateMips = false;

        gradientTexture.filterMode = FilterMode.Point;
        gradientTexture.wrapMode = TextureWrapMode.Clamp;

        Texture2D gradientTexture_ = new Texture2D(width, 1);
        for(int x = 0; x < width; x ++)
        {
            float t = ((float)x) / width;
            Color colour = colourGradient.Evaluate(t);

            gradientTexture_.SetPixel(x, 0, colour);
        }

        gradientTexture_.Apply();
        Graphics.Blit(gradientTexture_, gradientTexture);
    }

    void Update()
    {
        baseFrequency += Mathf.Sin(Time.frameCount * screenSinSpeed) * screenSinMag;

        if(Time.frameCount % 200 == 0 && !(Time.frameCount % 400 == 0))
        {
            targetMainColour = GetMainColourTarget();
        }
        if(Time.frameCount % 400 == 0)
        {
            targetSecondaryColour = GetSecondaryColourTarget();
        }

        mainColour = SmoothStepColour(
            mainColour, targetMainColour,
            ref mainColourVelocity, mainColourSmoothSpeed
        );

        secondaryColour = SmoothStepColour(
            secondaryColour, targetSecondaryColour,
            ref secondaryColourVelocity, secondaryColourSmoothSpeed
        );
    }

    void SetShaderParams()
    {
        _shader.SetTexture(0, "Result", _texture);
        _shader.SetFloat("totalTime", Time.time);
        _shader.SetFloat("scrollSpeed", scrollSpeed);
        _shader.SetInt("detailIterations", detailIterations);

        _shader.SetFloat("frequencyMult", frequencyMultPerIteration);
        _shader.SetFloat("apmlitudeMult", apmlitudeMultPerIteration);

        _shader.SetFloat("width", width);
        _shader.SetFloat("height", height);

        _shader.SetFloat("initialFrequency", initialFrequency);
        _shader.SetFloat("initialAmplitude", initialAmplitude);

        _shader.SetFloat("baseFrequency", baseFrequency);

        float maxColourValue = 1;

        _shader.SetVector("mainColour", new Vector4(
            mainColour.r / maxColourValue,
            mainColour.g / maxColourValue,
            mainColour.b / maxColourValue,
            1
        ));

        _shader.SetVector("secondaryColour", new Vector4(
            secondaryColour.r / maxColourValue,
            secondaryColour.g / maxColourValue,
            secondaryColour.b / maxColourValue,
            1
        ));

        _shader.SetTexture(0, "GradietTex", gradientTexture);
    }

    void RunCompute()
    {
        SetShaderParams();

        Vector3 iterations = new Vector3(
            width,
            height,
            1
        );

        Vector3 threadSizes = new Vector3(
            8,
            8,
            1
        );

        Vector3Int threadGroups = new Vector3Int(
            Mathf.CeilToInt(iterations.x / threadSizes.x),
            Mathf.CeilToInt(iterations.y / threadSizes.y),
            Mathf.CeilToInt(iterations.z / threadSizes.z)
        );

        _shader.Dispatch(
            0, threadGroups.x, threadGroups.y, 1
        );
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest) {
        try {
            if(_texture == null)
            {
                InitRenderTexture();
            }

            if(gradientTexture == null)
            {
                CreateGradient();
            }

            RunCompute();

            Graphics.Blit(_texture, dest);
        } catch(System.Exception err){
            Debug.Log("Got error in ONRENDERIMAGE");
            if(_texture == null)
            {
                Debug.Log("_TEXTURE IS NULL");
            }
            if(gradientTexture == null)
            {
                Debug.Log("GRADIENT TEXTURE IS NULL");
            }
        }
    }

    Color SmoothStepColour(Color a, Color b, ref Vector3 velocity, float smoothSpeed)
    {
        Vector3 a0 = new Vector3(
            a.r,
            a.g,
            a.b
        );

        Vector3 b0 = new Vector3(
            b.r,
            b.g,
            b.b
        );

        Vector3 sAB = Vector3.SmoothDamp(a0, b0, ref velocity, smoothSpeed);
        
        Color returnColour = new Color(
            sAB.x,
            sAB.y,
            sAB.z,
            1
        );

        return returnColour;
        
    }
}
