using UnityEngine;

public sealed class PembangkitTerrain
{
    private const int OktafWarp = 2;
    private const float LacunarityWarp = 2f;
    private const float PersistenceWarp = 0.5f;
    private const float FrekuensiVariasiDataranPesisir = 0.008f;
    private const int ResolusiPerPatchDetail = 16;

    private readonly KonfigurasiTerrain konfigurasi;
    private readonly System.Random acak;
    private readonly LapisanNoise[] lapisanNoise;
    private readonly LapisanNoise warpX;
    private readonly LapisanNoise warpZ;

    public PembangkitTerrain(KonfigurasiTerrain konfigurasi)
    {
        this.konfigurasi = konfigurasi;
        acak = new System.Random(konfigurasi.seed);

        lapisanNoise = new LapisanNoise[konfigurasi.lapisanNoise.Length];
        for (int i = 0; i < lapisanNoise.Length; i++)
        {
            lapisanNoise[i] = new LapisanNoise(konfigurasi.lapisanNoise[i], acak);
        }

        warpX = new LapisanNoise(BuatParameterWarp(), acak);
        warpZ = new LapisanNoise(BuatParameterWarp(), acak);
    }

    public void Bangun(Terrain terrain)
    {
        TerrainData data = terrain.terrainData;
        data.heightmapResolution = konfigurasi.resolusiHeightmap;
        data.size = new Vector3(konfigurasi.ukuranDuniaMeter.x, konfigurasi.ketinggianMaksimumMeter, konfigurasi.ukuranDuniaMeter.y);
        data.alphamapResolution = konfigurasi.resolusiAlphamap;
        data.SetDetailResolution(konfigurasi.resolusiDetail, ResolusiPerPatchDetail);

        int resolusi = data.heightmapResolution;
        float[,] tinggi = BangunKetinggian(resolusi);

        if (konfigurasi.erosiHidrolik.aktif)
        {
            ErosiHidrolik erosi = new ErosiHidrolik(konfigurasi.erosiHidrolik, acak);
            erosi.Terapkan(tinggi, resolusi);
        }

        data.SetHeights(0, 0, tinggi);

        PewarnaOtomatis pewarna = new PewarnaOtomatis(konfigurasi.lapisanPewarnaan, konfigurasi.lebarPembauran);
        pewarna.Terapkan(data);

        PenyebarVegetasi penyebar = new PenyebarVegetasi(konfigurasi.aturanVegetasi, acak, konfigurasi.ukuranDuniaMeter);
        penyebar.Terapkan(terrain);

        TerapkanPerforma(terrain);
    }

    private ParameterLapisanNoise BuatParameterWarp()
    {
        return new ParameterLapisanNoise
        {
            jenis = JenisLapisanNoise.Fbm,
            frekuensi = konfigurasi.domainWarp.frekuensi,
            oktaf = OktafWarp,
            lacunarity = LacunarityWarp,
            persistence = PersistenceWarp,
            bobot = 1f,
            rotasiDerajat = 0f
        };
    }

    private float[,] BangunKetinggian(int resolusi)
    {
        float[,] tinggi = new float[resolusi, resolusi];

        for (int baris = 0; baris < resolusi; baris++)
        {
            float normalisasiZ = baris / (float)(resolusi - 1);
            for (int kolom = 0; kolom < resolusi; kolom++)
            {
                float normalisasiX = kolom / (float)(resolusi - 1);
                float posisiDuniaX = normalisasiX * konfigurasi.ukuranDuniaMeter.x;
                float posisiDuniaZ = normalisasiZ * konfigurasi.ukuranDuniaMeter.y;

                Vector2 posisiTerwarp = TerapkanDomainWarp(posisiDuniaX, posisiDuniaZ);

                float nilaiGabungan = 0f;
                for (int i = 0; i < lapisanNoise.Length; i++)
                {
                    nilaiGabungan += lapisanNoise[i].Sampel(posisiTerwarp.x, posisiTerwarp.y) * konfigurasi.lapisanNoise[i].bobot;
                }

                nilaiGabungan = Mathf.Clamp01(nilaiGabungan);
                float tinggiNormalisasi = konfigurasi.kurvaKetinggian.Evaluate(nilaiGabungan);
                float tinggiMeter = tinggiNormalisasi * konfigurasi.ketinggianMaksimumMeter;

                tinggiMeter = TerapkanProfilPesisir(tinggiMeter, posisiDuniaX, posisiDuniaZ);

                tinggi[baris, kolom] = Mathf.Clamp01(tinggiMeter / konfigurasi.ketinggianMaksimumMeter);
            }
        }

        return tinggi;
    }

    private Vector2 TerapkanDomainWarp(float x, float z)
    {
        if (!konfigurasi.domainWarp.aktif)
        {
            return new Vector2(x, z);
        }

        float pergeseranX = (warpX.Sampel(x, z) * 2f - 1f) * konfigurasi.domainWarp.kekuatan;
        float pergeseranZ = (warpZ.Sampel(x + 1000f, z + 1000f) * 2f - 1f) * konfigurasi.domainWarp.kekuatan;
        return new Vector2(x + pergeseranX, z + pergeseranZ);
    }

    private float TerapkanProfilPesisir(float tinggiMeter, float posisiDuniaX, float posisiDuniaZ)
    {
        if (!konfigurasi.profilPesisir.aktif)
        {
            return tinggiMeter;
        }

        PengaturanDataranPesisir dataran = konfigurasi.profilPesisir.dataranPesisir;
        float jarakDariPantai = posisiDuniaZ;

        if (jarakDariPantai <= dataran.lebarMeter)
        {
            float variasi = (Mathf.PerlinNoise(posisiDuniaX * FrekuensiVariasiDataranPesisir, posisiDuniaZ * FrekuensiVariasiDataranPesisir) * 2f - 1f) * dataran.variasiMeter;
            float t = Mathf.SmoothStep(0f, 1f, jarakDariPantai / Mathf.Max(1f, dataran.lebarMeter));
            return Mathf.Lerp(konfigurasi.profilPesisir.ketinggianGarisPantaiMeter, dataran.ketinggianRataRataMeter + variasi, t);
        }

        float jarakTransisi = jarakDariPantai - dataran.lebarMeter;
        float lebarTransisi = Mathf.Max(1f, konfigurasi.profilPesisir.lebarTransisiMeter);
        if (jarakTransisi >= lebarTransisi)
        {
            return tinggiMeter;
        }

        float tTransisi = Mathf.SmoothStep(0f, 1f, jarakTransisi / lebarTransisi);
        return Mathf.Lerp(dataran.ketinggianRataRataMeter, tinggiMeter, tTransisi);
    }

    private void TerapkanPerforma(Terrain terrain)
    {
        terrain.heightmapPixelError = konfigurasi.performa.pixelErrorLod;
        terrain.basemapDistance = konfigurasi.performa.jarakDasarPetaMeter;
        terrain.detailObjectDistance = konfigurasi.performa.jarakPandangDetailMeter;
        terrain.treeBillboardDistance = konfigurasi.performa.billboardPohonMulaiMeter;
    }
}
