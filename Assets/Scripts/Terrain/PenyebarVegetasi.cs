using System.Collections.Generic;
using UnityEngine;

public sealed class PenyebarVegetasi
{
    private const int PengaliBatasPercobaan = 40;
    private const int KepadatanMaksimumDetailPerSel = 16;

    private readonly AturanVegetasi[] aturan;
    private readonly System.Random acak;
    private readonly Vector2 ukuranDuniaMeter;

    public PenyebarVegetasi(AturanVegetasi[] aturan, System.Random acak, Vector2 ukuranDuniaMeter)
    {
        this.aturan = aturan;
        this.acak = acak;
        this.ukuranDuniaMeter = ukuranDuniaMeter;
    }

    public void Terapkan(Terrain terrain)
    {
        TerrainData data = terrain.terrainData;
        List<TreePrototype> prototipePohon = new List<TreePrototype>();
        List<TreeInstance> instansiPohon = new List<TreeInstance>();
        List<DetailPrototype> prototipeDetail = new List<DetailPrototype>();
        List<List<Vector3>> titikDetail = new List<List<Vector3>>();

        for (int indeks = 0; indeks < aturan.Length; indeks++)
        {
            AturanVegetasi satuAturan = aturan[indeks];
            if (satuAturan.prefab == null)
            {
                continue;
            }

            List<Vector3> titik = SebarTitik(satuAturan, data);
            if (satuAturan.sebagaiDetail)
            {
                prototipeDetail.Add(BuatPrototipeDetail(satuAturan));
                titikDetail.Add(titik);
            }
            else
            {
                int indeksPrototipe = prototipePohon.Count;
                prototipePohon.Add(BuatPrototipePohon(satuAturan));
                TambahkanInstansiPohon(instansiPohon, titik, satuAturan, indeksPrototipe);
            }
        }

        data.treePrototypes = prototipePohon.ToArray();
        data.treeInstances = instansiPohon.ToArray();
        data.detailPrototypes = prototipeDetail.ToArray();

        for (int indeks = 0; indeks < titikDetail.Count; indeks++)
        {
            TerapkanLayerDetail(data, indeks, titikDetail[indeks]);
        }
    }

    private List<Vector3> SebarTitik(AturanVegetasi satuAturan, TerrainData data)
    {
        float luasHektar = ukuranDuniaMeter.x * ukuranDuniaMeter.y / 10000f;
        int targetJumlah = Mathf.RoundToInt(satuAturan.kepadatanPerHektar * luasHektar);
        float jarakMinimumKuadrat = satuAturan.jarakMinimumMeter * satuAturan.jarakMinimumMeter;
        float ukuranSel = Mathf.Max(0.5f, satuAturan.jarakMinimumMeter);

        Dictionary<Vector2Int, List<Vector2>> gridSpasial = new Dictionary<Vector2Int, List<Vector2>>();
        List<Vector3> hasil = new List<Vector3>();
        int batasPercobaan = Mathf.Max(1, targetJumlah) * PengaliBatasPercobaan;

        for (int percobaan = 0; hasil.Count < targetJumlah && percobaan < batasPercobaan; percobaan++)
        {
            float x = (float)acak.NextDouble() * ukuranDuniaMeter.x;
            float z = (float)acak.NextDouble() * ukuranDuniaMeter.y;
            float normalisasiX = x / ukuranDuniaMeter.x;
            float normalisasiZ = z / ukuranDuniaMeter.y;

            float tinggiMeter = data.GetInterpolatedHeight(normalisasiX, normalisasiZ);
            if (tinggiMeter < satuAturan.rentangKetinggianMeter.x || tinggiMeter > satuAturan.rentangKetinggianMeter.y)
            {
                continue;
            }

            float kemiringanDerajat = data.GetSteepness(normalisasiX, normalisasiZ);
            if (kemiringanDerajat < satuAturan.kemiringanMinimumDerajat || kemiringanDerajat > satuAturan.kemiringanMaksimumDerajat)
            {
                continue;
            }

            Vector2Int selGrid = new Vector2Int(Mathf.FloorToInt(x / ukuranSel), Mathf.FloorToInt(z / ukuranSel));
            if (TerlaluDekatDenganTetangga(gridSpasial, selGrid, x, z, jarakMinimumKuadrat))
            {
                continue;
            }

            Vector2 titikBaru = new Vector2(x, z);
            hasil.Add(new Vector3(x, tinggiMeter, z));
            TambahkanKeGrid(gridSpasial, selGrid, titikBaru);
        }

        return hasil;
    }

    private static bool TerlaluDekatDenganTetangga(
        Dictionary<Vector2Int, List<Vector2>> gridSpasial,
        Vector2Int selGrid,
        float x,
        float z,
        float jarakMinimumKuadrat)
    {
        for (int offsetZ = -1; offsetZ <= 1; offsetZ++)
        {
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                Vector2Int selTetangga = new Vector2Int(selGrid.x + offsetX, selGrid.y + offsetZ);
                if (!gridSpasial.TryGetValue(selTetangga, out List<Vector2> titikTetangga))
                {
                    continue;
                }

                for (int i = 0; i < titikTetangga.Count; i++)
                {
                    float bedaX = titikTetangga[i].x - x;
                    float bedaZ = titikTetangga[i].y - z;
                    if (bedaX * bedaX + bedaZ * bedaZ < jarakMinimumKuadrat)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static void TambahkanKeGrid(Dictionary<Vector2Int, List<Vector2>> gridSpasial, Vector2Int selGrid, Vector2 titik)
    {
        if (!gridSpasial.TryGetValue(selGrid, out List<Vector2> daftar))
        {
            daftar = new List<Vector2>();
            gridSpasial[selGrid] = daftar;
        }

        daftar.Add(titik);
    }

    private TreePrototype BuatPrototipePohon(AturanVegetasi satuAturan)
    {
        return new TreePrototype { prefab = satuAturan.prefab };
    }

    private DetailPrototype BuatPrototipeDetail(AturanVegetasi satuAturan)
    {
        return new DetailPrototype
        {
            usePrototypeMesh = true,
            prototype = satuAturan.prefab,
            renderMode = DetailRenderMode.VertexLit,
            minWidth = satuAturan.variasiSkala.x,
            maxWidth = satuAturan.variasiSkala.y,
            minHeight = satuAturan.variasiSkala.x,
            maxHeight = satuAturan.variasiSkala.y,
            useInstancing = true
        };
    }

    private void TambahkanInstansiPohon(
        List<TreeInstance> daftar,
        List<Vector3> titik,
        AturanVegetasi satuAturan,
        int indeksPrototipe)
    {
        for (int i = 0; i < titik.Count; i++)
        {
            float skala = Mathf.Lerp(satuAturan.variasiSkala.x, satuAturan.variasiSkala.y, (float)acak.NextDouble());
            float rotasi = satuAturan.rotasiAcak ? (float)acak.NextDouble() * Mathf.PI * 2f : 0f;

            daftar.Add(new TreeInstance
            {
                position = new Vector3(titik[i].x / ukuranDuniaMeter.x, titik[i].y, titik[i].z / ukuranDuniaMeter.y),
                widthScale = skala,
                heightScale = skala,
                rotation = rotasi,
                prototypeIndex = indeksPrototipe,
                color = Color.white,
                lightmapColor = Color.white
            });
        }
    }

    private void TerapkanLayerDetail(TerrainData data, int indeksLayer, List<Vector3> titik)
    {
        int resolusiDetail = data.detailResolution;
        int[,] peta = new int[resolusiDetail, resolusiDetail];

        for (int i = 0; i < titik.Count; i++)
        {
            int kolom = Mathf.Clamp(Mathf.FloorToInt(titik[i].x / ukuranDuniaMeter.x * resolusiDetail), 0, resolusiDetail - 1);
            int baris = Mathf.Clamp(Mathf.FloorToInt(titik[i].z / ukuranDuniaMeter.y * resolusiDetail), 0, resolusiDetail - 1);
            peta[baris, kolom] = Mathf.Min(KepadatanMaksimumDetailPerSel, peta[baris, kolom] + 1);
        }

        data.SetDetailLayer(0, 0, indeksLayer, peta);
    }
}
