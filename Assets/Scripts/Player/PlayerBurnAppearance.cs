using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerBurnAppearance : MonoBehaviour
{
    private static readonly int BurnAmountId = Shader.PropertyToID("_BurnAmount");

    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerCharacterVisual characterVisual;
    [SerializeField] private Shader burnShader;

    [Header("Health To Burn (Decimal 0-1)")]
    [SerializeField, Range(0f, 1f)] private float noBurnAtNormalizedHealth = 1f;
    [SerializeField, Range(0f, 1f)] private float fullyBurnedAtNormalizedHealth = 0.1f;

    [Header("Excluded Materials")]
    [SerializeField] private string[] excludedMaterialKeywords =
    {
        "Eye",
        "Glasses",
        "Metal"
    };

    private readonly List<Material> burnMaterials = new List<Material>();
    private bool initialized;

    public float BurnAmount { get; private set; }

    private void Reset()
    {
        playerHealth = GetComponent<PlayerHealth>();
        characterVisual = GetComponent<PlayerCharacterVisual>();
    }

    private void OnValidate()
    {
        noBurnAtNormalizedHealth = Mathf.Clamp01(noBurnAtNormalizedHealth);
        fullyBurnedAtNormalizedHealth = Mathf.Clamp01(fullyBurnedAtNormalizedHealth);
    }

    private void Awake()
    {
        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
        }

        if (characterVisual == null)
        {
            characterVisual = GetComponent<PlayerCharacterVisual>();
        }

        if (burnShader == null)
        {
            burnShader = Shader.Find("Horror/Character Burn Wounds");
        }
    }

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.HealthChanged += HandleHealthChanged;
        }
    }

    private void Start()
    {
        InitializeMaterials();
        RefreshFromCurrentHealth();
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.HealthChanged -= HandleHealthChanged;
        }
    }

    private void OnDestroy()
    {
        for (int i = 0; i < burnMaterials.Count; i++)
        {
            if (burnMaterials[i] != null)
            {
                Destroy(burnMaterials[i]);
            }
        }

        burnMaterials.Clear();
    }

    private void InitializeMaterials()
    {
        if (initialized || burnShader == null || characterVisual == null || characterVisual.ModelInstance == null)
        {
            return;
        }

        Renderer[] renderers = characterVisual.ModelInstance.GetComponentsInChildren<Renderer>(true);
        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            Renderer targetRenderer = renderers[rendererIndex];
            if (targetRenderer == null || targetRenderer is ParticleSystemRenderer)
            {
                continue;
            }

            Material[] sourceMaterials = targetRenderer.sharedMaterials;
            Material[] runtimeMaterials = new Material[sourceMaterials.Length];
            bool changed = false;

            for (int materialIndex = 0; materialIndex < sourceMaterials.Length; materialIndex++)
            {
                Material source = sourceMaterials[materialIndex];
                if (source == null || IsExcluded(source.name))
                {
                    runtimeMaterials[materialIndex] = source;
                    continue;
                }

                Material runtimeMaterial = CreateBurnMaterial(source);
                runtimeMaterials[materialIndex] = runtimeMaterial;
                burnMaterials.Add(runtimeMaterial);
                changed = true;
            }

            if (changed)
            {
                targetRenderer.sharedMaterials = runtimeMaterials;
            }
        }

        initialized = true;
    }

    private Material CreateBurnMaterial(Material source)
    {
        Texture mainTexture = source.HasProperty("_MainTex") ? source.GetTexture("_MainTex") : null;
        Color color = source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white;
        float metallic = source.HasProperty("_Metallic") ? source.GetFloat("_Metallic") : 0f;
        float smoothness = source.HasProperty("_Glossiness") ? source.GetFloat("_Glossiness") : 0.35f;

        Material material = new Material(source)
        {
            name = $"{source.name} (Player Burn)",
            shader = burnShader
        };

        material.SetTexture("_MainTex", mainTexture);
        material.SetColor("_Color", color);
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Glossiness", smoothness);
        material.SetFloat(BurnAmountId, 0f);
        return material;
    }

    private bool IsExcluded(string materialName)
    {
        if (excludedMaterialKeywords == null || string.IsNullOrEmpty(materialName))
        {
            return false;
        }

        for (int i = 0; i < excludedMaterialKeywords.Length; i++)
        {
            string keyword = excludedMaterialKeywords[i];
            if (!string.IsNullOrWhiteSpace(keyword)
                && materialName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private void HandleHealthChanged(float currentHealth, float maxHealth)
    {
        float normalizedHealth = maxHealth > 0f ? currentHealth / maxHealth : 0f;
        SetBurnAmount(EvaluateBurnAmount(normalizedHealth));
    }

    private void RefreshFromCurrentHealth()
    {
        float normalizedHealth = playerHealth != null ? playerHealth.NormalizedHealth : 1f;
        SetBurnAmount(EvaluateBurnAmount(normalizedHealth));
    }

    private float EvaluateBurnAmount(float normalizedHealth)
    {
        normalizedHealth = Mathf.Clamp01(normalizedHealth);
        if (normalizedHealth >= noBurnAtNormalizedHealth)
        {
            return 0f;
        }

        if (normalizedHealth <= fullyBurnedAtNormalizedHealth)
        {
            return 1f;
        }

        return Mathf.InverseLerp(
            noBurnAtNormalizedHealth,
            fullyBurnedAtNormalizedHealth,
            normalizedHealth);
    }

    private void SetBurnAmount(float amount)
    {
        BurnAmount = Mathf.Clamp01(amount);
        for (int i = 0; i < burnMaterials.Count; i++)
        {
            if (burnMaterials[i] != null)
            {
                burnMaterials[i].SetFloat(BurnAmountId, BurnAmount);
            }
        }
    }
}
