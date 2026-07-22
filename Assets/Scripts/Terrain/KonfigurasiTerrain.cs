using System;
using UnityEngine;

public enum JenisLapisanNoise
{
    Fbm,
    Ridge
}

[Serializable]
public sealed class ParameterLapisanNoise
{
    public string nama = "Lapisan";
    public JenisLapisanNoise jenis = JenisLapisanNoise.Fbm;
    [Range(0.0001f, 0.05f)] public float frekuensi = 0.001f;
    [Range(1, 8)] public int oktaf = 4;
    [Range(1f, 3f)] public float lacunarity = 2f;
    [Range(0f, 1f)] public float persistence = 0.5f;
    [Range(0f, 2f)] public float bobot = 1f;
    [Range(0.5f, 4f)] public float pangkat = 2f;
    [Range(0f, 360f)] public float rotasiDerajat;
}

[Serializable]
public sealed class PengaturanDomainWarp
{
    public bool aktif = true;
    [Range(0f, 400f)] public float kekuatan = 120f;
    [Range(0.0001f, 0.02f)] public float frekuensi = 0.0015f;
}

[Serializable]
public sealed class PengaturanErosiHidrolik
{
    public bool aktif = true;
    [Range(1000, 400000)] public int jumlahTetes = 150000;
    [Range(8, 64)] public int umurMaksimumTetes = 32;
    [Range(1f, 10f)] public float kapasitasSedimen = 4f;
    [Range(0f, 1f)] public float lajuErosi = 0.3f;
    [Range(0f, 1f)] public float lajuPengendapan = 0.3f;
    [Range(0f, 1f)] public float inersia = 0.05f;
    [Range(0f, 0.2f)] public float penguapan = 0.02f;
    [Range(1, 6)] public int radiusKikis = 3;
}

[Serializable]
public sealed class PengaturanDataranPesisir
{
    [Range(0f, 1000f)] public float lebarMeter = 250f;
    [Range(0f, 200f)] public float ketinggianRataRataMeter = 25f;
    [Range(0f, 100f)] public float variasiMeter = 10f;
}

[Serializable]
public sealed class PengaturanProfilPesisir
{
    public bool aktif = true;
    [Range(0f, 1000f)] public float lebarTransisiMeter = 400f;
    [Range(0f, 100f)] public float ketinggianGarisPantaiMeter;
    public PengaturanDataranPesisir dataranPesisir = new PengaturanDataranPesisir();
}

[Serializable]
public sealed class LapisanPewarnaan
{
    public string nama = "Lapisan";
    public TerrainLayer terrainLayer;
    public Vector2 rentangKetinggianMeter;
    public Vector2 rentangKemiringanDerajat;
    [Range(1, 10)] public int prioritas = 1;
}

[Serializable]
public sealed class AturanVegetasi
{
    public string nama = "Aturan";
    public GameObject prefab;
    public bool sebagaiDetail;
    [Range(0f, 2000f)] public float kepadatanPerHektar = 100f;
    public Vector2 rentangKetinggianMeter = new Vector2(0f, 500f);
    [Range(0f, 90f)] public float kemiringanMinimumDerajat;
    [Range(0f, 90f)] public float kemiringanMaksimumDerajat = 90f;
    [Range(0.1f, 20f)] public float jarakMinimumMeter = 2f;
    public Vector2 variasiSkala = new Vector2(1f, 1f);
    public bool rotasiAcak = true;
}

[Serializable]
public sealed class PengaturanPerforma
{
    [Range(1f, 200f)] public float pixelErrorLod = 8f;
    [Range(100f, 5000f)] public float jarakDasarPetaMeter = 1200f;
    [Range(10f, 1000f)] public float jarakPandangDetailMeter = 250f;
    [Range(10f, 1000f)] public float billboardPohonMulaiMeter = 180f;
}

[CreateAssetMenu(fileName = "Konfigurasi Terrain", menuName = "Horror Game/Terrain/Konfigurasi Terrain")]
public sealed class KonfigurasiTerrain : ScriptableObject
{
    [Header("Dunia")]
    public Vector2 ukuranDuniaMeter = new Vector2(2000f, 2000f);
    [Range(50f, 1000f)] public float ketinggianMaksimumMeter = 500f;
    public int resolusiHeightmap = 1025;
    public int resolusiAlphamap = 512;
    public int resolusiDetail = 1024;

    [Header("Seed")]
    public int seed = 20260721;

    [Header("Lapisan Noise")]
    public ParameterLapisanNoise[] lapisanNoise = Array.Empty<ParameterLapisanNoise>();

    [Header("Domain Warp")]
    public PengaturanDomainWarp domainWarp = new PengaturanDomainWarp();

    [Header("Erosi Hidrolik")]
    public PengaturanErosiHidrolik erosiHidrolik = new PengaturanErosiHidrolik();

    [Header("Profil Pesisir")]
    public PengaturanProfilPesisir profilPesisir = new PengaturanProfilPesisir();

    [Header("Kurva Ketinggian")]
    public AnimationCurve kurvaKetinggian = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Pewarnaan")]
    public LapisanPewarnaan[] lapisanPewarnaan = Array.Empty<LapisanPewarnaan>();
    [Range(0f, 1f)] public float lebarPembauran = 0.15f;

    [Header("Vegetasi")]
    public AturanVegetasi[] aturanVegetasi = Array.Empty<AturanVegetasi>();

    [Header("Performa")]
    public PengaturanPerforma performa = new PengaturanPerforma();
}
